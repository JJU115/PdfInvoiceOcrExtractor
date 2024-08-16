using Intuit.Ipp.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Web.WebView2.Core;
using Intuit.Ipp.OAuth2PlatformClient;
using System.IO;
using System.Text.Json;
using System.Collections.Specialized;
using System.Web;
using Intuit.Ipp.Data;
using System.Security.Claims;
using Intuit.Ipp.Security;
using Intuit.Ipp.QueryFilter;
using System.Net.Http;
using System.IdentityModel.Tokens.Jwt;
using Task = System.Threading.Tasks.Task;
using System.Diagnostics;
using System.Net.Http.Json;
using System.Collections;
using System.Windows;

namespace WpfOcrInvoiceExtractor
{
    
    class QBOUtility
    {
        readonly static string BASE_URL = "https://sandbox-quickbooks.api.intuit.com";
        public static QboAuthTokens? Tokens { get; set; } = null;
        public static OAuth2Client? Client { get; set; } = null;
        private static HttpClient? StaticClient = null;

        public static void Initialize(string path = ".\\Tokens.json")
        {
            // Loading the tokens and client once (on sign-in/start up)
            // and saving them in static properties saves us from
            // deserializing again when we want to read or write the data.
            Tokens = JsonSerializer.Deserialize<QboAuthTokens>(File.ReadAllText(path), new JsonSerializerOptions()
            {
                ReadCommentHandling = JsonCommentHandling.Skip
            }) ?? new();

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
                authWindow.Closed += (s, a) => authFinished.Start();
                authWindow.Show();
                await authFinished;
                return authFinished.Result;
            }
            else if (!accessValid)
            {
                var response = await Client!.RefreshTokenAsync("refreshToken");
                return response.HttpStatusCode == System.Net.HttpStatusCode.OK;
            }
            else
            {
                return true;
            }
        }

        public static async Task<bool> CreateNewBillToQbo()
        {
            //Check the tokens, if authentication failed for any reason, end and return false
            if (!await CheckTokens()) return false;
            //Otherwise we are now fully authorized to make requests

            //Prepare the request
            if (StaticClient == null) StaticClient = new HttpClient();

            //Set the body and headers
            string url = $"{BASE_URL}/v3/company/{Tokens!.RealmId}/bill?minorversion=73";
            StaticClient.SetBearerToken(Tokens.AccessToken);

            Bill bill = new Bill();
            AccountBasedExpenseLineDetail abeld = new()
            {
                AccountRef = new ReferenceType()
            };
            abeld.AccountRef.Value = "7";
            bill.VendorRef = new ReferenceType();
            bill.VendorRef.Value = "56";
            bill.Line = new Line[1];
            bill.Line[0] = new Line
            {
                DetailType = LineDetailTypeEnum.AccountBasedExpenseLineDetail,
                DetailTypeSpecified = true,
                Amount = 200
            };
            bill.Line[0].AnyIntuitObject = abeld;

            string url2 = $"{BASE_URL}/v3/company/9341452801840587/customer/2?minorversion=73";
            HttpResponseMessage comp = await StaticClient.GetAsync(url2);
            Debug.WriteLine($"{await comp.Content.ReadAsStreamAsync()}");

            //Receive and parse the response
            HttpResponseMessage response = await StaticClient.PostAsJsonAsync(url, bill);
            Debug.WriteLine($"{response.Content}");
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

        /// <summary>
        /// Serializes the static tokens instance (Local.Tokens) and writes the serialized string to the <paramref name="path"/>.
        /// </summary>
        /// <param name="path">Absolute or relative path to the target JSON file to be written.</param>
        public static void WriteTokensAsJson(QboAuthTokens authTokens, string path = ".\\Tokens.json")
        {
            // Serialize the passed object
            // to a JSON formatted string.
            string serialized = JsonSerializer.Serialize(authTokens, new JsonSerializerOptions()
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
    }
}
