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
    [Serializable]
    public class ActiveProbeController : ProbeController
    {
        private int _sleepDurationMS;
        [NonSerialized]
        private Task _pollTask;
        [NonSerialized]
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
        }

        public override Task StartAsync()
        {
            _pollTrigger = new AutoResetEvent(true); // start polling immediately

            return Task.Run(async () =>
                {
                    await base.StartAsync();                   

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
                                    catch (Exception ex) { if (App.LoggingLevel >= LoggingLevel.Normal) App.Get().SensusService.Log("Failed to poll probe \"" + activeProbe.Name + "\":  " + ex.Message + Environment.NewLine + ex.StackTrace); }

                                    try { activeProbe.StoreDatum(d); }
                                    catch (Exception ex) { if (App.LoggingLevel >= LoggingLevel.Normal) App.Get().SensusService.Log("Failed to store datum:  " + ex.Message + Environment.NewLine + ex.StackTrace); }
                                }
                            }
                        });
                });
        }

        public override Task StopAsync()
        {
            return Task.Run(async () =>
                {
                    // might have called stop immediately after start, in which case the poll task might not have been initialized. wait for the poll task to appear.
                    int triesLeft = 5;
                    while (_pollTask == null && triesLeft-- > 0)
                    {
                        if (App.LoggingLevel >= LoggingLevel.Normal)
                            App.Get().SensusService.Log("Waiting for controller poll task to appear.");

                        Thread.Sleep(1000);
                    }

                    if (_pollTask == null)
                        throw new SensusException("Failed to get poll task in ActiveProbeController.StopAsync");

                    await base.StopAsync();

                    _pollTrigger.Set();  // don't wait for current sleep cycle to end -- wake up immediately so task can complete

                    await _pollTask;
                });
        }
    }
}
