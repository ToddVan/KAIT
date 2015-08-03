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
