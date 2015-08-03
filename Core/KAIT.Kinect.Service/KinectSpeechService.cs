using KAIT.Common.Interfaces;
using KAIT.Common.Sensor;
using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Speech.AudioFormat;
using Microsoft.Speech.Recognition;
using System.Text;


namespace KAIT.Kinect.Service
{
    public class KinectSpeechService : ISpeechService
    {
        ISensorService<KinectSensor> _sensorService;
        KinectAudioStream _convertStream;
        RecognizerInfo _ri;
        SpeechRecognitionEngine _speechEngine;
        //SpeechSynthesizer _speechSynthesizer;

        public event EventHandler<Microsoft.Speech.Recognition.SpeechRecognizedEventArgs> SpeechRecognized;

        public event EventHandler<Microsoft.Speech.Recognition.SpeechRecognitionRejectedEventArgs> SpeechRejected;

        public KinectSpeechService(ISensorService<KinectSensor> sensorService)
        {
            _sensorService = sensorService;
            // _speechSynthesizer = new SpeechSynthesizer();

            IReadOnlyList<AudioBeam> audioBeamList = _sensorService.Sensor.AudioSource.AudioBeams;
            System.IO.Stream audioStream = audioBeamList[0].OpenInputStream();

            // create the convert stream
            _convertStream = new KinectAudioStream(audioStream);

            _ri = TryGetKinectRecognizer();
        }

        public void StartListening()
        {
            if (null != _ri)
            {
                _speechEngine = new SpeechRecognitionEngine(_ri.Id);

                // Create a grammar from grammar definition XML file.
                using (var memoryStream = new MemoryStream(Encoding.ASCII.GetBytes(KAIT.Kinect.Service.Properties.Resources.SpeechGrammar)))
                {
                    var g = new Grammar(memoryStream);
                    _speechEngine.LoadGrammar(g);
                }

                _speechEngine.SpeechRecognized += _speechEngine_SpeechRecognized;
                _speechEngine.SpeechRecognitionRejected += _speechEngine_SpeechRecognitionRejected;

                // let the convertStream know speech is going active
                _convertStream.SpeechActive = true;

                // For long recognition sessions (a few hours or more), it may be beneficial to turn off adaptation of the acoustic model. 
                // This will prevent recognition accuracy from degrading over time.
                ////speechEngine.UpdateRecognizerSetting("AdaptationOn", 0);

                _speechEngine.SetInputToAudioStream(
                    _convertStream, new SpeechAudioFormatInfo(EncodingFormat.Pcm, 16000, 16, 1, 32000, 2, null));
                _speechEngine.RecognizeAsync(RecognizeMode.Multiple);

                //_isInTrainingMode = true;
            }
            //else
            //    throw new InvalidOperationException("RecognizerInfo cannot be null");
        }

        void _speechEngine_SpeechRecognitionRejected(object sender, SpeechRecognitionRejectedEventArgs e)
        {
            var handler = this.SpeechRejected;
            if (handler != null)
                handler(this, e);
        }

        void _speechEngine_SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            var handler = this.SpeechRecognized;
            if (handler != null)
                handler(this, e);
        }
        public void StartListening(string recognizerId)
        {
            throw new NotImplementedException();
        }

        public void StartListening(Microsoft.Speech.Recognition.RecognizerInfo recognizerInfo)
        {
            _ri = recognizerInfo;
        }

        public void Speak(string textToSpeak)
        { }

        public void Stop()
        {
            if (_convertStream != null)
            {
                _convertStream.SpeechActive = false;
            }

            if (_speechEngine != null)
            {
                _speechEngine.SpeechRecognized -= _speechEngine_SpeechRecognized;
                _speechEngine.SpeechRecognitionRejected -= _speechEngine_SpeechRecognitionRejected;
                _speechEngine.RecognizeAsyncStop();
            }
        }

        private static RecognizerInfo TryGetKinectRecognizer()
        {
            IEnumerable<RecognizerInfo> recognizers;

            // This is required to catch the case when an expected recognizer is not installed.
            // By default - the x86 Speech Runtime is always expected. 
            try
            {
                recognizers = SpeechRecognitionEngine.InstalledRecognizers();
            }
            catch (COMException)
            {
                return null;
            }

            foreach (RecognizerInfo recognizer in recognizers)
            {
                string value;
                recognizer.AdditionalInfo.TryGetValue("Kinect", out value);
                if ("True".Equals(value, StringComparison.OrdinalIgnoreCase) && "en-US".Equals(recognizer.Culture.Name, StringComparison.OrdinalIgnoreCase))
                {
                    return recognizer;
                }
            }

            return null;
        }
    }
}


