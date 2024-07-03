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
    /// Presents all regions outlined by the user and allows them to remove/edit them for refinement
    /// Allow the running of the Tesseract engine on each snip as a test to let the user assess accuracy
    /// </summary>
    public partial class RegionViewer : Window
    {
        public RegionViewer(List<CroppedBitmap> regions)
        {
            InitializeComponent();
            this.Width = SystemParameters.PrimaryScreenWidth / 2;
            this.Height = SystemParameters.PrimaryScreenHeight;
        }
    }
}
