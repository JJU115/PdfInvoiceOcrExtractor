﻿using Newtonsoft.Json.Converters;
using Newtonsoft.Json;
using System.IO;
using System.Net.Http;
using System.Windows;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace WpfOcrInvoiceExtractor
{
    internal class KickServAuth
    {
        public string KickservToken { get; set; } = "";
        public string KickservAccount { get; set; } = "";
    }

    class KickServUtility
    {
        public static readonly string FUNCTION_URL = "https://kickservclient.azurewebsites.net/api/GetKickServJobType?code=vsRrEr5QLHRdbUdeBuxjvFU4I7j-p7TLgBoW5YVD9dQEAzFu_lDFTg%3D%3D";
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
                string serialized = System.Text.Json.JsonSerializer.Serialize(new KickServAuth(), new JsonSerializerOptions()
                {
                    WriteIndented = true,
                });

                // Write the string to the path.
                File.WriteAllText(".\\KickServ.json", serialized);
                MessageBox.Show("Add your KickServ API token and account SLUG to the KickServ.json file");
            }
        }

        //For now one job type at a time, can batch this up
        public async static Task<string> GetJobType(string jobNumber)
        {
            if (KickServAuth == null) GetAuthDetails();
            StaticClient ??= new HttpClient();
            string url = $"{FUNCTION_URL}&APIToken={KickServAuth!.KickservToken}&SLUG={KickServAuth.KickservAccount}&jobNumber={jobNumber}";
            HttpRequestMessage requestMessage = new(HttpMethod.Get, url);

            HttpResponseMessage response = await StaticClient.SendAsync(requestMessage);
            string jobType = await response.Content.ReadAsStringAsync();
            return jobType.Trim().ToUpper();
        }
    }
}
