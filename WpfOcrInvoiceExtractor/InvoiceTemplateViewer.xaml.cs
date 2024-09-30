using Ghostscript.NET.Rasterizer;
using System.Drawing;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

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
                CroppedBitmap cropped = new(this.invoiceDisplay, region);
                imageRegions.Add(new() { Image = cropped, SourceRegion = region, Index=index++ });
            }
            DialogResult = true;
            Close();           
        }

        private void EraseAll_Button_Click(object sender, RoutedEventArgs e)
        {
            ImageEditorControl.Invoice_SourceUpdated(new WriteableBitmap(this.invoiceDisplay), true);
            ImageEditorControl.RegionsSource = [];
        }
    }
}
