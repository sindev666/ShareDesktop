using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RDPClient
{
    public partial class Connect : Form
    {
        Form1 form;
        public Connect()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Hide();
            form = new Form1(System.Net.IPAddress.Parse(textRemote.Text));
            form.ShowDialog();
            Show();
        }

        private void Connect_FormClosed(object sender, FormClosedEventArgs e)
        {
            Environment.Exit(0);
        }
    }
}
