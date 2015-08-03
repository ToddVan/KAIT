using Inception.Common.Services.Messages;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;

namespace Inception.Common.Services.Communication
{
    public class NamedPipeBiometricClient : ILocalCommunicationClient
    {
        string _pipeName;

        public event EventHandler<ITrackingData> TrackingDataReceived;
        public event EventHandler<IBiometricData> BiometricsReceived;
        public event EventHandler<IFaceData> FaceReceived;
        public event EventHandler<IDemographicData> DemographicsReceived;

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
                            var msg = Newtonsoft.Json.JsonConvert.DeserializeObject<BiometricMessage<object>>(json);   // deserialize to message with object payload - payload will be a json string when <object> is used as generic type
                            var assembly = typeof(IBiometricMessage<object>).Assembly;                                  // find the assembly where are payload types can be found
                            var dataType = assembly.GetType(msg.DataType);                                          // get the type of the payload
                            var payload = Newtonsoft.Json.JsonConvert.DeserializeObject(msg.Data.ToString(), dataType); // deserialize the payload json to the correct type
                            switch (dataType.FullName)        // brittle switch statements based on type string - better way?
                            {
                                case "Inception.Common.Services.Messages.ITrackingData":
                                    RaiseTrackingDataReceived(payload as ITrackingData);
                                    break;
                                case "Inception.Common.Services.Messages.IFaceData":
                                    RaiseFaceReceived(payload as IFaceData);
                                    break;
                                case "Inception.Common.Services.Messages.IDemographicData":
                                    RaiseDemographicsReceived(payload as IDemographicData);
                                    break;
                                case "Inception.Common.Services.Messages.IBiometricData":
                                    RaiseBiometricsReceived(payload as IBiometricData);
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

        private string GetString(byte[] bytes)
        {
            char[] chars = new char[bytes.Length / sizeof(char)];
            System.Buffer.BlockCopy(bytes, 0, chars, 0, bytes.Length);
            return new string(chars);
        }

        private void RaiseTrackingDataReceived(ITrackingData data)
        {
            var handler = this.TrackingDataReceived;
            if (handler != null)
                handler(this, data);
        }

        private void RaiseBiometricsReceived(IBiometricData data)
        {
            var handler = this.BiometricsReceived;
            if (handler != null)
                handler(this, data);
        }
        private void RaiseFaceReceived(IFaceData data)
        {
            var handler = this.FaceReceived;
            if (handler != null)
                handler(this, data);
        }
        private void RaiseDemographicsReceived(IDemographicData data)
        {
            var handler = this.DemographicsReceived;
            if (handler != null)
                handler(this, data);
        }
    }
}
