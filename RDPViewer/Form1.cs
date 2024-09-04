using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;

namespace RDPViewer
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            var networks = System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces();
            foreach (var item in networks)
            {
                listBox1.Items.Add(item.Name);
            }
            name.Width = 250;
            IP.Width = 100;
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
        }

        private void Form1_KeyPress(object sender, KeyPressEventArgs e)
        {
            
        }

        private class AddressInfo
        {
            public string Name, IP, ping, rdp;
        }

        private async Task<AddressInfo> GetInfoAsync(string address)
        {
            string Name = "";
            try
            {
                IPHostEntry info = await Dns.GetHostEntryAsync(address);
                Name = info.HostName;
            }
            catch (Exception) { }
            var asker = new System.Net.NetworkInformation.Ping();
            System.Net.NetworkInformation.PingReply res = await Task.Run(() => asker.Send(address, 150));
            if (res.Status != System.Net.NetworkInformation.IPStatus.Success && Name == "")
                return null;
            string ping = res.RoundtripTime.ToString() + "ms";
            if (res.Status != System.Net.NetworkInformation.IPStatus.Success)
                ping = "?";
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.SendTimeout = 150;
            byte[] vs = Encoding.Default.GetBytes("watchdog");
            bool ok = true;
            try
            {
                socket.SendTo(vs, new IPEndPoint(IPAddress.Parse(address), 31573));
            }
            catch (Exception)
            {
                ok = false;
            }
            string rdp = ok ? "Active" : "Unknown";
            return new AddressInfo { Name = Name, IP = address, ping = ping, rdp = rdp };
        }

        private async void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyData == Keys.F5 || e.KeyData == Keys.F6)
            {
                var networks = System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces();
                var b = networks[Math.Max(0, listBox1.SelectedIndex)].GetIPProperties();
                string subnet = b.DhcpServerAddresses[0].ToString();
                string net = subnet.Substring(0, subnet.LastIndexOf('.')) + '.';
                //var asker = new System.Net.NetworkInformation.Ping();
                //networks[0].GetIPStatistics().
                listView1.Items.Clear();
                int k = 100;
                List<Task<AddressInfo>> queue = new List<Task<AddressInfo>>();
                if (e.KeyData == Keys.F6)
                {
                    for (int i = 0; i < k; i++)
                    {
                        queue.Add(GetInfoAsync(net + i));
                    }
                }
                for (int i = 0; i < k; i++)
                {
                    //string Name = "";
                    //try
                    //{
                    //    IPHostEntry info = Dns.GetHostEntry(net + i);
                    //    Name = info.HostName;
                    //}
                    //catch (Exception) { }
                    //string IP = net + i;
                    //System.Net.NetworkInformation.PingReply res = asker.Send(net + i, 150);
                    //if (res.Status != System.Net.NetworkInformation.IPStatus.Success && Name == "")
                    //    continue;
                    //string ping = res.RoundtripTime.ToString() + "ms";
                    //if (res.Status != System.Net.NetworkInformation.IPStatus.Success)
                    //    ping = "?";
                    //Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    //socket.SendTimeout = 150;
                    //byte[] vs = Encoding.Default.GetBytes("watchdog");
                    //bool ok = true;
                    //try
                    //{
                    //    socket.SendTo(vs, new IPEndPoint(IPAddress.Parse(net + i), 31573));
                    //}
                    //catch (Exception)
                    //{
                    //    ok = false;
                    //}
                    //string rdp = ok ? "Active" : "Unknown";
                    var res = (e.KeyData == Keys.F6 ? await queue[i] : await GetInfoAsync(net + i));
                    if (res == null)
                        continue;
                    ListViewItem item = new ListViewItem();
                    if (res.ping != "?")
                        item.BackColor = Color.Green;
                    if (res.rdp == "Active")
                        item.BackColor = Color.Yellow;
                    item.Text = res.Name;
                    item.SubItems.Add(res.IP);
                    item.SubItems.Add(res.ping);
                    item.SubItems.Add(res.rdp);
                    listView1.Items.Add(item);
                }
            }
        }

        private void listBox1_SelectedValueChanged(object sender, EventArgs e)
        {
            propertyGrid1.SelectedObject = System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces()[listBox1.SelectedIndex];
        }

        private void копироватьIPToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count == 0)
                return;
            string elem = listView1.SelectedItems[0].SubItems[1].Text;
            Clipboard.SetText(elem);
        }
    }
}
