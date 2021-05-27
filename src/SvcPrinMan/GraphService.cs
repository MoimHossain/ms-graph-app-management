

using Azure.Identity;
using Microsoft.Graph;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace SvcPrinMan
{
    public class MSGraph
    {   
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

