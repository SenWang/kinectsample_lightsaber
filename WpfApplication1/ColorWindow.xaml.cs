using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.Kinect;

namespace WpfApplication1
{

    public partial class ColorWindow : Window
    {
        KinectSensor kinect;
        public ColorWindow(KinectSensor sensor) : this()
        {
            kinect = sensor;
        }

        public ColorWindow()
        {
            InitializeComponent();
            Loaded += ColorWindow_Loaded;
            Unloaded += ColorWindow_Unloaded;
        }
        void ColorWindow_Unloaded(object sender, RoutedEventArgs e)
        {
            if (kinect != null)
            {
                kinect.ColorStream.Disable();
                kinect.SkeletonStream.Disable();
                kinect.Stop();
                kinect.ColorFrameReady -= myKinect_ColorFrameReady;
                kinect.SkeletonFrameReady -= mykinect_SkeletonFrameReady;
            }
        }
        private WriteableBitmap _ColorImageBitmap;
        private Int32Rect _ColorImageBitmapRect;
        private int _ColorImageStride;
        void ColorWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (kinect != null)
            {
                ColorImageStream colorStream = kinect.ColorStream;

                _ColorImageBitmap = new WriteableBitmap(colorStream.FrameWidth,colorStream.FrameHeight, 96, 96,PixelFormats.Bgr32, null);
                _ColorImageBitmapRect = new Int32Rect(0, 0, colorStream.FrameWidth,colorStream.FrameHeight);
                _ColorImageStride = colorStream.FrameWidth * colorStream.FrameBytesPerPixel;
                ColorData.Source = _ColorImageBitmap;

                kinect.ColorStream.Enable();
                kinect.ColorFrameReady += myKinect_ColorFrameReady;
                kinect.SkeletonStream.Enable();
                kinect.SkeletonStream.TrackingMode = SkeletonTrackingMode.Seated;
                kinect.SkeletonFrameReady += mykinect_SkeletonFrameReady;
                kinect.Start();
            }
        }

        Skeleton[] FrameSkeletons ;
        void mykinect_SkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            using (SkeletonFrame skframe = e.OpenSkeletonFrame())
            {
                if (skframe != null)
                {
                    FrameSkeletons = new Skeleton[skframe.SkeletonArrayLength];
                    skframe.CopySkeletonDataTo(FrameSkeletons);
                    for (int i = 0; i < FrameSkeletons.Length; i++)
                    {
                        if (FrameSkeletons[i].TrackingState == SkeletonTrackingState.Tracked)
                        {
                            GestureDetection(FrameSkeletons[i]);
                            ColorImagePoint cpl = MapToColorImage(FrameSkeletons[i].Joints[JointType.HandLeft]);
                            ColorImagePoint cpr = MapToColorImage(FrameSkeletons[i].Joints[JointType.HandRight]);
                            DrawLightsaber(cpl,cpr);                       
                        }
                    }
                }
            }
        }
        void GestureDetection(Skeleton skeleton)
        {
            Joint jhl = skeleton.Joints[JointType.HandLeft];
            Joint jhr = skeleton.Joints[JointType.HandRight];
            if (Distance(jhl, jhr) < 0.2)
                Lightsaber.Visibility = Visibility.Visible;
            else
                Lightsaber.Visibility = Visibility.Collapsed;

        }
        double Distance(Joint p1, Joint p2)
        {
            double dist = 0;
            dist = Math.Sqrt(Math.Pow(p2.Position.X - p1.Position.X, 2) + Math.Pow(p2.Position.Y - p1.Position.Y, 2));
            return dist;
        }
        ColorImagePoint MapToColorImage(Joint jp)
        {
            ColorImagePoint cp = kinect.MapSkeletonPointToColor(jp.Position, kinect.ColorStream.Format);
            return cp ;
        }
        void DrawLightsaber(ColorImagePoint cpl, ColorImagePoint cpr)
        {
            Lightsaber.X1 = cpl.X;
            Lightsaber.Y1 = cpl.Y;

            double dist = Math.Sqrt(Math.Pow(cpr.X - cpl.X, 2) + Math.Pow(cpr.Y - cpl.Y, 2));
            double length = 200; //光劍長度
            if (dist > 0)
            {
                Lightsaber.X2 = (length / dist) * (cpr.X - cpl.X) + cpl.X;
                Lightsaber.Y2 = (length / dist) * (cpr.Y - cpl.Y) + cpl.Y;
            }
        }

        void myKinect_ColorFrameReady(object sender, ColorImageFrameReadyEventArgs e)
        {
            using (ColorImageFrame frame = e.OpenColorImageFrame())
            {
                if (frame != null)
                {
                    byte[] pixelData = new byte[frame.PixelDataLength];
                    frame.CopyPixelDataTo(pixelData);
                    _ColorImageBitmap.WritePixels(_ColorImageBitmapRect, pixelData,_ColorImageStride, 0);
                }
            }
        }
    }
}
