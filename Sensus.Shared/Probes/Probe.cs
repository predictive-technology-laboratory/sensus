// Copyright 2014 The Rector & Visitors of the University of Virginia
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Newtonsoft.Json;
using Sensus.UI.UiProperties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using Syncfusion.SfChart.XForms;
using System.Threading.Tasks;
using Sensus.Context;
using Sensus.Probes.User.MicrosoftBand;

namespace Sensus.Probes
{
    /// <summary>
    /// An abstract probe.
    /// </summary>
    public abstract class Probe
    {
        #region static members

        public static List<Probe> GetAll()
        {
            List<Probe> probes = null;

            // the reflection stuff we do below (at least on android) needs to be run on the main thread.
            SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(() =>
            {
                probes = Assembly.GetExecutingAssembly().GetTypes().Where(t => !t.IsAbstract && t.IsSubclassOf(typeof(Probe))).Select(t => Activator.CreateInstance(t) as Probe).OrderBy(p => p.DisplayName).ToList();
            });

            return probes;
        }

        #endregion

        /// <summary>
        /// Fired when the most recently sensed datum is changed, regardless of whether the datum was stored.
        /// </summary>
        public event EventHandler<Tuple<Datum, Datum>> MostRecentDatumChanged;

        private bool _enabled;
        private bool _running;
        private Datum _mostRecentDatum;
        private Protocol _protocol;
        private bool _storeData;
        private DateTimeOffset? _mostRecentStoreTimestamp;
        private bool _originallyEnabled;
        private List<Tuple<bool, DateTime>> _startStopTimes;
        private List<DateTime> _successfulHealthTestTimes;
        private List<ChartDataPoint> _chartData;
        private int _maxChartDataCount;
        private bool _runLocalCommitOnStore;

        private readonly object _locker = new object();

        [JsonIgnore]
        public abstract string DisplayName { get; }

        [JsonIgnore]
        public abstract string CollectionDescription { get; }

        [OnOffUiProperty("Enabled:", true, 2)]
        public bool Enabled
        {
            get { return _enabled; }
            set
            {
                if (value != _enabled)
                {
                    _enabled = value;

                    // _protocol can be null when deserializing the probe -- if Enabled is set before Protocol
                    if (_protocol != null && _protocol.Running)
                    {
                        if (_enabled)
                            StartAsync();
                        else
                            StopAsync();
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets whether or not this probe was originally enabled within the protocol. Some probes can become disabled when 
        /// attempting to start them. For example, the temperature probe might not be supported on all hardware and will thus become 
        /// disabled after its failed initialization. Thus, we need a separate variable (other than Enabled) to tell us whether the 
        /// probe was originally enabled. We use this value to calculate participation levels and also to restore the probe before 
        /// sharing it with others (e.g., since other people might have temperature hardware in their devices).
        /// </summary>
        /// <value>Whether or not this probe was enabled the first time the protocol was started.</value>
        public bool OriginallyEnabled
        {
            get
            {
                return _originallyEnabled;
            }
            set
            {
                _originallyEnabled = value;
            }
        }

        [JsonIgnore]
        public bool Running
        {
            get { return _running; }
        }

        /// <summary>
        /// Gets or sets the datum that was most recently sensed, regardless of whether the datum was stored.
        /// </summary>
        /// <value>The most recent datum.</value>
        [JsonIgnore]
        public Datum MostRecentDatum
        {
            get { return _mostRecentDatum; }
            set
            {
                if (value != _mostRecentDatum)
                {
                    Datum previousDatum = _mostRecentDatum;

                    _mostRecentDatum = value;

                    MostRecentDatumChanged?.Invoke(this, new Tuple<Datum, Datum>(previousDatum, _mostRecentDatum));
                }
            }
        }

        [JsonIgnore]
        public DateTimeOffset? MostRecentStoreTimestamp
        {
            get { return _mostRecentStoreTimestamp; }
        }

        public Protocol Protocol
        {
            get { return _protocol; }
            set { _protocol = value; }
        }

        [OnOffUiProperty("Store Data:", true, 3)]
        public bool StoreData
        {
            get { return _storeData; }
            set { _storeData = value; }
        }

        [JsonIgnore]
        public abstract Type DatumType { get; }

        [JsonIgnore]
        protected abstract double RawParticipation { get; }

        /// <summary>
        /// Gets a list of times at which the probe was started (tuple bool = True) and stopped (tuple bool = False). Only includes 
        /// those that have occurred within the protocol's participation horizon.
        /// </summary>
        /// <value>The start stop times.</value>
        public List<Tuple<bool, DateTime>> StartStopTimes
        {
            get { return _startStopTimes; }
        }

        /// <summary>
        /// Gets the successful health test times. Only includes those that have occurred within the
        /// protocol's participation horizon.
        /// </summary>
        /// <value>The successful health test times.</value>
        public List<DateTime> SuccessfulHealthTestTimes
        {
            get { return _successfulHealthTestTimes; }
        }

        [EntryIntegerUiProperty("Max Chart Data Count:", true, 50)]
        public int MaxChartDataCount
        {
            get
            {
                return _maxChartDataCount;
            }
            set
            {
                if (value > 0)
                    _maxChartDataCount = value;

                // trim chart data collection
                lock (_chartData)
                {
                    while (_chartData.Count > 0 && _chartData.Count > _maxChartDataCount)
                        _chartData.RemoveAt(0);
                }
            }
        }

        [OnOffUiProperty("Run Local Commit On Store:", true, 14)]
        public bool RunLocalCommitOnStore
        {
            get
            {
                return _runLocalCommitOnStore;
            }
            set
            {
                _runLocalCommitOnStore = value;
            }
        }

        protected Probe()
        {
            _enabled = _running = false;
            _storeData = true;
            _startStopTimes = new List<Tuple<bool, DateTime>>();
            _successfulHealthTestTimes = new List<DateTime>();
            _maxChartDataCount = 10;
            _chartData = new List<ChartDataPoint>(_maxChartDataCount + 1);
        }

        /// <summary>
        /// Initializes this probe. Throws an exception if initialization fails. After successful completion the probe is considered to be
        /// running and a candidate for stopping at some future point.
        /// </summary>
        protected virtual void Initialize()
        {
            lock (_chartData)
            {
                _chartData.Clear();
            }

            _mostRecentDatum = null;
            _mostRecentStoreTimestamp = DateTimeOffset.UtcNow;  // mark storage delay from initialization of probe
        }

        /// <summary>
        /// Gets the participation level for the current probe. If this probe was originally enabled within the protocol, then
        /// this will be a value between 0 and 1, with 1 indicating perfect participation and 0 indicating no participation. If 
        /// this probe was not originally enabled within the protocol, then the returned value will be null, indicating that this
        /// probe should not be included in calculations of overall protocol participation. Probes can become disabled if they
        /// are not supported on the current device or if the user refuses to initialize them (e.g., by not signing into Facebook).
        /// Although they become disabled, they were originally enabled within the protocol and participation should reflect this.
        /// Lastly, this will return null if the probe is not storing its data, as might be the case if a probe is enabled in order
        /// to trigger scripts but not told to store its data.
        /// </summary>
        /// <returns>The participation level (null, or somewhere 0-1).</returns>
        public double? GetParticipation()
        {
            if (_originallyEnabled && _storeData)
            {
                return Math.Min(RawParticipation, 1);  // raw participations can be > 1, e.g. in the case of polling probes that the user can cause to poll repeatedly. cut off at 1 to maintain the interpretation of 1 as perfect participation.
            }
            else
            {
                return null;
            }
        }

        protected void StartAsync()
        {
            Task.Run(() =>
            {
                try
                {
                    Start();
                }
                catch (Exception)
                {
                }
            });
        }

        /// <summary>
        /// Start this instance, throwing an exception if anything goes wrong. If an exception is thrown, the caller can assume that any relevant
        /// information will have already been logged and displayed. Thus, the caller doesn't need to do anything with the exception information.
        /// </summary>
        public void Start()
        {
            try
            {
                InternalStart();
            }
            catch (Exception startException)
            {
                // stop probe to clean up any inconsistent state information. the only time we don't want to do this is when
                // starting a band probe and getting a client connect exception. in this case we want to leave the probe running
                // so that we can attempt to reconnect to the band at a later time via the band's health test.
                if (!(startException is MicrosoftBandClientConnectException))
                {
                    try
                    {
                        Stop();
                    }
                    catch (Exception stopException)
                    {
                        SensusServiceHelper.Get().Logger.Log("Failed to stop probe after failing to start it:  " + stopException.Message, LoggingLevel.Normal, GetType());
                    }
                }

                string message = "Failed to start probe \"" + GetType().Name + "\":  " + startException.Message;
                SensusServiceHelper.Get().Logger.Log(message, LoggingLevel.Normal, GetType());
                SensusServiceHelper.Get().FlashNotificationAsync(message, false, TimeSpan.FromSeconds(10));  // don't save failure messages for later, since they might be confusing at that future time.

                // disable probe if it is not supported on the device (or if the user has elected not to enable it -- e.g., by refusing to log into facebook)
                if (startException is NotSupportedException)
                {
                    Enabled = false;
                }

                throw startException;
            }
        }

        /// <summary>
        /// Throws an exception if start fails. Should be called first within child-class overrides. This should only be called within Start. This setup
        /// allows for child-class overrides, but since InternalStart is protected, it cannot be called from the outside. Outsiders only have access to
        /// Start (perhaps via Enabled), which takes care of any exceptions arising from the entire chain of InternalStart overrides.
        /// </summary>
        protected virtual void InternalStart()
        {
            lock (_locker)
            {
                if (_running)
                {
                    SensusServiceHelper.Get().Logger.Log("Attempted to start probe, but it was already running.", LoggingLevel.Normal, GetType());
                }
                else
                {
                    SensusServiceHelper.Get().Logger.Log("Starting.", LoggingLevel.Normal, GetType());

                    Initialize();

                    // the probe has successfully initialized and can now be considered started/running.
                    _running = true;

                    lock (_startStopTimes)
                    {
                        _startStopTimes.Add(new Tuple<bool, DateTime>(true, DateTime.Now));
                        _startStopTimes.RemoveAll(t => t.Item2 < Protocol.ParticipationHorizon);
                    }
                }
            }
        }

        public virtual Task<bool> StoreDatumAsync(Datum datum, CancellationToken? cancellationToken)
        {
            // track the most recent datum and call timestamp regardless of whether the datum is null or whether we're storing data
            MostRecentDatum = datum;
            _mostRecentStoreTimestamp = DateTimeOffset.UtcNow;

            // datum is allowed to be null, indicating the the probe attempted to obtain data but it didn't find any (in the case of polling probes).
            if (datum != null)
            {
                datum.ProtocolId = Protocol.Id;

                if (_storeData)
                {
                    ChartDataPoint chartDataPoint = null;

                    try
                    {
                        chartDataPoint = GetChartDataPointFromDatum(datum);
                    }
                    catch (NotImplementedException)
                    {
                    }

                    if (chartDataPoint != null)
                    {
                        lock (_chartData)
                        {
                            _chartData.Add(chartDataPoint);

                            while (_chartData.Count > 0 && _chartData.Count > _maxChartDataCount)
                            {
                                _chartData.RemoveAt(0);
                            }
                        }
                    }

                    return _protocol.LocalDataStore.AddAsync(datum, cancellationToken, _runLocalCommitOnStore);
                }
            }

            return Task.FromResult(false);
        }

        protected void StopAsync()
        {
            Task.Run(() =>
            {
                try
                {
                    Stop();
                }
                catch (Exception ex)
                {
                    SensusServiceHelper.Get().Logger.Log("Failed to stop:  " + ex.Message, LoggingLevel.Normal, GetType());
                }
            });
        }

        /// <summary>
        /// Should be called first within child-class overrides.
        /// </summary>
        public virtual void Stop()
        {
            lock (_locker)
            {
                if (_running)
                {
                    SensusServiceHelper.Get().Logger.Log("Stopping.", LoggingLevel.Normal, GetType());

                    _running = false;

                    lock (_startStopTimes)
                    {
                        _startStopTimes.Add(new Tuple<bool, DateTime>(false, DateTime.Now));
                        _startStopTimes.RemoveAll(t => t.Item2 < Protocol.ParticipationHorizon);
                    }
                }
                else
                {
                    SensusServiceHelper.Get().Logger.Log("Attempted to stop probe, but it wasn't running.", LoggingLevel.Normal, GetType());
                }
            }
        }

        public void Restart()
        {
            lock (_locker)
            {
                Stop();
                Start();
            }
        }

        public virtual bool TestHealth(ref string error, ref string warning, ref string misc)
        {
            bool restart = false;

            if (!_running)
            {
                restart = true;
                error += "Probe \"" + GetType().FullName + "\" is not running." + Environment.NewLine;
            }

            return restart;
        }

        public virtual void Reset()
        {
            if (_running)
            {
                throw new Exception("Cannot reset probe while it is running.");
            }

            lock (_chartData)
            {
                _chartData.Clear();
            }

            lock (_startStopTimes)
            {
                _startStopTimes.Clear();
            }

            lock (_successfulHealthTestTimes)
            {
                _successfulHealthTestTimes.Clear();
            }

            _mostRecentDatum = null;
            _mostRecentStoreTimestamp = null;
        }

        public SfChart GetChart()
        {
            ChartSeries series = GetChartSeries();

            if (series == null)
            {
                return null;
            }

            // provide the series with a copy of the chart data. if we provide the actual list, then the
            // chart wants to auto-update the display on subsequent additions to the list. if this happens,
            // then we'll need to update the list on the UI thread so that the chart is redrawn correctly.
            // and if this is the case then we're in trouble because xamarin forms is not always initialized 
            // when the list is updated with probed data (if the activity is killed).
            lock (_chartData)
            {
                series.ItemsSource = _chartData.ToList();
            }

            SfChart chart = new SfChart
            {
                PrimaryAxis = GetChartPrimaryAxis(),
                SecondaryAxis = GetChartSecondaryAxis(),
            };

            chart.Series.Add(series);

            chart.ChartBehaviors.Add(new ChartZoomPanBehavior
            {
                EnablePanning = true,
                EnableZooming = true,
                EnableDoubleTap = true
            });

            return chart;
        }

        protected abstract ChartSeries GetChartSeries();

        protected abstract ChartAxis GetChartPrimaryAxis();

        protected abstract RangeAxisBase GetChartSecondaryAxis();

        protected abstract ChartDataPoint GetChartDataPointFromDatum(Datum datum);
    }
}