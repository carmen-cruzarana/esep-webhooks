using System.Text;
using System.Net.Http;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

// This enables the Lambda function to automatically deserialize JSON strings
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace EsepWebhook
{
    public class Function
    {
        private static readonly HttpClient client = new HttpClient();

        public async Task<string> FunctionHandler(string input, ILambdaContext context)
        {
            context.Logger.LogInformation($"Raw input received: {input}");

            // Try to parse the input as JSON if it looks like JSON
            JObject json = null;
            if (!string.IsNullOrEmpty(input) && input.TrimStart().StartsWith("{"))
            {
                try
                {
                    json = JObject.Parse(input);
                }
                catch (JsonReaderException ex)
                {
                    context.Logger.LogWarning($"Failed to parse JSON: {ex.Message}");
                }
            }

            // Determine message text
            string messageText;
            string issueUrl = json?["issue"]?["html_url"]?.ToString();

            if (!string.IsNullOrEmpty(issueUrl))
            {
                messageText = $"Issue Created: {issueUrl}";
            }
            else
            {
                // Fallback if it's just a raw string or missing fields
                messageText = $"Received: {input}";
            }

            context.Logger.LogInformation($"Message to send: {messageText}");

            // Send to Slack if SLACK_URL environment variable is set
            string slackUrl = Environment.GetEnvironmentVariable("SLACK_URL");
            if (!string.IsNullOrEmpty(slackUrl))
            {
                var payload = new
                {
                    text = messageText
                };

                var content = new StringContent(
                    JsonConvert.SerializeObject(payload),
                    Encoding.UTF8,
                    "application/json"
                );

                try
                {
                    var response = await client.PostAsync(slackUrl, content);
                    var responseBody = await response.Content.ReadAsStringAsync();
                    context.Logger.LogInformation($"Slack response: {response.StatusCode} {responseBody}");
                }
                catch (Exception ex)
                {
                    context.Logger.LogError($"Error posting to Slack: {ex.Message}");
                }
            }
            else
            {
                context.Logger.LogWarning("SLACK_URL environment variable not set — skipping Slack post.");
            }

            return "Lambda executed successfully.";
        }
    }
}
