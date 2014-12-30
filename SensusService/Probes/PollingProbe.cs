using SensusUI.UiProperties;
using System;
using System.Collections.Generic;
using System.Threading;

namespace SensusService.Probes
{
    public abstract class PollingProbe : Probe
    {
        private Thread _pollThread;
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

                _pollThread = new Thread(() =>
                    {
                        PollingStarted();

                        int msToSleep = _pollingSleepDurationMS;

                        while (Running)
                        {
                            // in order to allow the commit thread to be interrupted by Stop, sleep for 1-second intervals.
                            Thread.Sleep(1000);
                            msToSleep -= 1000;

                            // have we slept enough to run a poll? if not, continue the loop.
                            if (msToSleep > 0)
                                continue;

                            // if we're still running, execute a poll.
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

                            msToSleep = _pollingSleepDurationMS;
                        }

                        PollingStopped();
                    });

                _pollThread.Start();
            }
        }

        protected virtual void PollingStarted() { }

        protected abstract IEnumerable<Datum> Poll();

        public sealed override void Stop()
        {
            lock (this)
            {
                base.Stop();

                // since Running is now false, the poll thread will be exiting soon. if it's in the middle of a poll, the poll will finish.
                _pollThread.Join();
            }
        }

        protected virtual void PollingStopped() { }

        public override bool Ping(ref string error, ref string warning, ref string misc)
        {
            bool restart = base.Ping(ref error, ref warning, ref misc);

            if (Running)
            {
                DateTimeOffset mostRecentReadingTimestamp = DateTimeOffset.MinValue;
                if (MostRecentlyStoredDatum != null)
                    mostRecentReadingTimestamp = MostRecentlyStoredDatum.Timestamp;

                double msElapsedSinceLastPoll = (DateTime.UtcNow - mostRecentReadingTimestamp).TotalMilliseconds;
                if (msElapsedSinceLastPoll > _pollingSleepDurationMS)
                    warning += "Probe \"" + GetType().FullName + "\" has not taken a reading in " + msElapsedSinceLastPoll + "ms (polling delay = " + _pollingSleepDurationMS + "ms)." + Environment.NewLine;
            }

            return restart;
        }
    }
}
