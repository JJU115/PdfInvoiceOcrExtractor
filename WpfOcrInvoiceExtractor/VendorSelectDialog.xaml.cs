using Intuit.Ipp.Data;
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
    /// Interaction logic for VendorSelectDialog.xaml
    /// </summary>
    public partial class VendorSelectDialog : Window
    {
        public VendorSelectDialog(List<Vendor> vendor)
        {
            InitializeComponent();
            vendorBox.ItemsSource = vendor;
        }

        private void okButton_Click(object sender, RoutedEventArgs e)
        {
            if (vendorBox.SelectedIndex > -1)
            {
                this.DialogResult = true;
                this.Close();
            }
           
        }
    }
}
