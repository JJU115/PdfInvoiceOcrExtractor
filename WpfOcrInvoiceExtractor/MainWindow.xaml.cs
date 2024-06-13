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

        //Zooming out always re-centers image and doesn't allow space between top and bottom edges
        //  - If no image edge is stuck, zoom out is from mouse position
        //  - Any stuck edges stay stuck when zooming out (max 2) until certain scale reached
        //  - Top and bottom edges always end up stuck to window edge, sides separate and always have equal width of image to window edge 
        //Animating zoom in/out?
        //Drawing rectangles on image
        //Extracting rectangles as images into new windows

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Matrix mtx = new Matrix((this.Height - 38) / invoice.RenderSize.Height, 0, 0, (this.Height - 38) / invoice.RenderSize.Height, 0, 0);
            baseScale = mtx.M11;
            invoice.RenderTransform = new MatrixTransform(mtx);
            Canvas.SetLeft(invoice, (this.ActualWidth - (mtx.M11 * invoice.RenderSize.Width)) / 2);
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

            MatrixTransform matrixTransform = (MatrixTransform)invoice.RenderTransform;
            var matrix = matrixTransform.Matrix;
            double offsetX = matrixTransform.Matrix.OffsetX;
            double offsetY = matrixTransform.Matrix.OffsetY;

            Vector topLeftSide = areaPosition - new Point(0,0);
            //Right side of image to right window edge
            Vector bottomRightSide = new Point(imageGrid.ActualWidth + 4, imageGrid.ActualHeight + 4) 
                - new Point(areaPosition.X + (matrixTransform.Matrix.M11 * invoice.RenderSize.Width), areaPosition.Y + (matrixTransform.Matrix.M22 * invoice.RenderSize.Height));

            Vector v = start - e.GetPosition(imageCanvas);
            if ((this.disableRightPanning && v.X < 0) || (this.disableLeftPanning && v.X > 0))
            {
                start.X = e.GetPosition(imageCanvas).X;
                origin.X = offsetX;
            }

            if ((this.disableUpPanning && v.Y > 0) || (this.disableDownPanning && v.Y < 0))
            {
                start.Y = e.GetPosition(imageCanvas).Y;
                origin.Y = offsetY;
            }
            
            //If image is not scaled enough to overflow at edges, return
            ignoreHTrans = topLeftSide.X >= 0 && bottomRightSide.X >= 0;
            ignoreVTrans = topLeftSide.Y >= 0 && bottomRightSide.Y >= 0;

            this.disableRightPanning = topLeftSide.X >= 0;
            this.disableLeftPanning = bottomRightSide.X >= 0;
            this.disableDownPanning = topLeftSide.Y >= 0;
            this.disableUpPanning = bottomRightSide.Y >= 0;
   
            double hTranslation = origin.X - v.X;
            double vTranslation = origin.Y - v.Y;
            
            //If the translation is left to right...
            if (v.X < 0)
            {
                //if (this.disableRightPanning) return;
                hTranslation = this.disableRightPanning ? offsetX : hTranslation;
                //If the translation has moved the left edge of the image past the left edge of the window...
                if (topLeftSide.X + (hTranslation - offsetX) >= 0)
                {
                    //Set the image at the left window edge
                    hTranslation = offsetX - topLeftSide.X;
                    this.disableRightPanning = true;
                }
                
            }

            if (v.X > 0) //Right to left translation
            {
                //if (this.disableLeftPanning) return;
                hTranslation = this.disableLeftPanning ? offsetX : hTranslation;
                //If the translation has moved the right edge of the image past the right edge of the window...
                if (bottomRightSide.X + (offsetX - hTranslation) >= 0)
                {
                    //Set the image at the right window edge
                    hTranslation = offsetX + bottomRightSide.X;
                    this.disableLeftPanning = true;
                }
                
            }

            //Downwards translation
            if (v.Y < 0)
            {
                //if (this.disableDownPanning) return;
                vTranslation = this.disableDownPanning ? offsetY : vTranslation;
                //If the translation has moved the top edge of the image past the top edge of the window...
                if (topLeftSide.Y + (vTranslation - offsetY) >= 0)
                {
                    //Set the image at the top window edge
                    vTranslation = offsetY - topLeftSide.Y;
                    this.disableDownPanning = true;
                }
                
            }

            if (v.Y > 0) //Upwards translation
            {
                //if (this.disableUpPanning) return;
                vTranslation = this.disableUpPanning ? offsetY : vTranslation;
                //If the translation has moved the bottom edge of the image past the bottom edge of the window...
                if (bottomRightSide.Y + (offsetY - vTranslation) >= 0)
                {
                    //Set the image at the right window edge
                    vTranslation = offsetY + bottomRightSide.Y;
                    this.disableUpPanning = true;
                }
                
            }
            
            matrix.OffsetX = ignoreHTrans ? offsetX : hTranslation;
            matrix.OffsetY = ignoreVTrans ? offsetY : vTranslation;
            matrixTransform.Matrix = matrix;
        }

        private void image_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            invoice.CaptureMouse();
            MatrixTransform matrixTransform = (MatrixTransform)invoice.RenderTransform;
            var matrix = matrixTransform.Matrix;
            start = e.GetPosition(imageCanvas);
            origin = new Point(matrix.OffsetX, matrix.OffsetY);
            var ta = invoice.TransformToAncestor(this);
            Point areaPosition = ta.Transform(new Point(0, 0));
            Vector topLeftSide = areaPosition - new Point(0, 0);
            Vector bottomRightSide = new Point(imageGrid.ActualWidth, imageGrid.ActualHeight)
                - new Point(areaPosition.X + (matrixTransform.Matrix.M11 * invoice.RenderSize.Width), areaPosition.Y + (matrixTransform.Matrix.M22 * invoice.RenderSize.Height));
            Debug.WriteLine($"{matrix} -- {bottomRightSide.Y}");
        }

        //If zooming out, must apply proper translations to re-center image
        private void image_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            MatrixTransform matrixTransform = (MatrixTransform)invoice.RenderTransform;

            var element = (UIElement)sender;
            var position = invoice.TransformToAncestor(imageCanvas).Transform(e.GetPosition(element));
            var matrix = matrixTransform.Matrix;
            var scale = e.Delta >= 0 ? 1.1 : (1.00 / 1.1);

            if (matrix.M11 == baseScale && e.Delta < 0) return;

            Point topLeftCorner = invoice.TransformToAncestor(imageCanvas).Transform(new Point(0, 0));
            Point bottomRightCorner = new Point(topLeftCorner.X + (matrixTransform.Matrix.M11 * invoice.RenderSize.Width), topLeftCorner.Y + (matrixTransform.Matrix.M22 * invoice.RenderSize.Height));

            

             if (e.Delta >=0 && invoice.RenderSize.Width * matrix.M11 * scale > this.ActualWidth)
            {
                //No side space left
                matrix.ScaleAtPrepend(scale, scale, position.X, position.Y);
            }
            else
            {
                    
                if (e.Delta < 0)
                {
                    if (matrix.M11 * scale <= this.baseScale)
                    {
                        scale = baseScale / matrix.M11;
                    }
                    //Following calculations of new y coordinates are fairly accurate, more so if Position is closer to the top
                    //If right at the bottom, calculation is off by ~4, if right at top calculation is within ~0.05
                    //Scaling the image in more increases error after this correction by up to ~4 pixels, but at those levels we don't need accurate coordinates
                    double oldHeight = invoice.RenderSize.Height * matrix.M11;
                    double newHeight = oldHeight * scale;
                    double heightDiff = oldHeight - newHeight;
                    double topYNew = topLeftCorner.Y + (position.Y / this.Height) * heightDiff + ((position.Y / this.Height) * 4);
                    double bottomYNew = topYNew + newHeight;
                    Debug.WriteLine($"{topYNew}");

                    if (topYNew > 0)
                    {
                        matrix.OffsetY -= topYNew;
                    }

                    if (bottomYNew < this.Height)
                    {
                        matrix.OffsetY += this.Height - bottomYNew - 35;
                    }
                }
                matrix.ScaleAtPrepend(scale, scale, invoice.ActualWidth / 2, position.Y);                              
            }
            matrixTransform.Matrix = matrix;
            topLeftCorner = invoice.TransformToAncestor(imageCanvas).Transform(new Point(0, 0));
            bottomRightCorner = new Point(topLeftCorner.X + (matrixTransform.Matrix.M11 * invoice.RenderSize.Width), topLeftCorner.Y + (matrixTransform.Matrix.M22 * invoice.RenderSize.Height));
            Debug.WriteLine($"{topLeftCorner.Y}");
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