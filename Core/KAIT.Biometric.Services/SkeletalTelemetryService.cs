using KAIT.EventHub.Messaging;
using KAIT.Common.Services.Messages;
using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KAIT.Biometric.Services
{
    public class SkeletalTelemetryService
    {
        private EventHubMessageSender _eventHub;

        public SkeletalTelemetryService(string EventHubConnectionString)
        {
            _eventHub = new EventHubMessageSender(EventHubConnectionString);
        }
        public void TrackSkeleton(Body body, string uniqueKinectId)
        {
            if (body.IsTracked)
            {
                var skeletonTrack = new SkeletonTrack(body.TrackingId);

                foreach (var joint in body.Joints)
                {
                    if ((int)joint.Key < 13 || (int)joint.Key > 20)  // exclude joints below the waist
                    {
                        var orientation = body.JointOrientations[joint.Key].Orientation.W; // not sure if JointOrientation XYZ are same/different than Joint XYZ
                        skeletonTrack.Add(joint.Value, orientation);
                    }
                }

                skeletonTrack.KinectDeviceId = uniqueKinectId;
                skeletonTrack.Location = new LocationCoordinates
                {
                    Latitude = float.Parse(ConfigurationManager.AppSettings["LocationLatitude"]),
                    Longitude = float.Parse(ConfigurationManager.AppSettings["LocationLongitutde"])
                };

                _eventHub.SendMessageToEventHub(skeletonTrack);
            }


        }
    }
}
