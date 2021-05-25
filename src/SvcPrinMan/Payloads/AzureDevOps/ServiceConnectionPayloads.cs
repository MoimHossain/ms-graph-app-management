using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace SvcPrinMan.Payloads.AzureDevOps
{
    public partial class VstsServiceEndpointCollection
    {
        [JsonPropertyName("count")]
        public long Count { get; set; }

        [JsonPropertyName("value")]
        public VstsServiceEndpoint[] Value { get; set; }
    }

    public partial class VstsServiceEndpoint
    {
        [JsonPropertyName("data")]
        public VstsData Data { get; set; }

        [JsonPropertyName("id")]
        public Guid Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("url")]
        public Uri Url { get; set; }

        [JsonPropertyName("createdBy")]
        public VstsCreatedBy CreatedBy { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("authorization")]
        public VstsAuthorization Authorization { get; set; }

        [JsonPropertyName("isShared")]
        public bool IsShared { get; set; }

        [JsonPropertyName("isReady")]
        public bool IsReady { get; set; }

        [JsonPropertyName("owner")]
        public string Owner { get; set; }

        [JsonPropertyName("serviceEndpointProjectReferences")]
        public ServiceEndpointProjectReference[] ServiceEndpointProjectReferences { get; set; }
    }

    public partial class VstsAuthorization
    {
        [JsonPropertyName("parameters")]
        public VstsParameters Parameters { get; set; }

        [JsonPropertyName("scheme")]
        public string Scheme { get; set; }
    }

    public partial class VstsParameters
    {
        [JsonPropertyName("serviceprincipalid")]
        public Guid Serviceprincipalid { get; set; }

        [JsonPropertyName("authenticationType")]
        public string AuthenticationType { get; set; }

        [JsonPropertyName("tenantid")]
        public Guid Tenantid { get; set; }

        [JsonPropertyName("serviceprincipalkey")]
        public object Serviceprincipalkey { get; set; }
    }

    public partial class VstsCreatedBy
    {
        [JsonPropertyName("displayName")]
        public string DisplayName { get; set; }

        [JsonPropertyName("url")]
        public Uri Url { get; set; }

        [JsonPropertyName("_links")]
        public VstsLinks Links { get; set; }

        [JsonPropertyName("id")]
        public Guid Id { get; set; }

        [JsonPropertyName("uniqueName")]
        public string UniqueName { get; set; }

        [JsonPropertyName("imageUrl")]
        public Uri ImageUrl { get; set; }

        [JsonPropertyName("descriptor")]
        public string Descriptor { get; set; }
    }

    public partial class VstsLinks
    {
        [JsonPropertyName("avatar")]
        public VstsAvatar Avatar { get; set; }
    }

    public partial class VstsAvatar
    {
        [JsonPropertyName("href")]
        public Uri Href { get; set; }
    }

    public partial class VstsData
    {
        [JsonPropertyName("environment")]
        public string Environment { get; set; }

        [JsonPropertyName("scopeLevel")]
        public string ScopeLevel { get; set; }

        [JsonPropertyName("subscriptionId")]
        public Guid SubscriptionId { get; set; }

        [JsonPropertyName("subscriptionName")]
        public string SubscriptionName { get; set; }

        [JsonPropertyName("creationMode")]
        public string CreationMode { get; set; }
    }

    public partial class ServiceEndpointProjectReference
    {
        [JsonPropertyName("projectReference")]
        public VstsProjectReference ProjectReference { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }
    }

    public partial class VstsProjectReference
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }
    }
    public partial class VstsProject
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }
    }

    public partial class VstsProjectCollection
    {
        [JsonPropertyName("count")]
        public long Count { get; set; }

        [JsonPropertyName("value")]
        public VstsProject[] Value { get; set; }
    }
}
