

using SvcPrinMan.Payloads.AzureDevOps;
using SvcPrinMan.Services.Abstracts;
using System;
using System.Threading.Tasks;

namespace SvcPrinMan.Services
{
    public class AzDoService : RestClientBase
    {
        public AzDoService(string orgName, string pat)
                    : base(orgName, pat)
        {

        }

        public async Task<VstsProjectCollection> ListProjectsAsync()
        {
            return await base.GetRestAsync<VstsProjectCollection>($"{OrgName}/_apis/projects?stateFilter=All&api-version=1.0");
        }

        public async Task<VstsServiceEndpointCollection> ListServiceEndpointsAsync(Guid projectId)
        {
            var requestPath = $"{OrgName}/{projectId}/_apis/serviceendpoint/endpoints?api-version=6.1-preview.4";
            return await GetRestAsync<VstsServiceEndpointCollection>(requestPath);
        }

        public async Task<VstsServiceEndpoint> GetServiceEndpointsAsync(Guid projectId, Guid endpointId)
        {
            var requestPath = $"{OrgName}/{projectId}/_apis/serviceendpoint/endpoints/{endpointId}?api-version=6.0-preview.4";
            return await GetRestAsync<VstsServiceEndpoint>(requestPath);
        }

        public async Task<VstsServiceEndpoint> UpdateServiceEndpointsAsync(Guid projectId, Guid endpointId, VstsServiceEndpoint endpoint)
        {
            var requestPath = $"{OrgName}/{projectId}/_apis/serviceendpoint/endpoints/{endpointId}?api-version=6.0-preview.4";
            return await PutRestAsync<VstsServiceEndpoint, VstsServiceEndpoint>(requestPath, endpoint);
        }
    }
}
