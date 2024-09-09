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
        public InvoiceTemplateViewer(string pdfFilePath)
        {
            InitializeComponent();
            this.DataContext = this;

            this.KeyDown += TemplateViewer_KeyDown;

            this.Width = SystemParameters.PrimaryScreenWidth / 2;
            this.Height = SystemParameters.PrimaryScreenHeight / 1.1;
            var oldBitmap = ConvertPdfToImage(pdfFilePath)[0];

            var hOldBitmap = oldBitmap.GetHbitmap(System.Drawing.Color.Transparent);
            var bitmapSource =
               Imaging.CreateBitmapSourceFromHBitmap(
                 hOldBitmap,
                 IntPtr.Zero,
                 new Int32Rect(0, 0, oldBitmap.Width, oldBitmap.Height),
                 null);

            ImageEditorControl.ImageBitmap = new WriteableBitmap(bitmapSource);
        }


        private async void TemplateViewer_KeyDown(object sender, KeyEventArgs e)
        {
            //Bill upload procedure:
            /*
             * 1. PDF uploaded, translate it to an image AND/OR read the PDF to get the vendor name
             * 2. Match vendor name to template by checking all templates to get template to use
             * 3. When invoice template was created the user would have selected an official vendor so template has vendor ID
             * 4. Pull stored image regions from template and pass them to process method
             * 5. Process method return Bill object, pass that and vendor id to upload method
             */

            //Store files of template data in this path


            List<ImageRegion> regionList = new();
            string sourceDirectory = @"C:\Users\Justin\source\repos\WpfOcrInvoiceExtractor\WpfOcrInvoiceExtractor\testimages";
            var txtFiles = Directory.EnumerateFiles(sourceDirectory).Where(f => f.EndsWith("jpg"));

            for (var i = 0; i < txtFiles.Count(); i++)
            {
                string f = txtFiles.ElementAt(i).Replace("\\", "/");
                regionList.Add(new ImageRegion(f, i));
            }

            if (e.Key == Key.R)
            {

                RegionViewer rv = new(regionList);
                rv.Show();
            }
            else if (e.Key == Key.F)
            {
                Vendor wesco = new()
                {
                    Id = "58"
                };
                await QBOUtility.CreateNewBillToQbo(regionList, wesco);
            }
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
    }
}
