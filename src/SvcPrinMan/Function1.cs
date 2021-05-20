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

/*GraphServiceClient graphClient = new GraphServiceClient( authProvider );

var passwordCredential = new PasswordCredential
{
	DisplayName = "Password friendly name"
};

await graphClient.Applications["{application-id}"]
	.AddPassword(passwordCredential)
	.Request()
	.PostAsync();

https://docs.microsoft.com/en-us/graph/api/application-post-owners?view=graph-rest-1.0&tabs=http
*/
namespace SvcPrinMan
{
    public static class Funcs
    {
        [FunctionName("Hello")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string name = req.Query["name"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            name = name ?? data?.name;

            string responseMessage = string.IsNullOrEmpty(name)
                ? "This HTTP triggered function executed successfully. Pass a name in the query string or in the request body for a personalized response."
                : $"Hello, {name}. This HTTP triggered function executed successfully.";

            return new OkObjectResult(responseMessage);
        }


        [FunctionName("TestRGPAccess")]
        public static async Task<IActionResult> TestRGPAccessAsync(
           [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
           ILogger log)
        {
            log.LogInformation("TestRGPAccess function processed a request.");
            var creds = SdkContext.AzureCredentialsFactory
                        .FromSystemAssignedManagedServiceIdentity(
                            MSIResourceType.AppService, AzureEnvironment.AzureGlobalCloud, tenantId);

            var azure = ResourceManager
                .Configure()
                .Authenticate(creds)
                .WithSubscription(subId);

            var resources = await azure.ResourceGroups.ListAsync();
            var responseMessage = string.Join($"{Environment.NewLine}", resources.Select(r => r.Name).ToList());
            return new OkObjectResult(responseMessage);
        }

        [FunctionName("TestMSGraphAccess")]
        public static async Task<IActionResult> TestMSGraphAccessAsync(
           [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
           ILogger log)
        {
            var responseMessage = string.Empty;
            log.LogInformation("TestMSGraphAccess function processed a request.");

            var graphServiceClient = await GetGraphClientAsync();

            var userPrincipals = new List<string>();
            try
            {
                var users = await graphServiceClient.Users.Request().GetAsync();
                foreach (var u in users)
                {
                    userPrincipals.Add(u.UserPrincipalName);
                }
                responseMessage = string.Join($"{Environment.NewLine}", userPrincipals);
            }
            catch (Exception ex)
            {
                responseMessage = ex.Message;
            }
            return new OkObjectResult(responseMessage);
        }

        [FunctionName("ListApps")]
        public static async Task<IActionResult> ListAppsAsync(
           [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
           ILogger log)
        {
            var responseMessage = string.Empty;
            log.LogInformation("ListAppsAsync function processed a request.");

            var graphServiceClient = await GetGraphClientAsync();
            var results = new List<string>();
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


        [FunctionName("GetApp")]
        public static async Task<IActionResult> GetAppAsync(
           [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
           ILogger log)
        {
            var responseMessage = string.Empty;
            var objectId = req.Query["id"];
            log.LogInformation("GetApp function processed a request.");

            if(!string.IsNullOrWhiteSpace(objectId))
            {
                var graphServiceClient = await GetGraphClientAsync();
                var results = new List<string>();
                try
                {   
                    var app = await graphServiceClient
                        .Applications[objectId].Request().GetAsync();


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

        [FunctionName("CreateApp")]
        public static async Task<IActionResult> CreateAppAsync(
           [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
           ILogger log)
        {
            var responseMessage = string.Empty;
            var name = req.Query["name"];
            log.LogInformation("CreateApp function processed a request.");

            if (!string.IsNullOrWhiteSpace(name))
            {
                var gc = await GetGraphClientAsync();
                var results = new List<string>();
                try
                {
                    var app = new Application
                    {
                        Description = "Application created by API",
                        DisplayName = name,                                                
                        Tags = new List<string> { "Automated", "Azure", "Venus" }
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


        [FunctionName("CreateServicePrincipal")]
        public static async Task<IActionResult> CreateServicePrincipalAsync(
           [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
           ILogger log)
        {
            var responseMessage = string.Empty;
            var appId = req.Query["appId"];
            log.LogInformation("CreateServicePrincipal function processed a request.");

            if (!string.IsNullOrWhiteSpace(appId))
            {
                var gc = await GetGraphClientAsync();
                var results = new List<string>();
                try
                {
                    var sp = new ServicePrincipal
                    {
                        AppId = appId,                        
                        Description = "Service Principal created by API",                        
                        Tags = new List<string> { "Automated", "Azure", "Venus" }
                    };

                    sp = await gc.ServicePrincipals.Request().AddAsync(sp);

                    results.Add(GetStringRepresentation(sp));

                    responseMessage = string.Join($"{Environment.NewLine}", results);
                }
                catch (Exception ex)
                {
                    responseMessage = ex.Message;
                }
            }
            return new OkObjectResult(responseMessage);
        }


        [FunctionName("ListServicePrincipals")]
        public static async Task<IActionResult> ListServicePrincipalsAsync(
           [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
           ILogger log)
        {
            var responseMessage = string.Empty;
            log.LogInformation("ListServicePrincipals function processed a request.");            

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

        [FunctionName("GetServicePrincipalsById")]
        public static async Task<IActionResult> GetServicePrincipalsByIdAsync(
           [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
           ILogger log)
        {
            var responseMessage = string.Empty;
            var objectId = req.Query["id"];
            log.LogInformation("GetServicePrincipalsById function processed a request.");

            var graphServiceClient = await GetGraphClientAsync();
            var results = new List<string>();
            try
            {
                var sp = await graphServiceClient.ServicePrincipals[objectId].Request().GetAsync();
                results.Add(GetStringRepresentation(sp));
                responseMessage = string.Join($"{Environment.NewLine}", results);
            }
            catch (Exception ex)
            {
                responseMessage = ex.Message;
            }
            return new OkObjectResult(responseMessage);
        }

        [FunctionName("ListServicePrincipalsByAppId")]
        public static async Task<IActionResult> ListServicePrincipalsByAppIdAsync(
           [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
           ILogger log)
        {
            var responseMessage = string.Empty;
            var appId = req.Query["appId"];
            log.LogInformation("ListServicePrincipalsByAppIdAsync function processed a request.");

            var graphServiceClient = await GetGraphClientAsync();
            var results = new List<string>();
            try
            {
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

            sb.AppendLine($"Key Credentials:  ");
            foreach (var creds in sp.KeyCredentials)
            {
                sb.AppendLine($"\t > {creds.DisplayName}; Type: {creds.Type}; Start Time: {creds.StartDateTime}; End Time: {creds.EndDateTime}");
            }

            sb.AppendLine($"Password Credentials:  ");
            foreach (var creds in sp.PasswordCredentials)
            {
                sb.AppendLine($"\t > {creds.DisplayName}; Start Time: {creds.StartDateTime}; End Time: {creds.EndDateTime}");
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
                    sb.AppendLine($"\t > {creds.DisplayName}; Start Time: {creds.StartDateTime}; End Time: {creds.EndDateTime}");
                }
            }
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
