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

        [FunctionName("GetAllKickServJobs")]
        public static async Task<IActionResult> GetAllKickServJobs(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("Getting all jobs...");

            string token = Environment.GetEnvironmentVariable("AuthToken");
            string SLUG = Environment.GetEnvironmentVariable("KickServSLUG");

            StaticClient.DefaultRequestHeaders.Add("accept", "application/xml");

            string url = $"{KICKSERV_URL}/{SLUG}/jobs.xml";
            string base64Auth = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($"{token}:{token}"));

            HttpRequestMessage requestMessage = new(HttpMethod.Get, url);
            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Basic", base64Auth);

            HttpResponseMessage response = await StaticClient.SendAsync(requestMessage);
            String content = await response.Content.ReadAsStringAsync();
            Debug.WriteLine($"{content}");

            return new OkObjectResult(content);
        }


        [FunctionName("GetKickServJobFromJobNumber")]
        public static async Task<IActionResult> GetKickServJobFromJobNumber(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            string jobNumber = req.Query["jobNumber"].ToString();

            string token = Environment.GetEnvironmentVariable("AuthToken");
            string SLUG = Environment.GetEnvironmentVariable("KickServSLUG");

            StaticClient.DefaultRequestHeaders.Add("accept", "application/xml");

            string url = $"{KICKSERV_URL}/{SLUG}/jobs/{jobNumber}.xml";
            string base64Auth = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($"{token}:{token}"));

            HttpRequestMessage requestMessage = new(HttpMethod.Get, url);
            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Basic", base64Auth);

            HttpResponseMessage response = await StaticClient.SendAsync(requestMessage);
            String content = await response.Content.ReadAsStringAsync();
            Debug.WriteLine($"{content}");

            return new OkObjectResult(content);
        }
    }
}
