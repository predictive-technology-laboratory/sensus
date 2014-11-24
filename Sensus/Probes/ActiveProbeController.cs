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
            return Task.Run(async () =>
                {
                    await base.StartAsync();

                    _pollTrigger = new AutoResetEvent(true);  // start polling immediately

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
                    await base.StopAsync();

                    if (_pollTask != null)  // might have called stop immediately after start, in which case the poll task will be null. if it's null at this point, it will soon be stopped because we have already set Running to false via base call, terminating the poll task while-loop upon startup.
                    {
                        // don't wait for current sleep cycle to end -- wake up immediately so task can complete. if the task is not null, neither will the trigger be.
                        _pollTrigger.Set();
                        await _pollTask;
                    }
                });
        }
    }
}
