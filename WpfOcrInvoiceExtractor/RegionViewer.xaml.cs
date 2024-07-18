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
            this.Loaded += Window_Loaded;
            
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
            spotlightRegion.Source = imageSources[0].Image;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Matrix mtx = new Matrix(spotlightCanvas.RenderSize.Width / spotlightRegion.RenderSize.Width, 0, 0, spotlightCanvas.RenderSize.Width / spotlightRegion.RenderSize.Width, 0, 0);
            Canvas.SetTop(spotlightRegion, (spotlightCanvas.RenderSize.Height - (mtx.M22 * spotlightRegion.RenderSize.Height)) / 2);
            spotlightRegion.RenderTransform = new MatrixTransform(mtx);
            this.SizeChanged += Window_Resize;
        }

        private void Window_Resize(object sender, RoutedEventArgs e)
        {
            MatrixTransform matrixTransform = (MatrixTransform)spotlightRegion.RenderTransform;
            var matrix = matrixTransform.Matrix;
            matrix.M11 = matrix.M22 = spotlightCanvas.RenderSize.Width / spotlightRegion.Source.Width;
            matrixTransform.Matrix = matrix;
            Canvas.SetTop(spotlightRegion, (spotlightCanvas.RenderSize.Height - (matrix.M22 * spotlightRegion.Source.Height)) / 2);
        }

        private void regionClicked(object sender, MouseButtonEventArgs e) {
            // Get the index of the clicked image - sender.content.index
            ImageRegion rgn = (ImageRegion)((ContentPresenter) sender).Content;
            this.focusedRegion = rgn.index;
            spotlightRegion.Source = imageSources[rgn.index].Image;
            MatrixTransform matrixTransform = (MatrixTransform) spotlightRegion.RenderTransform;
            var matrix = matrixTransform.Matrix;
            matrix.M11 = matrix.M22 = spotlightCanvas.RenderSize.Width / imageSources[rgn.index].Image.Width;
            matrixTransform.Matrix = matrix;            
            Canvas.SetTop(spotlightRegion, (spotlightCanvas.RenderSize.Height - (matrix.M22 * imageSources[rgn.index].Image.Height)) / 2);
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
