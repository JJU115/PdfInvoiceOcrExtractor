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

namespace WpfOcrInvoiceExtractor
{
    class QBOUtility
    {
        /* Refresh request
         * POST /oauth2/v1/tokens/bearer?grant_type
    =refresh_token&refresh_token
    =AB11731906050GY9gy8BWAGA9IUel94ZThM8
    jI2FpH4Rsmcxkh
Content-Type: application/x-www-form
    -urlencoded
Accept: application/json
Authorization: Basic 
    QUJDbU54MzVTWk9ZRW9jTHBFRG9GamtsOVc3M
    W9qY2xMbWs1VUVhUTk2OU9DN1VSUVI6NTBuRk
    pRVUdIVDFSSVRtN2JFMW9VaTFmM2JqN3RDQ0t
    1MmdXbFYySQ==
         */
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

        public static bool CreateNewBillToQbo()
        {
            //Create bill object
            Bill bill = new Bill();

            //Check the tokens
            Task<bool> authTask = new Task<bool>(() => { return true; });
            Task task = Task.Run(() =>
            {
                bool accessValid = Tokens != null && CheckTokenIsValid(Tokens.AccessToken ?? "");
                bool refreshValid = Tokens != null && CheckTokenIsValid(Tokens.RefreshToken ?? "");
                if (!accessValid && !refreshValid)
                {
                    //Alter authTask
                    QBOAuthWindow authWindow = new QBOAuthWindow();
                    authWindow.Closed += (s, a) => authTask.Start();
                    authWindow.Show();
                } else if (!accessValid) {
                    //Alter auth task
                    //Refresh token request
                    authTask.Start();
                }
            });

            task.Wait();
            authTask.Wait();

            if (!authTask.Result) return false;

            //Prepare the request
            if (StaticClient == null) StaticClient = new HttpClient();

            //Set the body and headers

            //Receive and parse the response

            /*
             * POST /v3/company/9341452801840587/bill?minorversion=73

Content type:application/json
Production Base URL:https://quickbooks.api.intuit.com
Sandbox Base URL:https://sandbox-quickbooks.api.intuit.com
             */
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
                TokenResponse response = Client.GetBearerTokenAsync(query["code"]).Result;

                // Set the token values with the client
                // responce and query parameters.
                Tokens.AccessToken = response.AccessToken;
                Tokens.RefreshToken = response.RefreshToken;
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
            var tokenTicks = GetTokenExpirationTime(token);
            var tokenDate = DateTimeOffset.FromUnixTimeSeconds(tokenTicks).UtcDateTime;

            var now = DateTime.Now.ToUniversalTime();

            var valid = tokenDate >= now;

            return valid;
        }
    }
}
