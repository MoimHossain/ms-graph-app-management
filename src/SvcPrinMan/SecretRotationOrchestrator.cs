

using Microsoft.Graph;
using SvcPrinMan.Payloads;
using SvcPrinMan.Services;
using SvcPrinMan.Supports;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SvcPrinMan
{
    public class SecretRotationOrchestrator
    {
        private const string AZURERM_TYPE = "azurerm";
        private const string SPN_KEY = "spnKey";
        private const string SPN_CERTIFICATE = "spnCertificate";

        public static async Task<Tuple<string, Dictionary<string, string>>> RotateSecretAsync(
            CredentialRotatePayload payload)
        {
            var executionLogs = new StringBuilder();
            var context = new Dictionary<string, string>();

            if (payload != null && !string.IsNullOrWhiteSpace(payload.OrgName)
                && !string.IsNullOrWhiteSpace(payload.PAT))
            {
                var azdo = new AzDoService(payload.OrgName, payload.PAT);
                context.Add("Organization Name", payload.OrgName);
                context.Add("Project Id", payload.ProjectId.ToString());

                var endpointIds = await CollectEndpointsAsync(payload, azdo);
                foreach (var endpointId in endpointIds)
                {
                    await ProcessEndpointAsync(payload, executionLogs, context, azdo, endpointId);
                }
            }
            return new Tuple<string, Dictionary<string, string>>(executionLogs.ToString(), context);
        }

        private static async Task<List<Guid>> CollectEndpointsAsync(
            CredentialRotatePayload payload, 
            AzDoService azdo)
        {
            var endpointIds = new List<Guid>();
            if (payload.RotateAllServiceConnections)
            {
                var allEndpoints = await azdo.ListServiceEndpointsAsync(payload.ProjectId);
                endpointIds.AddRange(allEndpoints.Value
                    .Where(e => AZURERM_TYPE.Equals(e.Type))
                    .Select(e => e.Id));
            }
            else if (payload.ServiceEndpoints != null && payload.ServiceEndpoints.Any())
            {
                endpointIds.AddRange(payload.ServiceEndpoints);
            }

            return endpointIds;
        }

        private static async Task ProcessEndpointAsync(
            CredentialRotatePayload payload, 
            StringBuilder executionLogs, 
            Dictionary<string, string> context, 
            AzDoService azdo, 
            Guid endpointId)
        {
            var endpoint = await azdo.GetServiceEndpointsAsync(payload.ProjectId, endpointId);
            if (endpoint != null && AZURERM_TYPE.Equals(endpoint.Type)
                && endpoint.Authorization != null && endpoint.Authorization.Parameters != null)
            {
                if(SPN_KEY.Equals(endpoint.Authorization.Parameters.AuthenticationType) ||
                    SPN_CERTIFICATE.Equals(endpoint.Authorization.Parameters.AuthenticationType))
                {
                    executionLogs.AppendLine($"Endpoint {endpoint.Name}({endpoint.Id}) loaded for credentials check.");
                    context.Add("Endpoint Name", endpoint.Name);
                    context.Add("Endpoint Id", endpoint.Id.ToString());
                    context.Add("Authentication Type", endpoint.Authorization.Parameters.AuthenticationType);
                    var graph = await MSGraph.GetGraphClientAsync();
                    var apps = await graph.Applications.Request().Filter($"appId eq '{endpoint.Authorization.Parameters.Serviceprincipalid}'").GetAsync();
                    if (apps != null && apps.Any())
                    {
                        var application = apps.First();
                        executionLogs.AppendLine($"AppRegistration {application.DisplayName}({application.AppId}) found for {endpoint.Name}({endpoint.Id}).");
                        context.Add("App Name", application.DisplayName);
                        context.Add("App Id", application.AppId.ToString());

                        if (SPN_KEY.Equals(endpoint.Authorization.Parameters.AuthenticationType))
                        {
                            await ProcessAppWithPasswordCredentailsAsync(payload, executionLogs, context, azdo, endpoint, graph, application);
                        }
                        else
                        {
                            await ProcessAppWithCertificateCredentailsAsync(payload, executionLogs, context, azdo, endpoint, graph, application);
                        }
                    }
                }
            }
        }

        private static async Task ProcessAppWithCertificateCredentailsAsync(
            CredentialRotatePayload payload,
            StringBuilder executionLogs,
            Dictionary<string, string> context,
            AzDoService azdo,
            Payloads.AzureDevOps.VstsServiceEndpoint endpoint,
            GraphServiceClient graph,
            Application application)
        {
            if (application.KeyCredentials.Any())
            {
                executionLogs.AppendLine($"Certificate Credentails found ({application.KeyCredentials.Count()}).");
                context.Add("Total Credentails", application.KeyCredentials.Count().ToString());
                var now = DateTimeOffset.UtcNow;
                var oldCertificateCred = application.KeyCredentials.First();
                if (oldCertificateCred.EndDateTime.HasValue)
                {
                    var rotationRequired = (now.AddDays(payload.DaysBeforeExpire) > oldCertificateCred.EndDateTime);
                    executionLogs.AppendLine($"{now.AddDays(payload.DaysBeforeExpire)} > {oldCertificateCred.EndDateTime} = {rotationRequired}");
                    if (rotationRequired)
                    {
                        await RotateCertificateCoreAsync(payload, executionLogs, context, azdo, endpoint, graph, application, now);
                    }
                }
            }
        }

        private static async Task RotateCertificateCoreAsync(
            CredentialRotatePayload payload,
            StringBuilder executionLogs,
            Dictionary<string, string> context,
            AzDoService azdo,
            Payloads.AzureDevOps.VstsServiceEndpoint endpoint,
            GraphServiceClient graph,
            Application application,
            DateTimeOffset now)
        {
            var selfSignedCertificate = 
                CertificateUtils.CreateSelfSignedCertificateAsync(validForDays: payload.LifeTimeInDays);
            var certificateCredentail = new KeyCredential
            {
                StartDateTime = now,
                EndDateTime = now.AddDays(payload.LifeTimeInDays),
                Type = "AsymmetricX509Cert",
                Usage = "Verify",
                Key = CertificateUtils.GetPfxAsBytes(selfSignedCertificate)
            };
            var app = new Application
            {
                KeyCredentials = new List<KeyCredential> { certificateCredentail }
            };
            await graph.Applications[application.Id].Request().UpdateAsync(app);            
            endpoint.Authorization.Parameters
                .ServicePrincipalCertificate = CertificateUtils.GeneratePEMWithPrivateKeyAsString(selfSignedCertificate);
            await azdo.UpdateServiceEndpointsAsync(payload.ProjectId, endpoint.Id, endpoint);
            context.Add("Certificate Key Id", certificateCredentail.KeyId.ToString());
            context.Add("Certificate Start Time", certificateCredentail.StartDateTime.ToString());
            context.Add("Certificate End Time", certificateCredentail.EndDateTime.ToString());
            context.Add("Certificate Thumbprint", selfSignedCertificate.Thumbprint);
        }

        private static async Task ProcessAppWithPasswordCredentailsAsync(
            CredentialRotatePayload payload, 
            StringBuilder executionLogs, 
            Dictionary<string, string> context, 
            AzDoService azdo, 
            Payloads.AzureDevOps.VstsServiceEndpoint endpoint, 
            GraphServiceClient graph, 
            Application application)
        { 
            if (application.PasswordCredentials.Any())
            {
                executionLogs.AppendLine($"Password Credentails found ({application.PasswordCredentials.Count()}).");
                context.Add("Total Credentails", application.PasswordCredentials.Count().ToString());
                var now = DateTimeOffset.UtcNow;
                var oldPassCred = application.PasswordCredentials.First();
                if (oldPassCred.EndDateTime.HasValue)
                {
                    var rotationRequired = (now.AddDays(payload.DaysBeforeExpire) > oldPassCred.EndDateTime);
                    executionLogs.AppendLine($"{now.AddDays(payload.DaysBeforeExpire)} > {oldPassCred.EndDateTime} = {rotationRequired}");
                    if (rotationRequired)
                    {
                        await RotatePasswordCoreAsync(payload, executionLogs, context, azdo, endpoint, graph, application, now, oldPassCred);
                    }
                }
            }
        }

        private static async Task RotatePasswordCoreAsync(
            CredentialRotatePayload payload, 
            StringBuilder executionLogs, 
            Dictionary<string, string> context, 
            AzDoService azdo, 
            Payloads.AzureDevOps.VstsServiceEndpoint endpoint, 
            GraphServiceClient graph, 
            Application application, 
            DateTimeOffset now, 
            PasswordCredential oldPassCred)
        {
            var newPassCred = await graph.Applications[application.Id]
              .AddPassword(new PasswordCredential
              {
                  DisplayName = $"AutoGen: {now}",
                  Hint = $"AutoGen: {now}",
                  StartDateTime = now,
                  EndDateTime = now.AddDays(payload.LifeTimeInDays)
              })
              .Request().PostAsync();

            endpoint.Authorization.Parameters.Serviceprincipalkey = newPassCred.SecretText;
            await azdo.UpdateServiceEndpointsAsync(payload.ProjectId, endpoint.Id, endpoint);
            context.Add("Secret Id", newPassCred.KeyId.ToString());
            context.Add("Secret Start Time", newPassCred.StartDateTime.ToString());
            context.Add("Secret End Time", newPassCred.EndDateTime.ToString());
            await graph
                .Applications[application.Id]
                .RemovePassword(oldPassCred.KeyId.Value)
                .Request().PostAsync();
            executionLogs.AppendLine($"App ({application.DisplayName}) password credentail ({oldPassCred.KeyId.Value}) deleted successfully");
            context.Add("Deleted Secret Id", oldPassCred.KeyId.ToString());            
        }
    }
}
