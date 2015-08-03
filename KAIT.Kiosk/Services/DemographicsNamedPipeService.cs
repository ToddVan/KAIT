using Inception.Common.Interfaces;
using Inception.Common.Services.Messages;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KinectKiosk.Services
{
    public class DemographicsNamedPipeService : IDemographicsService
    {
        string _pipeName;
        List<UserExperienceContext> _userExperiences = new List<UserExperienceContext>();

        public event EventHandler<DemographicData> DemographicsReceived;
        public void Listen(string source)
        {

            try
            {
                
                _pipeName = source;
                var pipeServer = new NamedPipeServerStream(_pipeName, PipeDirection.In, 1, PipeTransmissionMode.Message, PipeOptions.Asynchronous);
                //old school style - may want to go async/await in future
                pipeServer.BeginWaitForConnection(new AsyncCallback(ConnectionCallback), pipeServer);


            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private async void ConnectionCallback(IAsyncResult asyncResult)
        {
            try
            {
                using (var pipeServer = asyncResult.AsyncState as NamedPipeServerStream)        // get the pipeserver
                {
                    if (pipeServer != null)
                    {
                        pipeServer.EndWaitForConnection(asyncResult);       // finish connection

                        byte[] buffer = new byte[16 * 1024];
                        using (MemoryStream memoryStream = new MemoryStream())  // create mem stream to read in bytes from pipe stream
                        {
                            int read;
                            while ((read = await pipeServer.ReadAsync(buffer, 0, buffer.Length)) > 0)   // read to the end of the stream
                            {
                                memoryStream.Write(buffer, 0, read);    // write the bytes to memory
                            }

                            var json = GetString(memoryStream.ToArray());                                           // convert bytes to string
                            var msg = Newtonsoft.Json.JsonConvert.DeserializeObject<KioskMessage<object>>(json);   // deserialize to message with object payload - payload will be a json string when <object> is used as generic type
                            var assembly = typeof(KioskMessage<object>).Assembly;                                  // find the assembly where are payload types can be found
                            var dataType = assembly.GetType(msg.DataType);                                          // get the type of the payload
                            var payload = Newtonsoft.Json.JsonConvert.DeserializeObject(msg.Data.ToString(), dataType); // deserialize the payload json to the correct type

                            switch (dataType.FullName)        // brittle switch statements based on type string - better way?
                            {
                                case "KinectPOC.Common.Messages.Demographics":

                                    DemographicData demographics = (DemographicData)payload;
                                      
                                    RaiseDemographicsEvent(demographics);

                                    UserExperienceContext uec = new UserExperienceContext();
                                    uec.Age = ((DemographicData)payload).Age;
                                    uec.Gender = ((DemographicData)payload).Gender;
                                    uec.FaceID = ((DemographicData)payload).FaceID;

                                    uec.InteractionCount = 1;
                                   
                                    if(demographics.FaceMatch)
                                    {
                                        //Find out if we've already seen this person
                                        var orginalUser = (from users in _userExperiences where users.FaceID == demographics.FaceID select users).FirstOrDefault();

                                        if (orginalUser == null)
                                        {
                                            uec.TrackingId = demographics.TrackingId;
                                            _userExperiences.Add(uec);
                                        }
                                        else
                                        {
                                            orginalUser.TrackingId = demographics.TrackingId;
                                            orginalUser.InteractionCount++;
                                        }
                                        
                                    }
                                    else
                                    {
                                        uec.TrackingId = demographics.TrackingId;
                                        _userExperiences.Add(uec);
                                    }


                                    
                                    break;
                            }
                        }

                        pipeServer.Close();
                    }
                }

                var newServer = new NamedPipeServerStream(_pipeName, PipeDirection.In, 1, PipeTransmissionMode.Message, PipeOptions.Asynchronous);
                newServer.BeginWaitForConnection(new AsyncCallback(ConnectionCallback), newServer);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private void RaiseDemographicsEvent(DemographicData payload)
        {
            var handler = this.DemographicsReceived;
            if (handler != null && payload != null)
                handler(this, payload);
        }

        private string GetString(byte[] bytes)
        {
            char[] chars = new char[bytes.Length / sizeof(char)];
            System.Buffer.BlockCopy(bytes, 0, chars, 0, bytes.Length);
            return new string(chars);
        }


        public List<UserExperienceContext> UserExperiences
        {
            get { return _userExperiences; }
        }
    }
}
