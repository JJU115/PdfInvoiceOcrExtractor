using System.Drawing;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using Ghostscript.NET.Rasterizer;
using System.IO;
using Intuit.Ipp.Data;
using System.Xml.Serialization;
using Microsoft.Win32;
using System.Windows.Controls;
using System.Collections.ObjectModel;
using System.Windows.Media;
using System.Linq;
using System.Diagnostics;


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
            templateList.MouseDown += Template_Click;

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
                it.Display.DecodePixelHeight = 200;
                return it;
            });
            List<InvoiceTemplate> l = list.ToList();
            InvoiceTemplate addTemplate = new InvoiceTemplate();
            string fileName = @"C:\Users\Justin\source\repos\WpfOcrInvoiceExtractor\WpfOcrInvoiceExtractor\testimages\add-template.png";
            Uri uri = new Uri(fileName, UriKind.RelativeOrAbsolute);
            addTemplate.Display = new BitmapImage(uri);
            addTemplate.Vendor = new Vendor { DisplayName = "NewVendor" };
            addTemplate.HideButtons = "Hidden";
            l.Insert(0, addTemplate);
            return l;
        }

        private void WriteTemplateToData(InvoiceTemplate template)
        {
            string localDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (!Directory.Exists($"{localDataPath}\\QBO_Invoice_Parser")) Directory.CreateDirectory($"{localDataPath}\\QBO_Invoice_Parser");

            XmlSerializer serializer = new XmlSerializer(typeof(InvoiceTemplate));
            TextWriter writer = new StreamWriter($"{localDataPath}\\QBO_Invoice_Parser\\template_{template.Vendor.DisplayName}.xml");
            
            serializer.Serialize(writer, template);
            writer.Close();
            invoiceTemplates.Add(template);
        }


        private void Template_Click(object sender, MouseButtonEventArgs e)
        {
            var template = ((ItemsControl)sender).Items.CurrentItem as InvoiceTemplate;
            if (template.Vendor.DisplayName == "NewVendor") AddNewTemplate();
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
                    
                    regionViewer = new RegionViewer(templateViewer.imageRegions);
                    bool? regionViewerResult = regionViewer.ShowDialog();

                    if (regionViewerResult == true) {

                        InvoiceTemplate template = new() { ImageRegions = regionViewer.imageSources, Vendor = new Vendor(), Display = templateDisplay };
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

        }
    }
}