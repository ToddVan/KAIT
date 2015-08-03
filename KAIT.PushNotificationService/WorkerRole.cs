using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage;
using Microsoft.ServiceBus.Messaging;

namespace KAIT.PushNotificationService
{
    public class WorkerRole : RoleEntryPoint
    {
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private readonly ManualResetEvent runCompleteEvent = new ManualResetEvent(false);

        public override void Run()
        {
            Trace.TraceInformation("KAIT.PushNotificationService is running");

            try
            {
                this.RunAsync(this.cancellationTokenSource.Token).Wait();
            }
            finally
            {
                this.runCompleteEvent.Set();
            }
        }

        public override bool OnStart()
        {
            // Set the maximum number of concurrent connections
            ServicePointManager.DefaultConnectionLimit = 12;

            // For information on handling configuration changes
            // see the MSDN topic at http://go.microsoft.com/fwlink/?LinkId=166357.

            bool result = base.OnStart();

            Trace.TraceInformation("KAIT.PushNotificationService has been started");
        
            string storage = "DefaultEndpointsProtocol=https;AccountName=inceptionasamonitoring;AccountKey=Lwtx59G+gypbFHrk0+DT8ggdk045CQBp/qXrlUlclVqOhTyIIo7u72DnnEhWzu6bgPtnJ948Ad4M2/gSWx7osw==";

            string serviceBus = "Endpoint=sb://inceptioningess-ns.servicebus.windows.net/;SharedAccessKeyName=ListenPolicy;SharedAccessKey=jMkbgtpkPb9pwVTKLoIKhuKAgN6Q6BHHpf00kPQ2AxU=";

                                   
            string eventHubName = "interactionsnotifications";
                                   
            EventHubClient client = EventHubClient.CreateFromConnectionString(serviceBus, eventHubName);

            Trace.TraceInformation("Consumer group is: " + client.GetDefaultConsumerGroup().GroupName);

            _host = new EventProcessorHost("singleworker", eventHubName, client.GetDefaultConsumerGroup().GroupName, serviceBus, storage);

            Trace.TraceInformation("Created event processor host...");



            return result;
        }

        private EventProcessorHost _host;

        public override void OnStop()
        {
            Trace.TraceInformation("KAIT.PushNotificationService is stopping");
            
            _host.UnregisterEventProcessorAsync().Wait();

            this.cancellationTokenSource.Cancel();
            this.runCompleteEvent.WaitOne();

            base.OnStop();

            Trace.TraceInformation("KAIT.PushNotificationService has stopped");
        }

        private async Task RunAsync(CancellationToken cancellationToken)
        {
            await _host.RegisterEventProcessorAsync<CustomEventProcessor>();

            // TODO: Replace the following with your own logic.
            while (!cancellationToken.IsCancellationRequested)
            {
                Trace.TraceInformation("Working");
                await Task.Delay(1000);
            }
        }
    }
}
