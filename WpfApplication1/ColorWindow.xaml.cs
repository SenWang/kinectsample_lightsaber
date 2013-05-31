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
using System.Windows.Media.Animation;

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
                if (skframe == null)
                    return;

                FrameSkeletons = new Skeleton[skframe.SkeletonArrayLength];
                skframe.CopySkeletonDataTo(FrameSkeletons);
                Skeleton sk = (from s in FrameSkeletons
                               where s.TrackingState == SkeletonTrackingState.Tracked
                               select s).FirstOrDefault();

                if (sk == null)
                    return;

                ColorImagePoint cphl = MapToColorImage(sk.Joints[JointType.HandLeft]);
                ColorImagePoint cpwl = MapToColorImage(sk.Joints[JointType.WristLeft]);
                GestureConfirm(sk);
                DrawLightsaber(cphl,cpwl);                       

            }
        }

        void GestureConfirm(Skeleton sk)
        {
            float handheight = sk.Joints[JointType.HandLeft].Position.Y;
            float headheight = sk.Joints[JointType.Head].Position.Y;
            if (handheight > headheight)
                BeginLengthAnimation();
        }

        ColorImagePoint MapToColorImage(Joint jp)
        {
            ColorImagePoint cp = kinect.CoordinateMapper.MapSkeletonPointToColorPoint(jp.Position, kinect.ColorStream.Format);
            return cp ;
        }

        public int Length
        {
            get { return (int)GetValue(LengthProperty); }
            set { SetValue(LengthProperty, value); }
        }

        public static readonly DependencyProperty LengthProperty =
            DependencyProperty.Register("Length", typeof(int), typeof(ColorWindow), new PropertyMetadata(0));

        private void BeginLengthAnimation()
        {
            Int32Animation lenani = new Int32Animation();
            lenani.From = 0;
            lenani.To = 200;
            lenani.Duration = new Duration(TimeSpan.Parse("0:0:6"));
            this.BeginAnimation(ColorWindow.LengthProperty, lenani);
        }

        
        void DrawLightsaber(ColorImagePoint h, ColorImagePoint w)
        {
            Lightsaber.X1 = h.X;
            Lightsaber.Y1 = h.Y;
            LightsaberAngle.CenterX = h.X ;
            LightsaberAngle.CenterY = h.Y ; 

            double dist = Math.Sqrt(Math.Pow(w.X - h.X, 2) + Math.Pow(w.Y - h.Y, 2));
            
            if (dist > 0)
            {
                Lightsaber.X2 = (Length / dist) * (h.X - w.X) + h.X;
                Lightsaber.Y2 = (Length / dist) * (h.Y - w.Y) + h.Y;         
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
