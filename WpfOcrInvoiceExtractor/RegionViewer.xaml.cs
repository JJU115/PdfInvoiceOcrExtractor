using Intuit.Ipp.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml.Serialization;
using Tesseract;

namespace WpfOcrInvoiceExtractor
{
    /// <summary>
    /// Presents all regions outlined by the user and allows them to remove/edit them for refinement
    /// Allow the running of the Tesseract engine on each snip as a test to let the user assess accuracy
    /// </summary>
    public partial class RegionViewer : Window
    {
        TesseractEngine engine = new TesseractEngine("./tessdata", "eng");
        public List<ImageRegion> imageSources = new List<ImageRegion>();
        public Vendor selectedVendor;
        public List<string> existingVendors = new List<string>();
        public string vendorOCRResult = "";
        int focusedRegion;

        public Dictionary<int, BillTopic> billTopicDict;// = new Dictionary<int, BillTopic>();

        public RegionViewer(List<ImageRegion> regions, List<string> existingVendors)
        {
            InitializeComponent();
            this.DataContext = this;
            this.Width = SystemParameters.PrimaryScreenWidth * 0.75;
            this.Height = SystemParameters.PrimaryScreenHeight;

            this.imageSources = regions;
            regionList.ItemsSource = this.imageSources;
            ImageEditorControl.ImageBitmap = new WriteableBitmap(imageSources[0].Image);
            focusedRegion = 0;
            ((RegionDataTemplateSelector)this.Resources["RegionDataTemplateSelector"]).SelectedIndex = 0;
            this.existingVendors = existingVendors;
            
            billTopicDict = new()
            {
                { 0, BillTopic.Amount }
            };
        }

        private void regionClicked(object sender, MouseButtonEventArgs e) {
            // Get the index of the clicked image - sender.content.index
            ImageRegion rgn = (ImageRegion)((ContentPresenter) sender).Content;
            var selector = new RegionDataTemplateSelector();
            selector.SelectedIndex = rgn.Index;
            regionList.ItemTemplateSelector = selector;
            ImageEditorControl.Invoice_SourceUpdated(new WriteableBitmap(imageSources[rgn.Index].Image));
            this.focusedRegion = rgn.Index;
            this.billTopics.SelectedIndex = (int)imageSources[focusedRegion].BillTopic;
        }

        private void runOCRTestOnCurrent(object sender, RoutedEventArgs e)
        {
            using (MemoryStream outStream = new())
            {
                BitmapEncoder enc = new BmpBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(this.imageSources[this.focusedRegion].Image));
                enc.Save(outStream);
                runOCREngine(Pix.LoadFromMemory(outStream.ToArray()));
            }
        }

        private string runOCREngine(Pix pix)
        {
            var page = engine.Process(pix);
            string ocr = page.GetText();
            Debug.WriteLine($"{ocr}");
            page.Dispose();
            return ocr;
        }

        private async void SaveRegions_Click(object sender, RoutedEventArgs e)
        {
            List<Vendor> vendors = await QBOUtility.GetVendorList();
            //Dialog to get user to select a vendor
            VendorSelectDialog vsd = new VendorSelectDialog(vendors.Where(v => !existingVendors.Contains(v.Id)).ToList());
            bool? vendorResult = vsd.ShowDialog();          
            if (vendorResult == true) {
                this.selectedVendor = (Vendor)vsd.vendorBox.SelectedItem;
                ImageRegion vendImg = imageSources.Find(iSrc => iSrc.BillTopic == BillTopic.Vendor)!; //Check if user has defined vendor image
                using (MemoryStream outStream = new())
                {
                    BitmapEncoder enc = new BmpBitmapEncoder();
                    enc.Frames.Add(BitmapFrame.Create(vendImg.Image));
                    enc.Save(outStream);
                    this.vendorOCRResult = runOCREngine(Pix.LoadFromMemory(outStream.ToArray())).Trim();
                }
                DialogResult = true;
                Close();
            }          
        }

        private void billTopics_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedTopic = (sender as ComboBox).SelectedIndex;
            imageSources[focusedRegion].BillTopic = (BillTopic)selectedTopic;
        }
    }

    public class RegionDataTemplateSelector : DataTemplateSelector
    {
        public int SelectedIndex { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            FrameworkElement element = container as FrameworkElement;

            if (element != null && item != null && item is ImageRegion)
            {
                ImageRegion region = item as ImageRegion;

                if (region.Index == SelectedIndex)
                    return
                        element.FindResource("selectedRegion") as DataTemplate;
                else
                    return
                        element.FindResource("standardRegion") as DataTemplate;
            }

            return null;
        }
    }
}
