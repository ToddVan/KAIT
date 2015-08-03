using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Messaging;
using GalaSoft.MvvmLight.Threading;
using Inception.Common.Interfaces;
using KinectKiosk.Services;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace KinectKiosk.ViewModel
{
    public class PhotoViewModel : ViewModelBase
    {
        IImageService _imageService;
        IBarcodeGenerator _barCode;
        System.Timers.Timer _timer;

        string _photoId;
        string _rootUrl;

        string _prompt;
        public string Prompt
        {
            get { return _prompt; }
            set
            {
                if (_prompt == value)
                    return;
                _prompt = value;
                RaisePropertyChanged("Prompt");
            }
        }

        public WriteableBitmap Photo { get { return _imageService.Photo; } }

        int _countdown = 30;
        public int Countdown
        {
            get { return _countdown; }
            set
            {
                if (_countdown == value)
                    return;
                _countdown = value;
                RaisePropertyChanged("Countdown");
            }
        }

        BitmapImage _image;
        public BitmapImage QRCode
        {
            get { return _image; }
            set
            {
                if (_image == value)
                    return;
                _image = value;
                RaisePropertyChanged("QRCode");
            }
        }

        bool _isReady;
        public bool IsReady
        {
            get { return _isReady; }
            set
            {
                if (_isReady == value)
                    return;
                _isReady = value;
                RaisePropertyChanged("IsReady");
            }
        }

        public PhotoViewModel(IImageService imageService, IBarcodeGenerator barcode)
        {
            _imageService = imageService;
            _barCode = barcode;
            _rootUrl = ConfigurationManager.AppSettings["DownloadRootUrl"];

            _timer = new System.Timers.Timer();
            _timer.Interval = 1000;
            _timer.Elapsed += _timer_Tick;
            Messenger.Default.Register<string>(this,(msg) => {
                if (msg == "SPECIALSTART")
                {
                    Debug.WriteLine("PVM SPECIALSTART");
                    this.IsReady = false;
                    _timer.Start();
                    _imageService.Start();
                    //_imageService.TakePhoto();
                    //_imageService.Stop();
                    
                }
            });
        }

    

         void _timer_Tick(object sender, EventArgs e)
        {
            if (Countdown > 25)
                Prompt = "Let's Make a Postcard! Get Ready to Smile!";

            if (Countdown == 25)
                Prompt = "Ready!";

            if (Countdown == 23)
                Prompt = "Set!";

            if (Countdown == 21)
                Prompt = "Smile!";

            if (Countdown == 19)
            {
   
                DispatcherHelper.UIDispatcher.InvokeAsync(async () =>
                {
                    _photoId = await _imageService.TakePhoto(true);
                    var uri =  _rootUrl + _photoId;
                    this.QRCode = Convert( await _barCode.GetBarcodeAsync(uri));
                    this.IsReady = true;
                });
            }

            if (Countdown < 18)
                Prompt = "Scan QR Code with your smartphone to download your photo";

            Countdown--;
            
            if (Countdown == 0)
            {
                _imageService.Stop();
                Messenger.Default.Send<string>("SPECIALSTOP");
                _timer.Stop();

                Countdown = 30; 
            }
        }

         private BitmapImage Convert(System.Drawing.Image image)
         {
             if (image == null) { return null; }

             var bitmap = new System.Windows.Media.Imaging.BitmapImage();
             bitmap.BeginInit();
             MemoryStream memoryStream = new MemoryStream();
             image.Save(memoryStream, ImageFormat.Bmp);
             memoryStream.Seek(0, System.IO.SeekOrigin.Begin);
             bitmap.StreamSource = memoryStream;
             bitmap.EndInit();
             return bitmap;
         }
    }
}
