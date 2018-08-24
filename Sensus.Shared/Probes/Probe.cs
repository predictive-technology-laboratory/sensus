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
using Microsoft.AppCenter.Analytics;
using System.ComponentModel;
using Sensus.Extensions;

namespace Sensus.Probes
{
    /// <summary>
    /// Each Probe collects data of a particular type from the device. Sensus contains Probes for many of the hardware sensors present on many 
    /// smartphones. Sensus also contains Probes that can prompt the user for information, which the user supplies via speech or textual input.
    /// Sensus defines a variety of Probes, with platform availability and quality varying by device manufacturer (e.g., Apple, Motorola, Samsung, 
    /// etc.). Availability and reliability of Probes will depend on the device being used.
    /// </summary>
    public abstract class Probe : INotifyPropertyChanged
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
        public event PropertyChangedEventHandler PropertyChanged;

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
        private DataRateCalculator _rawRateCalculator;
        private DataRateCalculator _storageRateCalculator;
        private DataRateCalculator _uiUpdateRateCalculator;

        private readonly object _locker = new object();

        [JsonIgnore]
        public abstract string DisplayName { get; }

        [JsonIgnore]
        public abstract string CollectionDescription { get; }

        /// <summary>
        /// Whether the <see cref="Probe"/> should be turned on when the user starts the <see cref="Protocol"/>.
        /// </summary>
        /// <value><c>true</c> if enabled; otherwise, <c>false</c>.</value>
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
                        {
                            StartAsync();
                        }
                        else
                        {
                            StopAsync();
                        }
                    }

                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Enabled)));
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

        /// <summary>
        /// Whether the Probe should store the data it collects. This might be turned off if the <see cref="Probe"/> is used to trigger 
        /// the <see cref="User.Scripts.ScriptProbe"/> but the probed data are not needed.
        /// </summary>
        /// <value><c>true</c> if store data; otherwise, <c>false</c>.</value>
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

        [JsonIgnore]
        protected abstract long DataRateSampleSize { get; }

        public abstract double? MaxDataStoresPerSecond { get; set; }

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

        /// <summary>
        /// How much data to save from the <see cref="Probe"/>  for the purpose of charting within the Sensus app.
        /// </summary>
        /// <value>The maximum chart data count.</value>
        [EntryIntegerUiProperty("Max Chart Data Count:", true, 50, true)]
        public int MaxChartDataCount
        {
            get
            {
                return _maxChartDataCount;
            }
            set
            {
                if (value > 0)
                {
                    _maxChartDataCount = value;
                }

                // trim chart data collection
                lock (_chartData)
                {
                    while (_chartData.Count > 0 && _chartData.Count > _maxChartDataCount)
                    {
                        _chartData.RemoveAt(0);
                    }
                }
            }
        }

        [JsonIgnore]
        public string Caption
        {
            get
            {
                string type = "";
                if (this is ListeningProbe)
                {
                    type = "Listening";
                }
                else if (this is PollingProbe)
                {
                    type = "Polling";
                }

                return DisplayName + (type == "" ? "" : " (" + type + ")");
            }
        }

        [JsonIgnore]
        public string SubCaption
        {
            get
            {
                return _mostRecentDatum == null ? "[no data]" : _mostRecentDatum.DisplayDetail + "  " + _mostRecentDatum.Timestamp.ToLocalTime();
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

            // data rate calculators
            _rawRateCalculator = new DataRateCalculator(DataRateSampleSize, MaxDataStoresPerSecond);  // track/limit the raw data rate
            _storageRateCalculator = new DataRateCalculator(DataRateSampleSize);                      // track the storage rate
            _uiUpdateRateCalculator = new DataRateCalculator(DataRateSampleSize, 1);                  // track/limit the UI update rate
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
                ProtectedStart();
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
                SensusServiceHelper.Get().FlashNotificationAsync(message);

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
        protected virtual void ProtectedStart()
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

                    _rawRateCalculator.Start();
                    _storageRateCalculator.Start();
                    _uiUpdateRateCalculator.Start();
                }
            }
        }

        /// <summary>
        /// Stores a <see cref="Datum"/> within the <see cref="LocalDataStore"/>. Will not throw an <see cref="Exception"/>.
        /// </summary>
        /// <param name="datum">Datum.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public void StoreDatum(Datum datum, CancellationToken? cancellationToken = null)
        {
            // track/limit the raw rate of non-null data. all null data will pass this test, and this is 
            // fine given such data are generated by polling probes when no data were retrieved.
            if (datum != null && _rawRateCalculator.Add(datum) == DataRateCalculator.SamplingAction.Drop)
            {
                return;
            }

            // set properties that we were unable to set within the datum constructor. datum is allowed to 
            // be null, indicating the the probe attempted to obtain data but it didn't find any (in the 
            // case of polling probes).
            if (datum != null)
            {
                datum.ProtocolId = Protocol.Id;
                datum.ParticipantId = Protocol.ParticipantId;
            }

            // track the most recent datum regardless of whether the datum is null or whether we're storing data
            Datum previousDatum = _mostRecentDatum;
            _mostRecentDatum = datum;
            _mostRecentStoreTimestamp = DateTimeOffset.UtcNow;

            // fire events to notify observers of the stored data and associated UI values
            MostRecentDatumChanged?.Invoke(this, new Tuple<Datum, Datum>(previousDatum, _mostRecentDatum));

            // don't update the UI too often, as doing so at really high rates causes UI deadlocks. always let
            // null data update the UI, as these are only generated by polling probes at low rates.
            if (datum == null || _uiUpdateRateCalculator.Add(datum) == DataRateCalculator.SamplingAction.Keep)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SubCaption)));
            }

            // store non-null data
            if (_storeData && datum != null)
            {
                #region update chart data
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
                #endregion

                // write datum to local data store. catch any exceptions, as the caller (e.g., a listening 
                // probe) could very well be unprotected on the UI thread. throwing an exception here can crash the app.
                try
                {
                    _protocol.LocalDataStore.WriteDatum(datum, cancellationToken.GetValueOrDefault());

                    // track the storage rate
                    _storageRateCalculator.Add(datum);
                }
                catch (Exception ex)
                {
                    SensusServiceHelper.Get().Logger.Log("Failed to write datum:  " + ex, LoggingLevel.Normal, GetType());
                }
            }
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

        public virtual bool TestHealth(ref List<Tuple<string, Dictionary<string, string>>> events)
        {
            bool restart = false;

            if (!_running)
            {
                restart = true;
            }

            string eventName = TrackedEvent.Health + ":" + GetType().Name;
            Dictionary<string, string> properties = new Dictionary<string, string>
            {
                { "Running", _running.ToString() }
            };

            // we'll only have data rates if the probe is running -- it might have failed to initialize.
            if (_running)
            {
                double? rawDataPerSecond = _rawRateCalculator.GetDataPerSecond();
                double? storedDataPerSecond = _storageRateCalculator.GetDataPerSecond();
                double? percentageNominalStoreRate = null;
                if (storedDataPerSecond.HasValue && MaxDataStoresPerSecond.HasValue)
                {
                    percentageNominalStoreRate = storedDataPerSecond.Value / MaxDataStoresPerSecond.Value;
                }

                properties.Add("Percentage Nominal Storage Rate", Convert.ToString(percentageNominalStoreRate?.RoundToWhole(5)));

                Analytics.TrackEvent(eventName, properties);

                // we don't have a great way of tracking data rates, as they are continuous values and event tracking is string-based. so,
                // just add the rates to the properties after event tracking. this way it will still be included in the status.
                properties.Add("Raw Data / Second", Convert.ToString(rawDataPerSecond));
                properties.Add("Stored Data / Second", Convert.ToString(storedDataPerSecond));
            }
            else
            {
                Analytics.TrackEvent(eventName, properties);
            }

            events.Add(new Tuple<string, Dictionary<string, string>>(eventName, properties));

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