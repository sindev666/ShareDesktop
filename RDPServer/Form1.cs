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
using System.IO;
using System.Runtime.InteropServices;

namespace RDPServer
{
    public partial class Form1 : Form
    {
        WaveIn wave;
        Socket server, audio, video, control;
        Thread serverThread, controlThread, videoThread;
        Int32 port, rnd;
        EndPoint client;
        DateTime watchdog;
        string statusText;
        bool alive = false;

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
            port = int.Parse(System.Configuration.ConfigurationSettings.AppSettings["port"]);
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
            checkBoxControl.Checked = RDPServer.Properties.Settings.Default.RemoteControl;
            checkBoxAuto.Checked = Properties.Settings.Default.AutoConnections;
            InitScreenshot();
        }

        private void serverListen()
        {
            IPEndPoint localIP = new IPEndPoint(IPAddress.Any, port);
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
                            alive = true;
                            // lets connect
                            client = remoteIp;
                            audio = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                            video = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                            //control = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                            controlThread = new Thread(new ThreadStart(controlListen));
                            controlThread.Start();
                            videoThread = new Thread(new ThreadStart(videoSend));
                            videoThread.Start();
                            wave.DataAvailable += Voice_Input;
                            wave.StartRecording();
                            watchdog = DateTime.Now;
                            statusText = "connected to " + remoteIp;
                            serverThread = null;
                            server.Close();
                            rnd = BitConverter.ToInt32(vs, 0);
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

        #region Control
        [StructLayout(LayoutKind.Sequential)]
        public struct KeyboardInput
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct HardwareInput
        {
            public uint uMsg;
            public ushort wParamL;
            public ushort wParamH;
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct MouseInput
        {
            public int dx;
            public int dy;
            public uint mouseData;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }
        [StructLayout(LayoutKind.Explicit)]
        public struct InputUnion
        {
            [FieldOffset(0)] public MouseInput mi;
            [FieldOffset(0)] public KeyboardInput ki;
            [FieldOffset(0)] public HardwareInput hi;
        }
        public struct Input
        {
            public int type;
            public InputUnion u;
        }
        [Flags]
        public enum InputType
        {
            Mouse = 0,
            Keyboard = 1,
            Hardware = 2
        }
        [Flags]
        public enum KeyEventF
        {
            KeyDown = 0x0000,
            ExtendedKey = 0x0001,
            KeyUp = 0x0002,
            Unicode = 0x0004,
            Scancode = 0x0008
        }
        [Flags]
        public enum MouseEventF
        {
            Absolute = 0x8000,
            HWheel = 0x01000,
            Move = 0x0001,
            MoveNoCoalesce = 0x2000,
            LeftDown = 0x0002,
            LeftUp = 0x0004,
            RightDown = 0x0008,
            RightUp = 0x0010,
            MiddleDown = 0x0020,
            MiddleUp = 0x0040,
            VirtualDesk = 0x4000,
            Wheel = 0x0800,
            XDown = 0x0080,
            XUp = 0x0100
        }
        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint nInputs, Input[] pInputs, int cbSize);
        [DllImport("user32.dll")]
        private static extern IntPtr GetMessageExtraInfo();
        [DllImport("User32.dll")]
        public static extern bool SetCursorPos(int x, int y);
        #endregion

        private int genSecure()
        {
            return rnd ^= (rnd + (rnd >> 16));
        }

        private async void controlListen()
        {
            EndPoint end = new IPEndPoint(IPAddress.Any, port + 3);
            //control.Bind(end);
            //control.Listen(10);
            //TcpListener tcp = new TcpListener(port + 3);
            UdpClient udp = new UdpClient(port + 3);
            //tcp.AllowNatTraversal(false);
            //tcp.Start();
            while (alive)
            {
                try
                {
                    //TcpClient client = tcp.AcceptTcpClient();
                    //int len = client.GetStream().Read(vs, 0, 16);
                    var req = await udp.ReceiveAsync();
                    byte[] vs = req.Buffer;
                    if (vs.Length != 16)
                        continue;
                    Int32 oper = BitConverter.ToInt32(vs, 0),
                        x = BitConverter.ToInt32(vs, 4),
                        y = BitConverter.ToInt32(vs, 8),
                        p = BitConverter.ToInt32(vs, 12);
                    if (oper == 17)
                    {
                        watchdog = DateTime.Now;
                        statusText = "connected";
                    }
                    else if (oper == 24)
                    {
                        SetCursorPos(x, y);
                    }
                    else if (oper == 33)
                    {
                        Input[] inp = new Input[]
                        {
                            new Input
                            {
                                type = (int)InputType.Keyboard,
                                u = new InputUnion
                                {
                                    ki = new KeyboardInput
                                    {
                                        wVk = (ushort)y,
                                        wScan = (ushort)x, // W
                                        dwFlags = (uint)(KeyEventF.KeyDown | (x!=0?KeyEventF.Scancode:KeyEventF.ExtendedKey)),
                                        dwExtraInfo = GetMessageExtraInfo()
                                    }
                                }
                            }
                        };
                        SendInput(1, inp, Marshal.SizeOf(typeof(Input)));
                    }
                    else if (oper==73)
                    {
                        Input[] inp = new Input[]
                        {
                            new Input
                            {
                                type = (int)InputType.Keyboard,
                                u = new InputUnion
                                {
                                    ki = new KeyboardInput
                                    {
                                        wVk = (ushort)y,
                                        wScan = (ushort)x, // W
                                        dwFlags = (uint)(KeyEventF.KeyUp | (x!=0?KeyEventF.Scancode:KeyEventF.ExtendedKey)),
                                        dwExtraInfo = GetMessageExtraInfo()
                                    }
                                }
                            }
                        };
                        SendInput(1, inp, Marshal.SizeOf(typeof(Input)));
                    }
                    else if (oper == 55)
                    {
                        SetCursorPos(2 * x, 2 * y);
                    }
                    else if (oper == 117)
                    {
                        Input[] inputs = new Input[]
                        {
                            new Input
                            {
                                type = (int) InputType.Mouse,
                                u = new InputUnion
                                {
                                    mi = new MouseInput
                                    {
                                        dwFlags = (uint)(p==1?MouseEventF.LeftUp:MouseEventF.LeftDown),
                                        dwExtraInfo = GetMessageExtraInfo()
                                    }
                                }
                            }
                        };
                        SendInput(1, inputs, Marshal.SizeOf(typeof(Input)));
                    }
                    else if (oper == 119)
                    {
                        Input[] inputs = new Input[]
                        {
                            new Input
                            {
                                type = (int) InputType.Mouse,
                                u = new InputUnion
                                {
                                    mi = new MouseInput
                                    {
                                        dwFlags = (uint)(p==1?MouseEventF.RightUp:MouseEventF.RightDown),
                                        dwExtraInfo = GetMessageExtraInfo()
                                    }
                                }
                            }
                        };
                        SendInput(1, inputs, Marshal.SizeOf(typeof(Input)));
                    }
                    else if (oper == 127)
                    {
                        Input[] inputs = new Input[]
                        {
                            new Input
                            {
                                type = (int) InputType.Mouse,
                                u = new InputUnion
                                {
                                    mi = new MouseInput
                                    {
                                        mouseData= (uint)p,
                                        dwFlags = (uint)MouseEventF.Wheel,
                                        dwExtraInfo = GetMessageExtraInfo()
                                    }
                                }
                            }
                        };
                        SendInput(1, inputs, Marshal.SizeOf(typeof(Input)));
                    }
                    //Socket socket = control.Accept();
                    //byte[] vs = new byte[256];
                    //int len = socket.Receive(vs);
                    //byte[] cache = new byte[len];
                    //for (int i = 0; i < len; i++)
                    //{
                    //    cache[i] = vs[i];
                    //}
                    //string s = Encoding.Default.GetString(cache);
                    ////MessageBox.Show(s);
                    //switch (s)
                    //{
                    //    case "watchdog":
                    //        {
                    //            watchdog = DateTime.Now;
                    //            statusText = "connected";
                    //        }
                    //        break;

                    //}
                    //socket.Close();

                }
                catch (SocketException) { }
            }
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
            bool need = (RDPServer.Properties.Settings.Default.RemoteControl != checkBoxControl.Checked) ||
                (Properties.Settings.Default.AutoConnections != checkBoxAuto.Checked);
            RDPServer.Properties.Settings.Default.RemoteControl = checkBoxControl.Checked;
            Properties.Settings.Default.AutoConnections = checkBoxAuto.Checked;
            if (need)
            {
                Properties.Settings.Default.Save();
            }
            if (serverThread == null && (DateTime.Now - watchdog) > TimeSpan.FromSeconds(1.5))
            {
            //    alive = false;
            //    wave.StopRecording();
            //server = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            //    videoThread.Abort();
            //    controlThread.Interrupt();
            //    control.Close();
            //    serverThread = new Thread(new ThreadStart(serverListen));
            //    serverThread.Start();
                statusText = "disconnected";
            }
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
                BitmapData bmpData = bmp.LockBits(new Rectangle(Point.Empty, bmp.Size), ImageLockMode.WriteOnly, bmp.PixelFormat);
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
            IPEndPoint end = new IPEndPoint(((IPEndPoint)client).Address, port + 2);
            while (true)
            {
                Bitmap bmp = TakeScreenshot();
                //System.Drawing.Imaging.BitmapData bmpData = bmp.LockBits(new Rectangle(Point.Empty, bmp.Size), ImageLockMode.ReadOnly, bmp.PixelFormat);
                //System.IO.MemoryStream memoryStream = new System.IO.MemoryStream();
                //bmp.Save(memoryStream, ImageFormat.Bmp);
                try
                {
                        UdpClient client = new UdpClient();
                        client.Connect(end);
                        using (var ms = new MemoryStream())
                        {
                            Bitmap bmp2 = new Bitmap(bmp, 720, 450);
                            //using (var gr = Graphics.FromImage(bmp2))
                            //{
                            //    gr.DrawImage(bmp, new Point { X = id % 2 == 1 ? -480 : 0, Y = id >= 2 ? -300 : 0 });
                            //}
                            //ms.WriteByte((byte)id);
                            bmp2.Save(ms, ImageFormat.Jpeg);
                            var bytes = ms.ToArray();
                            client.Send(bytes, bytes.Length);
                        }
                        client.Close();
                        //video.Connect(end);
                        //video.Send(memoryStream.GetBuffer());
                        Thread.Sleep(250 / (int)FPSControl.Value);
                    //video.Close();
                    //video = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                }
                catch (Exception)
                {
                    //video = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    Thread.Sleep(1000 / (int)FPSControl.Value);
                }
            }
        }
    }
}
