using Intuit.Ipp.OAuth2PlatformClient;
using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
    /// Interaction logic for QBOAuthWindow.xaml
    /// </summary>
    public partial class QBOAuthWindow : Window
    {
        public QBOAuthWindow()
        {
            InitializeComponent();
            this.Loaded += Window_Loaded;
        }


        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            QBOUtility.Initialize();

            // Initialize the WebView2 control.
            await WebView.EnsureCoreWebView2Async();

            // Navigate the WebView2 control to
            // a generated authorization URL.
            WebView.CoreWebView2.Navigate(QBOUtility.GetAuthorizationURL(OidcScopes.Accounting));
        }
    }
}
