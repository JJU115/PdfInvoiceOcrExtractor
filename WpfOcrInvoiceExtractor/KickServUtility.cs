using Newtonsoft.Json.Converters;
using Newtonsoft.Json;
using System.IO;
using System.Net.Http;
using System.Windows;
using System.Diagnostics;
using System.Text.Json;
using System.Windows.Shapes;
using System.Net;

namespace WpfOcrInvoiceExtractor
{
    internal class KickServAuth
    {
        public string? KickservToken { get; set; }
        public string? KickservAccount { get; set; }
    }

    class KickServUtility
    {
        public static readonly string KICKSERV_URL = "https://app.kickserv.com";
        public static KickServAuth? KickServAuth = null;
        private static HttpClient? StaticClient = null;

        private static void GetAuthDetails()
        {
            FileInfo fi = new FileInfo(".\\KickServ.json");
            if (fi.Exists)
            {
                KickServAuth = JsonConvert.DeserializeObject<KickServAuth>(File.ReadAllText(".\\KickServ.json"), new JsonSerializerSettings
                {
                    Error = (object sender, Newtonsoft.Json.Serialization.ErrorEventArgs args) =>
                    {
                        args.ErrorContext.Handled = true;
                    },
                    Converters = { new IsoDateTimeConverter() }
                }
                    );
            }
            else
            {
                File.Create(".\\KickServ.json");
                string serialized = System.Text.Json.JsonSerializer.Serialize(new KickServAuth(), new JsonSerializerOptions()
                {
                    WriteIndented = true,
                });

                // Write the string to the path.
                File.WriteAllText(".\\KickServ.json", serialized);
                MessageBox.Show("Add your KickServ API token and account SLUG to the KickServ.json file");
            }
        }


        public async static void GetJobs()
        {
            if (KickServAuth == null) GetAuthDetails();
            StaticClient ??= new HttpClient();
            StaticClient.DefaultRequestHeaders.Add("accept", "application/xml");


            using (var client = new HttpClient())
            {
                string url = $"{KICKSERV_URL}/{KickServAuth!.KickservAccount}/jobs.xml";
                HttpRequestMessage requestMessage = new(HttpMethod.Get, url);
                //requestMessage.Version = HttpVersion.Version20;
                requestMessage.Headers.Authorization = new BasicAuthenticationHeaderValue(KickServAuth.KickservToken!, KickServAuth.KickservToken!);

                HttpResponseMessage response = await client.SendAsync(requestMessage);
                Debug.WriteLine($"{await response.Content.ReadAsStringAsync()}");
            }

/*
            string url = $"{KICKSERV_URL}/{KickServAuth!.KickservAccount}/jobs.xml";
            HttpRequestMessage requestMessage = new(HttpMethod.Get, url);
            requestMessage.Version = HttpVersion.Version20;
            requestMessage.Headers.Authorization = new BasicAuthenticationHeaderValue(KickServAuth.KickservToken!, KickServAuth.KickservToken!);

            HttpResponseMessage response = await StaticClient.SendAsync(requestMessage);
            Debug.WriteLine($"{await response.Content.ReadAsStringAsync()}");*/
        }
    }
}
