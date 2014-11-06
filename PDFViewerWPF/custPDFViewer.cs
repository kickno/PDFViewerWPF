using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PDFViewerWPF
{
    public partial class custPDFViewer : UserControl
    {

        private string pdfFilePath;
        public custPDFViewer(string filename)
        {
            InitializeComponent();
            axAcroPDF1.setShowToolbar(false);
            axAcroPDF1.setView("FitH");
            axAcroPDF1.LoadFile(filename);
        }
        public void GoToNextPage()
        {
            axAcroPDF1.gotoNextPage();
        }
               
    }

}
