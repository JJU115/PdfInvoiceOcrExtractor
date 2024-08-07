using Intuit.Ipp.OAuth2PlatformClient;
using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace WpfOcrInvoiceExtractor
{
    /// <summary>
    /// Handles authorization for connecting to QBO. This is not necessary on every startup. If the tokens have been received already check the access token and use the refresh token
    /// to refresh if needed. If the refresh token has expired then we need to repeat this entire process again.
    /// </summary>
    public partial class QBOAuthWindow : Window
    {
        private bool userAuthComplete = false;
        public QBOAuthWindow()
        {
            InitializeComponent();
            this.Loaded += Window_Loaded;
            this.Closing += Window_Closing;
        }


        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            QBOUtility.Initialize();

            // Initialize the WebView2 control.
            await WebView.EnsureCoreWebView2Async();

            // Navigate the WebView2 control to
            // a generated authorization URL. After the user authenticates we have what we need so can ignore the redirect.
            // How to know auth is done and can close window?
            WebView.CoreWebView2.Navigate(QBOUtility.GetAuthorizationURL(OidcScopes.Accounting));
            WebView.CoreWebView2.NavigationStarting += CoreWebView2_NavigationStarting;
            WebView.CoreWebView2.NavigationCompleted += CoreWebView2_NavigationCompleted;
        }

        private void CoreWebView2_NavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            if (userAuthComplete) this.Close();
        }

        private void CoreWebView2_NavigationStarting(object? sender, CoreWebView2NavigationStartingEventArgs e)
        {
            Debug.WriteLine($"Redirecting to {e.Uri}");
            Match mc = Regex.Match(e.Uri, "https:[/]{2}developer.intuit.com[/]{1}v2[/]{1}OAuth2Playground[/]{1}RedirectUrl[?]{1}code=(.+)&state=(.+)&realmId=(.+)");
            if (mc.Success)
            {
                Debug.WriteLine("QBO Redirect detected");
                this.userAuthComplete = true;
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // When the user closes the form we
            // assume that the operation has
            // completed with success or failure.

            // Get the current query parameters
            // from the current WebView source (page)
            string query = WebView.Source.Query;

            // Use the the shared helper library
            // to validate the query parameters
            // and write the output file.
            if (QBOUtility.CheckQueryParamsAndSet(query) == true && QBOUtility.Tokens != null)
            {
                QBOUtility.WriteTokensAsJson(QBOUtility.Tokens);
            }
            else
            {
                MessageBox.Show("Quickbooks Online failed to authenticate.");
            }
        }
    }
}
