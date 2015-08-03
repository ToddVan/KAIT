using KAIT.Common.Services.Communication;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace KAIT.Kiosk.Services
{
    public class TestTelemetryService : ITelemetryService
    {

        Task ITelemetryService.OpenAsync()
        {
            return null;
        }

        Task ITelemetryService.SendAsync<T>(T payload)
        {
            return Task.Factory.StartNew(() => { 
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(payload);
            Debug.WriteLine("[{0}] - {1}", DateTime.Now, json);

            });
        }
    }
}
