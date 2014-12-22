using SensusUI.UiProperties;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SensusService.Probes
{
    /// <summary>
    /// A probe that polls a data source for samples on a predetermined schedule.
    /// </summary>
    public class PollingProbeController : ProbeController
    {
        private int _sleepDurationMS;
        private Task _pollTask;
        private AutoResetEvent _pollTrigger;

        [EntryIntegerUiProperty("Sleep Duration (MS):", true, 3)]
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

        public PollingProbeController(IPollingProbe probe)
            : base(probe)
        {
            _sleepDurationMS = 1000;
        }

        public override void Start()
        {
            base.Start();

            _pollTrigger = new AutoResetEvent(true);  // start polling immediately

            _pollTask = Task.Run(() =>
                {
                    while (Running)
                    {
                        _pollTrigger.WaitOne(_sleepDurationMS);

                        if (Running)
                        {
                            IPollingProbe pollingProbe = Probe as IPollingProbe;

                            IEnumerable<Datum> data = null;
                            try { data = pollingProbe.Poll(); }
                            catch (Exception ex) { SensusServiceHelper.Get().Logger.Log("Failed to poll probe \"" + pollingProbe.DisplayName + "\":  " + ex.Message + Environment.NewLine + ex.StackTrace, LoggingLevel.Normal); }

                            if (data != null)
                                foreach (Datum datum in data)
                                    try { pollingProbe.StoreDatum(datum); }
                                    catch (Exception ex) { SensusServiceHelper.Get().Logger.Log("Failed to store datum:  " + ex.Message + Environment.NewLine + ex.StackTrace, LoggingLevel.Normal); }
                        }
                    }
                });
        }

        public override void Stop()
        {
            base.Stop();

            if (_pollTask != null)  // might have called stop immediately after start, in which case the poll task will be null. if it's null at this point, it will soon be stopped because we have already set Running to false via base call, terminating the poll task while-loop upon startup.
            {
                // don't wait for current sleep cycle to end -- wake up immediately so task can complete. if the task is not null, neither will the trigger be.
                _pollTrigger.Set();
                _pollTask.Wait();
            }
        }

        public override bool Ping(ref string error, ref string warning, ref string misc)
        {
            bool restart = base.Ping(ref error, ref warning, ref misc);

            DateTimeOffset mostRecentReadingTimestamp = DateTimeOffset.MinValue;
            if (Probe.MostRecentlyStoredDatum != null)
                mostRecentReadingTimestamp = Probe.MostRecentlyStoredDatum.Timestamp;

            double msElapsedSinceLastPoll = (DateTime.UtcNow - mostRecentReadingTimestamp).TotalMilliseconds;
            if (msElapsedSinceLastPoll > _sleepDurationMS)
                warning += "Probe \"" + Probe.DisplayName + "\" has not taken a reading in " + msElapsedSinceLastPoll + "ms (polling delay = " + _sleepDurationMS + "ms)." + Environment.NewLine;

            return restart;
        }
    }
}
