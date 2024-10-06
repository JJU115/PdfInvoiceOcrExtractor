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
using Intuit.Ipp.Data;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;

namespace WpfOcrInvoiceExtractor
{
    partial class QBOUtility
    {
        readonly static string BASE_URL = "https://sandbox-quickbooks.api.intuit.com";
        public static QboAuthTokens? Tokens { get; set; } = null;
        public static OAuth2Client? Client { get; set; } = null;
        private static HttpClient? StaticClient = null;

        public static TesseractEngine engine = new("./tessdata", "eng");

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
                Client ??= new(Tokens.ClientId, Tokens.ClientSecret, Tokens.RedirectUrl, Tokens.Environment);
                TokenResponse response = await Client.RefreshTokenAsync(Tokens.RefreshToken);
                
                if (!response.IsError)
                {
                    Tokens.AccessToken = response.AccessToken;
                    Tokens.RefreshToken = response.RefreshToken;
                    Tokens.AccessTokenExpiresIn = DateTime.Now.AddSeconds(response.AccessTokenExpiresIn);
                    Tokens.RefreshTokenExpiresIn = DateTime.Now.AddSeconds(response.RefreshTokenExpiresIn);
                    WriteTokensAsJson(Tokens);
                    return true;
                }
                return false;
            }
            else
            {
                return true;
            }
        }

        public static (Bill, InvoiceItemsTable) ResolveOnImageRegions(Bitmap invoiceImage, List<ImageRegion> imageRegions)
        {
            Bill bill = new() { Line = [new Line()] }; 
            AccountBasedExpenseLineDetail lineDetail = new();
            
            foreach (ImageRegion region in imageRegions)
            {

                Tesseract.Page page = engine.Process(invoiceImage, 
                    new Tesseract.Rect(region.SourceRegion.X, region.SourceRegion.Y, region.SourceRegion.Width, region.SourceRegion.Height));
                
                switch (region.BillTopic)
                {
                    case BillTopic.Amount:
                        bill.Line[0].Amount = Convert.ToDecimal(page.GetText().Trim().Replace(",", "")); //Remove all non numeric characters
                        break;
                    case BillTopic.BillDate:
                        bill.TxnDate = DateTime.Parse(page.GetText().Trim()); //Different possible date formats...
                        break;
                    case BillTopic.BillNumber:
                        bill.DocNumber = page.GetText().Trim(); //Remove all non numeric characters
                        break;

                }

                page.Dispose();
            }

            ImageRegion tableRegion = imageRegions.Find(ir => ir.BillTopic == BillTopic.ItemsTable)!;

            Tesseract.Page tablePage = engine.Process(invoiceImage,
                    new Tesseract.Rect(tableRegion.SourceRegion.X, tableRegion.SourceRegion.Y, tableRegion.SourceRegion.Width, tableRegion.SourceRegion.Height), 
                    PageSegMode.SingleBlock);

            InvoiceItemsTable itemTable = ProcessItemsTable(tablePage, (double)bill.Line[0].Amount);
            bill.Line[0].DetailType = LineDetailTypeEnum.AccountBasedExpenseLineDetail;
            bill.Line[0].Description = "Default description";
            bill.Line[0].AnyIntuitObject = lineDetail;
            return (bill, itemTable);
        }


        public static InvoiceItemsTable ProcessItemsTable(Tesseract.Page itemTablePage, double fullTotal)
        {
            //How to get text
            string tableOcr = itemTablePage.GetText().ToUpper();

            //Is there GST or PST found in the description column?
            InvoiceItemsTable table = new(tableOcr.Contains("BC-PST TAX"), tableOcr.Contains("GST LIABILITY"), fullTotal);
            string[] splitLines = tableOcr.Split('\n').Where(l => !String.IsNullOrEmpty(l)).ToArray();

            //Add all amounts in the amount column
            foreach (string line in splitLines)
            {
                //Get the last value in the line and try to isolate the number portion of it
                int lastWS = line.LastIndexOfAny([' ', '\t']);
                int lineEnd = line.LastIndexOfAny(['\r', '|']);
                lineEnd = lineEnd == -1 ? line.Length - 1 : lineEnd;
                string lastValue = line.Substring(lastWS + 1, lineEnd - lastWS).Replace(" ", "").Trim();

                //If this is not a numeric value, skip
                MatchCollection matches = Regex.Matches(lastValue, "[^0-9.]");
                if (matches.Count > 0) continue;

                //Pull GST and PST amounts if they exist in the amount column, GST=5%, PST=7%
                if (table.IncludesGST && line.Contains("GST LIABILITY")) table.SourceGSTAmount = Double.Parse(lastValue);
                else if (table.IncludesPST && line.Contains("BC-PST TAX")) table.SourcePSTAmount = Double.Parse(lastValue);
                else table.AddItem(Double.Parse(lastValue));
            }
            return table;
        }


        public static async Task<bool> CreateNewBillToQbo(Bitmap invoiceImage, InvoiceTemplate invoiceTemplate)
        {
            //Check the tokens, if authentication failed for any reason, end and return false
            if (!await CheckTokens()) return false;
            //Otherwise we are now fully authorized to make requests

            //Prepare the request
            StaticClient ??= new HttpClient();

            //Set the body and headers
            string url = $"{BASE_URL}/v3/company/{Tokens!.RealmId}/bill?minorversion=73";
            StaticClient.SetBearerToken(Tokens.AccessToken);

            (Bill billToSend, InvoiceItemsTable itemsTable) = ResolveOnImageRegions(invoiceImage, invoiceTemplate.ImageRegions);
            itemsTable.PreTaxSubtotal = Math.Round(itemsTable.PreTaxSubtotal, 2);

            //Get the Work order number or PO number
            ImageRegion WORegion = invoiceTemplate.ImageRegions.Find(ir => ir.BillTopic == BillTopic.WONumber)!;
            Page page = engine.Process(invoiceImage,
                    new Tesseract.Rect(WORegion.SourceRegion.X, WORegion.SourceRegion.Y, WORegion.SourceRegion.Width, WORegion.SourceRegion.Height));
            string jobType = await KickServUtility.GetJobType(page.GetText().Trim());

            //Highly customized to Wesco right now
            string billAccount = jobType == "EV-CHARGERS" ? "91" : "92";
            string billClass = jobType == "SERVICE" ? "533420" :"533421";

            var billObject = new
            {
                billToSend.TxnDate,
                billToSend.DocNumber,
                Line = new[] {
                new {
                    AccountRef = new {value = 64}, //Change
                    billToSend.Line[0].Description, 
                    DetailType = "AccountBasedExpenseLineDetail", 
                    Amount = billToSend.TotalAmt, 
                    AccountBasedExpenseLineDetail = new {
                        AccountRef = new {value = billAccount},  
                        ClassRef = new {value = billClass}, 
                        TaxCodeRef = new {value = 1}, //Probably correct, double check
                        TaxAmount = Math.Round((double)billToSend.TotalAmt - itemsTable.PreTaxSubtotal, 2), //Very likely wrong, need to see 
                    },
                }},
                TxnTaxDetail = new { 
                    //To fill out
                },
                VendorRef = new { value = invoiceTemplate.Vendor.Id, name = invoiceTemplate.Vendor.DisplayName },
            };
            
            return false;
            var billObjectJson = JsonConvert.SerializeObject(billObject);

            HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Post, url);
            requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("bearer", Tokens.AccessToken);
            requestMessage.Content = new StringContent(billObjectJson.ToString());
            requestMessage.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

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

            return Client.GetAuthorizationURL([.. scopes]);
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
            FileInfo fi = new(".\\Tokens.json");
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


        //All the following can be grouped into one method with generics and reflection

        static List<Vendor> CachedVendorList = [];

        public static async Task<List<Vendor>> GetVendorList() {
            StaticClient ??= new HttpClient();

            if (!await CheckTokens()) return [];
            if (CachedVendorList.Count > 0) return CachedVendorList;

            string url = $"{BASE_URL}/v3/company/{Tokens!.RealmId}/query?query=select DisplayName, Id from vendor";      
            
            HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Get, url);
            requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("bearer", Tokens.AccessToken);

            if (!StaticClient.DefaultRequestHeaders.Any(rh => rh.Value.Contains("application/json") && rh.Key == "Accept"))
                StaticClient.DefaultRequestHeaders.Add("Accept", "application/json");

            HttpResponseMessage response = await StaticClient.SendAsync(requestMessage);

            string queryResult = await response.Content.ReadAsStringAsync();
            JObject QueryResponse = JObject.Parse(queryResult);
            JArray vendors = (JArray)QueryResponse["QueryResponse"]["Vendor"];

            CachedVendorList = vendors.Select(vendor => new Vendor() { DisplayName = (string)vendor["DisplayName"], Id = (string)vendor["Id"] }).ToList();
            return CachedVendorList;
        }

        static List<Account> CachedAccountList = [];

        public async static Task<bool> PopulateAccountsList()
        {
            StaticClient ??= new HttpClient();

            if (!await CheckTokens()) return false;
            if (CachedAccountList.Count > 0) return false;

            string url = $"{BASE_URL}/v3/company/{Tokens!.RealmId}/query?query=select Name, Id from Account";

            HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Get, url);
            requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("bearer", Tokens.AccessToken);

            if (!StaticClient.DefaultRequestHeaders.Any(rh => rh.Value.Contains("application/json") && rh.Key == "Accept"))
                StaticClient.DefaultRequestHeaders.Add("Accept", "application/json");

            HttpResponseMessage response = await StaticClient.SendAsync(requestMessage);

            JObject QueryResponse = JObject.Parse(await response.Content.ReadAsStringAsync());
            JArray accounts = (JArray)QueryResponse["QueryResponse"]["Account"];

            CachedAccountList = accounts.Select(QboAccount => new Class() { Name = (string)QboAccount["Name"], Id = (string)QboAccount["Id"] }).ToList();
            return true;
        }


        static List<Class> CachedClassList = [];

        public async static Task<bool> PopulateQBOClassList()
        {
            StaticClient ??= new HttpClient();

            if (!await CheckTokens()) return false;
            if (CachedClassList.Count > 0) return false;

            string url = $"{BASE_URL}/v3/company/{Tokens!.RealmId}/query?query=select Name, Id from Class";

            HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Get, url);
            requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("bearer", Tokens.AccessToken);

            if (!StaticClient.DefaultRequestHeaders.Any(rh => rh.Value.Contains("application/json") && rh.Key == "Accept")) 
                StaticClient.DefaultRequestHeaders.Add("Accept", "application/json");

            HttpResponseMessage response = await StaticClient.SendAsync(requestMessage);

            JObject QueryResponse = JObject.Parse(await response.Content.ReadAsStringAsync());
            JArray classes = (JArray)QueryResponse["QueryResponse"]["Class"];

            CachedClassList = classes.Select(QboClass => new Class() { Name = (string)QboClass["Name"], Id = (string)QboClass["Id"] }).ToList();
            return true;
        }

        static List<TaxCode> CachedTaxCodeList = [];

        public async static Task<bool> PopulateQBOTaxCodesList()
        {
            StaticClient ??= new HttpClient();

            if (!await CheckTokens()) return false;
            if (CachedTaxCodeList.Count > 0) return false;

            string url = $"{BASE_URL}/v3/company/{Tokens!.RealmId}/query?query=select Name, Id from TaxCode";

            HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Get, url);
            requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("bearer", Tokens.AccessToken);

            if (!StaticClient.DefaultRequestHeaders.Any(rh => rh.Value.Contains("application/json") && rh.Key == "Accept"))
                StaticClient.DefaultRequestHeaders.Add("Accept", "application/json");

            HttpResponseMessage response = await StaticClient.SendAsync(requestMessage);

            JObject QueryResponse = JObject.Parse(await response.Content.ReadAsStringAsync());
            JArray taxCodes = (JArray)QueryResponse["QueryResponse"]["TaxCode"];

            CachedTaxCodeList = taxCodes.Select(taxCode => new TaxCode() { Name = (string)taxCode["Name"], Id = (string)taxCode["Id"] }).ToList();
            return true;
        }

    }
}