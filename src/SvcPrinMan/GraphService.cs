﻿

using Azure.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using SvcPrinMan.Payloads;
using SvcPrinMan.Supports;
using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace SvcPrinMan
{
    public class MSGraph
    {   
        public static async Task<string> ListAppsAsync()
        {
            var graphServiceClient = await GetGraphClientAsync();
            var results = new List<string>();
            string responseMessage;
            try
            {
                var apps = await graphServiceClient.Applications.Request().GetAsync();
                foreach (var app in apps)
                {
                    results.Add(GraphPayloadFormatter.GetStringRepresentation(app));
                }
                responseMessage = string.Join($"{Environment.NewLine}", results);
            }
            catch (Exception ex)
            {
                responseMessage = ex.Message;
            }
            return responseMessage;
        }
                
        public static async Task<string> GetAppAsync(Guid objectId)
        {
            string responseMessage;
            try
            {
                var graphServiceClient = await GetGraphClientAsync();
                var app = await graphServiceClient
                    .Applications[objectId.ToString()].Request().GetAsync();
                // loading the owners explicitly
                app.Owners = await graphServiceClient.Applications[objectId.ToString()]
                    .Owners.Request().GetAsync();
                responseMessage = GraphPayloadFormatter.GetStringRepresentation(app);
            }
            catch (Exception ex)
            {
                responseMessage = ex.Message;
            }
            return responseMessage;
        }

        public static async Task<string> ListAppsByAppIdAsync(Guid appId)
        {
            string responseMessage;
            try
            {
                var graphServiceClient = await GetGraphClientAsync();
                var results = new List<string>();
                var apps = await graphServiceClient.Applications.Request().Filter($"appId eq '{appId}'").GetAsync();
                foreach (var app in apps)
                {
                    results.Add(GraphPayloadFormatter.GetStringRepresentation(app));
                }
                responseMessage = string.Join($"{Environment.NewLine}", results);
            }
            catch (Exception ex)
            {
                responseMessage = ex.Message;
            }
            return responseMessage;
        }

        public static async Task<string> CreateAppAsync(ApplicationPayload payload)
        {
            var responseMessage = string.Empty;
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
                    results.Add(GraphPayloadFormatter.GetStringRepresentation(app));
                    responseMessage = string.Join($"{Environment.NewLine}", results);
                }
                catch (Exception ex)
                {
                    responseMessage = ex.Message;
                }
            }
            return responseMessage;
        }
                
        public static async Task<string> CreatePasswordForAppAsync(Guid objectId, 
            PasswordCredentialPayload passCred)
        {
            string responseMessage;
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
                    responseMessage = GraphPayloadFormatter.GetStringRepresentation(passCredResult);
                }
                catch (Exception ex)
                {
                    responseMessage = ex.Message;
                }
            }
            else
            {
                responseMessage = "Either start date or end date is not in correct format.";
            }
            return responseMessage;
        }
                
        public static async Task<string> DeleteAppPasswordCredentialsAsync(           
           Guid objectId, Guid keyId)
        {
            string responseMessage;
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
            return responseMessage;
        }

        public static async Task<string> AddOwnerToAppAsync(Guid objectId, Guid ownerObjectId)
        {
            string responseMessage;
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
            return responseMessage;
        }

        public static async Task<string> DeleteOwnerFromAppAsync(Guid objectId, Guid ownerObjectId)
        {
            string responseMessage;
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
            return responseMessage;
        }
                
        public static async Task<string> DeleteAppAsync(Guid objectId)
        {
            string responseMessage;            
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
            return responseMessage;
        }
                
        public static async Task<string> ListServicePrincipalsAsync()
        {
            string responseMessage;
            var graphServiceClient = await GetGraphClientAsync();
            var results = new List<string>();
            try
            {
                var sps = await graphServiceClient.ServicePrincipals.Request().GetAsync();
                foreach (var sp in sps)
                {
                    results.Add(GraphPayloadFormatter.GetStringRepresentation(sp));
                }
                responseMessage = string.Join($"{Environment.NewLine}", results);
            }
            catch (Exception ex)
            {
                responseMessage = ex.Message;
            }
            return responseMessage;
        }

        public static async Task<string> GetServicePrincipalsByIdAsync(Guid objectId)
        {
            string responseMessage;
            try
            {
                var graphServiceClient = await GetGraphClientAsync();
                var sp = await graphServiceClient.ServicePrincipals[objectId.ToString()].Request().GetAsync();
                sp.Owners = await graphServiceClient.ServicePrincipals[objectId.ToString()].Owners.Request().GetAsync();
                responseMessage = GraphPayloadFormatter.GetStringRepresentation(sp);
            }
            catch (Exception ex)
            {
                responseMessage = ex.Message;
            }
            return responseMessage;
        }

        public static async Task<string> ListServicePrincipalsByAppIdAsync(Guid appId)
        {
            string responseMessage;
            try
            {
                var graphServiceClient = await GetGraphClientAsync();
                var results = new List<string>();
                var sps = await graphServiceClient.ServicePrincipals.Request().Filter($"appId eq '{appId}'").GetAsync();
                foreach (var sp in sps)
                {
                    results.Add(GraphPayloadFormatter.GetStringRepresentation(sp));
                }
                responseMessage = string.Join($"{Environment.NewLine}", results);
            }
            catch (Exception ex)
            {
                responseMessage = ex.Message;
            }
            return responseMessage;
        }

        public static async Task<string> CreateServicePrincipalAsync(ServicePrincipalPayload payload)
        {
            string responseMessage;
            try
            {
                var gc = await GetGraphClientAsync();
                var sp = new ServicePrincipal
                {
                    AppId = payload.AppId.ToString(),
                    Description = payload.Description,
                    Tags = payload.Tags
                };
                sp = await gc.ServicePrincipals.Request().AddAsync(sp);
                responseMessage = GraphPayloadFormatter.GetStringRepresentation(sp);
            }
            catch (Exception ex)
            {
                responseMessage = ex.Message;
            }
            return responseMessage;
        }
                
        public static async Task<string> CreatePasswordForServicePrincipalAsync(Guid objectId,
            PasswordCredentialPayload passCred)
        {
            string responseMessage;
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
                responseMessage = GraphPayloadFormatter.GetStringRepresentation(passCredResponse);
            }
            catch (Exception ex)
            {
                responseMessage = ex.Message;
            }
            return responseMessage;
        }
        
        public static async Task<string> DeleteServicePrincipalPasswordCredentialsAsync(Guid objectId,
           Guid keyId)
        {
            string responseMessage;
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
            return responseMessage;
        }
                
        public static async Task<string> AddOwnerToServicePrincipalAsync(Guid objectId, Guid ownerObjectId)
        {
            string responseMessage;
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
            return responseMessage;
        }


        public static async Task<string> DeleteOwnerFromServicePrincipalAsync(Guid objectId, Guid ownerObjectId)
        {
            string responseMessage;
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
            return responseMessage;
        }

        public async static Task<GraphServiceClient> GetGraphClientAsync()
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
    }
}
