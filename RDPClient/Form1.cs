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
        WaveOut wave;
        Socket audio, control;
        Thread audioThread, videoThread;
        BufferedWaveProvider bufferStream;
        DateTime watchdog;
        bool alive = true;
        Int32 port;
        IPEndPoint server;
        int vid = 0, aud = 0;

        private async void videoServer()
        {
            //Listener.Start();
            var client = new UdpClient(port + 2);
            while (true)
            {
                try
                {
                    //TcpClient client = await Listener.AcceptTcpClientAsync();
                    var data = await client.ReceiveAsync();
                    using (var ms = new System.IO.MemoryStream(data.Buffer))
                    {
                        //int id = ms.ReadByte();
                        Bitmap bmp = new Bitmap(ms);
                        Color c = bmp.GetPixel(0, 0);
                        if (c.B <= 10 && c.G <= 10 && c.R <= 10)
                            continue;
                        pictureBox1.Image = bmp;
                        toolStripStatus.Text = bmp.GetPixel(0, 0).ToString();
                    }
                    vid += data.Buffer.Length;
                }
                catch (Exception) { }
            }
        }

        private byte[] Concat(byte[] a,byte[] b)
        {
            byte[] l = new byte[a.Length + b.Length];
            for (int i = 0; i < a.Length; i++)
            {
                l[i] = a[i];
            }
            for (int i = 0; i < b.Length; i++)
            {
                l[i + a.Length] = b[i];
            }
            return l;
        }

        private bool sendInput(int oper,int param,int x,int y, string name)
        {
            toolStripStatus.Text = name;
            try
            {
                UdpClient client = new UdpClient();
                client.Connect(server);
                //await client.GetStream().WriteAsync(BitConverter.GetBytes(oper), 0, 4);
                //await client.GetStream().WriteAsync(BitConverter.GetBytes(x), 0, 4);
                //await client.GetStream().WriteAsync(BitConverter.GetBytes(y), 0, 4);
                //await client.GetStream().WriteAsync(BitConverter.GetBytes(param), 0, 4);
                byte[] vs = BitConverter.GetBytes(oper);
                vs = Concat(vs, BitConverter.GetBytes(x));
                vs = Concat(vs, BitConverter.GetBytes(y));
                vs = Concat(vs, BitConverter.GetBytes(param));
                client.Send(vs, 16);
                client.Close();
                return true;
            }
            catch (Exception)
            {
                toolStripStatus.Text += " error";
                return false;
            }
        }

        private async void Form1_Load(object sender, EventArgs e)
        {
            
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            Environment.Exit(0);
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            sendInput(33, 0, 0, e.KeyValue, "keyboard");
        }

        private void Form1_KeyUp(object sender, KeyEventArgs e)
        {
            sendInput(73, 0, 0, e.KeyValue, "keyboard");
        }

        private void Form1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
                sendInput(117, 0, 0, 0, "mousedown");
            if (e.Button == MouseButtons.Right)
                sendInput(119, 0, 0, 0, "mousedown");
        }

        private void Form1_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
                sendInput(117, 0, 0, 1, "mouseup");
            if (e.Button == MouseButtons.Right)
                sendInput(119, 0, 0, 1, "mouseup");
        }

        private void Form1_MouseMove(object sender, MouseEventArgs e)
        {
            int dx = ((Point)pictureBox1.Size).X, dy = ((Point)pictureBox1.Size).Y;
            int x = e.X * 720 / dx;
            int y = e.Y * 450 / dy;
            sendInput(55, 0, x, y, "mousemove");
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            toolStripConnection.Value = Math.Min(500, toolStripConnection.Value + vid + aud / 1024);
            //try
            //{
            //    byte[] vs = Encoding.Default.GetBytes("watchdog");
            //    control = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            //    control.Connect(server);
            //    control.Send(vs);
            //    control.Disconnect(false);
            //    toolStripStatus.Text = "watchdog";
            //}
            //catch (SocketException)
            //{
            //    //timer1.Enabled = false;
            //    //MessageBox.Show("Disconnected!");
            //    //Close();
            //    toolStripStatus.Text = "watchdog error";
            //    //alive = false;
            //    //audioThread.Interrupt();
            //    //videoThread.Interrupt();
            //    //video.Close();
            //    //audio.Close();
            //}
            sendInput(17, 0, 0, 0, "watchdog");
            try
            {
                Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                socket.Connect(new IPEndPoint(server.Address, port));
                byte[] buf = new byte[8];
                buf[0] = 35;
                socket.Send(buf);
                socket.Close();
            }
            catch (Exception) { }
            toolStripAudio.Text = "Audio: " + (aud / 1024) + " kbps";
            toolStripVideo.Text = "Video: " + (vid / 1024) + " kbps";
            toolStripConnection.Value /= 2;
            aud /= 2;
            vid /= 2;
        }

        public Form1(IPAddress address)
        {
            port = int.Parse(System.Configuration.ConfigurationSettings.AppSettings["port"]);
            InitializeComponent();
            Text = address.ToString();
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.Connect(new IPEndPoint(address, port));
            byte[] buf = new byte[8];
            buf[0] = 35;
            socket.Send(buf);
            socket.Close();
            wave = new WaveOut();
            //создаем поток для буферного потока и определяем у него такой же формат как и потока с микрофона
            bufferStream = new BufferedWaveProvider(new WaveFormat(8000, 16, 2));
            //привязываем поток входящего звука к буферному потоку
            wave.Init(bufferStream);
            audio = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            //Listener = new TcpListener(port + 2);
            //video = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            control = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            audioThread = new Thread(new ThreadStart(audioProc));
            audioThread.Start();
            videoThread = new Thread(new ThreadStart(videoServer));
            videoThread.Start();
            bufferStream.DiscardOnBufferOverflow = true;
            watchdog = DateTime.Now;
            server = new IPEndPoint(address, port + 3);
        }

        private void audioProc()
        {
            //Прослушиваем по адресу
            IPEndPoint localIP = new IPEndPoint(IPAddress.Any, port + 1);
            audio.Bind(localIP);
            //начинаем воспроизводить входящий звук
            wave.Play();
            //адрес, с которого пришли данные
            EndPoint remoteIp = new IPEndPoint(IPAddress.Any, 0);
            //бесконечный цикл
            while (alive)
            {
                try
                {
                    //промежуточный буфер
                    byte[] data = new byte[65535];
                    //получено данных
                    int received = audio.ReceiveFrom(data, ref remoteIp);
                    //добавляем данные в буфер, откуда output будет воспроизводить звук
                    bufferStream.AddSamples(data, 0, received);
                    aud += received;
                }
                catch (SocketException)
                { }
                catch (NAudio.MmException)
                { bufferStream.ClearBuffer(); }
            }
        }
    }
}
