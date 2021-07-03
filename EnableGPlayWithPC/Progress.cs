using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace EnableGPlayWithPC
{
    public partial class Progress : Form
    {
        public Progress()
        {
            InitializeComponent();
        }
        public string Title
        {
            get { return this.Text; }
            set
            {
                if (InvokeRequired)
                {
                    this.Invoke((MethodInvoker)delegate () { this.Text = value; });
                }
                else
                {
                    this.Text = value;
                }
            }
        }
        public string Message
        {
            get { return label1.Text; }
            set
            {
                if (InvokeRequired)
                {
                    this.Invoke((MethodInvoker)delegate () { label1.Text = value; });
                }
                else
                {
                    label1.Text = value;
                }
            }
        }
        public int Value
        {
            get { return progressBar1.Value; }
            set
            {
                if (InvokeRequired)
                {
                    this.Invoke((MethodInvoker)delegate () { progressBar1.Value = value; });
                }
                else
                {
                    progressBar1.Value = value;
                }
            }
        }
        public BackgroundWorker Worker
        {
            get;
            set;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Worker.CancelAsync();
        }
    }
}
