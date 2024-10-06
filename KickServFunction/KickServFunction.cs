using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Diagnostics;

namespace KickServFunction
{
    public static class KickServFunction
    {

        static HttpClient StaticClient = new HttpClient();
        public static readonly string KICKSERV_URL = "https://app.kickserv.com";

        [FunctionName("GetKickServJobType")]
        public static async Task<IActionResult> GetKickServJobFromJobNumber(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            string apiToken = req.Query["APIToken"];
            string accountSLUG = req.Query["SLUG"];
            string jobNumber = req.Query["jobNumber"];

            StaticClient.DefaultRequestHeaders.Add("accept", "application/xml");

            string url = $"{KICKSERV_URL}/{accountSLUG}/jobs/{jobNumber}.xml";
            string base64Auth = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($"{apiToken}:{apiToken}"));

            HttpRequestMessage requestMessage = new(HttpMethod.Get, url);
            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Basic", base64Auth);

            HttpResponseMessage response = await StaticClient.SendAsync(requestMessage);
            string content = await response.Content.ReadAsStringAsync();

            string typeName = content[(content.IndexOf("<name>") + 6)..content.IndexOf("</name>")];

            return new OkObjectResult(typeName.Trim());
        }
    }
}
