using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Tesseract;

namespace WpfOcrInvoiceExtractor
{
    /// <summary>
    /// Presents all regions outlined by the user and allows them to remove/edit them for refinement
    /// Allow the running of the Tesseract engine on each snip as a test to let the user assess accuracy
    /// </summary>
    public partial class RegionViewer : Window
    {
        List<ImageRegion> imageSources = new List<ImageRegion>();
        int focusedRegion;

        public RegionViewer(List<CroppedBitmap> regions)
        {
            InitializeComponent();
            this.DataContext = this;
            this.Width = SystemParameters.PrimaryScreenWidth / 2;
            this.Height = SystemParameters.PrimaryScreenHeight;

            string sourceDirectory = @"C:\Users\Justin\source\repos\WpfOcrInvoiceExtractor\WpfOcrInvoiceExtractor\testimages";
            var txtFiles = Directory.EnumerateFiles(sourceDirectory).Where(f => f.EndsWith("jpg"));

            for (var i=0; i<txtFiles.Count(); i++)
            {
                string f = txtFiles.ElementAt(i).Replace("\\", "/");
                imageSources.Add(new ImageRegion(f, i));
            }

            regionList.ItemsSource = imageSources;
            ImageEditorControl.ImageBitmap = new WriteableBitmap(imageSources[0].Image);
            focusedRegion = 0;
        }

        private void regionClicked(object sender, MouseButtonEventArgs e) {
            // Get the index of the clicked image - sender.content.index
            ImageRegion rgn = (ImageRegion)((ContentPresenter) sender).Content;
            ImageEditorControl.Invoice_SourceUpdated(new WriteableBitmap(imageSources[rgn.index].Image));
        }

        private void runOCRTestOnCurrent(object sender, RoutedEventArgs e)
        {
            TesseractEngine engine = new TesseractEngine("./tessdata", "eng");
            using (MemoryStream outStream = new())
            {
                BitmapEncoder enc = new BmpBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(this.imageSources[this.focusedRegion].Image));
                enc.Save(outStream);
                Bitmap bitmap = new(outStream);

                var page = engine.Process(new Bitmap(bitmap));
                Debug.WriteLine($"{page.GetText()}");
            }
        }
    }
}
