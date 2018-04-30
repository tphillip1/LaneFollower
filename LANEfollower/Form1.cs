using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using Emgu.CV;
using Emgu.CV.Structure;
using System.IO.Ports;
using System.Diagnostics;

namespace LANEfollower
{
    public partial class Form1 : Form
    {
        private VideoCapture _capture;
        private Thread _captureThread;
        SerialPort _serialPort = new SerialPort("COM5", 2400);

        const byte STOP = 0x7F;
        const byte FLOAT = 0x0F;
        const byte FORWARD = 0x6f;
        const byte BACKWARD = 0x5F;
        byte left = FORWARD;
        byte right = FORWARD;

        int t1, t2; //Threshold values
        int lpercent = 0;//left white count %
        int cpercent = 0;//center white count %
        int rpercent = 0;//right white count %
        int redpercent = 0;//red count %
        
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            _capture = new VideoCapture();
            _captureThread = new Thread(DisplayWebcam);
            _captureThread.Start();

            _serialPort.DataBits = 8;
            _serialPort.Parity = Parity.None;
            _serialPort.StopBits = StopBits.Two;
            _serialPort.Open();
        }

        private void DisplayWebcam()
        {
            while (_capture.IsOpened)
            {
                Mat frame = _capture.QueryFrame();
                CvInvoke.Resize(frame, frame, pictureBox1.Size);
                Image<Gray, Byte> grayImage = frame.ToImage<Gray, Byte>();//gray image
                Image<Hsv, byte> red = frame.ToImage<Bgr, byte>().Convert<Hsv, byte>(); //change for red
                Image<Gray, Byte>[] channels = red.Split();

                channels[2] = channels[0].ThresholdBinaryInv(new Gray(65), new Gray(255));
                channels[2] &= channels[1].ThresholdBinary(new Gray(65), new Gray(255));

                grayImage = grayImage.ThresholdBinary(new Gray(t1), new Gray(t2));

                int redCount = 0;
                for (int x = 0; x < grayImage.Width; x++)
                {
                    for (int y = 0; y < grayImage.Height; y++)
                    {
                        if (channels[2].Data[y, x, 0] == 255)
                            redCount++;
                    }
                }
                redpercent = (redCount * 100) / (grayImage.Width * grayImage.Height);//converts red count into a %

                label6.Invoke(new Action(() => label6.Text = redpercent.ToString()));//outputs red % to screen

                int leftWhiteCount = 0;
                for (int x = 0; x < grayImage.Width / 3; x++)//calculates white pixals in the left (1/3) of screen
                {
                    for (int y = 0; y < grayImage.Height; y++)
                    {
                        if (grayImage.Data[y, x, 0] == 255)
                            leftWhiteCount++;
                    }
                }
                lpercent = (leftWhiteCount * 100) / ((grayImage.Width / 3) * grayImage.Height);//converts left white count to a %

                label3.Invoke(new Action(() => label3.Text = lpercent.ToString()));//outputs left % to screen

                int centerWhiteCount = 0;
                for (int x = grayImage.Width / 3; x < (2 * grayImage.Width) / 3; x++)//calculates white pixals in the center (2/3) of screen
                {
                    for (int y = 0; y < grayImage.Height; y++)
                    {
                        if (grayImage.Data[y, x, 0] == 255)
                            centerWhiteCount++;
                    }
                }
                cpercent = (centerWhiteCount * 100) / ((grayImage.Width / 3) * grayImage.Height);//converts center white count to a %

                label4.Invoke(new Action(() => label4.Text = cpercent.ToString()));//outputs center % to screen

                int rightWhiteCount = 0;
                for (int x = (2 * grayImage.Width) / 3; x < grayImage.Width; x++)//calculates white pixals in the right (3/3) of screen
                {
                    for (int y = 0; y < grayImage.Height; y++)
                    {
                        if (grayImage.Data[y, x, 0] == 255)
                            rightWhiteCount++;
                    }
                }
                rpercent = (rightWhiteCount * 100) / ((grayImage.Width / 3) * grayImage.Height);//converts right white count to a %

                label5.Invoke(new Action(() => label5.Text = rpercent.ToString()));//outputs right % to screen
                
                if (redpercent > 13)
                {
                    byte[] buffer = { 0x01, FORWARD, BACKWARD };
                    _serialPort.Write(buffer, 0, 3);

                    Thread.Sleep(1500);//this stopped the program to allow the robot to turn       
                }
                             
                if (leftWhiteCount > centerWhiteCount & rightWhiteCount > centerWhiteCount)//lpercent > 10 & rpercent > 10)
                {
                    byte[] buffer = { 0x01, FORWARD, FORWARD };//Go straight when both left and right are close or the same white count
                    _serialPort.Write(buffer, 0, 3);
                }
                else if (leftWhiteCount > rightWhiteCount)//lpercent > rpercent)
                {
                    byte[] buffer = { 0x01, STOP, FORWARD };//Go right when it is too far to the left
                    _serialPort.Write(buffer, 0, 3);
                }

                else if (rightWhiteCount > leftWhiteCount)//rpercent > lpercent)
                {
                    byte[] buffer = { 0x01, FORWARD, STOP };//Go left when it is too far to the right
                    _serialPort.Write(buffer, 0, 3);
                }
                
                pictureBox1.Image = grayImage.ToBitmap();//gray image
                pictureBox2.Image = channels[2].ToBitmap();//red image
            }
        }

        private void Form1_FormClosing(object sender, FormClosedEventArgs e)
        {
            _captureThread.Abort();//this closes the webcam after closing the form box. Stops the code from running
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            t1 = int.Parse(textBox1.Text);//input threshold 1
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            t2 = int.Parse(textBox2.Text);//input threshold 2
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {

        }
    }
}
