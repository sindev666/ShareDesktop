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
        Socket audio, video, control;
        Thread audioThread, videoThread;
        BufferedWaveProvider bufferStream;

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            Environment.Exit(0);
        }

        Int32 port;
        public Form1(IPAddress address)
        {
            port = 31570;
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
            video = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            control = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            audioThread = new Thread(new ThreadStart(audioProc));
            audioThread.Start();
            videoThread = new Thread(new ThreadStart(videoProc));
            videoThread.Start();
        }

        private void audioProc()
        {
            //Прослушиваем по адресу
            IPEndPoint localIP = new IPEndPoint(IPAddress.Parse("127.0.0.1"), port + 1);
            audio.Bind(localIP);
            //начинаем воспроизводить входящий звук
            wave.Play();
            //адрес, с которого пришли данные
            EndPoint remoteIp = new IPEndPoint(IPAddress.Any, 0);
            //бесконечный цикл
            while (true)
            {
                try
                {
                    //промежуточный буфер
                    byte[] data = new byte[65535];
                    //получено данных
                    int received = audio.ReceiveFrom(data, ref remoteIp);
                    //добавляем данные в буфер, откуда output будет воспроизводить звук
                    bufferStream.AddSamples(data, 0, received);
                }
                catch (SocketException)
                { }
            }
        }

        private void videoProc()
        {
            IPEndPoint localIP = new IPEndPoint(IPAddress.Parse("127.0.0.1"), port + 2);
            video.Bind(localIP);
            video.Listen(100);
            while (true)
            {
                Socket image = video.Accept();
                try
                {
                    Thread.Sleep(50);
                    System.IO.MemoryStream stream = new System.IO.MemoryStream();
                    while (image.Available > 0)
                    {
                        int len = image.Available;
                        byte[] vs = new byte[len];
                        image.Receive(vs);
                        stream.Write(vs, 0, len);
                    }
                    image.Close();
                    //byte[] vs = new byte[65535];
                    //while (image.Connected)
                    //{
                    //    int recieved = image.Receive(vs);
                    //    stream.Write(vs, 0, recieved);
                    //    if (recieved == 0)
                    //    {
                    //        Thread.Sleep(10);
                    //        if (image.Available == 0)
                    //        {
                    //            image.Send(vs);
                    //            image.Disconnect(true);
                    //            break;
                    //        }
                    //    }
                    //}
                    Bitmap bitmap = new Bitmap(stream);
                    Graphics gr = CreateGraphics();
                    gr.DrawImage(bitmap, new Point { X = 0, Y = 0 });
                }
                catch (Exception) { }
            }
        }
    }
}
