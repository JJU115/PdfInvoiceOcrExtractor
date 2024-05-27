using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Ghostscript.NET;
using Ghostscript.NET.Rasterizer;
using Tesseract;
using static System.Net.Mime.MediaTypeNames;

namespace WpfOcrInvoiceExtractor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private System.Windows.Point origin;
        private System.Windows.Point start;
        public MainWindow()
        {
            InitializeComponent();
            this.Width = SystemParameters.PrimaryScreenWidth / 2;
            this.Height = SystemParameters.PrimaryScreenHeight / 1.1;
            var oldBitmap = ConvertPdfToImage()[0];

            var hOldBitmap = oldBitmap.GetHbitmap(System.Drawing.Color.Transparent);
            var bitmapSource =
               Imaging.CreateBitmapSourceFromHBitmap(
                 hOldBitmap,
                 IntPtr.Zero,
                 new Int32Rect(0, 0, oldBitmap.Width, oldBitmap.Height),
                 null);
            invoice.Source = bitmapSource;           

            this.Loaded += MainWindow_Loaded;
            invoice.MouseWheel += image_MouseWheel;
            invoice.MouseLeftButtonDown += image_MouseLeftButtonDown;
            invoice.MouseLeftButtonUp += image_MouseLeftButtonUp;
            invoice.MouseMove += image_MouseMove;
        }

        //Position of mouse as center of scale point
        //Top and bottom edges of image stick to top and bottom edges of window respectively
        //If image is scaled so that sides go beyond window sides, make image sides stick to window sides 
        //No panning image edge past visible window edge
        //Drawing rectangles on image
        //Extracting rectangles as images into new windows

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            TransformGroup group = new TransformGroup();

            ScaleTransform scTransform = new ScaleTransform();
            scTransform.ScaleY = (this.Height - 40) / invoice.RenderSize.Height;
            scTransform.ScaleX = scTransform.ScaleY;
            scTransform.CenterX = invoice.RenderSize.Width / 2;
            scTransform.CenterY = invoice.RenderSize.Height / 2;

            group.Children.Add(scTransform);

            TranslateTransform tt = new TranslateTransform();
            group.Children.Add(tt);

            invoice.RenderTransform = group;

            Canvas.SetLeft(invoice, (this.ActualWidth - (scTransform.ScaleX * invoice.RenderSize.Width)) / 2);
        }

        private void image_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            invoice.ReleaseMouseCapture();
        }

        private void image_MouseMove(object sender, MouseEventArgs e)
        {
            if (!invoice.IsMouseCaptured) return;

            var tt = (TranslateTransform)((TransformGroup)invoice.RenderTransform).Children.First(tr => tr is TranslateTransform);
            Vector v = start - e.GetPosition(imageCanvas);
            tt.X = origin.X - v.X;
            tt.Y = origin.Y - v.Y;
        }

        private void image_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            invoice.CaptureMouse();
            var tt = (TranslateTransform)((TransformGroup)invoice.RenderTransform).Children.First(tr => tr is TranslateTransform);
            start = e.GetPosition(imageCanvas);
            origin = new System.Windows.Point(tt.X, tt.Y);
        }

        private void image_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            TransformGroup transformGroup = (TransformGroup)invoice.RenderTransform;
            ScaleTransform transform = (ScaleTransform)transformGroup.Children[0];

            double zoom = e.Delta > 0 ? .03 : -.03;
            transform.ScaleX += zoom;
            transform.ScaleY += zoom;
        }


        public List<Bitmap> ConvertPdfToImage()
        {
            int desired_dpi = 300;

            string inputPdfPath = @"testImages\wesco_264010700_20220521_23275357_9136520875.pdf";
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


        public string runTesseract(Bitmap img)
        {
            TesseractEngine engine = new TesseractEngine("./tessdata", "eng");
            var page = engine.Process(img);
            return page.GetText();
        }
    }
}