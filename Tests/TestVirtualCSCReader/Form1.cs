using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using IFS2.Common.CoreTechnical;
using IFS2.Equipment.Common;
using IFS2.Equipment.TicketingRules;
using IFS2.Equipment.TicketingRules.CommonTT;
using IFS2.Equipment.CSCReader;
using IFS2.Equipment.CSCReaderAdaptor;

namespace TestVirtualCSCReader
{
    public partial class Form1 : Form
    {
        Reader reader = null;
        int readerReference = 1;
        public Form1()
        {
            InitializeComponent();
            reader = new Reader();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Reader.Start(false, false);
            readerReference=reader.getCscHandle();
            Reader.StartPolling(CSC_READER_TYPE.VIRTUAL_READER, readerReference, null);
        }
    }
}
