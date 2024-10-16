using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace WpfOcrInvoiceExtractor
{
    /// <summary>
    /// To report on the progress of bill uploads to QBO
    /// </summary>
    public partial class ProgressReporter : Window
    {
        public ProgressReporter()
        {
            InitializeComponent();
        }
    }
}
