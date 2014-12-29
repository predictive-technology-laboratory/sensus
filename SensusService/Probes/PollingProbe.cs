using SensusUI.UiProperties;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SensusService.Probes
{
    public abstract class PollingProbe : Probe
    {
        private Task _pollTask;
        private AutoResetEvent _pollTrigger;
        private int _pollingSleepDurationMS;

        [EntryIntegerUiProperty("Sleep Duration:", true, 5)]
        public int PollingSleepDurationMS
        {
            get { return _pollingSleepDurationMS; }
            set
            {
                if (value != _pollingSleepDurationMS)
                {
                    _pollingSleepDurationMS = value;
                    OnPropertyChanged();

                    if (_pollTrigger != null)
                        _pollTrigger.Set();
                }
            }
        }

        public abstract int DefaultPollingSleepDurationMS { get; }

        protected PollingProbe()
        {
            _pollingSleepDurationMS = DefaultPollingSleepDurationMS;
        }

        /// <summary>
        /// Starts this probe. Throws an exception if start fails.
        /// </summary>
        public sealed override void Start()
        {
            lock (this)
            {
                base.Start();

                _pollTrigger = new AutoResetEvent(true);  // start polling immediately

                _pollTask = Task.Run(() =>
                    {
                        PollingStarted();

                        while (Running)
                        {
                            _pollTrigger.WaitOne(_pollingSleepDurationMS);

                            // Running could be false if the probe has been stopped
                            if (Running)
                            {
                                IEnumerable<Datum> data = null;
                                try { data = Poll(); }
                                catch (Exception ex) { SensusServiceHelper.Get().Logger.Log("Failed to poll probe \"" + GetType().FullName + "\":  " + ex.Message, LoggingLevel.Normal); }

                                if (data != null)
                                    foreach (Datum datum in data)
                                        try { StoreDatum(datum); }
                                        catch (Exception ex) { SensusServiceHelper.Get().Logger.Log("Failed to store datum in probe \"" + GetType().FullName + "\":  " + ex.Message, LoggingLevel.Normal); }
                            }
                        }

                        PollingStopped();
                    });
            }
        }

        protected virtual void PollingStarted() { }

        protected abstract IEnumerable<Datum> Poll();

        public sealed override void Stop()
        {
            lock (this)
            {
                base.Stop();

                if (_pollTask != null)  // might have called stop immediately after start, in which case the poll task will be null. if it's null at this point, it will soon be stopped because we have already set Running to false via base call, terminating the poll task while-loop upon startup.
                {
                    // don't wait for current sleep cycle to end -- wake up immediately so task can complete. if the task is not null, neither will the trigger be.
                    _pollTrigger.Set();
                    _pollTask.Wait();
                }
            }
        }

        protected virtual void PollingStopped() { }

        public override bool Ping(ref string error, ref string warning, ref string misc)
        {
            bool restart = base.Ping(ref error, ref warning, ref misc);

            DateTimeOffset mostRecentReadingTimestamp = DateTimeOffset.MinValue;
            if (MostRecentlyStoredDatum != null)
                mostRecentReadingTimestamp = MostRecentlyStoredDatum.Timestamp;

            double msElapsedSinceLastPoll = (DateTime.UtcNow - mostRecentReadingTimestamp).TotalMilliseconds;
            if (msElapsedSinceLastPoll > _pollingSleepDurationMS)
                warning += "Probe \"" + GetType().FullName + "\" has not taken a reading in " + msElapsedSinceLastPoll + "ms (polling delay = " + _pollingSleepDurationMS + "ms)." + Environment.NewLine;

            return restart;
        }
    }
}
