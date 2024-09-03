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

namespace RDPServer
{
    public partial class Form1 : Form
    {
        WaveIn wave;
        Socket server, audio, video, control;
        Thread serverThread, controlThread, videoThread;
        Int32 port;
        EndPoint client;

        private class AudioDevice
        {
            public int deviceId;
            public string name;

            public override string ToString()
            {
                return name;
            }
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            Environment.Exit(0);
        }

        public Form1()
        {
            port = 31570;
            InitializeComponent();
            server = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            serverThread = new Thread(new ThreadStart(serverListen));
            int waveInDevices = WaveIn.DeviceCount;
            for (int waveInDevice = 0; waveInDevice < waveInDevices; waveInDevice++)
            {
                WaveInCapabilities deviceInfo = WaveIn.GetCapabilities(waveInDevice);
                //Console.WriteLine("Device {0}: {1}, {2} channels", waveInDevice, deviceInfo.ProductName, deviceInfo.Channels);
                listAudio.Items.Add(new AudioDevice { deviceId = waveInDevice, name = deviceInfo.ProductName });
            }
            serverThread.Start();
            wave = new WaveIn();
            wave.WaveFormat = new WaveFormat(8000, 16, 2);
        }

        private void serverListen()
        {
            IPEndPoint localIP = new IPEndPoint(IPAddress.Parse("127.0.0.1"), port);
            server.Bind(localIP);
            EndPoint remoteIp = new IPEndPoint(IPAddress.Any, 0);
            while (true)
            {
                try
                {
                    byte[] vs = new byte[256];
                    int recieved = server.ReceiveFrom(vs, ref remoteIp);
                    if (recieved > 0)
                    {
                        if (checkBoxAuto.Checked || MessageBox.Show("Allow access from " + remoteIp + " to your PC?", remoteIp.ToString(),MessageBoxButtons.YesNo) == DialogResult.Yes)
                        {
                            // lets connect
                            client = remoteIp;
                            if (listAudio.SelectedItem != null)
                                wave.DeviceNumber = ((AudioDevice)listAudio.SelectedItem).deviceId;
                            audio = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                            video = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                            control = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                            controlThread = new Thread(new ThreadStart(controlListen));
                            controlThread.Start();
                            videoThread = new Thread(new ThreadStart(videoSend));
                            videoThread.Start();
                            wave.DataAvailable += Voice_Input;
                            wave.StartRecording();
                            status.Text = "connected to " + remoteIp;
                            return;
                        }
                    }
                }
                catch (SocketException)
                {
                    continue;
                }
            }
        }

        private void Voice_Input(object sender, WaveInEventArgs e)
        {
            try
            {
                //Подключаемся к удаленному адресу
                IPEndPoint remote_point = new IPEndPoint(((IPEndPoint)client).Address, port + 1);
                //посылаем байты, полученные с микрофона на удаленный адрес
                audio.SendTo(e.Buffer, e.BytesRecorded, SocketFlags.None, remote_point);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void controlListen()
        {

        }

        private void videoSend()
        {

        }
    }
}
