using MicroCoreKit.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace MicroCoreKit.Services.HttpService
{
    public class HttpService : IHttpService
    {
        private readonly HttpClient _httpClient;
        private readonly HttpServiceOptions _options;

        public HttpService(IHttpClientFactory httpClientFactory, HttpServiceOptions options, string clientName = "DefaultHttpServiceClient")
        {
            _httpClient = httpClientFactory?.CreateClient(clientName) ?? throw new ArgumentNullException(nameof(httpClientFactory));
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        // Same implementation as before, just using injected _httpClient
        public async Task<T> GetAsync<T>(string url, Dictionary<string, string>? headers = null)
        {
            return await ExecuteRequestAsync<T>(HttpMethod.Get, url, null, headers);
        }

        public async Task<T> PostAsync<T>(string url, object? data = null, Dictionary<string, string>? headers = null)
        {
            return await ExecuteRequestAsync<T>(HttpMethod.Post, url, data, headers);
        }

        public async Task<T> PutAsync<T>(string url, object? data = null, Dictionary<string, string>? headers = null)
        {
            return await ExecuteRequestAsync<T>(HttpMethod.Put, url, data, headers);
        }

        public async Task<T> DeleteAsync<T>(string url, Dictionary<string, string>? headers = null)
        {
            return await ExecuteRequestAsync<T>(HttpMethod.Delete, url, null, headers);
        }

        private async Task<T> ExecuteRequestAsync<T>(
            HttpMethod method,
            string url,
            object? data = null,
            Dictionary<string, string>? headers = null)
        {
            if (string.IsNullOrWhiteSpace(url))
                throw new ArgumentException("URL cannot be null or empty", nameof(url));

            using var request = new HttpRequestMessage(method, url);

            if (_options.DefaultHeaders != null)
            {
                foreach (var header in _options.DefaultHeaders)
                {
                    request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
            }

            if (headers != null)
            {
                foreach (var header in headers)
                {
                    request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
            }

            if (data != null && (method == HttpMethod.Post || method == HttpMethod.Put))
            {
                string jsonContent = JsonSerializer.Serialize(data);
                request.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            }

            try
            {
                using var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();
                return string.IsNullOrEmpty(responseContent)
                    ? default!
                    : JsonSerializer.Deserialize<T>(responseContent)!;
            }
            catch (HttpRequestException ex)
            {
                throw;
            }
            catch (JsonException ex)
            {
                throw;
            }
        }
    }
}
