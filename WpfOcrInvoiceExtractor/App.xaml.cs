using System.Net;
using System.Windows;

namespace WpfOcrInvoiceExtractor
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls13;
        }
    }

}
