using Sensus.Exceptions;
using Sensus.UI.Properties;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Sensus.Probes
{
    /// <summary>
    /// A probe that polls a data source for samples on a predetermined schedule.
    /// </summary>
    public class ActiveProbeController : ProbeController
    {
        private int _sleepDurationMS;
        private Task _pollTask;
        private AutoResetEvent _pollTrigger;

        [EntryIntegerUiProperty("Sleep Duration (MS):", true)]
        public int SleepDurationMS
        {
            get { return _sleepDurationMS; }
            set
            {
                if (value != _sleepDurationMS)
                {
                    _sleepDurationMS = value;
                    OnPropertyChanged();

                    // if the probe is running, trigger a new poll to start the new sleep duration
                    if (Running)
                        _pollTrigger.Set();
                }
            }
        }

        public ActiveProbeController(IActiveProbe probe)
            : base(probe)
        {
            _sleepDurationMS = 1000;
            _pollTrigger = new AutoResetEvent(true); // start polling immediately
        }

        public override void StartAsync()
        {
            base.StartAsync();

            _pollTask = Task.Run(() =>
                {
                    while (Running)
                    {
                        _pollTrigger.WaitOne(_sleepDurationMS);

                        if (Running)
                        {
                            IActiveProbe activeProbe = Probe as IActiveProbe;

                            Datum d = null;

                            try { d = activeProbe.Poll(); }
                            catch (Exception ex) { if (Logger.Level >= LoggingLevel.Normal) Logger.Log("Failed to poll probe \"" + activeProbe.Name + "\":  " + ex.Message + Environment.NewLine + ex.StackTrace); }

                            try { activeProbe.StoreDatum(d); }
                            catch (Exception ex) { if (Logger.Level >= LoggingLevel.Normal) Logger.Log("Failed to store datum:  " + ex.Message + Environment.NewLine + ex.StackTrace); }
                        }
                    }
                });
        }

        public override async void StopAsync()
        {
            base.StopAsync();

            await Task.Run(async () =>
                {
                    _pollTrigger.Set();  // don't wait for current sleep cycle to end -- wake up immediately so task can complete
                    await _pollTask;
                });
        }
    }
}
