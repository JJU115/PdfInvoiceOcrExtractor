using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfOcrInvoiceExtractor
{
    internal class QboAuthTokens
    {
        public string? AccessToken { get; set; }
        public DateTime? AccessTokenExpiresIn { get; set; }
        public string? RefreshToken { get; set; }
        public DateTime? RefreshTokenExpiresIn { get; set; }
        public string? RealmId { get; set; }
        public string? ClientId { get; set; }
        public string? ClientSecret { get; set; }
        public string RedirectUrl { get; set; } = "https://developer.intuit.com/v2/OAuth2Playground/RedirectUrl";
        public string Environment { get; set; } = "sandbox";
    }
}
