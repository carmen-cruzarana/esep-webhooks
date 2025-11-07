using System.Text;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Newtonsoft.Json;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace EsepWebhook;

public class Function
{
    public APIGatewayProxyResponse FunctionHandler(APIGatewayProxyRequest input, ILambdaContext context)
    {
        dynamic deserialize = JsonConvert.DeserializeObject(input.Body);

        string payload = JsonConvert.SerializeObject(new { text = $"Issue Created: {deserialize.issue.url}" });

        var client = new HttpClient();

        var environmentVariable = Environment.GetEnvironmentVariable("SLACK_URL");
        var content = new StringContent(payload, Encoding.UTF8, "application/json");

        var response = client.PostAsync(
            environmentVariable,
            content
        ).Result;

        var proxyResponse = new APIGatewayProxyResponse
        {
            StatusCode = 200,
            Body = response.Content.ReadAsStringAsync().Result,
            Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
        };

        return proxyResponse;
    }
}