using System.Drawing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using Ghostscript.NET.Rasterizer;
using Tesseract;

namespace WpfOcrInvoiceExtractor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private System.Windows.Point origin;
        private System.Windows.Point start;

        private double baseScale;
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

        //If entirety of image in view, disallow panning and outward scaling
        //If image is scaled inward and image edges are hidden allow panning until image edge gets to respective window edge
        //Drawing rectangles on image
        //Extracting rectangles as images into new windows

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            TransformGroup group = new();

            ScaleTransform scTransform = new()
            {
                ScaleY = (this.Height - 40) / invoice.RenderSize.Height,
                ScaleX = (this.Height - 40) / invoice.RenderSize.Height,
                CenterX = invoice.RenderSize.Width / 2,
                CenterY = invoice.RenderSize.Height / 2
            };

            baseScale = scTransform.ScaleX;
            group.Children.Add(scTransform);

            TranslateTransform tt = new();
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
            var ta = invoice.TransformToAncestor(imageCanvas);
            System.Windows.Point areaPosition = ta.Transform(new System.Windows.Point(0, 0));

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
            if (zoom < 0 && transform.ScaleX + zoom <= baseScale) return;
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