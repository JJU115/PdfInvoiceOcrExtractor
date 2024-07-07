using System.Diagnostics;
using System.Drawing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Ghostscript.NET.Rasterizer;
using Tesseract;

using Point = System.Windows.Point;
using Size = System.Windows.Size;
using Rectangle = System.Windows.Shapes.Rectangle;
using Rect = System.Windows.Rect;
using Brushes = System.Windows.Media.Brushes;
using Pen = System.Windows.Media.Pen;
using System.Windows.Ink;
using System.Windows.Shapes;

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
            var writeableBitmap = new WriteableBitmap(bitmapSource);

            invoice.Source = writeableBitmap;
            Canvas.SetZIndex(invoice, 100);

            this.Loaded += MainWindow_Loaded;
            this.KeyDown += OnKeyDownHandler;
            invoice.MouseWheel += image_MouseWheel;
            invoice.MouseLeftButtonDown += image_MouseLeftButtonDown;
            invoice.MouseRightButtonDown += image_MouseRightButtonDown;
            invoice.MouseRightButtonUp += image_MouseRightButtonUp;
            invoice.MouseLeftButtonUp += image_MouseLeftButtonUp;
            invoice.MouseMove += image_MouseMove;

            this.disableRightPanning = false;
            this.disableDownPanning = false;
            this.disableLeftPanning = false;
            this.disableUpPanning = false;
        }


        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Matrix mtx = new Matrix((this.Height - 38) / invoice.RenderSize.Height, 0, 0, (this.Height - 38) / invoice.RenderSize.Height, 0, 0);
            baseScale = mtx.M11;
            invoice.RenderTransform = new MatrixTransform(mtx);
            Canvas.SetLeft(invoice, (this.ActualWidth - (mtx.M11 * invoice.RenderSize.Width)) / 2);
            RegionViewer rv = new RegionViewer(this.regions.Select(R => new CroppedBitmap((BitmapSource)invoice.Source, R)).ToList());
            rv.Show();
            rv.Focus();
        }

        private void OnKeyDownHandler(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.A)
            {
                //CroppedBitmap cb = new CroppedBitmap((BitmapSource)invoice.Source, regions[0]);
                RegionViewer rv = new RegionViewer(this.regions.Select(R => new CroppedBitmap((BitmapSource)invoice.Source, R)).ToList());
                rv.Show();
            }
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

        Point drawPointStart;
        Rectangle currRect = new Rectangle();
        List<Int32Rect> regions = new List<Int32Rect>();

        private void draw_rectangle(object sender, MouseEventArgs e)
        {
            Point mousePoint = e.GetPosition(imageCanvas);
            Vector rectPoint = drawPointStart - mousePoint;
            ScaleTransform direction = (ScaleTransform)currRect.RenderTransform;
            direction.ScaleX = rectPoint.X < 0 ? 1 : -1;
            direction.ScaleY = rectPoint.Y < 0 ? 1 : -1;
            currRect.Width = Math.Abs(drawPointStart.X - mousePoint.X);
            currRect.Height = Math.Abs(drawPointStart.Y - mousePoint.Y);
        }

        private void image_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            //Also need to change the event handler for mouse move as long as the right mouse button is held down
            drawPointStart = e.GetPosition(imageCanvas);
            invoice.MouseMove -= image_MouseMove;
            invoice.MouseMove += draw_rectangle;

            currRect = new Rectangle();
            currRect.RenderTransform = new ScaleTransform(1, 1);
            currRect.Stroke = Brushes.Purple;
            currRect.StrokeThickness = 4;
            currRect.Cursor = Cursors.ScrollAll;
            currRect.MouseRightButtonUp += image_MouseRightButtonUp;
            Canvas.SetLeft(currRect, drawPointStart.X);
            Canvas.SetTop(currRect, drawPointStart.Y);
            Canvas.SetZIndex(currRect, 500);
            imageCanvas.Children.Add(currRect);
        }


        private void image_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            //Perform 4 writePixels calls, one for each side of the rectangle to draw
            invoice.MouseMove += image_MouseMove;
            invoice.MouseMove -= draw_rectangle;

            var drawPointTopLeft = imageCanvas.TransformToDescendant(invoice).Transform(drawPointStart);
            var drawPointBottomRight = imageCanvas.TransformToDescendant(invoice).Transform(new Point(drawPointStart.X + this.currRect.Width, drawPointStart.Y + this.currRect.Height));
            var wb = ((WriteableBitmap)invoice.Source);

            int bytesPerPixel = (wb.Format.BitsPerPixel + 7) / 8; // general formula
            var width = (int)Math.Abs(drawPointBottomRight.X - drawPointTopLeft.X);
            int height = (int)Math.Abs(drawPointBottomRight.Y - drawPointTopLeft.Y);
            var topStride = width * bytesPerPixel;
            var sideStride = 5 * bytesPerPixel;
            int bufferLen = Math.Max(topStride * 5, sideStride * (height + 5));
            byte[] topBuffer = new byte[bufferLen];
            byte[] sideBuffer = new byte[bufferLen];
            
            for (int b=0; b < bufferLen; b+=bytesPerPixel)
            {
                topBuffer[b] = sideBuffer[b] = 40;        
                topBuffer[b + 1] = sideBuffer[b + 1] = 232;  
                topBuffer[b + 2] = sideBuffer[b + 2] = 72;    
                topBuffer[b + 3] = sideBuffer[b + 3] = 0;
            }

            //Draw top
            var rect = new Int32Rect((int)drawPointTopLeft.X, (int)drawPointTopLeft.Y, width, 5);
            wb.WritePixels(rect, topBuffer, topStride, 0);

            //Draw right side
            rect = new Int32Rect((int)drawPointTopLeft.X + width, (int)drawPointTopLeft.Y, 5, height + 5);
            wb.WritePixels(rect, sideBuffer, sideStride, 0);

            //Draw bottom
            rect = new Int32Rect((int)drawPointTopLeft.X, (int)drawPointTopLeft.Y + height, width, 5);
            wb.WritePixels(rect, topBuffer, topStride, 0);

            //Draw left side
            rect = new Int32Rect((int)drawPointTopLeft.X, (int)drawPointTopLeft.Y, 5, height);
            wb.WritePixels(rect, sideBuffer, sideStride, 0);

            imageCanvas.Children.RemoveAt(imageCanvas.Children.Count - 1);
            regions.Add(new Int32Rect((int)drawPointTopLeft.X, (int)drawPointTopLeft.Y, width, height));
        }


        private void image_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            MatrixTransform matrixTransform = (MatrixTransform)invoice.RenderTransform;
            
            var element = (UIElement)sender;
            var position = Mouse.GetPosition(invoice);         
            var matrix = matrixTransform.Matrix;
            var scale = e.Delta >= 0 ? 1.12 : (1.00 / 1.12);

            if (matrix.M11 == baseScale && e.Delta < 0) return;

            Point topLeftCorner = invoice.TransformToAncestor(imageCanvas).Transform(new Point(0, 0));
            Point bottomRightCorner = new Point(topLeftCorner.X + (matrix.M11 * invoice.RenderSize.Width), topLeftCorner.Y + (matrix.M22 * invoice.RenderSize.Height));
            if (e.Delta >=0 && invoice.RenderSize.Width * matrix.M11 >= this.ActualWidth)
            {
                matrix.ScaleAtPrepend(scale, scale, position.X, position.Y);
            }
            else
            {
                //Zoom out
                if (e.Delta < 0)
                {
                    if (matrix.M11 * scale <= this.baseScale) scale = baseScale / matrix.M11;                   

                    Size oldSize = new(invoice.RenderSize.Width * matrix.M11, invoice.RenderSize.Height * matrix.M11);
                    Size newSize = new(oldSize.Width * scale, oldSize.Height * scale);
                    double topYNew = topLeftCorner.Y + (position.Y / invoice.RenderSize.Height) * (oldSize.Height - newSize.Height);
                    double leftXNew = topLeftCorner.X + (position.X / invoice.RenderSize.Width) * (oldSize.Width - newSize.Width);
                    double bottomYNew = topYNew + newSize.Height;
                    double rightXNew = leftXNew + newSize.Width;
                    double desiredSideSpace = (leftXNew + imageGrid.ActualWidth - rightXNew) / 2;

                    //If left edge would be separated from window edge..
                    if (leftXNew > 0)
                    {
                        matrix.OffsetX -= leftXNew - (invoice.RenderSize.Width * matrix.M11 * scale > imageGrid.ActualWidth ? 0 : desiredSideSpace);
                    }
                    else if (rightXNew < imageGrid.ActualWidth) {
                        matrix.OffsetX += (imageGrid.ActualWidth - rightXNew);
                    }

                    if (topYNew > 0) matrix.OffsetY -= topYNew;                 
                    if (bottomYNew < imageGrid.ActualHeight) matrix.OffsetY += imageGrid.ActualHeight - bottomYNew;
                    
                } 
                else //Zoom in
                {
                    //Zoom in and there is still side space between the image and the window
                    position.X = invoice.RenderSize.Width / 2;
                }
                matrix.ScaleAtPrepend(scale, scale, position.X, position.Y);                              
            }
            matrixTransform.Matrix = matrix;
            /*MatrixAnimator animator = new MatrixAnimator(matrixTransform.Matrix, matrix, new Duration(TimeSpan.FromMilliseconds(250)));
            animator.From = matrixTransform.Matrix;
            animator.To = matrix;
            matrixTransform.BeginAnimation(MatrixTransform.MatrixProperty, animator, HandoffBehavior.SnapshotAndReplace);
            */
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