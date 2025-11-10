using System;
using System.Net.Http;
using System.Text;
using Amazon.Lambda.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace EsepWebhook;

public class Function
{
    private static readonly HttpClient client = new HttpClient();

    public object FunctionHandler(object input, ILambdaContext context)
    {
        try
        {
            string json = input?.ToString();
            context.Logger.LogLine($"Raw input: {json}");

            if (string.IsNullOrEmpty(json))
                return new { error = "No input received from API Gateway." };

            // Try to parse as JObject
            JObject root = JObject.Parse(json);

            // Detect if wrapped (i.e., input contains "body")
            string payloadJson = root["body"]?.ToString() ?? json;

            context.Logger.LogLine($"Payload JSON: {payloadJson}");

            dynamic data = JsonConvert.DeserializeObject(payloadJson);

            // Optional safety checks
            if (data?.issue == null)
            {
                context.Logger.LogLine("⚠️ data.issue is null — payload may not be a GitHub issue event.");
                return new { error = "Invalid or unexpected payload format." };
            }

            string issueUrl = data.issue.html_url;
            context.Logger.LogLine($"Extracted issue URL: {issueUrl}");

            // Prepare Slack message
            string payload = JsonConvert.SerializeObject(new
            {
                text = $"Issue Created: {issueUrl}"
            });

            string slackUrl = Environment.GetEnvironmentVariable("SLACK_URL");
            if (string.IsNullOrEmpty(slackUrl))
            {
                return new { error = "SLACK_URL environment variable is not set." };
            }

            // Send message to Slack
            var content = new StringContent(payload, Encoding.UTF8, "application/json");
            var response = client.PostAsync(slackUrl, content).Result;
            string responseBody = response.Content.ReadAsStringAsync().Result;

            return new
            {
                statusCode = (int)response.StatusCode,
                body = responseBody
            };
        }
        catch (Exception ex)
        {
            context.Logger.LogError(ex.ToString());
            return new { error = $"Error processing webhook: {ex.Message}" };
        }
    }
}
