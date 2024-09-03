using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using NAudio.Wave;
using NAudio.CoreAudioApi;
using System.Threading;
using System.Net;
using System.Net.Sockets;

namespace RDPClient
{
    public partial class Form1 : Form
    {
        public Form1(IPAddress address)
        {
            InitializeComponent();
            
        }
    }
}
