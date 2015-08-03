using Inception.Common.Interfaces;
using Inception.Common.Sensor;
using Microsoft.Kinect;
using System;
using System.Reactive.Subjects;

namespace KinectKiosk.Services
{
    public class KinectDepthService : IDepthService
    {
        private const int MapDepthToByte = 8000 / 256;

        ISensorService<KinectSensor> _sensorService;
        bool _isRunning;

        DepthFrameReader _depthReader;
        FrameDescription _depthFrameDescription;
        byte[] _depthPixels;

        public IntPtr DepthFrameDataSize { get; private set; }
        public Subject<byte[]> DepthBytes { get; private set; }
        public int PixelWidth { get; private set; }
        public int PixelHeight { get; private set; }

        public uint BytesPerPixel { get; private set; }

        public KinectDepthService(ISensorService<KinectSensor> sensorService)
        {
            _sensorService = sensorService;

            this.DepthBytes = new Subject<byte[]>();

            _depthFrameDescription = _sensorService.Sensor.DepthFrameSource.FrameDescription;
            this.PixelHeight = _depthFrameDescription.Height;
            this.PixelWidth = _depthFrameDescription.Width;
            _depthPixels = new byte[this.PixelWidth * this.PixelHeight];
        }

        public void Start()
        {
            if (_isRunning)
                return;

            _depthReader = _sensorService.Sensor.DepthFrameSource.OpenReader();
            _depthReader.FrameArrived += _depthReader_FrameArrived;
            _isRunning = true;
        }

        public void Stop()
        {
            if (!_isRunning)
                return;

            _depthReader.FrameArrived -= _depthReader_FrameArrived;
            _isRunning = false;
        }

        void _depthReader_FrameArrived(object sender, DepthFrameArrivedEventArgs e)
        {            
            bool depthFrameProcessed = false;

            using (DepthFrame depthFrame = e.FrameReference.AcquireFrame())
            {
                if (depthFrame != null)
                {

                    // the fastest way to process the body index data is to directly access 
                    // the underlying buffer
                    using (Microsoft.Kinect.KinectBuffer depthBuffer = depthFrame.LockImageBuffer())
                    {
                        // verify data and write the color data to the display bitmap
                        if (((this._depthFrameDescription.Width * this._depthFrameDescription.Height) == (depthBuffer.Size / this._depthFrameDescription.BytesPerPixel)))
                        {
                            // Note: In order to see the full range of depth (including the less reliable far field depth)
                            // we are setting maxDepth to the extreme potential depth threshold
                            ushort maxDepth = ushort.MaxValue;

                            // If you wish to filter by reliable depth distance, uncomment the following line:
                            //// maxDepth = depthFrame.DepthMaxReliableDistance

                            this.ProcessDepthFrameData(depthBuffer.UnderlyingBuffer, depthBuffer.Size, depthFrame.DepthMinReliableDistance, maxDepth);
                            depthFrameProcessed = true;
                        }
                    }
                }
            }

            if (depthFrameProcessed)
            {
                this.DepthBytes.OnNext(_depthPixels);
            }
        }
       
        private unsafe void ProcessDepthFrameData(IntPtr depthFrameData, uint depthFrameDataSize, ushort minDepth, ushort maxDepth)
        {
            this.DepthFrameDataSize = depthFrameData;
            //_isInTrainingMode = TrainingModes.ActiveTraining;
            // depth frame data is a 16 bit value
            ushort* frameData = (ushort*)depthFrameData;
            int rowNum = 0;
            int colNum = 0;
            int pixelCnt = 0;
            int pixelColCnt = 0;
            int numberOfPixelsPerRow = ((int)(depthFrameDataSize / _depthFrameDescription.BytesPerPixel)) / _depthFrameDescription.Height;
            int numberOfPixelsPerCol = ((int)(depthFrameDataSize / _depthFrameDescription.BytesPerPixel)) / _depthFrameDescription.Width;

            // convert depth to a visual representation
            for (int i = 0; i < (int)(depthFrameDataSize / _depthFrameDescription.BytesPerPixel); ++i)
            {
                pixelCnt++;
                pixelColCnt++;
                colNum++;
                if (pixelCnt == numberOfPixelsPerRow)
                {
                    rowNum++;
                    pixelCnt = 0;
                    colNum = 0;
                }

                //if (pixelColCnt == 1)
                //{
                //    colNum++;
                //    pixelColCnt = 0;
                //}
                // Get the depth for this pixel
                ushort depth = frameData[i];


                if (depth <= (ushort)700 && depth >= (ushort)10)
                {
                    _depthPixels[i] = 4000 / MapDepthToByte;

                    if (rowNum == 345)
                        _depthPixels[i] = 7000 / MapDepthToByte;
                }
                else
                {
                    // To convert to a byte, we're mapping the depth value to the byte range.
                    // Values outside the reliable depth range are mapped to 0 (black).
                    //if ( this.depthPixels[i] !=  7000 / MapDepthToByte)

                    _depthPixels[i] = (byte)(depth >= minDepth && depth <= maxDepth ? (depth / MapDepthToByte) : 0);
                }
                // To convert to a byte, we're mapping the depth value to the byte range.
                // Values outside the reliable depth range are mapped to 0 (black).
                // this.depthPixels[i] = (byte)(depth >= minDepth && depth <= maxDepth ? (depth / MapDepthToByte) : 0);
                // }
            }            
        }
        
    }
}
