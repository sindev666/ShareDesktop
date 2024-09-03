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
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System.Drawing.Imaging;

namespace RDPServer
{
    public partial class Form1 : Form
    {
        WaveIn wave;
        Socket server, audio, video, control;
        Thread serverThread, controlThread, videoThread;
        Int32 port;
        EndPoint client;
        string statusText;

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
            statusText = "not connected";
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
                            audio = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                            video = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                            control = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                            controlThread = new Thread(new ThreadStart(controlListen));
                            controlThread.Start();
                            videoThread = new Thread(new ThreadStart(videoSend));
                            videoThread.Start();
                            wave.DataAvailable += Voice_Input;
                            wave.StartRecording();
                            statusText = "connected to " + remoteIp;
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

        Factory1 factory;
        Adapter1 adapter;
        Output output;
        Output1 output1;
        Texture2DDescription textureDesc;
        Texture2D screenTexture;
        SharpDX.Direct3D11.Device device;

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (listAudio.SelectedItem != null)
                wave.DeviceNumber = ((AudioDevice)listAudio.SelectedItem).deviceId;
            status.Text = statusText;
        }

        OutputDuplication duplicatedOutput;
        int width, height;

        private void InitScreenshot()
        {
            factory = new Factory1();
            adapter = factory.GetAdapter1(0);
            Console.WriteLine(adapter.Description1.Description);
            device = new SharpDX.Direct3D11.Device(adapter);
            output = adapter.GetOutput(0);
            Console.WriteLine(output.Description.DeviceName);
            output1 = output.QueryInterface<Output1>();

            width = output.Description.DesktopBounds.Right;
            height = output.Description.DesktopBounds.Bottom;

            textureDesc = new Texture2DDescription
            {
                CpuAccessFlags = CpuAccessFlags.Read,
                BindFlags = BindFlags.None,
                Format = Format.B8G8R8A8_UNorm,
                Width = width,
                Height = height,
                OptionFlags = ResourceOptionFlags.None,
                MipLevels = 1,
                ArraySize = 1,
                SampleDescription = { Count = 1, Quality = 0 },
                Usage = ResourceUsage.Staging
            };
            screenTexture = new Texture2D(device, textureDesc);
            duplicatedOutput = output1.DuplicateOutput(device);
            Thread.Sleep(20); // захватчику экрана надо время проинициализироваться
        }

        private Bitmap TakeScreenshot()
        {
            Bitmap bmp = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            SharpDX.DXGI.Resource screenResource = null;
            try
            {
                if (duplicatedOutput.TryAcquireNextFrame(10, out OutputDuplicateFrameInformation duplicateFrameInformation, out screenResource) != Result.Ok)
                    return bmp;

                using (Texture2D screenTexture2D = screenResource.QueryInterface<Texture2D>())
                {
                    device.ImmediateContext.CopyResource(screenTexture2D, screenTexture);
                }

                DataBox mapSource = device.ImmediateContext.MapSubresource(screenTexture, 0, MapMode.Read, SharpDX.Direct3D11.MapFlags.None);
                System.Drawing.Imaging.BitmapData bmpData = bmp.LockBits(new Rectangle(Point.Empty, bmp.Size), ImageLockMode.WriteOnly, bmp.PixelFormat);
                IntPtr sourcePtr = mapSource.DataPointer;
                IntPtr destPtr = bmpData.Scan0;
                Utilities.CopyMemory(destPtr, sourcePtr, mapSource.RowPitch * height);
                bmp.UnlockBits(bmpData);
                device.ImmediateContext.UnmapSubresource(screenTexture, 0);
                duplicatedOutput.ReleaseFrame();
            }
            catch (SharpDXException ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                screenResource?.Dispose();
            }
            return bmp;
        }

        private void videoSend()
        {
            EndPoint end = new IPEndPoint(((IPEndPoint)client).Address, port + 2);
            InitScreenshot();
            while (true)
            {
                Bitmap bmp = TakeScreenshot();
                //System.Drawing.Imaging.BitmapData bmpData = bmp.LockBits(new Rectangle(Point.Empty, bmp.Size), ImageLockMode.ReadOnly, bmp.PixelFormat);
                System.IO.MemoryStream memoryStream = new System.IO.MemoryStream();
                bmp.Save(memoryStream, ImageFormat.Bmp);
                try
                {
                    video.Connect(end);
                    video.Send(memoryStream.GetBuffer());
                    Thread.Sleep(1000 / (int)FPSControl.Value);
                    video.Disconnect(true);
                }
                catch (Exception)
                {
                    video = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    Thread.Sleep(1000 / (int)FPSControl.Value);
                }
            }
        }
    }
}
