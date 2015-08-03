using System;
using Microsoft.Speech.Recognition;

namespace KAIT.Common.Interfaces
{
    public interface ISpeechService
    {
        event EventHandler<SpeechRecognizedEventArgs> SpeechRecognized;
        event EventHandler<SpeechRecognitionRejectedEventArgs> SpeechRejected;

        void StartListening();
        void StartListening(string recognizerId);
        void StartListening(RecognizerInfo recognizerInfo);

        void Speak(string textToSpeak);
        void Stop();
    }
}
