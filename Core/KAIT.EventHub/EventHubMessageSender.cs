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


using Microsoft.ServiceBus.Messaging;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Configuration;
using System.Diagnostics;
using System.Text;

namespace KAIT.EventHub.Messaging
{
    public class EventHubMessageSender
    {
        internal string ServiceBusConnectionString { get; set; }

        internal EventHubClient EventHubMessageSenderClient { get; set; }

        private DateTime _lastException = DateTime.MaxValue;
        private int _retryPeriod;
        private bool IsInFailureMode = false;

        public EventHubMessageSender(string serviceBusConnectionString = null)
        {
            try
            {
                _retryPeriod = string.IsNullOrEmpty(ConfigurationManager.AppSettings["EventHubMessengerSender.RetryPeriodInSeconds"]) ? 60 : Convert.ToInt16(ConfigurationManager.AppSettings["EventHubMessengerSender.RetryPeriodInSeconds"]);

                ServiceBusConnectionString = serviceBusConnectionString;
                EventHubMessageSenderClient = EventHubClient.CreateFromConnectionString(ServiceBusConnectionString);
            }
            catch (Exception ex)
            {
                Trace.TraceError("Exception creating EventHubMessageSender: {0}", ex.Message);
            }
        }

        public string ConvertMessageToJSon<T>(T Object, JsonSerializerSettings serializerSettings = null)
        {
            string convertedMessage = String.Empty;

            try
            {
                if (serializerSettings != null)
                {
                                                                                                    //\r\n HACK to deal with ASA not putting JSON objects on seperate lines
                    convertedMessage = JsonConvert.SerializeObject(Object, serializerSettings) + "\r\n";
                }
                else
                {
                    convertedMessage = JsonConvert.SerializeObject(Object) + "\r\n";
                }

            }
            catch (Exception ex)
            {
                Trace.TraceError("Exception serializing object of type '{0}'\r\nMessage: {1}", typeof(T).FullName, ex.Message);
            }

            return convertedMessage;
        }
     

        public void SendMessageToEventHub<T>(T Message)
        {
            try
            {
                // evaluate here if I'm in an exception mode
                if (IsInFailureMode)    // there has been an exception
                {
                    if (DateTime.Now < _lastException.AddSeconds(_retryPeriod))
                    {
                        //TODO: add to queue
                        return;
                    }
                    else
                    {
                        IsInFailureMode = false;
                        // outside of the wait period, re-establish connection
                        EventHubMessageSenderClient = EventHubClient.CreateFromConnectionString(ServiceBusConnectionString);
                        _lastException = DateTime.MaxValue;
                    }
                }

                //Trace.TraceInformation("Applying custom serialization settings to object");
                var serializationSettings = new JsonSerializerSettings();

                serializationSettings.Converters.Add(new StringEnumConverter() { AllowIntegerValues = false, CamelCaseText = false });
                serializationSettings.DateFormatHandling = DateFormatHandling.IsoDateFormat;
                serializationSettings.ConstructorHandling = ConstructorHandling.Default;
                serializationSettings.Formatting = Formatting.None;
                serializationSettings.MissingMemberHandling = MissingMemberHandling.Ignore;
                serializationSettings.NullValueHandling = NullValueHandling.Include;
                serializationSettings.StringEscapeHandling = StringEscapeHandling.Default;
                serializationSettings.TypeNameHandling = TypeNameHandling.None;

                string JSonSerializedMessage = ConvertMessageToJSon(Message, serializationSettings);

                //Trace.TraceInformation("JSON Message:\r\n{0}\r\n", JSonSerializedMessage);
                EventHubMessageSenderClient.SendAsync(new EventData(Encoding.UTF8.GetBytes(JSonSerializedMessage)));
            }
            catch (Exception ex)
            {
                Trace.TraceInformation("Exception sending message to the event hub. Message: {0}", ex.Message);
                _lastException = DateTime.Now;
                IsInFailureMode = true;
            }

            return;
        }

    }
}
