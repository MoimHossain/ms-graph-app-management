using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using System.Linq;
using Azure.Identity;
using Microsoft.Graph;
using System.Net.Http.Headers;
using System.Collections.Generic;
using System.Text;
using SvcPrinMan.Payloads;

namespace SvcPrinMan
{
    public static class Funcs
    {

        [FunctionName("ListAppsAsync")]
        public static async Task<IActionResult> ListAppsAsync(
           [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "applications")] HttpRequest req,
           ILogger log)
        {
            log.LogInformation("ListAppsAsync function processed a request.");

            var graphServiceClient = await GetGraphClientAsync();
            var results = new List<string>();
            string responseMessage;
            try
            {
                var apps = await graphServiceClient.Applications.Request().GetAsync();
                foreach (var app in apps)
                {
                    results.Add(GetStringRepresentation(app));
                }
                responseMessage = string.Join($"{Environment.NewLine}", results);
            }
            catch (Exception ex)
            {
                responseMessage = ex.Message;
            }
            return new OkObjectResult(responseMessage);
        }


        [FunctionName("GetAppAsync")]
        public static async Task<IActionResult> GetAppAsync(
           [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "applications/{objectId:guid}")] HttpRequest req,
           Guid objectId,
           ILogger log)
        {
            var responseMessage = string.Empty;            
            log.LogInformation("GetApp function processed a request.");
            try
            {
                var graphServiceClient = await GetGraphClientAsync();
                var app = await graphServiceClient
                    .Applications[objectId.ToString()].Request().GetAsync();

                // loading the owners explicitely
                app.Owners = await graphServiceClient.Applications[objectId.ToString()].Owners.Request().GetAsync();                 
                responseMessage = GetStringRepresentation(app);
            }
            catch (Exception ex)
            {
                responseMessage = ex.Message;
            }
            return new OkObjectResult(responseMessage);
        }


        [FunctionName("CreateAppAsync")]
        public static async Task<IActionResult> CreateAppAsync(
           [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "applications")]ApplicationPayload payload,
           ILogger log)
        {
            var responseMessage = string.Empty;            
            log.LogInformation("CreateAppAsync function processed a request.");

            if (payload != null)
            {
                var gc = await GetGraphClientAsync();
                var results = new List<string>();
                try
                {
                    var app = new Application
                    {
                        Description = payload.Description,
                        DisplayName = payload.DisplayName,
                        Tags = payload.Tags
                    };
                    app = await gc.Applications.Request().AddAsync(app);
                    results.Add(GetStringRepresentation(app));
                    responseMessage = string.Join($"{Environment.NewLine}", results);
                }
                catch (Exception ex)
                {
                    responseMessage = ex.Message;
                }
            }
            return new OkObjectResult(responseMessage);
        }

        [FunctionName("CreatePasswordForAppAsync")]
        public static async Task<IActionResult> CreatePasswordForAppAsync(
           [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "applications/{objectId:guid}/password-credentials")] PasswordCredentialPayload passCred,
           Guid objectId,
           ILogger log)
        {
            var responseMessage = string.Empty;
            log.LogInformation("CreatePasswordForAppAsync function processed a request.");
            if (passCred != null && !string.IsNullOrWhiteSpace(passCred.DisplayName)
                && passCred.StartDateTime != null && passCred.EndDateTime != null)
            {
                try
                {
                    var gc = await GetGraphClientAsync();
                    var passCredResult = await gc.Applications[objectId.ToString()]
                        .AddPassword(new PasswordCredential
                        {
                            DisplayName = passCred.DisplayName,
                            Hint = passCred.Hint,
                            StartDateTime = passCred.StartDateTime,
                            EndDateTime = passCred.EndDateTime
                        })
                        .Request().PostAsync();
                    responseMessage = GetStringRepresentation(passCredResult);
                }
                catch (Exception ex)
                {
                    responseMessage = ex.Message;
                }
            }
            else {
                responseMessage = "Either start date or end date is not in correct format.";
            }

            return new OkObjectResult(responseMessage);
        }

        [FunctionName("DeleteAppPasswordCredentialsAsync")]
        public static async Task<IActionResult> DeleteAppPasswordCredentialsAsync(
           [HttpTrigger(AuthorizationLevel.Anonymous, "DELETE", Route = "applications/{objectId:guid}/password-credentials/{keyId:guid}")] HttpRequest req,
           Guid objectId,
           Guid keyId,
           ILogger log)
        {
            var responseMessage = string.Empty;
            log.LogInformation("DeleteAppPasswordCredentialsAsync function processed a request.");
            try
            {
                var graphServiceClient = await GetGraphClientAsync();
                await graphServiceClient
                    .Applications[objectId.ToString()]
                    .RemovePassword(keyId)
                    .Request().PostAsync();
                responseMessage = $"App ({objectId}) password credentail ({keyId}) deleted successfully";
            }
            catch (Exception ex)
            {
                responseMessage = ex.Message;
            }
            return new OkObjectResult(responseMessage);
        }


        [FunctionName("AddOwnersToAppAsync")]
        public static async Task<IActionResult> AddOwnerToAppAsync(
           [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "applications/{objectId:guid}/owners/{ownerObjectId:guid}")] HttpRequest req,
           Guid objectId,
           Guid ownerObjectId,
           ILogger log)
        {
            var responseMessage = string.Empty;
            log.LogInformation("AddOwnersToAppAsync function processed a request.");
            try
            {
                var gc = await GetGraphClientAsync();
                await gc.Applications[objectId.ToString()].Owners.References
                    .Request()
                    .AddAsync(new DirectoryObject { Id = ownerObjectId.ToString() });
                responseMessage = $"Owner ({ownerObjectId}) added successfully.";
            }
            catch (Exception ex)
            {
                responseMessage = ex.Message;
            }
            return new OkObjectResult(responseMessage);
        }

        [FunctionName("DeleteOwnerFromAppAsync")]
        public static async Task<IActionResult> DeleteOwnerFromAppAsync(
           [HttpTrigger(AuthorizationLevel.Anonymous, "DELETE", Route = "applications/{objectId:guid}/owners/{ownerObjectId:guid}")] HttpRequest req,
           Guid objectId,
           Guid ownerObjectId,
           ILogger log)
        {
            string responseMessage;
            log.LogInformation("DeleteOwnerFromAppAsync function processed a request.");
            try
            {
                var graphServiceClient = await GetGraphClientAsync();
                await graphServiceClient
                    .Applications[objectId.ToString()].Owners[ownerObjectId.ToString()]
                    .Reference.Request().DeleteAsync();
                responseMessage = "App owner deleted successfully";
            }
            catch (Exception ex)
            {
                responseMessage = ex.Message;
            }
            return new OkObjectResult(responseMessage);
        }

        [FunctionName("DeleteAppAsync")]
        public static async Task<IActionResult> DeleteAppAsync(
           [HttpTrigger(AuthorizationLevel.Anonymous, "DELETE", Route = "applications/{objectId:guid}")] HttpRequest req,
           Guid objectId,
           ILogger log)
        {
            var responseMessage = string.Empty;
            log.LogInformation("DeleteAppAsync function processed a request.");
            try
            {
                var graphServiceClient = await GetGraphClientAsync();
                await graphServiceClient
                    .Applications[objectId.ToString()].Request().DeleteAsync();
                responseMessage = "App deleted successfully";
            }
            catch (Exception ex)
            {
                responseMessage = ex.Message;
            }
            return new OkObjectResult(responseMessage);
        }

        [FunctionName("ListServicePrincipalsAsync")]
        public static async Task<IActionResult> ListServicePrincipalsAsync(
           [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "service-principals")] HttpRequest req,
           ILogger log)
        {
            string responseMessage;
            log.LogInformation("ListServicePrincipalsAsync function processed a request.");

            var graphServiceClient = await GetGraphClientAsync();
            var results = new List<string>();
            try
            {
                var sps = await graphServiceClient.ServicePrincipals.Request().GetAsync();
                foreach (var sp in sps)
                {
                    results.Add(GetStringRepresentation(sp));
                }
                responseMessage = string.Join($"{Environment.NewLine}", results);
            }
            catch (Exception ex)
            {
                responseMessage = ex.Message;
            }
            return new OkObjectResult(responseMessage);
        }

        [FunctionName("GetServicePrincipalsByIdAsync")]
        public static async Task<IActionResult> GetServicePrincipalsByIdAsync(
           [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "service-principals/{objectId:guid}")] HttpRequest req,
           Guid objectId,
           ILogger log)
        {
            string responseMessage;            
            log.LogInformation("GetServicePrincipalsByIdAsync function processed a request.");
                                    
            try
            {
                var graphServiceClient = await GetGraphClientAsync();
                var sp = await graphServiceClient.ServicePrincipals[objectId.ToString()].Request().GetAsync();
                sp.Owners = await graphServiceClient.ServicePrincipals[objectId.ToString()].Owners.Request().GetAsync();
                responseMessage = GetStringRepresentation(sp);
            }
            catch (Exception ex)
            {
                responseMessage = ex.Message;
            }
            return new OkObjectResult(responseMessage);
        }

        [FunctionName("ListServicePrincipalsByAppIdAsync")]
        public static async Task<IActionResult> ListServicePrincipalsByAppIdAsync(
           [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "service-principals/app-ids/{appId:guid}")] HttpRequest req,
           Guid appId,
           ILogger log)
        {
            string responseMessage;
            log.LogInformation("ListServicePrincipalsByAppIdAsync function processed a request.");
            
            try
            {
                var graphServiceClient = await GetGraphClientAsync();
                var results = new List<string>();
                var sps = await graphServiceClient.ServicePrincipals.Request().Filter($"appId eq '{appId}'").GetAsync();
                foreach (var sp in sps)
                {
                    results.Add(GetStringRepresentation(sp));
                }
                responseMessage = string.Join($"{Environment.NewLine}", results);
            }
            catch (Exception ex)
            {
                responseMessage = ex.Message;
            }
            return new OkObjectResult(responseMessage);
        }

        [FunctionName("CreateServicePrincipalAsync")]
        public static async Task<IActionResult> CreateServicePrincipalAsync(
           [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "service-principals")] ServicePrincipalPayload payload,
           ILogger log)
        {
            string responseMessage;
            log.LogInformation("CreateServicePrincipalAsync function processed a request.");

            var gc = await GetGraphClientAsync();
            try
            {
                var sp = new ServicePrincipal
                {
                    AppId = payload.AppId.ToString(),
                    Description = payload.Description,
                    Tags = payload.Tags
                };
                sp = await gc.ServicePrincipals.Request().AddAsync(sp);
                responseMessage = GetStringRepresentation(sp);
            }
            catch (Exception ex)
            {
                responseMessage = ex.Message;
            }
            return new OkObjectResult(responseMessage);
        }

        [FunctionName("CreatePasswordForServicePrincipalAsync")]
        public static async Task<IActionResult> CreatePasswordForServicePrincipalAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "service-principals/{objectId:guid}/password-credentials")] PasswordCredentialPayload passCred,
            Guid objectId,
            ILogger log)
        {
            string responseMessage;
            log.LogInformation("CreatePasswordForServicePrincipalAsync function processed a request.");

            try
            {
                var gc = await GetGraphClientAsync();                
                var passCredResponse = await gc.ServicePrincipals[objectId.ToString()]
                    .AddPassword(new PasswordCredential
                    {
                        DisplayName = passCred.DisplayName,
                        Hint = passCred.Hint,
                        StartDateTime = passCred.StartDateTime,
                        EndDateTime = passCred.EndDateTime
                    })
                    .Request().PostAsync();                
                responseMessage = GetStringRepresentation(passCredResponse);
            }
            catch (Exception ex)
            {
                responseMessage = ex.Message;
            }
            return new OkObjectResult(responseMessage);
        }

        [FunctionName("DeleteServicePrincipalPasswordCredentialsAsync")]
        public static async Task<IActionResult> DeleteServicePrincipalPasswordCredentialsAsync(
           [HttpTrigger(AuthorizationLevel.Anonymous, "DELETE", Route = "service-principals/{objectId:guid}/password-credentials/{keyId:guid}")] HttpRequest req,
           Guid objectId,
           Guid keyId,
           ILogger log)
        {
            var responseMessage = string.Empty;
            log.LogInformation("DeleteServicePrincipalPasswordCredentialsAsync function processed a request.");
            try
            {
                var graphServiceClient = await GetGraphClientAsync();
                await graphServiceClient
                    .ServicePrincipals[objectId.ToString()]
                    .RemovePassword(keyId)
                    .Request().PostAsync();
                responseMessage = $"Service Principal ({objectId}) password credentail ({keyId}) deleted successfully";
            }
            catch (Exception ex)
            {
                responseMessage = ex.Message;
            }
            return new OkObjectResult(responseMessage);
        }

        [FunctionName("AddOwnerToServicePrincipalAsync")]
        public static async Task<IActionResult> AddOwnerToServicePrincipalAsync(
           [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "service-principals/{objectId:guid}/owners/{ownerObjectId:guid}")] HttpRequest req,
           Guid objectId,
           Guid ownerObjectId,
           ILogger log)
        {
            var responseMessage = string.Empty;
            log.LogInformation("AddOwnerToServicePrincipalAsync function processed a request.");
            try
            {
                var gc = await GetGraphClientAsync();
                await gc.ServicePrincipals[objectId.ToString()].Owners.References
                    .Request()
                    .AddAsync(new DirectoryObject { Id = ownerObjectId.ToString() });
                responseMessage = $"Service Principal Owner ({ownerObjectId}) added successfully.";
            }
            catch (Exception ex)
            {
                responseMessage = ex.Message;
            }
            return new OkObjectResult(responseMessage);
        }

        [FunctionName("DeleteOwnerFromServicePrincipalAsync")]
        public static async Task<IActionResult> DeleteOwnerFromServicePrincipalAsync(
           [HttpTrigger(AuthorizationLevel.Anonymous, "DELETE", Route = "service-principals/{objectId:guid}/owners/{ownerObjectId:guid}")] HttpRequest req,
           Guid objectId,
           Guid ownerObjectId,
           ILogger log)
        {
            string responseMessage;
            log.LogInformation("DeleteOwnerFromServicePrincipalAsync function processed a request.");
            try
            {
                var graphServiceClient = await GetGraphClientAsync();
                await graphServiceClient
                    .ServicePrincipals[objectId.ToString()].Owners[ownerObjectId.ToString()]
                    .Reference.Request().DeleteAsync();
                responseMessage = "Service Principal owner deleted successfully";
            }
            catch (Exception ex)
            {
                responseMessage = ex.Message;
            }
            return new OkObjectResult(responseMessage);
        }


        private static string GetStringRepresentation(ServicePrincipal sp)
        {
            var sb = new StringBuilder();

            sb.AppendLine($"Name: {sp.DisplayName} ");
            sb.AppendLine($"Description: {sp.Description} ");
            sb.AppendLine($"AppDisplayName: {sp.AppDisplayName} ");
            sb.AppendLine($"AppDescription: {sp.AppDescription} ");

            sb.AppendLine($"Alternative names:  ");
            foreach (var name in sp.AlternativeNames)
            {
                sb.AppendLine($"\t > {name} ");
            }

            sb.AppendLine($"App Id: {sp.AppId} ");
            sb.AppendLine($"Object Id: {sp.Id} ");
            sb.AppendLine($"AppOwnerOrganizationId: {sp.AppOwnerOrganizationId} ");

            if (sp.Owners != null)
            {
                sb.AppendLine($"Owners:  ");
                foreach (var owner in sp.Owners)
                {
                    sb.AppendLine($"\t > {owner.Id} ");
                }
            }

            sb.AppendLine($"Key Credentials:  ");
            foreach (var creds in sp.KeyCredentials)
            {
                sb.AppendLine($"\t > {creds.DisplayName}; Type: {creds.Type}; Start Time: {creds.StartDateTime}; End Time: {creds.EndDateTime}");
            }

            sb.AppendLine($"Password Credentials:  ");
            foreach (var creds in sp.PasswordCredentials)
            {
                sb.AppendLine($"\t > {creds.DisplayName}; Start Time: {creds.StartDateTime}; End Time: {creds.EndDateTime}; ID: {creds.KeyId}");
            }
            return sb.ToString();
        }

        private static string GetStringRepresentation(Application app)
        {
            var sb = new StringBuilder();

            sb.AppendLine($"Name: {app.DisplayName} ");
            sb.AppendLine($"Description: {app.Description} ");

            if(app.Owners != null )
            {
                sb.AppendLine($"Owners:  ");
                foreach (var owner in app.Owners)
                {
                    sb.AppendLine($"\t > {owner.Id} ");
                }
            }            

            sb.AppendLine($"App Id: {app.AppId} ");
            sb.AppendLine($"Object Id: {app.Id} ");

            if(app.Tags != null )
            {
                sb.AppendLine($"Tags:  ");
                foreach (var tag in app.Tags)
                {
                    sb.AppendLine($"\t > {tag}");
                }
            }
            
            if(app.AppRoles != null )
            {
                sb.AppendLine($"App Roles:  ");
                foreach (var role in app.AppRoles)
                {
                    sb.AppendLine($"\t > {role.DisplayName}; Id={role.Id}; Enabled={role.IsEnabled}; ");
                }
            }

            if(app.KeyCredentials != null )
            {
                sb.AppendLine($"Key Credentials:  ");
                foreach (var creds in app.KeyCredentials)
                {
                    sb.AppendLine($"\t > {creds.DisplayName}; Type: {creds.Type}; Start Time: {creds.StartDateTime}; End Time: {creds.EndDateTime}");
                }
            }
            

            if (app.PasswordCredentials != null)
            {
                sb.AppendLine($"Password Credentials:  ");
                foreach (var creds in app.PasswordCredentials)
                {
                    sb.AppendLine($"\t > {creds.DisplayName}; Start Time: {creds.StartDateTime}; End Time: {creds.EndDateTime}; ID: {creds.KeyId}");
                }
            }
            return sb.ToString();
        }
        private static string GetStringRepresentation(PasswordCredential passCred)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"{passCred.DisplayName} ({passCred.StartDateTime} - {passCred.EndDateTime})");
            sb.AppendLine($"Password: {passCred.SecretText}");
            return sb.ToString();
        }

        private async static Task<GraphServiceClient> GetGraphClientAsync()
        {
            // Create the Microsoft Graph service client
            // with a DefaultAzureCredential class, which gets an access token
            // by using the available Managed Identity.
            // In order to use user assigned managed identity use the following two lines.
            // string userAssignedClientId = "<your managed identity client Id>";
            // var credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions { ManagedIdentityClientId = userAssignedClientId });

            var credential = new DefaultAzureCredential();
            var token = await credential.GetTokenAsync(
                new Azure.Core.TokenRequestContext(
                    new[] { "https://graph.microsoft.com/.default" }));
            var graphServiceClient = new GraphServiceClient(
                new DelegateAuthenticationProvider((requestMessage) =>
                {
                    requestMessage
                    .Headers
                    .Authorization = new AuthenticationHeaderValue("bearer", token.Token);
                    return Task.CompletedTask;
                }));
            return graphServiceClient;
        }
        private const string tenantId = "cac2cc32-7de9-4f3d-8d79-76375427b620";
        private const string subId = "c5b22afb-96db-4354-99a4-5f808d5221c6";
    }
}
