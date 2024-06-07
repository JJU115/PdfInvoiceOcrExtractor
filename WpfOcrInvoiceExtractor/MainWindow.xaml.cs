using System.Diagnostics;
using System.Drawing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Ghostscript.NET.Rasterizer;
using Tesseract;

using Point = System.Windows.Point;

namespace WpfOcrInvoiceExtractor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Point origin;
        private Point start;
        private bool disableRightPanning;
        private bool disableLeftPanning;
        private bool disableUpPanning;
        private bool disableDownPanning;

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

            this.disableRightPanning = false;
            this.disableDownPanning = false;
            this.disableLeftPanning = false;
            this.disableUpPanning = false;
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

            MatrixTransform mt = new();
            group.Children.Add(mt);

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

            bool ignoreHTrans = false;
            bool ignoreVTrans = false;

            var ta = invoice.TransformToAncestor(imageCanvas);
            Point areaPosition = ta.Transform(new Point(0, 0));

            TransformGroup transformGroup = (TransformGroup)invoice.RenderTransform;
            var tt = (TranslateTransform)transformGroup.Children[1];
            MatrixTransform matrixTransform = (MatrixTransform)transformGroup.Children[2];

            Vector topLeftSide = areaPosition - new Point(0,0);
            //Right side of image to right window edge
            Vector bottomRightSide = new Point(imageGrid.ActualWidth + 4, imageGrid.ActualHeight + 4) 
                - new Point(areaPosition.X + (matrixTransform.Matrix.M11 * invoice.RenderSize.Width), areaPosition.Y + (matrixTransform.Matrix.M22 * invoice.RenderSize.Height));

            Vector v = start - e.GetPosition(imageCanvas);
            if ((this.disableRightPanning && v.X < 0) || (this.disableLeftPanning && v.X > 0))
            {
                start.X = e.GetPosition(imageCanvas).X;
                origin.X = tt.X;
            }

            if ((this.disableUpPanning && v.Y > 0) || (this.disableDownPanning && v.Y < 0))
            {
                start.Y = e.GetPosition(imageCanvas).Y;
                origin.Y = tt.Y;
            }
            
            //If image is not scaled enough to overflow at edges, return
            ignoreHTrans = topLeftSide.X >= 0 && bottomRightSide.X >= 0;
            ignoreVTrans = topLeftSide.Y >= 0 && bottomRightSide.Y >= 0;

            this.disableRightPanning = topLeftSide.X >= 0;
            this.disableLeftPanning = bottomRightSide.X >= 0;
            this.disableDownPanning = topLeftSide.Y >= 0;
            this.disableUpPanning = bottomRightSide.Y >= 0;
            
            //Image is overflowing at edges     
            double hTranslation = origin.X - v.X;
            double vTranslation = origin.Y - v.Y;
            
            //If the translation is left to right...
            if (v.X < 0)
            {
                //if (this.disableRightPanning) return;
                hTranslation = this.disableRightPanning ? tt.X : hTranslation;
                //If the translation has moved the left edge of the image past the left edge of the window...
                if (topLeftSide.X + (hTranslation - tt.X) >= 0)
                {
                    //Set the image at the left window edge
                    hTranslation = tt.X - topLeftSide.X;
                    this.disableRightPanning = true;
                }
                
            }

            if (v.X > 0) //Right to left translation
            {
                //if (this.disableLeftPanning) return;
                hTranslation = this.disableLeftPanning ? tt.X : hTranslation;
                //If the translation has moved the right edge of the image past the right edge of the window...
                if (bottomRightSide.X + (tt.X - hTranslation) >= 0)
                {
                    //Set the image at the right window edge
                    hTranslation = tt.X + bottomRightSide.X;
                    this.disableLeftPanning = true;
                }
                
            }

            //Downwards translation
            if (v.Y < 0)
            {
                //if (this.disableDownPanning) return;
                vTranslation = this.disableDownPanning ? tt.Y : vTranslation;
                //If the translation has moved the top edge of the image past the top edge of the window...
                if (topLeftSide.Y + (vTranslation - tt.Y) >= 0)
                {
                    //Set the image at the top window edge
                    vTranslation = tt.Y - topLeftSide.Y;
                    this.disableDownPanning = true;
                }
                
            }

            if (v.Y > 0) //Upwards translation
            {
                //if (this.disableUpPanning) return;
                vTranslation = this.disableUpPanning ? tt.Y : vTranslation;
                //If the translation has moved the bottom edge of the image past the bottom edge of the window...
                if (bottomRightSide.Y + (tt.Y - vTranslation) >= 0)
                {
                    //Set the image at the right window edge
                    vTranslation = tt.Y + bottomRightSide.Y;
                    this.disableUpPanning = true;
                }
                
             }

            tt.X = ignoreHTrans ? tt.X : hTranslation;
            tt.Y = ignoreVTrans ? tt.Y : vTranslation;
        }

        private void image_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            invoice.CaptureMouse();
            TransformGroup transformGroup = (TransformGroup)invoice.RenderTransform;
            MatrixTransform matrixTransform = (MatrixTransform)transformGroup.Children[2];
            var tt = (TranslateTransform)transformGroup.Children[1];
            start = e.GetPosition(imageCanvas);
            origin = new Point(tt.X, tt.Y);
            var ta = invoice.TransformToAncestor(this);
            Point areaPosition = ta.Transform(new Point(0, 0));
            Vector topLeftSide = areaPosition - new Point(0, 0);
            Vector bottomRightSide = new Point(imageGrid.ActualWidth, imageGrid.ActualHeight)
                - new Point(areaPosition.X + (matrixTransform.Matrix.M11 * invoice.RenderSize.Width), areaPosition.Y + (matrixTransform.Matrix.M22 * invoice.RenderSize.Height));
        }

        //If zooming out, must apply proper translations to re-center image
        private void image_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            TransformGroup transformGroup = (TransformGroup)invoice.RenderTransform;
            MatrixTransform matrixTransform = (MatrixTransform)transformGroup.Children[2];

            var element = (UIElement)sender;
            var position = e.GetPosition(element);
            var matrix = matrixTransform.Matrix;
            var scale = e.Delta >= 0 ? 1.01 : (1.09 / 1.1);

            if (e.Delta < 0 && matrix.M11 * scale <= this.baseScale)
            {
                matrix.M11 = this.baseScale;
                matrix.M22 = this.baseScale;
            } 
            else
            {
                matrix.ScaleAtPrepend(scale, scale, 0, 0);
            }          
            matrixTransform.Matrix = matrix;
            Debug.WriteLine($"{matrix}");
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