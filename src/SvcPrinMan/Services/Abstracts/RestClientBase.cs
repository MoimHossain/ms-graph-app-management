

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace SvcPrinMan.Services.Abstracts
{
    public abstract class RestClientBase
    {
        private static readonly HttpClient http = new HttpClient();
        private readonly string pat;
        public readonly Uri API = new Uri("https://dev.azure.com");
        public readonly JsonSerializerOptions serializationOptions = new JsonSerializerOptions()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };

        public RestClientBase(string orgName, string pat)
        {
            this.OrgName = orgName;
            this.pat = pat;
            http.BaseAddress = API;
            var credentials =
            Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes(
                string.Format("{0}:{1}", "", this.pat)));
            http.DefaultRequestHeaders.Accept.Clear();
            http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);
        }

        public string OrgName { get; private set; }

        public async Task<TPayload> GetRestAsync<TPayload>(string requestPath)
        {
            var response = await http.GetAsync(requestPath);
            if (response.IsSuccessStatusCode)
            {
                return await ReadContentAsync<TPayload>(response.Content);
            }
            return default;
        }

        public async Task<TResponsePayload> PutRestAsync<TRequestPayload, TResponsePayload>(
           string requestPath, TRequestPayload payload)
        {
            var jsonString = JsonSerializer.Serialize(payload, serializationOptions);
            var jsonContent = new StringContent(jsonString, Encoding.UTF8, "application/json");
            var response = await http.PutAsync(requestPath, jsonContent);
            if (response.IsSuccessStatusCode)
            {
                return await ReadContentAsync<TResponsePayload>(response.Content);
            }
            return default;
        }

        public async Task<TPayload> ReadContentAsync<TPayload>(HttpContent content)
        {
            var contentString = await content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<TPayload>(contentString);
        }
    }
}
