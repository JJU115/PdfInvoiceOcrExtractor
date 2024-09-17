using Intuit.Ipp.OAuth2PlatformClient;
using System.IO;
using System.Text.Json;
using System.Collections.Specialized;
using System.Web;
using System.Net.Http;
using System.IdentityModel.Tokens.Jwt;
using System.Diagnostics;
using System.Windows;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json;
using Tesseract;
using System.Drawing;
using System.Windows.Media.Imaging;
using Intuit.Ipp.Data;
using System.Security.Policy;
using Intuit.Ipp.Core.Configuration;
using System.Net.Http.Json;
using Newtonsoft.Json.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Controls;
using System;

namespace WpfOcrInvoiceExtractor
{

    class QBOUtility
    {
        readonly static string BASE_URL = "https://sandbox-quickbooks.api.intuit.com";
        public static QboAuthTokens? Tokens { get; set; } = null;
        public static OAuth2Client? Client { get; set; } = null;
        private static HttpClient? StaticClient = null;

        public static TesseractEngine engine = new TesseractEngine("./tessdata", "eng");

        public static void Initialize(string path = ".\\Tokens.json")
        {
            // Loading the tokens and client once (on sign-in/start up)
            // and saving them in static properties saves us from
            // deserializing again when we want to read or write the data.
            if (Tokens == null) ReadTokensFromJson();

            // In the case that the data failed to deserialize, the ClientId
            // and ClientSecret will be null, we need to make sure that's
            // handled correctly.
            if (!string.IsNullOrEmpty(Tokens.ClientId) && !string.IsNullOrEmpty(Tokens.ClientSecret))
            {
                Client = new(Tokens.ClientId, Tokens.ClientSecret, Tokens.RedirectUrl, Tokens.Environment);
            }
            else
            {
                throw new InvalidDataException(
                    "The ClientId or ClientSecret was null or empty.\n" +
                    "Make sure that 'Tokens.json' is setup with your credentials."
                );
            }
        }

        public static async Task<bool> CheckTokens()
        {
            if (Tokens == null) ReadTokensFromJson();

            bool accessValid = Tokens != null && Tokens.AccessTokenExpiresIn > DateTime.Now;
            bool refreshValid = Tokens != null && Tokens.RefreshTokenExpiresIn > DateTime.Now;

            QBOAuthWindow authWindow = new();
            Task<bool> authFinished = new(() =>
            {
                if (CheckQueryParamsAndSet(authWindow.webviewSourceQuery) == true && Tokens != null)
                {
                    WriteTokensAsJson(Tokens);
                    return true;
                }
                else
                {
                    MessageBox.Show("Quickbooks Online failed to authenticate.");
                    return false;
                }
            });

            if (!accessValid && !refreshValid)
            {
                MessageBox.Show("You must authenticate to QuickBooks Online", "Authentication needed", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK);
                authWindow.Closed += (s, a) => authFinished.Start();
                authWindow.Show();
                await authFinished;
                return authFinished.Result;
            }
            else if (!accessValid)
            {
                MessageBox.Show("You must authenticate to QuickBooks Online", "Authentication needed", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK);
                Client ??= new(Tokens.ClientId, Tokens.ClientSecret, Tokens.RedirectUrl, Tokens.Environment);
                TokenResponse response = await Client.RefreshTokenAsync(Tokens.RefreshToken);
                if (!response.IsError)
                {
                    Tokens.AccessToken = response.AccessToken;
                    Tokens.RefreshToken = response.RefreshToken;
                    return true;
                }
                return false;
            }
            else
            {
                return true;
            }
        }

        public static Bill ResolveOnImageRegions(List<ImageRegion> imageRegions)
        {
            Bill bill = new()
            {
                Line = new Line[1]
            };
            bill.Line[0].DetailType = LineDetailTypeEnum.AccountBasedExpenseLineDetail;
            AccountBasedExpenseLineDetail lineDetail = new AccountBasedExpenseLineDetail();

            foreach (ImageRegion region in imageRegions)
            {
                JpegBitmapEncoder encoder = new JpegBitmapEncoder();
                MemoryStream memoryStream = new MemoryStream();

                encoder.Frames.Add(BitmapFrame.Create(region.Image));
                encoder.Save(memoryStream);
                memoryStream.Position = 0;

                Pix pix = PixConverter.ToPix(new Bitmap(memoryStream));
                Tesseract.Page page = engine.Process(pix);
                Debug.WriteLine($"{page.GetText()}");

                switch (region.BillTopic)
                {
                    case BillTopic.Amount:
                        bill.Line[0].Amount = Convert.ToDecimal(page.GetText()); //Remove all non numeric characters
                        break;
                    case BillTopic.BillDate:
                        bill.TxnDate = DateTime.Parse(page.GetText()); //Different possible date formats...
                        break;
                    case BillTopic.BillNumber:
                        bill.DocNumber = page.GetText(); //Remove all non numeric characters
                        break;
                    case BillTopic.Category:
                        lineDetail.AccountRef = new ReferenceType();
                        break;
                    case BillTopic.Class:
                        lineDetail.ClassRef = new ReferenceType();
                        break;
                    case BillTopic.Description:
                        bill.Line[0].Description = "Default description";
                        break;
                    case BillTopic.SalesTax:
                        lineDetail.TaxCodeRef = new ReferenceType();
                        break;
                    case BillTopic.Vendor:
                        bill.VendorRef = new ReferenceType();
                        break;
                }

                page.Dispose();
            }
            bill.Line[0].AnyIntuitObject = lineDetail;
            return bill;
        }

        public static async Task<bool> CreateNewBillToQbo(List<ImageRegion> imageRegions, Vendor vendor)
        {
            //Check the tokens, if authentication failed for any reason, end and return false
            if (!await CheckTokens()) return false;
            //Otherwise we are now fully authorized to make requests

            //Prepare the request
            if (StaticClient == null) StaticClient = new HttpClient();

            //Set the body and headers
            string url = $"{BASE_URL}/v3/company/{Tokens!.RealmId}/bill?minorversion=73";
            StaticClient.SetBearerToken(Tokens.AccessToken);

            /* Sample object, Have to query for all ref values, some can be hardcoded lists
             * {
                  "Line": [
                    {
                        "Description": "Lumber", 
                        "DetailType": "AccountBasedExpenseLineDetail", 
                        "ProjectRef": {
                          "value": "39298034"
                        }, 
                        "Amount": 103.55, 
                        "Id": "1", 
                        "AccountBasedExpenseLineDetail": {
                          "TaxCodeRef": {"value": "TAX"}, 
                          "AccountRef": {"name": "Job Expenses:Job Materials:Decks and Patios", "value": "64"}, 
                          "CustomerRef": {"name": "Travis Waldron", "value": "26"},
                          "ClassRef": {"name": "className, "value": 0}
                        }
                      }
                  ], 
                  "VendorRef": {
                    "value": "56"
                  }
                }
             */

            Bill billToSend = ResolveOnImageRegions(imageRegions);

            var billObject = new
            {
                Line = new[] {
                new {DetailType = "AccountBasedExpenseLineDetail", Amount = 768.33, Id = "1", AccountBasedExpenseLineDetail = new {AccountRef = new {value = "7"}}}},
                VendorRef = new { value = vendor.Id, name = vendor.DisplayName }
            };


            var billObjectJson = JsonConvert.SerializeObject(billObject);

            HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Post, url);
            requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("bearer", Tokens.AccessToken);
            requestMessage.Content = new StringContent(billObjectJson.ToString());
            requestMessage.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

            string url2 = $"{BASE_URL}/v3/company/9341452801840587/customer/2?minorversion=73";
            HttpResponseMessage comp = await StaticClient.GetAsync(url2);
            Debug.WriteLine($"{await comp.Content.ReadAsStringAsync()}");

            //Receive and parse the response
            HttpResponseMessage response = await StaticClient.SendAsync(requestMessage);
            Debug.WriteLine($"{await response.Content.ReadAsStringAsync()}");
            return true;
        }

        public static string GetAuthorizationURL(params OidcScopes[] scopes)
        {
            // Initialize the OAuth2Client and
            // AuthTokens if either is null.
            if (Client == null || Tokens == null)
            {
                Initialize();
            }

            return Client.GetAuthorizationURL(scopes.ToList());
        }


        public static bool CheckQueryParamsAndSet(string queryString, bool suppressErrors = true)
        {
            // Parse the query string into a
            // NameValueCollection for easy access
            // to each parameter.
            NameValueCollection query = HttpUtility.ParseQueryString(queryString);

            // Make sure the required query
            // parameters exist.
            if (query["code"] != null && query["realmId"] != null)
            {

                // Use the OAuth2Client to get a new
                // access token from the QBO servers.
                TokenResponse response = Client!.GetBearerTokenAsync(query["code"]).Result;

                // Set the token values with the client
                // response and query parameters.
                Tokens!.AccessToken = response.AccessToken;
                Tokens.AccessTokenExpiresIn = DateTime.Now.AddSeconds(response.AccessTokenExpiresIn);
                Tokens.RefreshToken = response.RefreshToken;
                Tokens.RefreshTokenExpiresIn = DateTime.Now.AddSeconds(response.RefreshTokenExpiresIn);
                Tokens.RealmId = query["realmId"];

                // Return true. The Tokens have
                // been set as expected.
                return true;
            }
            else
            {

                // Is the caller chooses to suppress
                // errors return false instead
                // of throwing an exception.
                if (suppressErrors)
                {
                    return false;
                }
                else
                {
                    throw new InvalidDataException(
                        $"The 'code' or 'realmId' was not present in the query parameters '{query}'."
                    );
                }
            }
        }

        public static void ReadTokensFromJson()
        {
            FileInfo fi = new FileInfo(".\\Tokens.json");
            if (fi.Exists)
            {
                Tokens = JsonConvert.DeserializeObject<QboAuthTokens>(File.ReadAllText(".\\Tokens.json"), new JsonSerializerSettings
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
                File.Create(".\\Tokens.json");
                Tokens = new();
            }
        }

        /// <summary>
        /// Serializes the static tokens instance (Local.Tokens) and writes the serialized string to the <paramref name="path"/>.
        /// </summary>
        /// <param name="path">Absolute or relative path to the target JSON file to be written.</param>
        public static void WriteTokensAsJson(QboAuthTokens authTokens, string path = ".\\Tokens.json")
        {
            // Serialize the passed object
            // to a JSON formatted string.
            string serialized = System.Text.Json.JsonSerializer.Serialize(authTokens, new JsonSerializerOptions()
            {
                WriteIndented = true,
            });

            // Create the parent directory
            // to avoid possible conflicts.
            Directory.CreateDirectory(new FileInfo(path).Directory.FullName);

            // Write the string to the path.
            File.WriteAllText(path, serialized);
        }


        public static long GetTokenExpirationTime(string token)
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtSecurityToken = handler.ReadJwtToken(token);
            var tokenExp = jwtSecurityToken.Claims.First(claim => claim.Type.Equals("exp")).Value;
            var ticks = long.Parse(tokenExp);
            return ticks;
        }

        public static bool CheckTokenIsValid(string token)
        {
            try
            {
                var tokenTicks = GetTokenExpirationTime(token);
                var tokenDate = DateTimeOffset.FromUnixTimeSeconds(tokenTicks).UtcDateTime;

                var now = DateTime.Now.ToUniversalTime();

                var valid = tokenDate >= now;

                return valid;
            } catch { return false; }

        }

        static List<Vendor> CachedVendorList = new List<Vendor>();

        public static async Task<List<Vendor>> GetVendorList() {
            if (StaticClient == null) StaticClient = new HttpClient();

            if (!await CheckTokens()) return new List<Vendor>();
            if (CachedVendorList.Count > 0) return CachedVendorList;

            string url = $"{BASE_URL}/v3/company/{Tokens!.RealmId}/query?query=select DisplayName, Id from vendor";          
            HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Get, url);
            requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("bearer", Tokens.AccessToken);
            StaticClient.DefaultRequestHeaders.Add("Accept", "application/json");
            HttpResponseMessage response = await StaticClient.SendAsync(requestMessage);
            string queryResult = await response.Content.ReadAsStringAsync();
            JObject QueryResponse = JObject.Parse(queryResult);
            JArray vendors = (JArray)QueryResponse["QueryResponse"]["Vendor"];
            CachedVendorList = vendors.Select(vendor => new Vendor() { DisplayName = (string)vendor["DisplayName"], Id = (string)vendor["Id"] }).ToList();
            return CachedVendorList;
        }
    }
}