using Intuit.Ipp.Data;
using Intuit.Ipp.Utility;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.NetworkInformation;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Xml.Serialization;

namespace WpfOcrInvoiceExtractor
{
    public class InvoiceTemplate
    {
        public List<ImageRegion> ImageRegions { get; set; }

        public Vendor Vendor { get; set; }

        public string VendorOCRResult { get; set; }

        [XmlIgnore]
        public string HideButtons
        {
            get
            {
                return Vendor.DisplayName == "New Vendor Invoice Template" ? "Hidden" : "Visible";
            }
        }

        [XmlIgnore]
        public BitmapImage Display { get; set; }

        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        [XmlElement("Display")]
        public byte[] DisplaySerialized
        {
            get
            {
                JpegBitmapEncoder encoder = new();
                MemoryStream memoryStream = new();
                MemoryStream compressedStream = new();
               
                encoder.Frames.Add(BitmapFrame.Create(Display));
                encoder.Save(memoryStream);
                Display.StreamSource.CopyTo(memoryStream);

                //Consider compression here
                return memoryStream.ToArray();
            }
            set
            { 
                MemoryStream ms = new(value);
                Display = new BitmapImage();
                Display.BeginInit();
                Display.StreamSource = ms;
                Display.EndInit();
            }
        }
    }
}
