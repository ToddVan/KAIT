using Inception.Common.Interfaces;
using Inception.Common.Services.Messages;
using KinectKiosk.Services;
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
    /// <summary>
    /// Named Pipe Server that will listen for incoming messages from other
    /// Kinect applications
    /// </summary>
    public class NamedPipedServer : IDemographicsService
    {
        string _pipeName;   // name of pipe to be listened to

        public event EventHandler<DemographicData> BiometricsReceived;

        /// <summary>
        /// Start the listening process
        /// </summary>
        /// <param name="source">Name of pipe to be listend to</param>
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
                                    RaiseDemographicsEvent(payload as DemographicData);
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
            var handler = this.BiometricsReceived;
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
            get { throw new NotImplementedException(); }
        }

        public event EventHandler<DemographicData> DemographicsReceived;
    }
}
