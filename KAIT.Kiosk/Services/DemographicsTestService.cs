
using KAIT.Common.Interfaces;
using KAIT.Common.Services.Messages;
using System;
using System.Collections.Generic;

namespace KAIT.Kiosk.Services
{
    public class DemographicsTestService : IDemographicsService
    {
        public event EventHandler<BiometricData> DemographicsReceived;


        public void Listen(string source)
        {
            Random rnd = new Random();
            var demogrphics = new BiometricData() { Age = rnd.Next(19, 55), Gender = (rnd.NextDouble() < 0.5) ? Gender.Male : Gender.Female };

            var handler = this.DemographicsReceived;
            if (handler != null)
                handler(this, demogrphics);
        }


        public List<UserExperienceContext> UserExperiences
        {
            get { throw new NotImplementedException(); }
        }

        public event EventHandler<string> DemographicsProcessingFailure;


        public void EnrollFace(string FaceID, System.Drawing.Bitmap FaceImage)
        {
            throw new NotImplementedException();
        }
    }
}
