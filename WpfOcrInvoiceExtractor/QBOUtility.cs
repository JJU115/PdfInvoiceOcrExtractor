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

namespace WpfOcrInvoiceExtractor
{
    class QBOUtility
    {
        public static QboAuthTokens? Tokens { get; set; } = null;
        public static OAuth2Client? Client { get; set; } = null;

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
    }
}
