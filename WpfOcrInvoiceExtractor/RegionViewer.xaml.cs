using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
        List<BitmapSource> imageSources = new List<BitmapSource>();
        public RegionViewer(List<CroppedBitmap> regions)
        {
            InitializeComponent();
            this.Loaded += Window_Loaded;
            this.Width = SystemParameters.PrimaryScreenWidth / 2;
            this.Height = SystemParameters.PrimaryScreenHeight;

            string sourceDirectory = @"C:\Users\Justin\source\repos\WpfOcrInvoiceExtractor\WpfOcrInvoiceExtractor\testimages";
            var txtFiles = Directory.EnumerateFiles(sourceDirectory).Where(f => f.EndsWith("jpg"));

            foreach (string currentFile in txtFiles)
            {
                string f = currentFile.Replace("\\", "/");
                Uri uri = new Uri(f, UriKind.RelativeOrAbsolute);
                BitmapImage img = new BitmapImage(uri);
                img.DecodePixelWidth = 75;
                imageSources.Add(img);
            }

            regionList.ItemsSource = imageSources;
            spotlightRegion.Source = imageSources[0];

        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Matrix mtx = new Matrix(spotlightCanvas.RenderSize.Width / spotlightRegion.RenderSize.Width, 0, 0, spotlightCanvas.RenderSize.Width / spotlightRegion.RenderSize.Width, 0, 0);
            spotlightRegion.RenderTransform = new MatrixTransform(mtx);
        }
    }
}
