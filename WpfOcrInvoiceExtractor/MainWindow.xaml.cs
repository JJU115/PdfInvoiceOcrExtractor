﻿using System.Drawing;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using Ghostscript.NET.Rasterizer;
using System.IO;
using Intuit.Ipp.Data;
using System.Xml.Serialization;
using Microsoft.Win32;


namespace WpfOcrInvoiceExtractor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        List<InvoiceTemplate> invoiceTemplates;
        //public List<Int32Rect> Regions = new List<Int32Rect>(); //Bound to image editor
        public MainWindow()
        {           
            InitializeComponent();
            this.DataContext = this;

            invoiceTemplates = RetrieveTemplateData();
            templateList.ItemsSource = invoiceTemplates;

            this.Width = SystemParameters.PrimaryScreenWidth * 0.75;
            this.Height = SystemParameters.PrimaryScreenHeight * 0.75;
            /*var oldBitmap = ConvertPdfToImage()[0];

            var hOldBitmap = oldBitmap.GetHbitmap(System.Drawing.Color.Transparent);
            var bitmapSource =
               Imaging.CreateBitmapSourceFromHBitmap(
                 hOldBitmap,
                 IntPtr.Zero,
                 new Int32Rect(0, 0, oldBitmap.Width, oldBitmap.Height),
                 null);*/

            //this.Loaded += Window_Loaded;
            this.KeyDown += MainWindow_KeyDown;

            //ImageEditorControl.ImageBitmap = new WriteableBitmap(bitmapSource);
          
        }

        private async void MainWindow_KeyDown(object sender, KeyEventArgs e)
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
            if (e.Key == Key.I) {
                InvoiceTemplateViewer itv = new(@"testImages\wesco_264010700_20220521_23275357_9136520875.pdf");
                itv.Show();
                return;
            }

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
            else if (e.Key == Key.F) {
                Vendor wesco = new()
                {
                    Id = "58"
                };
               await QBOUtility.CreateNewBillToQbo(regionList, wesco);
            }
        }

        private List<InvoiceTemplate> RetrieveTemplateData()
        {
            string localDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (!Directory.Exists($"{localDataPath}\\QBO_Invoice_Parser")) Directory.CreateDirectory($"{localDataPath}\\QBO_Invoice_Parser");

            XmlSerializer serializer = new XmlSerializer(typeof(InvoiceTemplate));
            var list = Directory.EnumerateFiles($"{localDataPath}\\QBO_Invoice_Parser").Where(f => f.StartsWith("template_")).Select(f =>
            {
                FileStream fs = new FileStream(f, FileMode.Open);
                InvoiceTemplate it = (InvoiceTemplate)serializer.Deserialize(fs);
                return it;
            });
            List<InvoiceTemplate> l = list.ToList();
            InvoiceTemplate addTemplate = new InvoiceTemplate();
            string fileName = @"C:\Users\Justin\source\repos\WpfOcrInvoiceExtractor\WpfOcrInvoiceExtractor\testimages\add-template.png";
            Uri uri = new Uri(fileName, UriKind.RelativeOrAbsolute);
            addTemplate.Display = new BitmapImage(uri);
            addTemplate.Vendor.DisplayName = "NewVendor";
            l.Insert(0, addTemplate);
            return l;
        }

        private static void WriteTemplateToData(InvoiceTemplate template)
        {
            string localDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (!Directory.Exists($"{localDataPath}\\QBO_Invoice_Parser")) Directory.CreateDirectory($"{localDataPath}\\QBO_Invoice_Parser");

            XmlSerializer serializer = new XmlSerializer(typeof(InvoiceTemplate));
            TextWriter writer = new StreamWriter($"{localDataPath}\\QBO_Invoice_Parser\\template_{template.Vendor.DisplayName}.xml");
            
            serializer.Serialize(writer, template);
            writer.Close();

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

        private void Template_Click(object sender, MouseButtonEventArgs e)
        {
            var template = sender;
            //if template vendor equals 
        }

        private void AddNewTemplate()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "PDF Files|*.pdf";

            Nullable<bool> result = openFileDialog.ShowDialog();

            // Process open file dialog box results
            if (result == true)
            {
                InvoiceTemplateViewer itr = new InvoiceTemplateViewer(openFileDialog.FileName);
                itr.Show();
            }
        }
    }
}