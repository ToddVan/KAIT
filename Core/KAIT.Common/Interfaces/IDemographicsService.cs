using KAIT.Common.Services.Messages;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace KAIT.Common.Interfaces
{
    public interface IDemographicsService
    {
        event EventHandler<BiometricData> DemographicsReceived;

        event EventHandler<string> DemographicsProcessingFailure;
        void Listen(string source);
        List<UserExperienceContext> UserExperiences { get; }

        void EnrollFace(string FaceID, Bitmap FaceImage);
    }

    public class UserExperienceContext : BiometricData
    {
        public string LastContentDisplayed;
        public string LastItemInteraction;
        public int InteractionCount;
    }
}
