using System;
using System.Collections.Generic;
using System.Linq;
using KAIT.Common.Interfaces;
using System.Configuration;
using Microsoft.Kinect;
using System.ComponentModel;
using System.IO;
using Newtonsoft.Json;

namespace KAIT.Kinect.Service
{

    public class BodyTrackingService : IBodyTrackingService
    {
        private Dictionary<ITrackedBody, Body> _trackedBodies;
        private List<ITrackedBody> _trackedBodiesMissing;
        private List<ITrackedBody> _trackedBodiesHijacked;
        private int _trackedBodiesCount;
        private BodyTrackingConfiguration _bodyTrackingConfiguration = new BodyTrackingConfiguration();

        public BodyTrackingService()
        {
            _trackedBodies = new Dictionary<ITrackedBody, Body>();
            _trackedBodiesMissing = new List<ITrackedBody>();
            _trackedBodiesHijacked = new List<ITrackedBody>();
            BodyPositionJoint = JointType.Neck; //default to Neck Joint

            LoadConfiguration();
        }        
        
        public float LeftBodySelectionTrackLimit 
        {
            get { return _bodyTrackingConfiguration.LeftBodySelectionTrackLimit; }
            set 
            {
                if (value != _bodyTrackingConfiguration.LeftBodySelectionTrackLimit)
                    _bodyTrackingConfiguration.LeftBodySelectionTrackLimit = value;
            } 
        }
        public float RightBodySelectionTrackLimit
        {
            get { return _bodyTrackingConfiguration.RightBodySelectionTrackLimit; }
            set
            {
                if (value != _bodyTrackingConfiguration.RightBodySelectionTrackLimit)
                    _bodyTrackingConfiguration.RightBodySelectionTrackLimit = value;
            } 
        }
        public float MissingBodyLeftEdgeBoundary
        {
            get { return _bodyTrackingConfiguration.MissingBodyLeftEdgeBoundary; }
            set
            {
                if (value != _bodyTrackingConfiguration.MissingBodyLeftEdgeBoundary)
                    _bodyTrackingConfiguration.MissingBodyLeftEdgeBoundary = value;
            }
        }
        public float MissingBodyRightEdgeBoundary
        {
            get { return _bodyTrackingConfiguration.MissingBodyRightEdgeBoundary; }
            set
            {
                if (value != _bodyTrackingConfiguration.MissingBodyRightEdgeBoundary)
                    _bodyTrackingConfiguration.MissingBodyRightEdgeBoundary = value;
            }
        }
        public float MissingBodyBackDepthBoundary
        {
            get { return _bodyTrackingConfiguration.MissingBodyBackDepthBoundary; }
            set
            {
                if (value != _bodyTrackingConfiguration.MissingBodyBackDepthBoundary)
                    _bodyTrackingConfiguration.MissingBodyBackDepthBoundary = value;
            }
        }
        public int MissingBodyExpiredTimeLimit
        {
            get { return _bodyTrackingConfiguration.MissingBodyExpiredTimeLimit; }
            set
            {
                if (value != _bodyTrackingConfiguration.MissingBodyExpiredTimeLimit)
                    _bodyTrackingConfiguration.MissingBodyExpiredTimeLimit = value;
            }
        }
        public float NewBodyLeftEdgeBoundary
        {
            get { return _bodyTrackingConfiguration.NewBodyLeftEdgeBoundary; }
            set
            {
                if (value != _bodyTrackingConfiguration.NewBodyLeftEdgeBoundary)
                    _bodyTrackingConfiguration.NewBodyLeftEdgeBoundary = value;
            }
        }
        public float NewBodyRightEdgeBoundary
        {
            get { return _bodyTrackingConfiguration.NewBodyRightEdgeBoundary; }
            set
            {
                if (value != _bodyTrackingConfiguration.NewBodyRightEdgeBoundary)
                    _bodyTrackingConfiguration.NewBodyRightEdgeBoundary = value;
            }
        }
        public float NewBodyBackDepthBoundary
        {
            get { return _bodyTrackingConfiguration.NewBodyBackDepthBoundary; }
            set
            {
                if (value != _bodyTrackingConfiguration.NewBodyBackDepthBoundary)
                    _bodyTrackingConfiguration.NewBodyBackDepthBoundary = value;
            }
        }
        public float SwitchActiveBodyZPositionVariance
        {
            get { return _bodyTrackingConfiguration.SwitchActiveBodyZPositionVariance; }
            set
            {
                if (value != _bodyTrackingConfiguration.SwitchActiveBodyZPositionVariance)
                    _bodyTrackingConfiguration.SwitchActiveBodyZPositionVariance = value;
            }
        }
        public float HijackedBodyJumpTolerance
        {
            get { return _bodyTrackingConfiguration.HijackedBodyJumpTolerance; }
            set
            {
                if (value != _bodyTrackingConfiguration.HijackedBodyJumpTolerance)
                    _bodyTrackingConfiguration.HijackedBodyJumpTolerance = value;
            }
        }
        public float HighVelocityPlayerJumpThreshold
        {
            get { return _bodyTrackingConfiguration.HighVelocityPlayerJumpThreshold; }
            set
            {
                if (value != _bodyTrackingConfiguration.HighVelocityPlayerJumpThreshold)
                    _bodyTrackingConfiguration.HighVelocityPlayerJumpThreshold = value;
            }
        }
        public float HighVelocityPlayerJumpMultiplier
        {
            get { return _bodyTrackingConfiguration.HighVelocityPlayerJumpMultiplier; }
            set
            {
                if (value != _bodyTrackingConfiguration.HighVelocityPlayerJumpMultiplier)
                    _bodyTrackingConfiguration.HighVelocityPlayerJumpMultiplier = value;
            }
        }
        public int RequiredJointsTrackedCount
        {
            get { return _bodyTrackingConfiguration.RequiredJointsTrackedCount; }
            set
            {
                if (value != _bodyTrackingConfiguration.RequiredJointsTrackedCount)
                    _bodyTrackingConfiguration.RequiredJointsTrackedCount = value;
            }
        }
        public int TrackedKinectBodyCount { get; set; }        
        
        /// <summary>
        /// The Joint Type to use in the Body Position Coordinates
        /// </summary>
        public JointType BodyPositionJoint { get; set; }

        public int MissingBodyCount { get { return _trackedBodiesMissing != null && _trackedBodiesMissing.Count() > 0 ? _trackedBodiesMissing.Count() : 0; } }

        public int HijackedBodyCount { get { return _trackedBodiesHijacked != null && _trackedBodiesHijacked.Count() > 0 ? _trackedBodiesHijacked.Count() : 0; } }

        public ulong MissingBodyId { get { return _trackedBodiesMissing != null && _trackedBodiesMissing.Count() > 0 ? _trackedBodiesMissing.First().CurrentPlayerTrackingId : 0; } }
        public ulong HijackedBodyId { get { return _trackedBodiesHijacked != null && _trackedBodiesHijacked.Count() > 0 ? _trackedBodiesHijacked.First().CurrentPlayerTrackingId : 0; } }

        public ulong ActiveBodyCorrelationId
        {
            get
            {
                return ActiveBody != null ? _trackedBodies.FirstOrDefault(tb => tb.Value.Equals(ActiveBody)).Key.CorrelationPlayerId : 0;
            }
        }

        public ulong ActiveBodyId 
        {
            get 
            {
                return ActiveBody != null ? ActiveBody.TrackingId : 0;
            }
        }
        public Body ActiveBody { get; set; }

        public int ActiveBodyTrackedJointsCount 
        {            
            get
            {
                return ActiveBody != null ?
                       ActiveBody.Joints.Count(joint => joint.Key == JointType.Head && joint.Value.TrackingState == TrackingState.Tracked
                                                || joint.Key == JointType.Neck && joint.Value.TrackingState == TrackingState.Tracked
                                                || joint.Key == JointType.ShoulderLeft && joint.Value.TrackingState == TrackingState.Tracked
                                                || joint.Key == JointType.ShoulderRight && joint.Value.TrackingState == TrackingState.Tracked
                                                || joint.Key == JointType.ElbowLeft && joint.Value.TrackingState == TrackingState.Tracked
                                                || joint.Key == JointType.ElbowRight && joint.Value.TrackingState == TrackingState.Tracked
                                                || joint.Key == JointType.WristLeft && joint.Value.TrackingState == TrackingState.Tracked
                                                || joint.Key == JointType.WristRight && joint.Value.TrackingState == TrackingState.Tracked)
                        : 0;
            }
        }

        public void LoadConfiguration(string fileName = null)
        {
            try
            {
                string config = string.Empty;
                using (FileStream fs = new FileStream("BodyTrackingConfig.txt", FileMode.Open))
                {
                    StreamReader sr = new StreamReader(fs);
                    config = sr.ReadToEnd();
                    fs.Close();
                }
                _bodyTrackingConfiguration = (BodyTrackingConfiguration)JsonConvert.DeserializeObject(config, typeof(BodyTrackingConfiguration));
                //OnPropertyChanged("BodyTrackingConfiguration");
            }
            catch
            {
                LoadConfigurationDefaults();
            }
        }
        public void SaveConfiguration()
        {
            string filename = "BodyTrackingConfig.txt";
            try
            {
                // Save document
                using (FileStream fs = new FileStream(filename, FileMode.Create))
                {
                    using (StreamWriter sw = new StreamWriter(fs))
                    {
                        var configStream = JsonConvert.SerializeObject(_bodyTrackingConfiguration, Formatting.Indented);
                        sw.Write(configStream);
                        sw.Flush();
                        fs.Flush();
                        //fs.Close(); 
                    }
                }                
            }
            catch (Exception)
            {
                //
            }
        }

        public void LoadConfigurationDefaults()
        {
            _bodyTrackingConfiguration.MissingBodyLeftEdgeBoundary = float.Parse(ConfigurationManager.AppSettings["MissingBodyLeftEdgeBoundary"]);
            _bodyTrackingConfiguration.MissingBodyRightEdgeBoundary = float.Parse(ConfigurationManager.AppSettings["MissingBodyRightEdgeBoundary"]);
            _bodyTrackingConfiguration.MissingBodyBackDepthBoundary = System.Convert.ToInt32(ConfigurationManager.AppSettings["MissingBodyBackDepthBoundary"]);
            _bodyTrackingConfiguration.MissingBodyExpiredTimeLimit = System.Convert.ToInt32(ConfigurationManager.AppSettings["MissingBodyExpiredTimeLimit"]);
            _bodyTrackingConfiguration.NewBodyLeftEdgeBoundary = float.Parse(ConfigurationManager.AppSettings["NewBodyLeftEdgeBoundary"]);
            _bodyTrackingConfiguration.NewBodyRightEdgeBoundary = float.Parse(ConfigurationManager.AppSettings["NewBodyRightEdgeBoundary"]);
            _bodyTrackingConfiguration.NewBodyBackDepthBoundary = System.Convert.ToInt32(ConfigurationManager.AppSettings["NewBodyBackDepthBoundary"]);
            _bodyTrackingConfiguration.SwitchActiveBodyZPositionVariance = float.Parse(ConfigurationManager.AppSettings["SwitchActiveBodyZPositionVariance"]);
            _bodyTrackingConfiguration.HijackedBodyJumpTolerance = float.Parse(ConfigurationManager.AppSettings["HijackedBodyJumpTolerance"]);
            _bodyTrackingConfiguration.HighVelocityPlayerJumpThreshold = float.Parse(ConfigurationManager.AppSettings["HighVelocityPlayerJumpThreshold"]);
            _bodyTrackingConfiguration.HighVelocityPlayerJumpMultiplier = float.Parse(ConfigurationManager.AppSettings["HighVelocityPlayerJumpMultiplier"]);
            _bodyTrackingConfiguration.LeftBodySelectionTrackLimit = float.Parse(ConfigurationManager.AppSettings["LeftBodySelectionTrackLimit"]);
            _bodyTrackingConfiguration.RightBodySelectionTrackLimit = float.Parse(ConfigurationManager.AppSettings["RightBodySelectionTrackLimit"]);
            _bodyTrackingConfiguration.RequiredJointsTrackedCount = System.Convert.ToInt32(ConfigurationManager.AppSettings["RequiredJointsTrackedCount"]);            
        }

        public Body[] GetOtherBodies(Body[] bodies)
        {
            return (from body in bodies where body.TrackingId != ActiveBodyId && body.IsTracked select body).OrderBy(body => body.TrackingId).ToArray(); ;
        }

        public ulong SetActivePlayer(Body[] kinectBodies)
        {
            //If we don't have an active player, set the closest player as the active player
            //if we do have an active player, set the active player to the closest player if the closest player is closer than the active player within our variance setting
            //else keep the active player as the active player
            //and
            //ignore bodies that look to be candidtes for missing or hijacked bodies
            //ignore ghost bodies - bodies that don't have a count of the head, neck, shoulders and wrist joints in a valid Tracked State
            if (kinectBodies != null && kinectBodies.Count() > 0)
            {
                var closestPlayer = from bodies in kinectBodies
                                    where bodies.IsTracked == true
                                        && bodies.Joints.Count(joint => joint.Key == JointType.Head && joint.Value.TrackingState == TrackingState.Tracked
                                                                || joint.Key == JointType.Neck && joint.Value.TrackingState == TrackingState.Tracked
                                                                || joint.Key == JointType.ShoulderLeft && joint.Value.TrackingState == TrackingState.Tracked
                                                                || joint.Key == JointType.ShoulderRight && joint.Value.TrackingState == TrackingState.Tracked
                                                                || joint.Key == JointType.ElbowLeft && joint.Value.TrackingState == TrackingState.Tracked
                                                                || joint.Key == JointType.ElbowRight && joint.Value.TrackingState == TrackingState.Tracked
                                                                || joint.Key == JointType.WristLeft && joint.Value.TrackingState == TrackingState.Tracked
                                                                || joint.Key == JointType.WristRight && joint.Value.TrackingState == TrackingState.Tracked) >= RequiredJointsTrackedCount
                                        && bodies.Joints[BodyPositionJoint].Position.X > LeftBodySelectionTrackLimit
                                        && bodies.Joints[BodyPositionJoint].Position.X < RightBodySelectionTrackLimit
                                        && !_trackedBodiesMissing.Any(tbMissing => tbMissing.BodyPosition.Z - bodies.Joints[BodyPositionJoint].Position.Z <= SwitchActiveBodyZPositionVariance)
                                        && !_trackedBodiesHijacked.Any(tbHijacked => tbHijacked.BodyPosition.Z - bodies.Joints[BodyPositionJoint].Position.Z <= SwitchActiveBodyZPositionVariance)                                        
                                    orderby bodies.Joints[BodyPositionJoint].Position.Z
                                    select bodies;


                //Z Depth of 152.4 millimeters = 6 inches

                if ((closestPlayer.FirstOrDefault() != null && ActiveBodyId == 0)
                    || (closestPlayer.FirstOrDefault() != null && closestPlayer.First().TrackingId != ActiveBodyId && ActiveBody != null
                        && ActiveBody.Joints[BodyPositionJoint].Position.Z - closestPlayer.First().Joints[BodyPositionJoint].Position.Z >= SwitchActiveBodyZPositionVariance))
                {
                    ActiveBody = closestPlayer.FirstOrDefault();
                }
                else if (closestPlayer != null && closestPlayer.Count() == 0)
                {
                    ActiveBody = null;
                }

                //System.Diagnostics.Debug.Print("GetNextPlayer returned ActiveBodyId: {0}", ActiveBodyId);

                return ActiveBodyId;
            }
            return 0;
        }

        /// <summary>
        /// Manages Body Tracking Id by detecting when a body moves is front of an existing body and the existing body loses it's body
        /// </summary>
        /// <param name="bodies">The Kinect bodies collection</param>
        public void TrackBodies(Body[] bodies)
        {
            bool writeOutput = false;            

            //if our body count hasn't changed just return null
            //we're doing this for performance
            //this could fail if someone quickly runs in front of a existing body or if an existing player leaves quickly and a new player enters quickly at the same time
            if (bodies.Where(b => b.IsTracked).Count() == TrackedKinectBodyCount)
            {
                CleanupMissingBodies();
                CleanupHijackedBodies();
                return;
            }            

            #region debug output
#if DEBUG
            {
                if (writeOutput)
                {
                    System.Diagnostics.Debug.Print("-------------------- Tracking Bodies ---------------------- ");

                    var trackedBodies = bodies.Where(b => b.IsTracked);
                    System.Diagnostics.Debug.Print("trackedBodies Count:{0}", trackedBodies.Count());
                    var ghostBodies = bodies.Where(b => b.Joints.Count(joint => joint.Key == JointType.Head && joint.Value.TrackingState == TrackingState.Tracked
                                                                    || joint.Key == JointType.Neck && joint.Value.TrackingState == TrackingState.Tracked
                                                                    || joint.Key == JointType.ShoulderLeft && joint.Value.TrackingState == TrackingState.Tracked
                                                                    || joint.Key == JointType.ShoulderRight && joint.Value.TrackingState == TrackingState.Tracked
                                                                    || joint.Key == JointType.ElbowLeft && joint.Value.TrackingState == TrackingState.Tracked
                                                                    || joint.Key == JointType.ElbowRight && joint.Value.TrackingState == TrackingState.Tracked
                                                                    || joint.Key == JointType.WristLeft && joint.Value.TrackingState == TrackingState.Tracked
                                                                    || joint.Key == JointType.WristRight && joint.Value.TrackingState == TrackingState.Tracked) >= RequiredJointsTrackedCount);
                    System.Diagnostics.Debug.Print("ghostBodies Count:{0}", ghostBodies.Count());
                }

                if (writeOutput && _trackedBodiesMissing != null && _trackedBodiesMissing.Count() > 0)
                {
                    foreach (var missingBody in _trackedBodiesMissing)
                    {
                        System.Diagnostics.Debug.Print("MissingBody Output - CorrelatedPlayerId:{0}, CurrentPlayerTrackingId:{1}, BodyPosition.X:{2}, BodyPosition.Y:{3}, BodyPosition.Z{4}, TimeWentMissing{5}",
                            missingBody.CorrelationPlayerId
                            , missingBody.CurrentPlayerTrackingId
                            , missingBody.BodyPosition.X
                            , missingBody.BodyPosition.Y
                            , missingBody.BodyPosition.Z
                            , missingBody.TimeWentMissing.ToString());

                        foreach (var trackingId in missingBody.PlayerTrackingIds)
                            System.Diagnostics.Debug.Print("MissingBody Output PlayerTrackingIds for CorrelatedPlayerID: {0} - {1}", missingBody.CorrelationPlayerId, trackingId);
                    }

                }
                else if (writeOutput)
                    System.Diagnostics.Debug.Print("MissingBody Output - there are no missing bodies ");

                if (writeOutput && bodies.Any(b => b.IsTracked) && _trackedBodies != null)
                {
                    foreach (var body in bodies.Where(b => b.IsTracked))
                        System.Diagnostics.Debug.Print("Body - IsTracked:{0}, TrackingId:{1}, X:{2}, Y:{3}, Z{4}", body.IsTracked, body.TrackingId, body.Joints[BodyPositionJoint].Position.X, body.Joints[BodyPositionJoint].Position.Y, body.Joints[BodyPositionJoint].Position.Z);

                    foreach (var tb in _trackedBodies)
                    {
                        System.Diagnostics.Debug.Print("TrackedBody - BodyIsTracked:{0}, BodyTrackingId:{1}, TrackedBodyCorrelatedPlayerId:{2}, TrackedBodyCurrentPlayerTrackingId:{3}, BodyX:{4}, BodyY:{5}, BodyZ{6}, TrackedBodyX:{7}, TrackedBodyY:{8}, TrackedBodyZ{9}"
                            , tb.Value.IsTracked
                            , tb.Value.TrackingId
                            , tb.Key.CorrelationPlayerId
                            , tb.Key.CurrentPlayerTrackingId
                            , tb.Value.Joints[BodyPositionJoint].Position.X
                            , tb.Value.Joints[BodyPositionJoint].Position.Y
                            , tb.Value.Joints[BodyPositionJoint].Position.Z
                            , tb.Key.BodyPosition.X
                            , tb.Key.BodyPosition.Y
                            , tb.Key.BodyPosition.Z);

                        foreach (var trackingId in tb.Key.PlayerTrackingIds)
                            System.Diagnostics.Debug.Print("PlayerTrackingIds for CorrelatedPlayerID: {0} - {1}", tb.Key.CorrelationPlayerId, trackingId);
                    }
                }
            }
#endif //DEBUG
            #endregion debug output

            if (bodies != null && bodies.Length > 0)
            {
                //first time in, just add bodies to TrackedBodies collection                
                if (_trackedBodies.Count() == 0)
                {
                    foreach (var body in bodies)
                    {
                        var trackedBody = new TrackedBody(this)
                        {
                            BodyPosition = body.Joints[BodyPositionJoint].Position,
                        };
                        trackedBody.PlayerTrackingIds.Add(body.TrackingId);
                        _trackedBodies.Add(trackedBody, body);
                    }
                }
                else
                {
                    //scenarios 1 - Missing Body - A second player body slowly passes in front of an existing player, 
                    //in this scenario, the first body losses it's tracking and get's a new body assigned                    
                    //scenario 2 - Hijacked Body - A second body quickly passes in front of an existing body and takes the body of the first player
                
                    #region debug output
#if DEBUG
                    {
                        if (writeOutput && TrackedKinectBodyCount != bodies.Where(b => b.IsTracked).Count())
                            System.Diagnostics.Debug.Print("BodyCount changed from {0} to {1}", TrackedKinectBodyCount, bodies.Where(b => b.IsTracked).Count());
                    }
#endif
                    #endregion debug output

                    ManageMissingBodies(bodies);

                    ManageHijackedBodies();

                    ManageNewBodies();

                    TrackedKinectBodyCount = bodies.Where(b => b.IsTracked).Count();
                    _trackedBodiesCount = _trackedBodies.Where(tb => tb.Key.CurrentPlayerTrackingId != 0).Count();
                }
            }
        }

        public void ManageMissingBodies(Body[] bodies)
        {
            bool writeOutput = false;

            #region handle missing Bodies
            //A Missing Body Candidate is initially identified by a TrackedBody that has a .Key.CurrentPlayerTrackingId that does not match the body.Tracking Id
            //we could also use IsTracked flag (body.IsTracked and Key.CurrentPlayerTrackingId != 0)
            //once we have a candidate missing body we need to check if it is missing because it disappaearred while in the viewing area or if it just gradually- walked out of the viewing area

            var missingTrackedBodies = (from trackedBodies in _trackedBodies
                                        where !bodies.Any(b => b.TrackingId == trackedBodies.Key.CurrentPlayerTrackingId)
                                        && !_trackedBodiesMissing.Any(tbMissing => tbMissing.CurrentPlayerTrackingId == trackedBodies.Key.CurrentPlayerTrackingId)
                                        && !_trackedBodiesHijacked.Any(tbHijacked => tbHijacked.CurrentPlayerTrackingId == trackedBodies.Key.CurrentPlayerTrackingId)
                                        select trackedBodies);

            foreach (var missingTrackedBody in missingTrackedBodies)
            {
                //check if the body was within the viewing area before being lost, if it was assume it's missing due to someone walking in front of it                                        
                if (missingTrackedBody.Key.BodyPosition.X > MissingBodyLeftEdgeBoundary && missingTrackedBody.Key.BodyPosition.X < MissingBodyRightEdgeBoundary
                    && missingTrackedBody.Key.BodyPosition.Z < MissingBodyBackDepthBoundary)
                {
                    #region debug output
#if DEBUG
                    {
                        if (writeOutput)
                        {
                            System.Diagnostics.Debug.Print("MISSING FromTrackedBody - CorrelatedPlayerId:{0}, CurrentPlayerTrackingId{1}, TrackedBodyX:{2}, TrackedBodyY:{3}, TrackedBodyZ{4}"
                            , missingTrackedBody.Key.CorrelationPlayerId
                            , missingTrackedBody.Key.CurrentPlayerTrackingId
                            , missingTrackedBody.Key.BodyPosition.X
                            , missingTrackedBody.Key.BodyPosition.Y
                            , missingTrackedBody.Key.BodyPosition.Z);

                            foreach (var trackingId in missingTrackedBody.Key.PlayerTrackingIds)
                                System.Diagnostics.Debug.Print("MISSING FromTrackedBody - PlayerTrackingIds for CorrelatedPlayerId: {0} - {1}", missingTrackedBody.Key.CorrelationPlayerId, trackingId);
                        }
                    }
#endif
                    #endregion debug output

                    //create missing body and add to missing body collection
                    var newMissingBody = new TrackedBody(this)
                    {
                        BodyPosition = new CameraSpacePoint() { X = missingTrackedBody.Key.BodyPosition.X, Y = missingTrackedBody.Key.BodyPosition.Y, Z = missingTrackedBody.Key.BodyPosition.Z },
                    };

                    foreach (var trackedPlayerId in missingTrackedBody.Key.PlayerTrackingIds)
                    {
                        newMissingBody.PlayerTrackingIds.Add(trackedPlayerId);
                    }
                    newMissingBody.TimeWentMissing = DateTime.Now;
                    _trackedBodiesMissing.Add(newMissingBody);

                    #region debug output
#if DEBUG
                    {
                        if (writeOutput)
                        {
                            System.Diagnostics.Debug.Print("MissingBody Added - CorrelatedPlayerID:{0}, CurrentPlayerId{1}, BodyPosition.X{2}, BodyPosition.Y{3}, BodyPosition.Z{4}, TimeWentMissing{5}"
                                , newMissingBody.CorrelationPlayerId
                                , newMissingBody.CurrentPlayerTrackingId
                                , newMissingBody.BodyPosition.X
                                , newMissingBody.BodyPosition.Y
                                , newMissingBody.BodyPosition.Z
                                , newMissingBody.TimeWentMissing.ToString());
                            foreach (var trackingId in newMissingBody.PlayerTrackingIds)
                                System.Diagnostics.Debug.Print("MissingBody Added - PlayerTrackingIds for CorrelatedPlayerId: {0} - {1}", newMissingBody.CorrelationPlayerId, trackingId);
                        }
                    }
#endif
                    #endregion end debug output
                }

                //sync up the TrackedBody associated with the missing body with it's new body
                missingTrackedBody.Key.PlayerTrackingIds.Clear();
                missingTrackedBody.Key.PlayerTrackingIds.Add(missingTrackedBody.Value.TrackingId);
                missingTrackedBody.Key.BodyPosition = missingTrackedBody.Value.Joints[BodyPositionJoint].Position;
            }
            #endregion handle missing bodies

            #region clean up Missing Bodies

            #region debug output
            //#if DEBUG
            //                    {
            //                        if (writeOutput)
            //                        {
            //                            System.Diagnostics.Debug.Print("TrackedMissingBody Count before Removing expired missing bodies {0}", TrackedBodiesMissing.Count());
            //                            System.Diagnostics.Debug.Print("TrackedMissingBodies Count to be removed:{0}", TrackedBodiesMissing.Where(tb => tb.TimeWentMissing.Value.AddSeconds(+_missingBodyExpiredTimeLimit) < DateTime.Now).Count());

            //                            foreach (var trackedMissingBody in TrackedBodiesMissing.Where(tb => tb.TimeWentMissing.Value.AddSeconds(+_missingBodyExpiredTimeLimit) < DateTime.Now))
            //                                System.Diagnostics.Debug.Print("MissingBody to be Removed - Missing Body CorrelatedPlayerId:{0}, CurrentPlayerId:{1}", trackedMissingBody.CorrelationPlayerId, trackedMissingBody.CurrentPlayerTrackingId);
            //                        }
            //                    }
            //#endif
            #endregion debug output

            CleanupMissingBodies();

            #region debug output
            //#if DEBUG
            //                    {
            //                        if (writeOutput)
            //                        {
            //                            System.Diagnostics.Debug.Print("TrackedMissingBody Count after Removing expired missing bodies {0}", TrackedBodiesMissing.Count());

            //                            if (TrackedBodiesCount != TrackedBodies.Where(tb => tb.Key.CurrentPlayerTrackingId != 0).Count())
            //                                System.Diagnostics.Debug.Print("TrackedBodyCount changed from {0} to {1}", TrackedBodiesCount, TrackedBodies.Where(tb => tb.Key.CurrentPlayerTrackingId != 0).Count());
            //                        }
            //                    }
            //#endif
            #endregion debug output
            #endregion clean up Missing Bodies
        }

        public void CleanupMissingBodies()
        {
            //remove missing tracked bodies that have been missing for more than the expired time setting in seconds, assume they did actually leave
            _trackedBodiesMissing.RemoveAll(tb => tb.TimeWentMissing.Value.AddSeconds(+MissingBodyExpiredTimeLimit) < DateTime.Now);
        }

        public void ManageHijackedBodies()
        {
            var writeOutput = false;

            #region handle body Hi jacking
            //handle when a body walks in front of another body and takes the initial bodies body
            //when this occurs the initial body may get it's body back or the initial body will eventually get a new body
            
            var hijackedBodies = _trackedBodies.Where(tb => tb.Key.DetectBodyHijack(tb.Value.Joints[BodyPositionJoint].Position)
                                                    && !_trackedBodiesHijacked.Any(tbHijacked => tbHijacked.CurrentPlayerTrackingId == tb.Key.CurrentPlayerTrackingId)
                                                    && !_trackedBodiesMissing.Any(tbMissing => tbMissing.CurrentPlayerTrackingId == tb.Key.CurrentPlayerTrackingId));


            if (hijackedBodies != null && hijackedBodies.Count() > 0)
            {
                if (writeOutput)
                    System.Diagnostics.Debug.Print("Body Hijack Detected for BodyTrackingId:{0}", hijackedBodies.First().Value.TrackingId);

                foreach (var hijackedBody in hijackedBodies)
                {
                    if (!_trackedBodiesHijacked.Contains(hijackedBody.Key))
                    {
                        var newHijackedBody = new TrackedBody(this)
                        {
                            BodyPosition = new CameraSpacePoint() { X = hijackedBody.Key.BodyPosition.X, Y = hijackedBody.Key.BodyPosition.Y, Z = hijackedBody.Key.BodyPosition.Z },
                        };

                        foreach (var trackedPlayerId in hijackedBody.Key.PlayerTrackingIds)
                        {
                            newHijackedBody.PlayerTrackingIds.Add(trackedPlayerId);
                        }
                        newHijackedBody.TimeWentMissing = DateTime.Now;
                        _trackedBodiesHijacked.Add(newHijackedBody);
                    }
                }
            }

            CleanupHijackedBodies();
            
            #endregion handle body Hi jacking
        }

        public void CleanupHijackedBodies()
        {
            //clean up HijackedBodies
            _trackedBodiesHijacked.RemoveAll(tb => tb.TimeWentMissing.Value.AddSeconds(+MissingBodyExpiredTimeLimit) < DateTime.Now);
        }

        /// <summary>
        /// For all new bodies, locates the closest missing/hijacked body and associate the missing body with the new body
        /// </summary>
        /// <returns>Tracking Id of the new body which was determined to be a missing/hijacked body </returns>
        public void ManageNewBodies()
        {

            bool writeOutput = false;

            //get new bodies and check if a new body is a missing body
            //if new body is a missing body, update the new body Tracking Ids (newbody tracking Ids = missing Body Tracking Ids + the tracking id for the new body)
            var newBodies = _trackedBodies.Where(tb => tb.Value.IsTracked && tb.Key.CurrentPlayerTrackingId == 0).ToList();

            foreach (var newBody in newBodies)
            {
                #region Debug output
#if DEBUG
                {
                    if (writeOutput)
                    {
                        System.Diagnostics.Debug.Print("NEW TrackedBody - BodyIsTracked:{0}, BodyTrackingId:{1}, TrackedBodyCorrelatedPlayerId:{2}, TrackedBodyCurrentPlayerTrackingId:{3}, BodyX:{4}, BodyY:{5}, BodyZ{6}, TrackedBodyX:{7}, TrackedBodyY:{8}, TrackedBodyZ{9}"
                        , newBody.Value.IsTracked
                        , newBody.Value.TrackingId
                        , newBody.Key.CorrelationPlayerId
                        , newBody.Key.CurrentPlayerTrackingId
                        , newBody.Value.Joints[BodyPositionJoint].Position.X
                        , newBody.Value.Joints[BodyPositionJoint].Position.Y
                        , newBody.Value.Joints[BodyPositionJoint].Position.Z
                        , newBody.Key.BodyPosition.X
                        , newBody.Key.BodyPosition.Y
                        , newBody.Key.BodyPosition.Z);
                        foreach (var trackingId in newBody.Key.PlayerTrackingIds)
                            System.Diagnostics.Debug.Print("NEW TrackedBody - PlayerTrackingIds for CorrelatedPlayerId: {0} - {1}", newBody.Key.CorrelationPlayerId, trackingId);
                    }
                }
#endif
                #endregion


                #region handle if new body is a missing body

                var newBodyIsMissingBody = false;

                if (_trackedBodiesMissing.Count() > 0 &&
                    (newBody.Value.Joints[BodyPositionJoint].Position.X > NewBodyLeftEdgeBoundary
                    && newBody.Value.Joints[BodyPositionJoint].Position.X < NewBodyRightEdgeBoundary
                    & newBody.Value.Joints[BodyPositionJoint].Position.Z < NewBodyBackDepthBoundary))
                {
                    ITrackedBody closestMissingBody = null;
                    //if we have more than one missing body try to find the best match based on body postion
                    if (_trackedBodiesMissing.Count() == 1)
                    {
                        //use First()
                        closestMissingBody = _trackedBodiesMissing.First();
                    }
                    else
                    {
                        closestMissingBody = GetClosestTrackedBody(this._trackedBodiesMissing, newBody.Value);
                    }

                    if (writeOutput)
                        System.Diagnostics.Debug.Print("******** New Body is Missing Body ********** - BodyTrackingId:{0}, Missing Body CorrelationPlayerId:{1}", newBody.Value.TrackingId, closestMissingBody.CorrelationPlayerId);

                    //Update the newBody.PlayerTrackingIds to be the Missing Body's PlayerTrackingIds + the new body TrackingId
                    newBody.Key.PlayerTrackingIds = closestMissingBody.PlayerTrackingIds;

                    newBody.Key.MinBodyPositionX = closestMissingBody.MinBodyPositionX;
                    newBody.Key.MaxBodyPositionX = closestMissingBody.MaxBodyPositionX;
                    newBody.Key.MinBodyPositionY = closestMissingBody.MinBodyPositionY;
                    newBody.Key.MaxBodyPositionY = closestMissingBody.MaxBodyPositionY;
                    newBody.Key.MinBodyPositionZ = closestMissingBody.MinBodyPositionZ;
                    newBody.Key.MaxBodyPositionZ = closestMissingBody.MaxBodyPositionZ;

                    //the new body is now the Active Body
                    ActiveBody = newBody.Value;

                    _trackedBodiesMissing.Remove(closestMissingBody);
                }

                if (newBody.Key.CurrentPlayerTrackingId == 0)
                    newBody.Key.PlayerTrackingIds.Remove(newBody.Key.PlayerTrackingIds.Last());

                newBody.Key.PlayerTrackingIds.Add(newBody.Value.TrackingId);

                newBody.Key.BodyPosition = newBody.Value.Joints[BodyPositionJoint].Position;

                #endregion handle if new body is a missing body

                #region handle if new body is a hijacked body

                if (!newBodyIsMissingBody && _trackedBodiesHijacked.Count > 0
                    && (newBody.Value.Joints[BodyPositionJoint].Position.X > NewBodyLeftEdgeBoundary
                    && newBody.Value.Joints[BodyPositionJoint].Position.X < NewBodyRightEdgeBoundary
                    & newBody.Value.Joints[BodyPositionJoint].Position.Z < NewBodyBackDepthBoundary))
                {
                    ITrackedBody closestHijackedBody = null;
                    //if we have more than one missing body try to find the best match based on body postion
                    if (_trackedBodiesHijacked.Count() == 1)
                    {
                        //use First()
                        closestHijackedBody = _trackedBodiesHijacked.First();
                    }
                    else
                    {
                        closestHijackedBody = GetClosestTrackedBody(this._trackedBodiesHijacked, newBody.Value);
                    }

                    if (writeOutput)
                        System.Diagnostics.Debug.Print("******** New Body is Hijacked Body ********** - BodyTrackingId:{0}, Hijacked Body CorrelationPlayerId:{1}", newBody.Value.TrackingId, closestHijackedBody.CorrelationPlayerId);

                    //Update the newBody.PlayerTrackingIds to be the Hijacked Body's PlayerTrackingIds + the new body TrackingId
                    newBody.Key.PlayerTrackingIds = closestHijackedBody.PlayerTrackingIds;

                    newBody.Key.MinBodyPositionX = closestHijackedBody.MinBodyPositionX;
                    newBody.Key.MaxBodyPositionX = closestHijackedBody.MaxBodyPositionX;
                    newBody.Key.MinBodyPositionY = closestHijackedBody.MinBodyPositionY;
                    newBody.Key.MaxBodyPositionY = closestHijackedBody.MaxBodyPositionY;
                    newBody.Key.MinBodyPositionZ = closestHijackedBody.MinBodyPositionZ;
                    newBody.Key.MaxBodyPositionZ = closestHijackedBody.MaxBodyPositionZ;

                    //the new body is now the Active Body
                    ActiveBody = newBody.Value;

                    _trackedBodiesHijacked.Remove(closestHijackedBody);
                }
                #endregion handle if new body is a hijacked body

            }
        }

        /// <summary>
        /// Gets the missing or hijacked tracked body that is/was closest to the new body
        /// </summary>
        /// <param name="trackedBodies">List of currently missing or hijacked bodies</param>
        /// <param name="newBody">A new body that has been detected</param>
        /// <returns>The missing or hijacked body that is closest to the new body</returns>
        public ITrackedBody GetClosestTrackedBody(List<ITrackedBody> trackedBodies, Body newBody)
        {
            return trackedBodies
                        .OrderBy(tb => Math.Abs(tb.BodyPosition.X - newBody.Joints[BodyPositionJoint].Position.X))
                        .ThenBy(tb => Math.Abs(tb.BodyPosition.Y - newBody.Joints[BodyPositionJoint].Position.Y))
                        .First();
        }

    }

    public class TrackedBody : ITrackedBody
    {
        private BodyTrackingService _bodyTrackingService;

        public TrackedBody(BodyTrackingService bodyTrackingService)
        {
            PlayerTrackingIds = new List<ulong>();
            _bodyTrackingService = bodyTrackingService;
        }
        public List<ulong> PlayerTrackingIds { get; set; }

        public ulong CorrelationPlayerId
        {
            get { return PlayerTrackingIds.FirstOrDefault(); }
        }

        public ulong CurrentPlayerTrackingId
        {
            get { return PlayerTrackingIds.Last(); }
        }

        public DateTime? TimeWentMissing { get; set; }

        private CameraSpacePoint _bodyPosition;
        public CameraSpacePoint BodyPosition
        {
            get { return _bodyPosition; }
            set
            {
                _bodyPosition = value;
                SetMinMaxBodyPositionValues(value);
            }
        }

        public float MinBodyPositionX { get; set; }
        public float MaxBodyPositionX { get; set; }
        public float MinBodyPositionY { get; set; }
        public float MaxBodyPositionY { get; set; }
        public float MinBodyPositionZ { get; set; }
        public float MaxBodyPositionZ { get; set; }

        /// <summary>
        /// Indicates how much a player moves around while interacting with the system
        /// </summary>
        public bool IsPlayerHighVelocity
        {
            get
            {
                return (MaxBodyPositionX - MinBodyPositionX >= _bodyTrackingService.HighVelocityPlayerJumpThreshold ||
                        MaxBodyPositionY - MinBodyPositionY >= _bodyTrackingService.HighVelocityPlayerJumpThreshold);
                        //|| MaxBodyPositionZ - MinBodyPositionZ >= _bodyTrackingService.HighVelocityPlayerJumpThreshold);
            }
        }

        //Indicates that a Body Position has jumped which may indicate that the body has been hyjacked by the sensor
        public bool DetectBodyHijack(CameraSpacePoint newBodyPosition)
        {
            bool isBodyHijacked = false;
            float jumpTolerance = 0f;

            if (IsPlayerHighVelocity)
                jumpTolerance = _bodyTrackingService.HijackedBodyJumpTolerance * _bodyTrackingService.HighVelocityPlayerJumpMultiplier;
            else
                jumpTolerance = _bodyTrackingService.HijackedBodyJumpTolerance;

            if ((newBodyPosition.X > 0.0f && BodyPosition.X > 0.0f) && (newBodyPosition.X < BodyPosition.X - jumpTolerance || newBodyPosition.X > BodyPosition.X + jumpTolerance))
                isBodyHijacked = true;

            if ((newBodyPosition.Y > 0.0f && BodyPosition.Y > 0.0f) && (newBodyPosition.Y < BodyPosition.Y - jumpTolerance || newBodyPosition.Y > BodyPosition.Y + jumpTolerance))
                isBodyHijacked = true;

            if ((newBodyPosition.Z > 0.0f && BodyPosition.Z > 0.0f) && (newBodyPosition.Z < BodyPosition.Z - jumpTolerance || newBodyPosition.Z > BodyPosition.Z + jumpTolerance))
                isBodyHijacked = true;

            bool writeOutput = false;
            if (writeOutput)
            {
                System.Diagnostics.Debug.Print("DetectBodyHijack - isBodyHijacked:{0}", isBodyHijacked);
                System.Diagnostics.Debug.Print("DetectBodyHijack - Jump Tolerance Used:{0}", jumpTolerance);
                System.Diagnostics.Debug.Print("DetectBodyHijack - newBodyPosition X{0} Y{1} Z{2}", newBodyPosition.X, newBodyPosition.Y, newBodyPosition.Z);
                System.Diagnostics.Debug.Print("DetectBodyHijack - BodyPosition X{0} Y{1} Z{2}", BodyPosition.X, BodyPosition.Y, BodyPosition.Z);
            }
            return isBodyHijacked;
        }

        public void SetMinMaxBodyPositionValues(CameraSpacePoint bodyPosition)
        {
            if (bodyPosition.X < MinBodyPositionX)
                MinBodyPositionX = bodyPosition.X;
            if (bodyPosition.X > MaxBodyPositionX)
                MaxBodyPositionX = bodyPosition.X;

            if (bodyPosition.Y < MinBodyPositionY)
                MinBodyPositionY = bodyPosition.Y;
            if (bodyPosition.Y > MaxBodyPositionY)
                MaxBodyPositionY = bodyPosition.Y;

            if (bodyPosition.Z < MinBodyPositionZ)
                MinBodyPositionZ = bodyPosition.Z;
            if (bodyPosition.Z > MaxBodyPositionZ)
                MaxBodyPositionZ = bodyPosition.Z;
        }
    }

    public class BodyTrackingConfiguration
    {
        public float LeftBodySelectionTrackLimit { get; set; }
        public float RightBodySelectionTrackLimit { get; set; }
        public float MissingBodyLeftEdgeBoundary { get; set; }
        public float MissingBodyRightEdgeBoundary { get; set; }
        public float MissingBodyBackDepthBoundary { get; set; }
        public int MissingBodyExpiredTimeLimit { get; set; }
        public float NewBodyLeftEdgeBoundary { get; set; }
        public float NewBodyRightEdgeBoundary { get; set; }
        public float NewBodyBackDepthBoundary { get; set; }
        public float SwitchActiveBodyZPositionVariance { get; set; }
        public float HijackedBodyJumpTolerance { get; set; }
        public float HighVelocityPlayerJumpThreshold { get; set; }
        public float HighVelocityPlayerJumpMultiplier { get; set; }
        public int RequiredJointsTrackedCount { get; set; }
    }
}
