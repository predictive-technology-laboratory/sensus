using Sensus.Probes.Device;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Sensus.iOS.Probes.Device
{
    public class iOSProcessorUtilizationProbe : ProcessorUtilizationProbe
    {
        private bool _running = false;
        protected override async Task InitializeAsync()
        {
            await base.InitializeAsync();
        }

        protected override async Task StartListeningAsync()
        {
            while (Running)
            {
                await RecordUtilization(); //remove the await
                await Task.Delay(MinDataStoreDelay.HasValue ? MinDataStoreDelay.Value.Milliseconds : 1000);
            }
        }

        protected override Task StopListeningAsync()
        {
            return Task.CompletedTask;
        }

        private async Task RecordUtilization()
        {
            try
            {

            }
            catch(Exception exc)
            {
                SensusServiceHelper.Get().Logger.Log("Error getting CPU Utilization. msg:" + exc.Message, LoggingLevel.Normal, GetType());
            }
        }
    }
}
