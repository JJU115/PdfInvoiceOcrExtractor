using Ghostscript.NET.Rasterizer;
using Intuit.Ipp.Data;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace WpfOcrInvoiceExtractor
{
    /// <summary>
    /// Interaction logic for InvoiceTemplateViewer.xaml
    /// </summary>
    public partial class InvoiceTemplateViewer : Window
    {
        public BitmapSource invoiceDisplay;
        public List<ImageRegion> imageRegions = new List<ImageRegion>();
        public InvoiceTemplateViewer(string pdfFilePath)
        {
            InitializeComponent();
            this.DataContext = this;

            this.Width = SystemParameters.PrimaryScreenWidth / 2;
            this.Height = SystemParameters.PrimaryScreenHeight / 1.1;
            var oldBitmap = ConvertPdfToImage(pdfFilePath)[0];

            var hOldBitmap = oldBitmap.GetHbitmap(System.Drawing.Color.Transparent);
            this.invoiceDisplay =
               Imaging.CreateBitmapSourceFromHBitmap(
                 hOldBitmap,
                 IntPtr.Zero,
                 new Int32Rect(0, 0, oldBitmap.Width, oldBitmap.Height),
                 null);

            ImageEditorControl.ImageBitmap = new WriteableBitmap(this.invoiceDisplay);
        }

        public List<Bitmap> ConvertPdfToImage(string filePath)
        {
            int desired_dpi = 300;
          
            string inputPdfPath = filePath;
            string outputPath = @"C:\Users\Justin\Pictures";

            List<Bitmap> pdfImages = new List<Bitmap>();
            using (var rasterizer = new GhostscriptRasterizer())
            {
                rasterizer.Open(inputPdfPath);

                for (var pageNumber = 1; pageNumber <= rasterizer.PageCount; pageNumber++)
                {
                    var pageFilePath = System.IO.Path.Combine(outputPath, string.Format("Page-{0}.png", pageNumber));

                    var img = rasterizer.GetPage(desired_dpi, pageNumber);
                    pdfImages.Add(new Bitmap(img));
                }
            }
            return pdfImages;
        }

        private void Continue_Button_Click(object sender, RoutedEventArgs e)
        {
            int index = 0;
            foreach (var region in ImageEditorControl.RegionsSource) {
                CroppedBitmap cropped = new(ImageEditorControl.ImageBitmap, region);
                imageRegions.Add(new() { Image = cropped, Name="", Index=index++ });
            }
            DialogResult = true;
            Close();           
        }

        private void EraseAll_Button_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
