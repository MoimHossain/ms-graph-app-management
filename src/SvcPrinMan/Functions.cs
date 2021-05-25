using Azure.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using SvcPrinMan.Payloads;
using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

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
            return new OkObjectResult(await GraphService.ListAppsAsync());
        }


        [FunctionName("GetAppAsync")]
        public static async Task<IActionResult> GetAppAsync(
           [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "applications/{objectId:guid}")] HttpRequest req,
           Guid objectId,
           ILogger log)
        {   
            log.LogInformation("GetApp function processed a request.");            
            return new OkObjectResult(await GraphService.GetAppAsync(objectId));
        }


        [FunctionName("CreateAppAsync")]
        public static async Task<IActionResult> CreateAppAsync(
           [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "applications")]ApplicationPayload payload,
           ILogger log)
        {
            log.LogInformation("CreateAppAsync function processed a request.");
            return new OkObjectResult(await GraphService.CreateAppAsync(payload));
        }

        [FunctionName("CreatePasswordForAppAsync")]
        public static async Task<IActionResult> CreatePasswordForAppAsync(
           [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "applications/{objectId:guid}/password-credentials")] PasswordCredentialPayload passCred,
           Guid objectId,
           ILogger log)
        {            
            log.LogInformation("CreatePasswordForAppAsync function processed a request.");            
            return new OkObjectResult(await GraphService.CreatePasswordForAppAsync(objectId, passCred));
        }

        [FunctionName("DeleteAppPasswordCredentialsAsync")]
        public static async Task<IActionResult> DeleteAppPasswordCredentialsAsync(
           [HttpTrigger(AuthorizationLevel.Anonymous, "DELETE", Route = "applications/{objectId:guid}/password-credentials/{keyId:guid}")] HttpRequest req,
           Guid objectId,
           Guid keyId,
           ILogger log)
        {            
            log.LogInformation("DeleteAppPasswordCredentialsAsync function processed a request.");            
            return new OkObjectResult(await GraphService.DeleteAppPasswordCredentialsAsync(objectId, keyId));
        }


        [FunctionName("AddOwnersToAppAsync")]
        public static async Task<IActionResult> AddOwnerToAppAsync(
           [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "applications/{objectId:guid}/owners/{ownerObjectId:guid}")] HttpRequest req,
           Guid objectId,
           Guid ownerObjectId,
           ILogger log)
        {
            log.LogInformation("AddOwnersToAppAsync function processed a request.");
            return new OkObjectResult(await GraphService.AddOwnerToAppAsync(objectId, ownerObjectId));
        }

        [FunctionName("DeleteOwnerFromAppAsync")]
        public static async Task<IActionResult> DeleteOwnerFromAppAsync(
           [HttpTrigger(AuthorizationLevel.Anonymous, "DELETE", Route = "applications/{objectId:guid}/owners/{ownerObjectId:guid}")] HttpRequest req,
           Guid objectId,
           Guid ownerObjectId,
           ILogger log)
        {
            log.LogInformation("DeleteOwnerFromAppAsync function processed a request.");
            return new OkObjectResult(await GraphService.DeleteOwnerFromAppAsync(objectId, ownerObjectId));
        }

        [FunctionName("DeleteAppAsync")]
        public static async Task<IActionResult> DeleteAppAsync(
           [HttpTrigger(AuthorizationLevel.Anonymous, "DELETE", Route = "applications/{objectId:guid}")] HttpRequest req,
           Guid objectId,
           ILogger log)
        {
            log.LogInformation("DeleteAppAsync function processed a request.");            
            return new OkObjectResult(await GraphService.DeleteAppAsync(objectId));
        }

        [FunctionName("ListServicePrincipalsAsync")]
        public static async Task<IActionResult> ListServicePrincipalsAsync(
           [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "service-principals")] HttpRequest req,
           ILogger log)
        {
            log.LogInformation("ListServicePrincipalsAsync function processed a request.");
            return new OkObjectResult(await GraphService.ListServicePrincipalsAsync());
        }

        [FunctionName("GetServicePrincipalsByIdAsync")]
        public static async Task<IActionResult> GetServicePrincipalsByIdAsync(
           [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "service-principals/{objectId:guid}")] HttpRequest req,
           Guid objectId,
           ILogger log)
        {
            log.LogInformation("GetServicePrincipalsByIdAsync function processed a request.");
            return new OkObjectResult(await GraphService.GetServicePrincipalsByIdAsync(objectId));
        }

        [FunctionName("ListServicePrincipalsByAppIdAsync")]
        public static async Task<IActionResult> ListServicePrincipalsByAppIdAsync(
           [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "service-principals/app-ids/{appId:guid}")] HttpRequest req,
           Guid appId,
           ILogger log)
        {
            log.LogInformation("ListServicePrincipalsByAppIdAsync function processed a request.");
            return new OkObjectResult(await GraphService.ListServicePrincipalsByAppIdAsync(appId));
        }

        [FunctionName("CreateServicePrincipalAsync")]
        public static async Task<IActionResult> CreateServicePrincipalAsync(
           [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "service-principals")] ServicePrincipalPayload payload,
           ILogger log)
        {
            log.LogInformation("CreateServicePrincipalAsync function processed a request.");
            return new OkObjectResult(await GraphService.CreateServicePrincipalAsync(payload));
        }

        [FunctionName("CreatePasswordForServicePrincipalAsync")]
        public static async Task<IActionResult> CreatePasswordForServicePrincipalAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "service-principals/{objectId:guid}/password-credentials")] PasswordCredentialPayload passCred,
            Guid objectId,
            ILogger log)
        {
            log.LogInformation("CreatePasswordForServicePrincipalAsync function processed a request.");
            return new OkObjectResult(await GraphService.CreatePasswordForServicePrincipalAsync(objectId, passCred));
        }

        [FunctionName("DeleteServicePrincipalPasswordCredentialsAsync")]
        public static async Task<IActionResult> DeleteServicePrincipalPasswordCredentialsAsync(
           [HttpTrigger(AuthorizationLevel.Anonymous, "DELETE", Route = "service-principals/{objectId:guid}/password-credentials/{keyId:guid}")] HttpRequest req,
           Guid objectId,
           Guid keyId,
           ILogger log)
        {
            log.LogInformation("DeleteServicePrincipalPasswordCredentialsAsync function processed a request.");
            return new OkObjectResult(await GraphService.DeleteServicePrincipalPasswordCredentialsAsync(objectId, keyId));
        }

        [FunctionName("AddOwnerToServicePrincipalAsync")]
        public static async Task<IActionResult> AddOwnerToServicePrincipalAsync(
           [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "service-principals/{objectId:guid}/owners/{ownerObjectId:guid}")] HttpRequest req,
           Guid objectId,
           Guid ownerObjectId,
           ILogger log)
        {
            log.LogInformation("AddOwnerToServicePrincipalAsync function processed a request.");
            return new OkObjectResult(await GraphService.AddOwnerToServicePrincipalAsync(objectId, ownerObjectId));
        }

        [FunctionName("DeleteOwnerFromServicePrincipalAsync")]
        public static async Task<IActionResult> DeleteOwnerFromServicePrincipalAsync(
           [HttpTrigger(AuthorizationLevel.Anonymous, "DELETE", Route = "service-principals/{objectId:guid}/owners/{ownerObjectId:guid}")] HttpRequest req,
           Guid objectId,
           Guid ownerObjectId,
           ILogger log)
        {
            log.LogInformation("DeleteOwnerFromServicePrincipalAsync function processed a request.");
            return new OkObjectResult(await GraphService.DeleteOwnerFromServicePrincipalAsync(objectId, ownerObjectId));
        }
    }
}
