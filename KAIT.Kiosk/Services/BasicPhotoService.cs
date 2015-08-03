using Inception.Common.Interfaces;
using Inception.Common.Sensor;
using KinectKiosk.Kiosk;
using Microsoft.Kinect;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace KinectKiosk.Services
{
    public class BasicPhotoService : IImageService
    {
        CloudBlobClient _blobClient;
        CloudBlobContainer _blobContainer;

        ISensorService<KinectSensor> _sensorService;
        bool _isRunning;

        ColorFrameReader _colorFrameReader;
        FrameDescription _colorFrameDescription;
        KinectNoBackgroundView _greenScreen;
        public WriteableBitmap Photo { get; private set; }

        public BasicPhotoService(ISensorService<KinectSensor> sensorService)
        {
            _greenScreen = new KinectNoBackgroundView();
            //_greenScreen.Start();
            _sensorService = sensorService;
            _colorFrameDescription = _sensorService.Sensor.ColorFrameSource.CreateFrameDescription(ColorImageFormat.Bgra);
            this.Photo = new WriteableBitmap(_colorFrameDescription.Width, _colorFrameDescription.Height, 96.0, 96.0, PixelFormats.Bgr32, null);
            Init();
        }

        private void Init()
        {
            var connectionString = ConfigurationManager.ConnectionStrings["BlobStorage"].ConnectionString;
            var storageAccount = CloudStorageAccount.Parse(connectionString);
            _blobClient = storageAccount.CreateCloudBlobClient();
            _blobContainer = _blobClient.GetContainerReference(ConfigurationManager.AppSettings["PhotoContainer"]);
        }

        public void Start() {
            if (_isRunning)
                return;

            _colorFrameReader = _sensorService.Sensor.ColorFrameSource.OpenReader();
            _colorFrameReader.FrameArrived +=_colorFrameReader_FrameArrived;

            _sensorService.Open();
            _isRunning = true;
        }

        public void Stop() {
            if (!_isRunning)
                return;

            if (_colorFrameReader != null)
            {
                _colorFrameReader.FrameArrived -= _colorFrameReader_FrameArrived;
                _colorFrameReader.Dispose();
                _colorFrameReader = null;
                Debug.WriteLine("^^^^BASIC PHOTO STOPPED");
            }
            _isRunning = false;
        }

        public async Task<string> TakePhoto(bool stop = false)
        {
            PixelFormat format = PixelFormats.Bgr32;
            int stride = _colorFrameDescription.Width * format.BitsPerPixel / 8;

            if (stop)
                Stop();
            Debug.WriteLine("CLICK!");
            if (this.Photo != null)
            {
               
                var photoId = Guid.NewGuid().ToString() + ".png";
                //var bytes = _greenScreen.RenderFrame();

                //this.Photo.WritePixels(new Int32Rect(0, 0, this.Photo.PixelWidth, this.Photo.PixelHeight), bytes, stride, 0);
                //MemoryStream stream = new MemoryStream((byte[])bytes);
                

                //// create a png bitmap encoder which knows how to save a .png file
                BitmapEncoder encoder = new PngBitmapEncoder();

                // create frame from the writable bitmap and add to encoder
                encoder.Frames.Add(BitmapFrame.Create(this.Photo));


                // write the new file to disk
                try
                {
                    using (MemoryStream fs = new MemoryStream())
                    {
                        encoder.Save(fs);
                        fs.Position = 0;
                        var picBlob = _blobContainer.GetBlockBlobReference(photoId);
                        await picBlob.UploadFromStreamAsync(fs);
                        Debug.WriteLine("Image uploaded");
                    }

                }
                catch (IOException)
                {
                    return null;
                }

                return photoId;
            }

            return null;                     
        }

        void _colorFrameReader_FrameArrived(object sender, ColorFrameArrivedEventArgs e)
        {    

            using (ColorFrame colorFrame = e.FrameReference.AcquireFrame())
            {
                if (colorFrame != null)
                {
                    FrameDescription colorFrameDescription = colorFrame.FrameDescription;

                    using (KinectBuffer colorBuffer = colorFrame.LockRawImageBuffer())
                    {
                        this.Photo.Lock();

                        // verify data and write the new color frame data to the display bitmap
                        if ((colorFrameDescription.Width == this.Photo.PixelWidth) && (colorFrameDescription.Height == this.Photo.PixelHeight))
                        {
                            colorFrame.CopyConvertedFrameDataToIntPtr(
                                this.Photo.BackBuffer,
                                (uint)(colorFrameDescription.Width * colorFrameDescription.Height * 4),
                                ColorImageFormat.Bgra);

                            this.Photo.AddDirtyRect(new Int32Rect(0, 0, this.Photo.PixelWidth, this.Photo.PixelHeight));                         
                        }
                        
                        this.Photo.Unlock();
                        Debug.WriteLine("YOU TOOK A PHOTO " + Thread.CurrentThread.ManagedThreadId );                        
                    }
                }
            }         
        }        

    }
}
