using Microsoft.Azure.NotificationHubs;
using Microsoft.ServiceBus.Messaging;
using Microsoft.WindowsAzure.ServiceRuntime;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KAIT.PushNotificationService
{
    public class CustomEventProcessor : IEventProcessor
    {
        Stopwatch checkpointStopWatch;

        async Task IEventProcessor.CloseAsync(PartitionContext context, CloseReason reason)
        {
            Console.WriteLine(string.Format("Processor Shuting Down.  Partition '{0}', Reason: '{1}'.", context.Lease.PartitionId, reason.ToString()));
            if (reason == CloseReason.Shutdown)
            {
                await context.CheckpointAsync();
            }
        }

        Task IEventProcessor.OpenAsync(PartitionContext context)
        {
            Console.WriteLine(string.Format("CustomEventProcessor initialize.  Partition: '{0}', Offset: '{1}'", context.Lease.PartitionId, context.Lease.Offset));
            this.checkpointStopWatch = new Stopwatch();
            this.checkpointStopWatch.Start();
            return Task.FromResult<object>(null);
        }

        async Task IEventProcessor.ProcessEventsAsync(PartitionContext context, IEnumerable<EventData> messages)
        {
            foreach (EventData eventData in messages)
            {
                string data = Encoding.UTF8.GetString(eventData.GetBytes());

                Debug.WriteLine(string.Format("Message received.  Partition: '{0}', Data: '{1}'",
                    context.Lease.PartitionId, data));

                NotificationHubClient hub = NotificationHubClient
                .CreateClientFromConnectionString("Endpoint=sb://inception-ns.servicebus.windows.net/;SharedAccessKeyName=DefaultFullSharedAccessSignature;SharedAccessKey=Qy+U8BBbQOpjUAX8v2FfhoNInn3CygnIfrjDh2F3xR4=", "notificationapp");

                var alerts = JsonConvert.DeserializeObject(data);
                
                //if (alerts != null && alerts.Length > 0)
                //{
                //    var alert = alerts[0];
                   // if (alerts.> 0) {
                        var msg = string.Format("Customer needs assistance for {0}", "TEST");
                        var toast = "<toast launch=\"{&quot;type&quot;:&quot;toast&quot;,&quot;item&quot;:&quot;" + "Need Help " + DateTime.Now.ToShortTimeString() + "&quot;}\"><audio src=\"ms-winsoundevent:Notification.Looping.Alarm1\" loop=\"false\"/><visual><binding template=\"ToastText01\"><text id=\"1\">Guest Assistance</text><text id=\"2\">Guest needs help</text></binding></visual></toast>"; 



                     //  var toast = string.Format("<toast launch=\"{&quot;type&quot;:&quot;toast&quot;,&quot;item&quot;:&quot;" + msg + "&quot;}\"> <audio src=\"ms-winsoundevent:Notification.Looping.Alarm1\" loop=\"false\"/><visual><binding template=\"ToastText01\"><text id=\"1\">{0}</text></binding></visual></toast>", msg);
                        await hub.SendWindowsNativeNotificationAsync(toast);
                   // }
                //}
            }

            //Call checkpoint every 5 minutes, so that worker can resume processing from the 5 minutes back if it restarts.
            if (this.checkpointStopWatch.Elapsed > TimeSpan.FromMinutes(5))
            {
                await context.CheckpointAsync();
                this.checkpointStopWatch.Restart();
            }
        }
    }


 }
