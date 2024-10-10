using System.Drawing;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Ghostscript.NET.Rasterizer;
using System.IO;
using Intuit.Ipp.Data;
using System.Xml.Serialization;
using Microsoft.Win32;
using System.Windows.Controls;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Tesseract;


namespace WpfOcrInvoiceExtractor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        ObservableCollection<InvoiceTemplate> invoiceTemplates;
        InvoiceTemplateViewer? templateViewer;
        RegionViewer? regionViewer;

        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = this;

            invoiceTemplates = new ObservableCollection<InvoiceTemplate>(RetrieveTemplateData());
            templateList.ItemsSource = invoiceTemplates;

            this.Width = SystemParameters.PrimaryScreenWidth * 0.75;
            this.Height = SystemParameters.PrimaryScreenHeight * 0.75;
            this.KeyDown += MainWindow_KeyDown;
            
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
            if (e.Key == Key.K)
            {
                await KickServUtility.GetJobType("20005");
            }

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

            if (e.Key == Key.S)
            {
                string test = await KickServUtility.GetJobType("20005");
                Debug.WriteLine(test);
            }

            if (e.Key == Key.R)
            {
                
                RegionViewer rv = new(regionList, []);
                rv.Show();
            }
            else if (e.Key == Key.F) {
                Vendor wesco = new()
                {
                    Id = "58"
                };
               //await QBOUtility.CreateNewBillToQbo(new InvoiceTemplate());
            } else if (e.Key == Key.L)
            {
                this.RetrieveTemplateData();
            } else if (e.Key == Key.V)
            {
                List<Vendor> vendors = await QBOUtility.GetVendorList();
                Debug.WriteLine(vendors);
            }
        }

        public List<InvoiceTemplate> RetrieveTemplateData()
        {
            string localDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (!Directory.Exists($"{localDataPath}\\QBO_Invoice_Parser")) Directory.CreateDirectory($"{localDataPath}\\QBO_Invoice_Parser");
            
            XmlSerializer serializer = new XmlSerializer(typeof(InvoiceTemplate));
            var list = Directory.GetFiles($"{localDataPath}\\QBO_Invoice_Parser").Where(f => f.EndsWith("xml")).Select(f =>
            {
                FileStream fs = new FileStream(f, FileMode.Open);
                InvoiceTemplate it = (InvoiceTemplate)serializer.Deserialize(fs);
                it.ImageRegions.ForEach(ir => ir.Image = new CroppedBitmap(it.Display, ir.SourceRegion));
                it.Display.DecodePixelHeight = 200;
                return it;
            });
            List<InvoiceTemplate> l = list.ToList();
            InvoiceTemplate addTemplate = new InvoiceTemplate();
            string fileName = @"C:\Users\Justin\source\repos\WpfOcrInvoiceExtractor\WpfOcrInvoiceExtractor\testimages\add-template.png";
            Uri uri = new Uri(fileName, UriKind.RelativeOrAbsolute);
            addTemplate.Display = new BitmapImage(uri);
            addTemplate.Vendor = new Vendor { DisplayName = "New Vendor Invoice Template" };
            l.Insert(0, addTemplate);
            return l;
        }

        private void WriteTemplateToData(InvoiceTemplate template)
        {
            string localDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (!Directory.Exists($"{localDataPath}\\QBO_Invoice_Parser")) Directory.CreateDirectory($"{localDataPath}\\QBO_Invoice_Parser");

            XmlSerializer serializer = new XmlSerializer(typeof(InvoiceTemplate));
            TextWriter writer = new StreamWriter($"{localDataPath}\\QBO_Invoice_Parser\\template_{template.Vendor.DisplayName}_{template.Vendor.Id}.xml");
            
            serializer.Serialize(writer, template);
            writer.Close();
            invoiceTemplates.Add(template);
        }

        private void Template_Click(object sender, MouseButtonEventArgs e)
        {
            var template = (InvoiceTemplate)(sender as System.Windows.Controls.Image).DataContext;
            if (template.Vendor.DisplayName == "New Vendor Invoice Template") AddNewTemplate();
            else
            {

            }
        }

        private void AddNewTemplate()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "PDF Files|*.pdf";

            bool? result = openFileDialog.ShowDialog();

            // Process open file dialog box results
            if (result == true)
            {
                templateViewer = new InvoiceTemplateViewer(openFileDialog.FileName);
                bool? viewerResult = templateViewer.ShowDialog();


                if (viewerResult == true) {
                    JpegBitmapEncoder encoder = new();
                    MemoryStream memoryStream = new();
                    BitmapImage templateDisplay = new BitmapImage();

                    encoder.Frames.Add(BitmapFrame.Create(templateViewer.invoiceDisplay));
                    encoder.Save(memoryStream);
                    memoryStream.Position = 0;
              
                    templateDisplay.BeginInit();
                    templateDisplay.StreamSource = memoryStream;
                    templateDisplay.EndInit();

                    regionViewer = new RegionViewer(templateViewer.imageRegions, this.invoiceTemplates.Select(it => it.Vendor.Id).ToList());
                    bool? regionViewerResult = regionViewer.ShowDialog();

                    if (regionViewerResult == true) {

                        InvoiceTemplate template = new() { ImageRegions = regionViewer.imageSources, Vendor = regionViewer.selectedVendor, VendorOCRResult = regionViewer.vendorOCRResult, Display = templateDisplay };
                        WriteTemplateToData(template);
                    }
                    memoryStream.Close();
                }
            }
        }

        private void Edit_Template(object sender, RoutedEventArgs e)
        {

        }

        private void Delete_Template(object sender, RoutedEventArgs e)
        {
            string localDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (!Directory.Exists($"{localDataPath}\\QBO_Invoice_Parser"))
            {
                Directory.CreateDirectory($"{localDataPath}\\QBO_Invoice_Parser");
                return;
            }
            InvoiceTemplate template = (InvoiceTemplate)(sender as Button).DataContext;
            File.Delete($"{localDataPath}\\QBO_Invoice_Parser\\template_{template.Vendor.DisplayName}_{template.Vendor.Id}.xml");
            invoiceTemplates.Remove(template);
        }

        private async void NewImportButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new()
            {
                Multiselect = true,
                Filter = "PDF Files|*.pdf"
            };

            bool? result = openFileDialog.ShowDialog();

            if (result == true)
            {
                //These will all be grouped into one method later
                await QBOUtility.PopulateAccountsList();
                await QBOUtility.PopulateQBOClassList();
                await QBOUtility.PopulateQBOTaxCodesList();

                List<Bitmap> pdfImages = ConvertPdfsToImages(openFileDialog.FileNames);
                List<string> failed = new(openFileDialog.FileNames);

                for (int b=0; b<pdfImages.Count; b++) {
                    Bitmap bmp = pdfImages[b];
                    InvoiceTemplate? templateMatch = null;

                    foreach (InvoiceTemplate template in invoiceTemplates.Skip(1))
                    {
                        var vendorReg = template.ImageRegions.Find(reg => reg.BillTopic == BillTopic.Vendor)!.SourceRegion;
                        var page = QBOUtility.engine.Process(bmp, new Tesseract.Rect(vendorReg.X, vendorReg.Y, vendorReg.Width, vendorReg.Height));
                        string ocr = page.GetText().Trim();
                        page.Dispose();
                        if (ocr == template.VendorOCRResult)
                        {
                            templateMatch = template;
                            break;
                        }
                    }

                    if (templateMatch != null)
                    {
                        ImageRegion tableRegion = templateMatch.ImageRegions.Find(ir => ir.BillTopic == BillTopic.ItemsTable)!;
                        failed.RemoveAt(b - (pdfImages.Count - failed.Count));
                        await QBOUtility.CreateNewBillToQbo(bmp, templateMatch!); //Return a enum? Success, tax mismatch, failed to send...
                    }
                }

                if (failed.Count > 0)
                    MessageBox.Show($"Could not detect a saved template for the following files:\n{failed.Aggregate((acc, curr) => $"{acc}{curr}\n")}", "Invoices failed to process", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK);
            }
        }

        public List<Bitmap> ConvertPdfsToImages(string[] filePaths)
        {
            int desired_dpi = 300;
            List<Bitmap> pdfImages = new List<Bitmap>();

            using (var rasterizer = new GhostscriptRasterizer())
            {
                foreach (string path in filePaths)
                {
                    rasterizer.Open(path);
                    var img = rasterizer.GetPage(desired_dpi, 1);
                    pdfImages.Add(new Bitmap(img));
                }
            }
            return pdfImages;
        }
    }
}