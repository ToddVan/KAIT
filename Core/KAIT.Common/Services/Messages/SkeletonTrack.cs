
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

using Microsoft.Kinect;
using System;
using System.Collections.Generic;

namespace KAIT.Common.Services.Messages
{
    public struct JointInfo
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public float Orientation { get; set; }
    }

    public struct LocationCoordinates
    {
        public float Longitude { get; set; }
        public float Latitude { get; set; }
    }

    public class SkeletonTrack
    {
        public LocationCoordinates Location { get; set; }
        public string KinectDeviceId { get; set; }

        public ulong TrackingId { get; set; }
        public Dictionary<string, JointInfo> Joints { get; set; }        
        public DateTimeOffset TimeStamp { get; set; }

        public SkeletonTrack(ulong id)
        {
            this.TrackingId = id;
            this.Joints = new Dictionary<string, JointInfo>();
            this.TimeStamp = DateTimeOffset.UtcNow;
        }

        public void Add(Joint joint, float orientation)
        {
            var point3d = new JointInfo();
            point3d.X = joint.Position.X;
            point3d.Y = joint.Position.Y;
            point3d.Z = joint.Position.Z;
            point3d.Orientation = orientation;

            this.Joints.Add(joint.JointType.ToString(), point3d);
        }
    }
}
