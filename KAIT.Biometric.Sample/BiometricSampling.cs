using Kinect.Biometric.Services.Communication;
using Kinect.Biometric.Services.Messages;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace Kinect.Biometric
{
    public class BiometricSampling
    {

        public event EventHandler<BiometricData> PlayerIdentified;

        protected virtual void OnPlayerIdentified(BiometricData e)
        {
            // Note the use of a temporary variable here to make the event raisin
            // thread-safe; may or may not be necessary in your case.
            var evt = this.PlayerIdentified;
            if (evt != null)
                evt(this, e);
        }

        ILocalCommunicationService _commClient;
        ITelemetryService _dataBroker;
        bool _cloudConnectionFailure = false;

        
        public bool IsPrimaryRoleBiometricID = false;
        public BiometricSampling()
        {

            Init();
        }

        private async void Init()
        {
            _commClient = new NamedPipeService();

            try
            {
                _dataBroker = new ServiceBusTelemetryService();
                await _dataBroker.OpenAsync();
                _cloudConnectionFailure = false;
            }
            catch
            {

                _cloudConnectionFailure = true;
            }

        }

        private PlayerBiometrics _activePlayerBiometricData; 

        private List<PlayerBiometrics> _playerBiometrics = new List<PlayerBiometrics>();

        public async Task<bool> ProcessFace(ulong trackingId, WriteableBitmap bitmap)
        {
            System.Drawing.Bitmap bmp = CreateBitmap(trackingId, bitmap);
            Debug.Print("Process Face ");

            List<NEC.MAFunc.Wrapper.Face> result;

            if (!Properties.Settings.Default.TestMode)
                result = NEC.MAFunc.Wrapper.FaceAnalyser.DetectFace(bmp);
            else
                result = null;

            Debug.Print("Evaluate Demographics ");                                              //Male                             //Female
            if (Properties.Settings.Default.TestMode || (result != null && result.Count > 0 && (result[0].GenderConfidence > .7 || result[0].GenderConfidence < .3 )))
            {
                Debug.Print("Get Demographcis ");
                var current = GetDemographics(trackingId, result);


                if (current != null)
                {
                    // this should only occur if we are primarily in a Biometric authentication mode in which case we already processed 5 sensor reads to create the demographics profile.
                    // but there are some cases were we may still identify the user so we need to reset the profile is we suddenly recognize the person.
                    if (_activePlayerBiometricData != null 
                        && IsPrimaryRoleBiometricID
                        && _activePlayerBiometricData.SamplingCount >= 4 
                        && _activePlayerBiometricData.Transmitted && current.FaceMatch)
                    {
                        Debug.Print("Update base on Biometric Auth. Resetting profile " + current.TrackingId.ToString());
                        _activePlayerBiometricData.ResetBiometricSamples();
                        _activePlayerBiometricData.Transmitted = false;
                        _activePlayerBiometricData.Add(current);
                    
                    }//Once we reach the 5 sample threshold and we're not in a contiuation we transmit the data to the subscribers
                    else if (_activePlayerBiometricData != null
                        && _activePlayerBiometricData.Transmitted == false 
                        && _activePlayerBiometricData.SamplingCount >= 4 
                        && !IsNewUserAContinuation(_activePlayerBiometricData))
                    {
                        Debug.Print("Returned Demographics " + current.TrackingId.ToString());
                        _activePlayerBiometricData.Add(current);
                        var PlaysTrueBiometrics = _activePlayerBiometricData.FilteredBiometricData;
                       
                        PlaysTrueBiometrics.TrackingState = BiometricTrackingState.TrackingStarted;

                        OnPlayerIdentified(PlaysTrueBiometrics);

                        _commClient.Send<IBiometricData>(PlaysTrueBiometrics, "biometrics");

                        if (!_cloudConnectionFailure)
                            await _dataBroker.SendAsync<IBiometricData>(PlaysTrueBiometrics);

                        _activePlayerBiometricData.Transmitted = true;
                    }
                    else
                    {
                        if (_activePlayerBiometricData == null)
                        {
                            Debug.Print("Added New Player " + current.TrackingId.ToString());
                            _activePlayerBiometricData = new PlayerBiometrics(current);
                            _playerBiometrics.Add(_activePlayerBiometricData);
                        }
                        else
                        {
                            Debug.Print("Added New Sample " + current.TrackingId.ToString());
                            _activePlayerBiometricData.Add(current);
                        }
                    }
                }

            }
            else
            {
                Debug.Print("Quality Issue Rejecting Image");
                if (Properties.Settings.Default.DebugImages)
                    bmp.Save("C:\\TEMP\\REJECTED" + trackingId + ".png", System.Drawing.Imaging.ImageFormat.Png);
            }

            return true;
        }

        public void LostTrackingForPlayer(ulong TrackingID)
        {
            var lostPlayer = (from player in _playerBiometrics where player.TrackingID == TrackingID select player).FirstOrDefault();

            if (lostPlayer != null)
            {
                lostPlayer.ActivelyTracked = false;


                if(lostPlayer.FaceID == "")
                {
                    var playersBiometrics = lostPlayer.FilteredBiometricData;

                    playersBiometrics.TrackingState = BiometricTrackingState.TrackingLost;

                 
                   
                    OnPlayerIdentified(playersBiometrics);

                    _playerBiometrics.Remove(lostPlayer);
                }
            }
         

        }
        public bool IsNewPlayer(ulong TrackingID)
        {
            _activePlayerBiometricData = (from player in _playerBiometrics where player.TrackingID == TrackingID select player).FirstOrDefault();
           
            if (_activePlayerBiometricData == null)
            {
                Debug.Print("IsNewPlayer: True " + TrackingID.ToString());
                return true;
            }                                                         //Set things so we continue to see if we can identify this player...
            else if (_activePlayerBiometricData.SamplingCount < 5 || (IsPrimaryRoleBiometricID && _activePlayerBiometricData.FilteredBiometricData.FaceID == ""))
            {
                Debug.Print("IsNewPlayer: Sample " + TrackingID.ToString());
                return true;
            }
            else
            {
                _activePlayerBiometricData.LastSeen = DateTime.Now;
                var t = _activePlayerBiometricData.FaceID;
                Debug.Print("IsNewPlayer: False " + TrackingID.ToString());
                return false;
            }
                
        }
        private bool IsNewUserAContinuation(PlayerBiometrics bioMetricData)
        {
            if (bioMetricData.FaceID != "")
            {
                var CheckList = from users in _playerBiometrics where users.FaceID == bioMetricData.FaceID orderby users.LastSeen select users;

                if (CheckList != null && CheckList.Count() > 1)
                {
                    var orginalRecord = CheckList.FirstOrDefault();

                    var diff = DateTime.Now.Subtract(orginalRecord.LastSeen);


                    if (diff.Minutes == 0)
                    {
                        Debug.Print("I'VE SEEN YOU BEFORE " + orginalRecord.TrackingID.ToString() + " New ID " + bioMetricData.TrackingID.ToString());
                        _playerBiometrics.Remove(orginalRecord);
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {

                    return false;
                }
            }
            else
            {
                return false;
                
            }

        }

        Random _rnd = new Random();
        private BiometricData GetDemographics(ulong id, List<NEC.MAFunc.Wrapper.Face> Results)
        {
            if (Properties.Settings.Default.TestMode)
            {
                var confidence = _rnd.NextDouble();
                var result = new BiometricData()
                {
                    Age = _rnd.Next(10, 90),
                    Gender = (_rnd.NextDouble() <= 0.3) ? Gender.Male : Gender.Female,
                    GenderConfidence = (confidence < 0.5) ? 0.5 : confidence,
                    TrackingId = id
                };

                return result;
            }
            else
            {

                if (Results.Count() > 0)
                {
                    var demographic = new BiometricData();
                    demographic.TrackingId = id;
                    demographic.Age = Results[0].Age;
                    demographic.Gender = Results[0].Gender.ToUpper().Contains("F") ? Gender.Female : Gender.Male;
                    demographic.GenderConfidence = Results[0].GenderConfidence;
                    demographic.FaceMatch = Results[0].MatchResult;
                    demographic.FaceID = Results[0].Name;
                    demographic.FaceConfidence = Results[0].FaceExtractInfo.FaceConfidence;
                    demographic.FrontalFaceScore = Results[0].FaceExtractInfo.FrontalFaceScore;
                    demographic.HeadConfidence = Results[0].FaceExtractInfo.HeadConfidence;
                    return demographic;
                }
                else
                {
                    return null;
                }
            }

        }

        public static Bitmap CreateBitmap(ulong trackingId, WriteableBitmap bitmap)
        {
            System.Drawing.Bitmap bmp;

            using (MemoryStream outStream = new MemoryStream())
            {
                PngBitmapEncoder enc = new PngBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create((BitmapSource)bitmap));
                enc.Save(outStream);
                outStream.Seek(0, SeekOrigin.Begin);
                bmp = new System.Drawing.Bitmap(outStream);
                //  bmp = await EnhanceImage(bmp);

                //  bmp =  ResizeImage((Image)bmp,(int)(bmp.Width * 2.0),(int)( bmp.Height * 2.0));
            }


            try
            {
               //if (Properties.Settings.Default.DebugImages)
                //    bmp.Save("C:\\TEMP\\" + trackingId + ".png", System.Drawing.Imaging.ImageFormat.Png);
            }
            catch (Exception ex)
            {
                var s = ex;
            }
            return bmp;
        }

        public void  UpdateAndPublishLostTracks()
        {
            var expiredTracks = (from users in _playerBiometrics where !users.ActivelyTracked && (DateTime.Now.Subtract(users.LastSeen).Minutes > 0 || users.FaceID == "") select users).ToArray();

          
            //we don't automatically publish a lost track when the sensor loses a players track in case its an intermitent loss
            foreach(PlayerBiometrics pbm in expiredTracks)
            {
                if(!pbm.ActivelyTracked && DateTime.Now.Subtract(pbm.LastSeen).Minutes > 0)
                {
                    var playersBiometrics = pbm.FilteredBiometricData;
                    
                    playersBiometrics.TrackingState = BiometricTrackingState.TrackingLost;

                    _commClient.Send<IBiometricData>(playersBiometrics, "biometrics");

                    OnPlayerIdentified(playersBiometrics);

                    _playerBiometrics.Remove(pbm);
                    
                }

            }


        }

        /// <summary>

        /// Resize the image to the specified width and height.

        /// </summary>

        /// <param name="image">The image to resize.</param>

        /// <param name="width">The width to resize to.</param>

        /// <param name="height">The height to resize to.</param>

        /// <returns>The resized image.</returns>

        private static Bitmap ResizeImage(Image image, int width, int height)
        {

            var destRect = new Rectangle(0, 0, width, height);

            var destImage = new Bitmap(width, height);



            destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);



            using (var graphics = Graphics.FromImage(destImage))
            {

                graphics.CompositingMode = CompositingMode.SourceCopy;

                graphics.CompositingQuality = CompositingQuality.HighQuality;

                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;

                graphics.SmoothingMode = SmoothingMode.HighQuality;

                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;



                using (var wrapMode = new ImageAttributes())
                {

                    wrapMode.SetWrapMode(WrapMode.TileFlipXY);

                    graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);

                }

            }



            return destImage;

        }

        private static async Task<Bitmap> EnhanceImage(Bitmap originalImage)
        {

            Bitmap adjustedImage = new Bitmap(originalImage);
            float brightness = 1.0f; // no change in brightness
            float contrast = 3.5f; // twice the contrast
            float gamma = 1.0f; // no change in gamma

            float adjustedBrightness = brightness - 1.0f;
            // create matrix that will brighten and contrast the image
            float[][] ptsArray ={
            new float[] {contrast, 0, 0, 0, 0}, // scale red
            new float[] {0, contrast, 0, 0, 0}, // scale green
            new float[] {0, 0, contrast, 0, 0}, // scale blue
            new float[] {0, 0, 0, 1.0f, 0}, // don't scale alpha
            new float[] {adjustedBrightness, adjustedBrightness, adjustedBrightness, 0, 1}};

            var imageAttributes = new ImageAttributes();
            imageAttributes.ClearColorMatrix();
            imageAttributes.SetColorMatrix(new ColorMatrix(ptsArray), ColorMatrixFlag.Default, ColorAdjustType.Bitmap);
            imageAttributes.SetGamma(gamma, ColorAdjustType.Bitmap);

            Graphics g = Graphics.FromImage(adjustedImage);
            g.DrawImage(originalImage, new Rectangle(0, 0, adjustedImage.Width, adjustedImage.Height)
                , 0, 0, originalImage.Width, originalImage.Height,
                GraphicsUnit.Pixel, imageAttributes);

            //originalImage.Save("C:\\TEMP\\ORG.PNG");
            //adjustedImage.Save("C:\\TEMP\\ADJ11.PNG");
            return adjustedImage;
        }
    }

    public class PlayerBiometrics
    {
        ulong _trackingID;
        string _faceID;
        Gender _gender = Gender.Unknown;
        int _age = 0;

        //Is being tracked by the Kinect Sensor
        public bool ActivelyTracked = false;

        public bool Transmitted = false;
        public ulong TrackingID
        {
            get {
            //    LastSeen = DateTime.Now;
                 return _trackingID;
            }
            set { _trackingID = value; }
        }
        public string FaceID
        {
            get
            {
               
                return GetFaceID();
            }
        }
        public int SamplingCount
        {
            get { return _biometricDataSamples.Count; }
        }

        public DateTime LastSeen;

        public PlayerBiometrics(BiometricData Data)
        {
            LastSeen = DateTime.Now;
            this.ActivelyTracked = true;
            _biometricDataSamples = new List<BiometricData>();
            _biometricDataSamples.Add(Data);
            TrackingID = Data.TrackingId;
        }

        private List<BiometricData> _biometricDataSamples;

        public BiometricData FilteredBiometricData
        {
            get
            {
                var returnedData = new BiometricData();
                returnedData = _biometricDataSamples[0];
                returnedData.FaceID = GetFaceID();
                returnedData.Age = GetAge();
                returnedData.Gender = GetGender();

                return returnedData;
            }
            private set
            {

            }
        }

        public void Add(BiometricData BiometricSample)
        {
            LastSeen = DateTime.Now;
            this.ActivelyTracked = true;
            _biometricDataSamples.Add(BiometricSample);
        }

        public void ResetBiometricSamples()
        {
            _biometricDataSamples.Clear();
        }

        private string GetFaceID()
        {
            Dictionary<string, int> FaceIDs = new Dictionary<string, int>();
            foreach (BiometricData bod in _biometricDataSamples)
            {
                if(FaceIDs.ContainsKey(bod.FaceID + ""))
                {
                    FaceIDs[bod.FaceID + ""]++;
                }
                else
                {
                    FaceIDs.Add(bod.FaceID +"", 1);
                    
                }

            }

            var results = (from ids in FaceIDs orderby ids.Value descending select ids).FirstOrDefault();

            return results.Key;
        }

        private int GetAge()
        {
            if(_age == 0)
                _age = (from data in _biometricDataSamples orderby data.Age descending select data.Age).FirstOrDefault();

            return _age;
        }

        private Gender GetGender()
        {
            if (_gender == Gender.Unknown)
            {
                var val = (from data in _biometricDataSamples select data.GenderConfidence).Sum() / _biometricDataSamples.Count;

                if (val > .7)
                    _gender = Gender.Male;
                else if (val < .3)
                    _gender = Gender.Female;
                else
                    _gender = Gender.Unknown;
            }
            
            return _gender;
        }

    }
}
