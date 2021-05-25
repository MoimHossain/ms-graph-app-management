

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using SvcPrinMan.Payloads;
using SvcPrinMan.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SvcPrinMan
{
    public static class Funcs
    {
        [FunctionName("RotateCredentailsIfRequiredAsync")]
        public static async Task<IActionResult> RotateCredentailsIfRequiredAsync(
           [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "rotate")] CredentialRotatePayload payload,
           ILogger log)
        {
            log.LogInformation("RotateCredentailsIfRequiredAsync function processed a request.");
            var executionLogs = new StringBuilder();
            const string AZURERM_TYPE = "azurerm";
            const string SPN_KEY = "spnKey";
            if (payload != null && !string.IsNullOrWhiteSpace(payload.OrgName)
                && !string.IsNullOrWhiteSpace(payload.PAT))
            {
                var azdo = new AzDoService(payload.OrgName, payload.PAT);
                var endpointIds = new List<Guid>();
                if(payload.RotateAllServiceConnections)
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

                foreach(var endpointId in endpointIds)
                {
                    var endpoint = await azdo.GetServiceEndpointsAsync(payload.ProjectId, endpointId);
                    if (endpoint != null && AZURERM_TYPE.Equals(endpoint.Type)
                        && endpoint.Authorization != null && endpoint.Authorization.Parameters != null
                        && SPN_KEY.Equals(endpoint.Authorization.Parameters.AuthenticationType))
                    {
                        executionLogs.AppendLine($"Endpoint {endpoint.Name}({endpoint.Id}) loaded for credentials check.");
                        var graph = await MSGraph.GetGraphClientAsync();
                        var apps = await graph.Applications.Request().Filter($"appId eq '{endpoint.Authorization.Parameters.Serviceprincipalid}'").GetAsync();
                        if (apps != null && apps.Any())
                        {                            
                            var application = apps.First();
                            executionLogs.AppendLine($"AppRegistration {application.DisplayName}({application.AppId}) found for {endpoint.Name}({endpoint.Id}).");
                            if (application.PasswordCredentials.Any())
                            {
                                executionLogs.AppendLine($"Password Credentails found ({application.PasswordCredentials.Count()}).");

                                var now = DateTimeOffset.UtcNow;
                                var oldPassCred = application.PasswordCredentials.First();
                                if(oldPassCred.EndDateTime.HasValue)
                                {
                                    var rotationRequired = (now.AddDays(payload.DaysBeforeExpire) > oldPassCred.EndDateTime);
                                    executionLogs.AppendLine($"{now.AddDays(payload.DaysBeforeExpire)} > {oldPassCred.EndDateTime} = {rotationRequired}");
                                    if (rotationRequired)
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
                                        endpoint = await azdo.UpdateServiceEndpointsAsync(payload.ProjectId, endpoint.Id, endpoint);

                                        await graph
                                            .Applications[application.Id]
                                            .RemovePassword(oldPassCred.KeyId.Value)
                                            .Request().PostAsync();
                                        executionLogs.AppendLine($"App ({application.DisplayName}) password credentail ({oldPassCred.KeyId.Value}) deleted successfully");
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return new OkObjectResult(executionLogs.ToString());
        }

        [FunctionName("ListAppsAsync")]
        public static async Task<IActionResult> ListAppsAsync(
           [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "applications")] HttpRequest req,
           ILogger log)
        {
            log.LogInformation("ListAppsAsync function processed a request.");            
            return new OkObjectResult(await MSGraph.ListAppsAsync());
        }


        [FunctionName("GetAppAsync")]
        public static async Task<IActionResult> GetAppAsync(
           [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "applications/{objectId:guid}")] HttpRequest req,
           Guid objectId,
           ILogger log)
        {   
            log.LogInformation("GetApp function processed a request.");            
            return new OkObjectResult(await MSGraph.GetAppAsync(objectId));
        }

        [FunctionName("ListAppsByAppIdAsync")]
        public static async Task<IActionResult> ListAppsByAppIdAsync(
           [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "applications/app-ids/{appId:guid}")] HttpRequest req,
           Guid appId,
           ILogger log)
        {
            log.LogInformation("ListServicePrinciListAppsByAppIdAsyncpalsByAppIdAsync function processed a request.");
            return new OkObjectResult(await MSGraph.ListAppsByAppIdAsync(appId));
        }

        [FunctionName("CreateAppAsync")]
        public static async Task<IActionResult> CreateAppAsync(
           [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "applications")]ApplicationPayload payload,
           ILogger log)
        {
            log.LogInformation("CreateAppAsync function processed a request.");
            return new OkObjectResult(await MSGraph.CreateAppAsync(payload));
        }

        [FunctionName("CreatePasswordForAppAsync")]
        public static async Task<IActionResult> CreatePasswordForAppAsync(
           [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "applications/{objectId:guid}/password-credentials")] PasswordCredentialPayload passCred,
           Guid objectId,
           ILogger log)
        {            
            log.LogInformation("CreatePasswordForAppAsync function processed a request.");            
            return new OkObjectResult(await MSGraph.CreatePasswordForAppAsync(objectId, passCred));
        }

        [FunctionName("DeleteAppPasswordCredentialsAsync")]
        public static async Task<IActionResult> DeleteAppPasswordCredentialsAsync(
           [HttpTrigger(AuthorizationLevel.Anonymous, "DELETE", Route = "applications/{objectId:guid}/password-credentials/{keyId:guid}")] HttpRequest req,
           Guid objectId,
           Guid keyId,
           ILogger log)
        {            
            log.LogInformation("DeleteAppPasswordCredentialsAsync function processed a request.");            
            return new OkObjectResult(await MSGraph.DeleteAppPasswordCredentialsAsync(objectId, keyId));
        }


        [FunctionName("AddOwnersToAppAsync")]
        public static async Task<IActionResult> AddOwnerToAppAsync(
           [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "applications/{objectId:guid}/owners/{ownerObjectId:guid}")] HttpRequest req,
           Guid objectId,
           Guid ownerObjectId,
           ILogger log)
        {
            log.LogInformation("AddOwnersToAppAsync function processed a request.");
            return new OkObjectResult(await MSGraph.AddOwnerToAppAsync(objectId, ownerObjectId));
        }

        [FunctionName("DeleteOwnerFromAppAsync")]
        public static async Task<IActionResult> DeleteOwnerFromAppAsync(
           [HttpTrigger(AuthorizationLevel.Anonymous, "DELETE", Route = "applications/{objectId:guid}/owners/{ownerObjectId:guid}")] HttpRequest req,
           Guid objectId,
           Guid ownerObjectId,
           ILogger log)
        {
            log.LogInformation("DeleteOwnerFromAppAsync function processed a request.");
            return new OkObjectResult(await MSGraph.DeleteOwnerFromAppAsync(objectId, ownerObjectId));
        }

        [FunctionName("DeleteAppAsync")]
        public static async Task<IActionResult> DeleteAppAsync(
           [HttpTrigger(AuthorizationLevel.Anonymous, "DELETE", Route = "applications/{objectId:guid}")] HttpRequest req,
           Guid objectId,
           ILogger log)
        {
            log.LogInformation("DeleteAppAsync function processed a request.");            
            return new OkObjectResult(await MSGraph.DeleteAppAsync(objectId));
        }

        [FunctionName("ListServicePrincipalsAsync")]
        public static async Task<IActionResult> ListServicePrincipalsAsync(
           [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "service-principals")] HttpRequest req,
           ILogger log)
        {
            log.LogInformation("ListServicePrincipalsAsync function processed a request.");
            return new OkObjectResult(await MSGraph.ListServicePrincipalsAsync());
        }

        [FunctionName("GetServicePrincipalsByIdAsync")]
        public static async Task<IActionResult> GetServicePrincipalsByIdAsync(
           [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "service-principals/{objectId:guid}")] HttpRequest req,
           Guid objectId,
           ILogger log)
        {
            log.LogInformation("GetServicePrincipalsByIdAsync function processed a request.");
            return new OkObjectResult(await MSGraph.GetServicePrincipalsByIdAsync(objectId));
        }

        [FunctionName("ListServicePrincipalsByAppIdAsync")]
        public static async Task<IActionResult> ListServicePrincipalsByAppIdAsync(
           [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "service-principals/app-ids/{appId:guid}")] HttpRequest req,
           Guid appId,
           ILogger log)
        {
            log.LogInformation("ListServicePrincipalsByAppIdAsync function processed a request.");
            return new OkObjectResult(await MSGraph.ListServicePrincipalsByAppIdAsync(appId));
        }

        [FunctionName("CreateServicePrincipalAsync")]
        public static async Task<IActionResult> CreateServicePrincipalAsync(
           [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "service-principals")] ServicePrincipalPayload payload,
           ILogger log)
        {
            log.LogInformation("CreateServicePrincipalAsync function processed a request.");
            return new OkObjectResult(await MSGraph.CreateServicePrincipalAsync(payload));
        }

        [FunctionName("CreatePasswordForServicePrincipalAsync")]
        public static async Task<IActionResult> CreatePasswordForServicePrincipalAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "service-principals/{objectId:guid}/password-credentials")] PasswordCredentialPayload passCred,
            Guid objectId,
            ILogger log)
        {
            log.LogInformation("CreatePasswordForServicePrincipalAsync function processed a request.");
            return new OkObjectResult(await MSGraph.CreatePasswordForServicePrincipalAsync(objectId, passCred));
        }

        [FunctionName("DeleteServicePrincipalPasswordCredentialsAsync")]
        public static async Task<IActionResult> DeleteServicePrincipalPasswordCredentialsAsync(
           [HttpTrigger(AuthorizationLevel.Anonymous, "DELETE", Route = "service-principals/{objectId:guid}/password-credentials/{keyId:guid}")] HttpRequest req,
           Guid objectId,
           Guid keyId,
           ILogger log)
        {
            log.LogInformation("DeleteServicePrincipalPasswordCredentialsAsync function processed a request.");
            return new OkObjectResult(await MSGraph.DeleteServicePrincipalPasswordCredentialsAsync(objectId, keyId));
        }

        [FunctionName("AddOwnerToServicePrincipalAsync")]
        public static async Task<IActionResult> AddOwnerToServicePrincipalAsync(
           [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "service-principals/{objectId:guid}/owners/{ownerObjectId:guid}")] HttpRequest req,
           Guid objectId,
           Guid ownerObjectId,
           ILogger log)
        {
            log.LogInformation("AddOwnerToServicePrincipalAsync function processed a request.");
            return new OkObjectResult(await MSGraph.AddOwnerToServicePrincipalAsync(objectId, ownerObjectId));
        }

        [FunctionName("DeleteOwnerFromServicePrincipalAsync")]
        public static async Task<IActionResult> DeleteOwnerFromServicePrincipalAsync(
           [HttpTrigger(AuthorizationLevel.Anonymous, "DELETE", Route = "service-principals/{objectId:guid}/owners/{ownerObjectId:guid}")] HttpRequest req,
           Guid objectId,
           Guid ownerObjectId,
           ILogger log)
        {
            log.LogInformation("DeleteOwnerFromServicePrincipalAsync function processed a request.");
            return new OkObjectResult(await MSGraph.DeleteOwnerFromServicePrincipalAsync(objectId, ownerObjectId));
        }
    }
}
