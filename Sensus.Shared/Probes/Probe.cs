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
using Sensus.Exceptions;
using Sensus.Probes.User.Scripts;

namespace Sensus.Probes
{
    /// <summary>
    /// Each Probe collects data of a particular type from the device. Sensus contains Probes for many of the hardware sensors present on many 
    /// smartphones as well as several software events (e.g., receipt of SMS messages). Sensus also contains Probes that can prompt the user 
    /// for information, which the user supplies via speech or textual input. Sensus defines a variety of Probes, with platform availability 
    /// and quality varying by device manufacturer (e.g., Apple, Motorola, Samsung, etc.). Availability and reliability of Probes will depend 
    /// on the device being used.
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
        /// Delegate for methods that handle <see cref="MostRecentDatumChanged"/> events from <see cref="Probe"/>s.
        /// </summary>
        public delegate Task MostRecentDatumChangedDelegateAsync(Datum previous, Datum current);

        /// <summary>
        /// Fired when the most recently sensed datum is changed, regardless of whether the datum was stored.
        /// </summary>
        public event MostRecentDatumChangedDelegateAsync MostRecentDatumChanged;

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
        private EventHandler<bool> _powerConnectionChanged;
        private CancellationTokenSource _processDataCanceller;
        private readonly object _restartLocker = new object();
        private bool _restarting = false;

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

        /// <summary>
        /// Whether or not to allow the user to disable this <see cref="Probe"/> when starting the <see cref="Protocol"/>.
        /// </summary>
        /// <value>Allow user to disable on start up.</value>
        [OnOffUiProperty("Allow Disable On Startup:", true, 5)]
        public bool AllowDisableOnStartUp { get; set; } = false;

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
                // get and check reference to most recent datum, as it might change due to
                // concurrent access by probe reading.
                Datum mostRecentDatum = _mostRecentDatum;
                if (mostRecentDatum == null)
                {
                    return "[no data]";
                }
                else
                {
                    return mostRecentDatum.DisplayDetail + "  " + mostRecentDatum.Timestamp.ToLocalTime();
                }
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

            _powerConnectionChanged = async (sender, connected) =>
            {
                if (connected)
                {
                    // ask the probe to start processing its data
                    try
                    {
                        SensusServiceHelper.Get().Logger.Log("AC power connected. Initiating data processing within probe.", LoggingLevel.Normal, GetType());
                        _processDataCanceller = new CancellationTokenSource();
                        await ProcessDataAsync(_processDataCanceller.Token);
                        SensusServiceHelper.Get().Logger.Log("Probe data processing complete.", LoggingLevel.Normal, GetType());
                    }
                    catch (OperationCanceledException)
                    {
                        // don't report task cancellation exceptions. these are expected whenever the user unplugs the device while processing data.
                        SensusServiceHelper.Get().Logger.Log("Data processing task was cancelled.", LoggingLevel.Normal, GetType());
                    }
                    catch (Exception ex)
                    {
                        // the data processing actually failed prior to cancellation. this should not happen, so report it.
                        SensusException.Report("Non-cancellation exception while processing probe data:  " + ex.Message, ex);
                    }
                }
                else
                {
                    // cancel any previous attempt to process data
                    _processDataCanceller?.Cancel();
                }
            };
        }

        protected virtual Task InitializeAsync()
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

            return Task.CompletedTask;
        }

        public async Task StartAsync()
        {
            // don't attempt to start the probe if it is not enabled. this can happen, e.g., when remote protocol updates
            // disable the probe and the probe is subsequently restarted to take on the update values. bail out.
            if (!Enabled)
            {
                SensusServiceHelper.Get().Logger.Log("Probe is not enabled. Not starting", LoggingLevel.Normal, GetType());
                return;
            }

            try
            {
                await ProtectedStartAsync();
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
                        await StopAsync();
                    }
                    catch (Exception stopException)
                    {
                        SensusServiceHelper.Get().Logger.Log("Failed to stop probe after failing to start it:  " + stopException.Message, LoggingLevel.Normal, GetType());
                    }
                }

                string message = "Sensus failed to start probe \"" + GetType().Name + "\":  " + startException.Message;
                SensusServiceHelper.Get().Logger.Log(message, LoggingLevel.Normal, GetType());
                await SensusServiceHelper.Get().FlashNotificationAsync(message);

                // disable probe if it is not supported on the device (or if the user has elected not to enable it -- e.g., by refusing to log into facebook)
                if (startException is NotSupportedException)
                {
                    Enabled = false;
                }

                throw startException;
            }
        }

        protected virtual async Task ProtectedStartAsync()
        {
            if (_running)
            {
                SensusServiceHelper.Get().Logger.Log("Attempted to start probe, but it was already running.", LoggingLevel.Normal, GetType());
            }
            else
            {
                SensusServiceHelper.Get().Logger.Log("Starting.", LoggingLevel.Normal, GetType());

                await InitializeAsync();

                lock (_startStopTimes)
                {
                    _startStopTimes.Add(new Tuple<bool, DateTime>(true, DateTime.Now));
                    _startStopTimes.RemoveAll(t => t.Item2 < Protocol.ParticipationHorizon);
                }

                _rawRateCalculator.Start();
                _storageRateCalculator.Start();
                _uiUpdateRateCalculator.Start();

                // hook into the AC charge event signal -- add handler to AC broadcast receiver
                SensusContext.Current.PowerConnectionChangeListener.PowerConnectionChanged += _powerConnectionChanged;

                // the probe has successfully initialized and can now be considered started/running. derived classes might
                // still have work to do upon return from the current method, but that's okay as far as code in this class
                // is concerned.
                _running = true;
            }
        }

        /// <summary>
        /// Stores a <see cref="Datum"/> within the <see cref="LocalDataStore"/>. Will not throw an <see cref="Exception"/>.
        /// </summary>
        /// <param name="datum">Datum.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public async Task StoreDatumAsync(Datum datum, CancellationToken? cancellationToken = null)
        {
            // it's possible for the current method to be called when the protocol is not running. the obvious case is when
            // the protocol is paused, but there are other race-like conditions. we try to prevent this (e.g., by forcing 
            // the user to start the protocol before taking a survey saved from a previous run of the app), but there are 
            // probably corner cases we haven't accounted for. at the very least, there are race conditions (e.g., taking a 
            // survey when a protocol is about to stop) that could cause data to be stored without a running protocol.
            if (_protocol.State != ProtocolState.Running)
            {
                return;
            }

            // track/limit the raw rate of non-null data. all null data will pass this test, and this is 
            // fine given such data are generated by polling probes when no data were retrieved. such 
            // return values from polling probes are used to indicate that the poll was completed, which
            // will be reflected in the _mostRecentStoreTimestamp below.
            if (datum != null)
            {
                // impose a limit on the raw data rate
                if (_rawRateCalculator.Add(datum) == DataRateCalculator.SamplingAction.Drop)
                {
                    return;
                }

                // set properties that we were unable to set within the datum constructor.
                datum.ProtocolId = Protocol.Id;
                datum.ParticipantId = Protocol.ParticipantId;

                // tag the data if we're in tagging mode, indicated with a non-null event id on the protocol. avoid 
                // any race conditions related to starting/stopping a tagging by getting the required values and
                // then checking both for validity. we need to guarantee that any tagged datum has both an id and tags.
                string taggedEventId = Protocol.TaggedEventId;
                List<string> taggedEventTags = Protocol.TaggedEventTags.ToList();
                if (!string.IsNullOrWhiteSpace(taggedEventId) && taggedEventTags.Count > 0)
                {
                    datum.TaggedEventId = taggedEventId;
                    datum.TaggedEventTags = taggedEventTags;
                }
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

            // update the timestamp of the most recent store. this is used to calculate storage latency, so we
            // do not restrict its values to those obtained when non-null data are stored (see above). some
            // probes call this method with null data to signal that they have run their collection to completion.
            _mostRecentStoreTimestamp = DateTimeOffset.UtcNow;

            // don't update the UI too often, as doing so at really high rates causes UI deadlocks. always let
            // null data update the UI, as these are only generated by polling probes at low rates.
            if (datum == null || _uiUpdateRateCalculator.Add(datum) == DataRateCalculator.SamplingAction.Keep)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SubCaption)));
            }

            // track the most recent datum regardless of whether the datum is null or whether we're storing data
            Datum previousDatum = _mostRecentDatum;
            _mostRecentDatum = datum;

            // notify observers of the stored data and associated UI values
            await (MostRecentDatumChanged?.Invoke(previousDatum, _mostRecentDatum) ?? Task.CompletedTask);

            // let the script probe's agent observe the data, as long as the probe is enabled and there is an agent.
            Protocol.TryGetProbe(typeof(ScriptProbe), out Probe scriptProbe);
            if (scriptProbe?.Enabled ?? false)
            {
                await ((scriptProbe as ScriptProbe).Agent?.ObserveAsync(datum) ?? Task.CompletedTask);
            }
        }

        /// <summary>
        /// Instructs the current probe to process data that it has collected. This call does not provide the data
        /// to process. Rather, it is up to each probe to cache data in memory or on disk as appropriate, in such a
        /// way that they can be processed when this method is called. This method will only be
        /// called under suitable conditions (e.g., when the device is charging). Any <see cref="Datum"/> objects that
        /// result from this processing should be stored via calls to <see cref="StoreDatumAsync(Datum, CancellationToken?)"/>. 
        /// The <see cref="CancellationToken"/> passed to this method should be monitored carefully when processing data.
        /// If the token is cancelled, then the data processing should abort immediately and the method should return as quickly
        /// as possible. The <see cref="CancellationToken"/> passed to this method should also be passed to 
        /// <see cref="StoreDatumAsync(Datum, CancellationToken?)"/>, as this ensures that all operations associated 
        /// with data storage terminate promptly if the token is cancelled. It is up to the overriding implementation to
        /// handle multiple calls to this method (even in quick succession and/or concurrently) properly.
        /// </summary>
        /// <returns>The data async.</returns>
        /// <param name="cancellationToken">Cancellation token.</param>
        public virtual Task ProcessDataAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
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

        public virtual Task StopAsync()
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

                // unhook from the AC charge event signal -- remove handler to AC broadcast receiver
                SensusContext.Current.PowerConnectionChangeListener.PowerConnectionChanged -= _powerConnectionChanged;
            }
            else
            {
                SensusServiceHelper.Get().Logger.Log("Attempted to stop probe, but it wasn't running.", LoggingLevel.Normal, GetType());
            }

            return Task.CompletedTask;
        }

        public async Task RestartAsync()
        {
            // prevent concurrent restarts
            lock (_restartLocker)
            {
                if (_restarting)
                {
                    return;
                }
                else
                {
                    _restarting = true;
                }
            }

            try
            {
                await StopAsync();
                await StartAsync();
            }
            finally
            {
                _restarting = false;
            }
        }

        public virtual Task<HealthTestResult> TestHealthAsync(List<AnalyticsTrackedEvent> events)
        {
            HealthTestResult result = HealthTestResult.Okay;

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
                    percentageNominalStoreRate = (storedDataPerSecond.Value / MaxDataStoresPerSecond.Value) * 100;
                }

                properties.Add("Percentage Nominal Storage Rate", Convert.ToString(percentageNominalStoreRate?.RoundToWhole(5)));

                Analytics.TrackEvent(eventName, properties);

                // we don't have a great way of tracking data rates, as they are continuous values and event tracking is string-based. so,
                // just add the rates to the properties after event tracking. this way it will still be included in the status.
                properties.Add("Raw Data / Second", Convert.ToString(rawDataPerSecond));
                properties.Add("Stored Data / Second", Convert.ToString(storedDataPerSecond));
            }
            // the probe might not be running because it's in the middle of being restarted. only 
            // return a signal to resart the probe if it is not currently being restarted.
            else if (!_restarting)
            {
                Analytics.TrackEvent(eventName, properties);
                result = HealthTestResult.Restart;
            }

            events.Add(new AnalyticsTrackedEvent(eventName, properties));

            return Task.FromResult(result);
        }

        public virtual Task ResetAsync()
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

            return Task.CompletedTask;
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