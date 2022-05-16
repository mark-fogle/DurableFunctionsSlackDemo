using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DurableFunctionsSlackDemo.Services
{
    public class SlackApprovalService : ISlackApprovalService
    {
        private readonly HttpClient _httpClient;
        private readonly SlackApprovalServiceOptions _options;

        public SlackApprovalService(HttpClient httpClient, SlackApprovalServiceOptions options)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public async Task<HttpResponseMessage> RequestApproval(string instanceId)
        {
            const string resourceName = "DurableFunctionsSlackDemo.Services.SlackTemplates.SlackApprovalTemplate.json";
            
            var keywords = new Dictionary<string, string>
            {
                { nameof(instanceId), instanceId }
            };
            
            var body = await ReadResourceFileTextAsync(resourceName, keywords);

            var response = await _httpClient.PostAsync(
                new Uri(_options.SlackWebhookUrl),
                new StringContent(body, Encoding.UTF8, "application/json"));
            return response;
        }

        public async Task<HttpResponseMessage> SendMessageAsync(string messageText)
        {
            var body = $@"{{
                            ""text"": ""{messageText}""
                         }}";

            var response = await _httpClient.PostAsync(
                new Uri(_options.SlackWebhookUrl),
                new StringContent(body, Encoding.UTF8, "application/json"));
            return response;
        }

        public async Task<HttpResponseMessage> SendApprovalResponseAsync(string responseUrl, string instanceId, string approvalValue, string userName)
        {
            const string resourceName = "DurableFunctionsSlackDemo.Services.SlackTemplates.SlackApprovalResponseTemplate.json";

            var keywords = new Dictionary<string, string>
            {
                { nameof(instanceId), instanceId },
                { nameof(userName), userName },
                { nameof(approvalValue), approvalValue }
            };

            var body = await ReadResourceFileTextAsync(resourceName, keywords);
            
            return await _httpClient.PostAsync(new Uri(responseUrl), new StringContent(body));
        }

        private static async Task<string> ReadResourceFileTextAsync(string resourceName, IReadOnlyDictionary<string,string> keywords = null)
        {
            //Load template from embedded resource
            var assembly = Assembly.GetExecutingAssembly();
            await using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
                return null;
            using var reader = new StreamReader(stream);
            var body = await reader.ReadToEndAsync();
            if (keywords != null)
            {
                foreach (var (key, value) in keywords)
                {
                    // Replace keywords in template with values from dictionary
                    body = body.Replace($"{{{{{key}}}}}", value);
                }
            }

            return body;
        }
    }
}
