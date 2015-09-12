//----------------------------------------------------------------------------------------------
//    Copyright 2014 Microsoft Corporation
//
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.

using KAIT.Common.Interfaces;
using KAIT.Common.Sensor;
using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Speech.AudioFormat;
using Microsoft.Speech.Recognition;
using System.Speech.Synthesis;
using System.Text;


namespace KAIT.Kinect.Service
{
    public class KinectSpeechService : ISpeechService
    {
        ISensorService<KinectSensor> _sensorService;
        KinectAudioStream _convertStream;
        RecognizerInfo _ri;
        SpeechRecognitionEngine _speechEngine;
        SpeechSynthesizer _speechSynthizer;
     
        public event EventHandler<Microsoft.Speech.Recognition.SpeechRecognizedEventArgs> SpeechRecognized;

        public event EventHandler<Microsoft.Speech.Recognition.SpeechRecognitionRejectedEventArgs> SpeechRejected;

        public KinectSpeechService(ISensorService<KinectSensor> sensorService)
        {
            _sensorService = sensorService;

            _speechSynthizer = new SpeechSynthesizer();

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

     
                _speechEngine.SetInputToAudioStream(
                    _convertStream, new SpeechAudioFormatInfo(EncodingFormat.Pcm, 16000, 16, 1, 32000, 2, null));
                _speechEngine.RecognizeAsync(RecognizeMode.Multiple);

     
            }
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
        {
            if(_speechSynthizer != null)
                _speechSynthizer.Speak(textToSpeak);
        }

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


