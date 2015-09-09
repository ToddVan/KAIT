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


using KAIT.EventHub.Messaging;
using KAIT.Common.Services.Messages;
using Microsoft.Kinect;
using System.Configuration;

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
