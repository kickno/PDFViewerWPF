using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Expression.Interactivity.Core;

namespace PDFViewerWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        KinectSensor ksensor;
        InfraredFrameReader irReader;
        FrameDescription fd;
        ushort[] irData;
        byte[] irDataConverted;
        WriteableBitmap irBitmap;

        Body[] bodies;
        MultiSourceFrameReader msfr;
        DepthSpacePoint lastPoint;
        int detected = 0;
        Shape lastMarker;

        /// <summary>
        /// Maximum value (as a float) that can be returned by the InfraredFrame
        /// </summary>
        private const float InfraredSourceValueMaximum = (float)ushort.MaxValue;
        
        /// <summary>
        /// The value by which the infrared source data will be scaled
        /// </summary>
        private const float InfraredSourceScale = 0.75f;

        /// <summary>
        /// Smallest value to display when the infrared data is normalized
        /// </summary>
        private const float InfraredOutputValueMinimum = 0.01f;

        /// <summary>
        /// Largest value to display when the infrared data is normalized
        /// </summary>
        private const float InfraredOutputValueMaximum = 1.0f;


        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += MainPage_Loaded;
            this.Unloaded += MainPage_Unloaded;
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.DefaultExt = ".PDF"; // Default file extension
            dlg.Filter = "PDF documents (.PDF)|*.PDF"; // Filter files by extension 

            Nullable<bool> result = dlg.ShowDialog();

            // Process open file dialog box results 
            if (result == true)
            {
                // Open document 
                string filename = dlg.FileName;
                var uc = new custPDFViewer(filename);
                this.wPDFWinFormHost.Child = uc;

            }
          }


        /// <summary>
        /// INotifyPropertyChangedPropertyChanged event to allow window controls to bind to changeable data
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;
        void MainPage_Unloaded(object sender, RoutedEventArgs e)
        {
            if (irReader != null)
            {
                irReader.Dispose();
                irReader = null;
            }
            if (msfr != null)
            {
                msfr.Dispose();
                msfr = null;

            }
            if (ksensor != null)
            {
                ksensor.Close();
                ksensor = null;
            }
        }

        private void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            ksensor = KinectSensor.GetDefault();
            irReader = ksensor.InfraredFrameSource.OpenReader();
            fd = ksensor.InfraredFrameSource.FrameDescription;
            irData = new ushort[fd.LengthInPixels];
            irDataConverted = new byte[fd.LengthInPixels * 4];
            irBitmap = new WriteableBitmap(fd.Width, fd.Height, 96, 96, PixelFormats.Gray32Float, null);
            image.Source = irBitmap;
            bodies = new Body[6];
            msfr = ksensor.OpenMultiSourceFrameReader(FrameSourceTypes.Body | FrameSourceTypes.Infrared);
            msfr.MultiSourceFrameArrived += msfr_MultiSourceFrameArrived;
            ksensor.Open();
        }
       //  *** Only StoreApp with WindowsPreview.Kinect;
     //    private void msfr_MultiSourceFrameArrived(MultiSourceFrameReader sender, MultiSourceFrameArrivedEventArgs args)
        private void msfr_MultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
        {
         //   using (MultiSourceFrame msf = e.FrameReference.AcquireFrame())  *** Only StoreApp with WindowsPreview.Kinect;
           // {
            MultiSourceFrame msf = e.FrameReference.AcquireFrame();
                if (msf != null)
                {
                    using (BodyFrame bodyframe = msf.BodyFrameReference.AcquireFrame())
                    {
                        using (InfraredFrame irf = msf.InfraredFrameReference.AcquireFrame())
                        {
                            if (bodyframe != null && irf != null)
                            {
                                /* *** only with Windows.UI.Xaml.Media.Imaging;
                                  irf.CopyFrameDataToArray(irData);
                                  for (int i = 0; i < irData.Length; i++)
                                  {

                                      byte intensity = (byte)(irData[i] >> 8);
                                      irDataConverted[i * 4] = intensity;
                                      irDataConverted[i * 4 + 1] = intensity;
                                      irDataConverted[i * 4 + 2] = intensity;
                                      irDataConverted[i * 4 + 3] = 255;
                                  }
                              
                                  irDataConverted.CopyTo(irBitmap.PixelBuffer);  
                                  irBitmap.Invalidate();
                                  */
                                // The below is from Kinect Studio WPF infrared sample
                               // irf.CopyFrameDataToArray(irData);
                                using (Microsoft.Kinect.KinectBuffer infraredBuffer = irf.LockImageBuffer())
                                {
                                    // verify data and write the new infrared frame data to the display bitmap
                                    if (((this.fd.Width * this.fd.Height) == (infraredBuffer.Size / this.fd.BytesPerPixel)) &&
                                        (this.fd.Width == this.irBitmap.PixelWidth) && (this.fd.Height == this.irBitmap.PixelHeight))
                                    {
                                        this.ProcessInfraredFrameData(infraredBuffer.UnderlyingBuffer, infraredBuffer.Size);
                                    }
                                }

                                bodyframe.GetAndRefreshBodyData(bodies);
                                bodyCanvas.Children.Clear();

                                foreach (Body b in bodies)
                                {
                                    if (b.IsTracked)
                                    {
                                        Joint hand = b.Joints[JointType.HandLeft];
                                        if (hand.TrackingState == TrackingState.Tracked)
                                        {
                                            DepthSpacePoint dsp = ksensor.CoordinateMapper.MapCameraPointToDepthSpace(hand.Position);
                                            var circle = CreateCircle(dsp);
                                            tbox.Content = "x:" + (dsp.X/2).ToString() + " y" + (dsp.Y/2).ToString();
                                          //   bodyCanvas.Children.Add(circle);
                                             DetectPageTurn(dsp, circle);

                                           //  Canvas.SetLeft(circle, dsp.X);
                                            //Canvas.SetTop(circle, dsp.Y);

                                        }
                                    }
                                }
                            }
                        }
                    }
                    msf = null;
                }
            //}
        }
        private unsafe void ProcessInfraredFrameData(IntPtr infraredFrameData, uint infraredFrameDataSize)
        {
            // infrared frame data is a 16 bit value
            ushort* frameData = (ushort*)infraredFrameData;

            // lock the target bitmap
            this.irBitmap.Lock();

            // get the pointer to the bitmap's back buffer
            float* backBuffer = (float*)this.irBitmap.BackBuffer;

            // process the infrared data
            for (int i = 0; i < (int)(infraredFrameDataSize / this.fd.BytesPerPixel); ++i)
            {
                // since we are displaying the image as a normalized grey scale image, we need to convert from
                // the ushort data (as provided by the InfraredFrame) to a value from [InfraredOutputValueMinimum, InfraredOutputValueMaximum]
                backBuffer[i] = Math.Min(InfraredOutputValueMaximum, (((float)frameData[i] / InfraredSourceValueMaximum * InfraredSourceScale) * (1.0f - InfraredOutputValueMinimum)) + InfraredOutputValueMinimum);
            }

            // mark the entire bitmap as needing to be drawn
            this.irBitmap.AddDirtyRect(new Int32Rect(0, 0, this.irBitmap.PixelWidth, this.irBitmap.PixelHeight));

            // unlock the bitmap
            this.irBitmap.Unlock();
        }
        private void DetectPageTurn(DepthSpacePoint dsp, Shape circle)
        {
            if (lastMarker != null)
                bodyCanvas.Children.Remove(lastMarker);

            if (detected == 0 && (dsp.Y < bodyCanvas.Height / 2) && (dsp.X < bodyCanvas.Width))
            {
                lastPoint = dsp;
                detected = 1;
                lastMarker = circle;
                tDetected.Content = "";
                bodyCanvas.Children.Add(circle);
                return;
            }
            if (detected > 0 && dsp.Y < (bodyCanvas.Height / 2) && (dsp.X < bodyCanvas.Width)
                && dsp.Y > lastPoint.Y + (bodyCanvas.Height / 32))
            {
                detected++;
                lastMarker = circle;
                bodyCanvas.Children.Add(circle);

            }
            else { detected = 0; }
            if (detected > 3)
            {
                tDetected.Content = "Page Turn Detected";
                GoToNextPage();
                detected = 0;
            }

        }
        private Shape CreateCircle(DepthSpacePoint colorPoint)
        {
            var circle = new Ellipse();
            circle.Fill = new SolidColorBrush(Color.FromArgb(255, 0, 255, 0));
            circle.Height = 20;
            circle.Width = 20;
            circle.Stroke = new SolidColorBrush(Color.FromArgb(255, 0, 255, 0));
            circle.StrokeThickness = 2;

            Canvas.SetLeft(circle, colorPoint.X/2);
            Canvas.SetTop(circle, colorPoint.Y/2);
            return circle;
        }


        protected void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            PropertyChangedEventHandler handler = PropertyChanged;

            if (handler != null)
            {
                handler(this, e);
            }
        }

       // private void button_temp_Click(object sender, RoutedEventArgs e)
        private void GoToNextPage()
        {
           // var uc = new custPDFViewer(filename);
            var us = (custPDFViewer)wPDFWinFormHost.Child;
            us.GoToNextPage();
        }
    }
}
