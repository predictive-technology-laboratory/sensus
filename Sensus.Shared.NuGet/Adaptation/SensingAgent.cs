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

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Sensus.Adaptation
{
    /// <summary>
    /// An agent that observes data collected by the app and controls sensing parameters. See the
    /// [adaptive sensing](xref:adaptive_sensing) article for more information.
    /// </summary>
    public abstract partial class SensingAgent : INotifyPropertyChanged
    {
        /// <summary>
        /// Specifies a check to run periodically to (1) assess whether sensing control has been 
        /// completed and (2) to transition sensing parameters into subsequent settings.
        /// </summary>
        public class ControlCompletionCheck
        {
            public delegate Task<SensingAgentState> ControlCompletionCheckAsyncDelegate(CancellationToken cancellationToken);

            public ControlCompletionCheckAsyncDelegate CheckControlCompletionAsync { get; set; }
            public TimeSpan CheckControlCompletionInterval { get; set; }
            public string UserNotificationMessage { get; set; }
            public string NotificationUserResponseMessage { get; set; }

            public ControlCompletionCheck(ControlCompletionCheckAsyncDelegate checkControlCompletionAsync, TimeSpan checkControlCompletionInterval, string userNotificationMessage, string notificationUserResponseMessage)
            {
                CheckControlCompletionAsync = checkControlCompletionAsync;
                CheckControlCompletionInterval = checkControlCompletionInterval;
                UserNotificationMessage = userNotificationMessage;
                NotificationUserResponseMessage = notificationUserResponseMessage;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private readonly Dictionary<Type, List<IDatum>> _typeData = new Dictionary<Type, List<IDatum>>();

        private readonly object _stateLocker = new object();

        /// <summary>
        /// Whether or not the <see cref="SensingAgent"/> is in the process of transitioning to another <see cref="SensingAgentState"/>.
        /// </summary>
        private bool _stateIsTransitioning = false;

        /// <summary>
        /// Gets the <see cref="ISensusServiceHelper"/>.
        /// </summary>
        /// <value>The sensus service helper.</value>
        public ISensusServiceHelper SensusServiceHelper { get; set; }

        /// <summary>
        /// Gets the <see cref="IProtocol"/>.
        /// </summary>
        /// <value>The protocol.</value>
        public IProtocol Protocol { get; set; }

        /// <summary>
        /// Current <see cref="SensingAgentState"/> of the <see cref="SensingAgent"/>.
        /// </summary>
        /// <value>The state.</value>
        public SensingAgentState State { get; private set; }

        /// <summary>
        /// Unique identifier for the <see cref="SensingAgent"/>.
        /// </summary>
        /// <value>The identifier.</value>
        public string Id { get; set; }

        /// <summary>
        /// Readable description for the <see cref="SensingAgent"/>.
        /// </summary>
        /// <value>The description.</value>
        public string Description { get; set; }

        /// <summary>
        /// Interval of time between successive calls to <see cref="ActAsync(CancellationToken)"/>. If <c>null</c>,
        /// then a repeating call to <see cref="ActAsync(CancellationToken)"/> will not be made and only calls to 
        /// <see cref="ObserveAsync(IDatum,CancellationToken)"/> will provide opportunities for the 
        /// <see cref="SensingAgent"/> to control sensing parameters.
        /// </summary>
        /// <value>The action interval.</value>
        public TimeSpan? ActionInterval { get; set; }

        /// <summary>
        /// Tolerance for <see cref="ActionInterval"/> before the scheduled time, if doing so 
        /// will increase the number of batched actions and thereby decrease battery consumption.
        /// </summary>
        /// <value>The delay tolerance before.</value>
        public TimeSpan? ActionIntervalToleranceBefore { get; set; }

        /// <summary>
        /// Tolerance for <see cref="ActionInterval"/> after the scheduled time, if doing so 
        /// will increase the number of batched actions and thereby decrease battery consumption.
        /// </summary>
        /// <value>The delay tolerance before.</value>
        public TimeSpan? ActionIntervalToleranceAfter { get; set; }

        /// <summary>
        /// How long to observe data before checking control criteria. If <see cref="ActionInterval"/>
        /// is not <c>null</c>, then this value may not be <c>null</c> either. If <see cref="ActionInterval"/>
        /// is <c>null</c>, then this value is ignored.
        /// </summary>
        /// <value>The observation interval.</value>
        public TimeSpan? ObservationDuration { get; set; }

        /// <summary>
        /// How much time between checks for control completion.
        /// </summary>
        /// <value>The control completion check interval.</value>
        public TimeSpan ControlCompletionCheckInterval { get; set; }

        /// <summary>
        /// Maximum number of observed <see cref="IDatum"/> readings to retain for state estimation. Can be
        /// <c>null</c> to place no limit on the number of retained readings; however, either
        /// <see cref="MaxObservedDataCount"/> or <see cref="MaxObservedDataAge"/> should be enabled, or
        /// readings will be retained indefinitely, likely exhausting memory over time.
        /// </summary>
        /// <value>The max observed data count.</value>
        public int? MaxObservedDataCount { get; set; }

        /// <summary>
        /// Maximum age of observed <see cref="IDatum"/> readings to retain for state estimation. Can be
        /// <c>null</c> to place no limit on the age of retained readings; however, either
        /// <see cref="MaxObservedDataCount"/> or <see cref="MaxObservedDataAge"/> should be enabled, or
        /// readings will be retained indefinitely, likely exhausting memory over time.
        /// </summary>
        /// <value>The max observed data age.</value>
        public TimeSpan? MaxObservedDataAge { get; set; }

        /// <summary>
        /// A human-readable description of the <see cref="SensingAgent"/>'s current state. Used to track
        /// the state trajectory of the <see cref="SensingAgent"/> and to tag each <see cref="IDatum"/>
        /// produced while the <see cref="SensingAgent"/> is exerting sensing control.
        /// </summary>
        /// <value>The state description.</value>
        public abstract string StateDescription { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Sensus.SensingAgent"/> class, with a repeating action call.
        /// </summary>
        /// <param name="id">Identifier.</param>
        /// <param name="description">Description.</param>
        /// <param name="controlCompletionCheckInterval">Control completion check interval.</param>
        /// <param name="actionInterval">Action interval.</param>
        /// <param name="observationDuration">Observation duration.</param>
        protected SensingAgent(string id, string description, TimeSpan controlCompletionCheckInterval, TimeSpan actionInterval, TimeSpan observationDuration)
        {
            Construct(id, description, controlCompletionCheckInterval, actionInterval, observationDuration);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Sensus.SensingAgent"/> class, without a repeating action call.
        /// </summary>
        /// <param name="id">Identifier.</param>
        /// <param name="description">Description.</param>
        /// <param name="controlCompletionCheckInterval">Control completion check interval.</param>
        protected SensingAgent(string id, string description, TimeSpan controlCompletionCheckInterval)
        {
            Construct(id, description, controlCompletionCheckInterval, null, null);
        }

        /// <summary>
        /// Private constructor.
        /// </summary>
        /// <param name="id">Identifier.</param>
        /// <param name="description">Description.</param>
        /// <param name="controlCompletionCheckInterval">Control completion check interval.</param>
        /// <param name="actionInterval">Action interval.</param>
        /// <param name="observationDuration">Observation duration.</param>
        private void Construct(string id, string description, TimeSpan controlCompletionCheckInterval, TimeSpan? actionInterval, TimeSpan? observationDuration)
        {
            Id = id;
            Description = description;
            ControlCompletionCheckInterval = controlCompletionCheckInterval;
            ActionInterval = actionInterval;
            ObservationDuration = observationDuration;
            MaxObservedDataCount = 100;
        }

        /// <summary>
        /// Initializes the <see cref="SensingAgent"/>. This is called when the <see cref="IProtocol"/> associated with this
        /// <see cref="SensingAgent"/> is started.
        /// </summary>
        public virtual async Task InitializeAsync()
        {
            State = SensingAgentState.Idle;

            // download the initial policy
            try
            {
                await Protocol.UpdateSensingAgentPolicyAsync(CancellationToken.None);
            }
            catch (Exception ex)
            {
                SensusServiceHelper?.Logger.Log("Exception while downloading the policy:  " + ex.Message, LoggingLevel.Normal, GetType());
            }
        }

        /// <summary>
        /// Sets the control policy of the current <see cref="SensingAgent"/>. This method will be called in the following
        /// situations:
        /// 
        ///   * When a push notification arrives with a new policy.
        ///   * When the <see cref="SensingAgent"/> itself instructs the app to update the policy, through a call to
        ///     <see cref="IProtocol.UpdateSensingAgentPolicyAsync(System.Threading.CancellationToken)"/>.
        ///   * When the user manually sets the policy from with the <see cref="Protocol"/> settings.
        /// 
        /// In any case, the new policy will be passed to this method as a <see cref="JObject"/>. Note that when overriding
        /// this method, the base class should be called at the end of the overriding method.
        /// </summary>
        /// <param name="policy">Policy.</param>
        public virtual async Task SetPolicyAsync(JObject policy)
        {
            Id = policy["id"].ToString();
            Description = policy["description"].ToString();
            ActionInterval = TimeSpan.Parse(policy["action-interval"].ToString());
            ObservationDuration = TimeSpan.Parse(policy["observation-duration"].ToString());
            ControlCompletionCheckInterval = TimeSpan.Parse(policy["control-completion-check-interval"].ToString());

            JObject observedDataSettings = policy["observed-data"] as JObject;
            MaxObservedDataCount = observedDataSettings["max-count"].ToObject<int?>();
            MaxObservedDataAge = observedDataSettings["max-age"].ToObject<TimeSpan?>();

            // save policy within app state (agent itself is not serialized)
            Protocol.AgentPolicy = policy;
            await SensusServiceHelper.SaveAsync();
        }

        /// <summary>
        /// Asks the agent to observe an <see cref="IDatum"/> object that was generated by Sensus.
        /// </summary>
        /// <returns>A <see cref="ControlCompletionCheck"/> to be configured upon return, or <c>null</c> for no such check.</returns>
        /// <param name="datum">Datum.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public async Task<ControlCompletionCheck> ObserveAsync(IDatum datum, CancellationToken cancellationToken)
        {
            // accumulate observed data by type for later analysis
            lock (_typeData)
            {
                Type datumType = datum.GetType();

                if (!_typeData.TryGetValue(datumType, out List<IDatum> data))
                {
                    data = new List<IDatum>();
                    _typeData.Add(datumType, data);
                }

                data.Add(datum);

                UpdateObservedData(_typeData);
            }

            // check control criterion and begin opportunistic control if warranted. note that we
            // do not in principal need to check the state before doing this. if we disregard the
            // state and the observed data meet the control criterion, then the state machine will
            // engage to restrict transition to opportunistic control as appropriate. however, 
            // letting allowing this sequence of events to unfold, only to arrive at a prohibited
            // transition to opportunistic control is particularly costly given the context of the
            // current method. the current method will be called upon arrival of each stored datum,
            // the rate of which will be substantial (e.g., for accelerometry). control criterion
            // checking can be costly if large amounts of data are processed. thus, it makes sense
            // to do a bit of preemptive state checking and only proceed with criterion evaluation
            // when the outcome might possibly be useful. there is certainly a race condition here,
            // in that the current state might be idle causing us to proceed, but by the time we
            // attempt to transition to opportunistic control another call might have already 
            // transitioned the state. this is fine.
            ControlCompletionCheck opportunisticControlCompletionCheck = null;
            if (State == SensingAgentState.Idle && ObservedDataMeetControlCriterion())
            {
                try
                {
                    opportunisticControlCompletionCheck = await BeginControlAsync(SensingAgentState.OpportunisticControl, cancellationToken);
                }
                catch (Exception ex)
                {
                    await EndControlAsync(cancellationToken);
                    throw ex;
                }
            }

            return opportunisticControlCompletionCheck;
        }

        /// <summary>
        /// Updates the observed data (e.g., by trimming observed data to a particular size and/or time range). When overriding
        /// this method, be sure to call the base class implementation if you wish to apply the size and age restrictions 
        /// provided here.
        /// </summary>
        /// <param name="typeData">Observed data, by type. This collection will be locked prior to calling the method.</param>
        protected virtual void UpdateObservedData(Dictionary<Type, List<IDatum>> typeData)
        {
            foreach (Type type in typeData.Keys)
            {
                List<IDatum> data = typeData[type];

                // trim collection by size
                int maxCount = MaxObservedDataCount.GetValueOrDefault(int.MaxValue);
                while (data.Count > maxCount)
                {
                    data.RemoveAt(0);
                }

                // trim collection by age. oldest data are first, so we can stop as soon as
                // we find a datum that doesn't exceed the age threshold.
                TimeSpan maxAge = MaxObservedDataAge.GetValueOrDefault(TimeSpan.MaxValue);
                while (data.Count > 0 && DateTimeOffset.UtcNow - data[0].Timestamp > maxAge)
                {
                    data.RemoveAt(0);
                }
            }
        }

        /// <summary>
        /// Checks whether the observed data meet a control criterion.
        /// </summary>
        /// <returns><c>true</c>, if data meet control criterion, <c>false</c> otherwise.</returns>
        private bool ObservedDataMeetControlCriterion()
        {
            lock (_typeData)
            {
                return ObservedDataMeetControlCriterion(_typeData);
            }
        }

        /// <summary>
        /// Checks whether the observed data meet a control criterion.
        /// </summary>
        /// <returns><c>true</c>, if data meet control criterion, <c>false</c> otherwise.</returns>
        /// <param name="typeData">All data by type. This collection will be locked prior to calling the concrete implementation.</param>
        protected abstract bool ObservedDataMeetControlCriterion(Dictionary<Type, List<IDatum>> typeData);

        /// <summary>
        /// Gets observed <see cref="IDatum"/> objects for a particular type.
        /// </summary>
        /// <returns>The observed data.</returns>
        /// <typeparam name="DatumInterface">The type of data to retrieve.</typeparam>
        protected List<IDatum> GetObservedData<DatumInterface>() where DatumInterface : IDatum
        {
            lock (_typeData)
            {
                List<IDatum> observedData = new List<IDatum>();

                foreach (Type type in _typeData.Keys)
                {
                    if (type.GetInterfaces().Contains(typeof(DatumInterface)))
                    {
                        observedData = _typeData[type];
                        break;
                    }
                }

                return observedData;
            }
        }

        /// <summary>
        /// Requests that the <see cref="SensingAgent"/> consider beginning sensing control.
        /// </summary>
        /// <returns>A <see cref="ControlCompletionCheck"/> to be configured upon return, or <c>null</c> for no such check.</returns>
        /// <param name="cancellationToken">Cancellation token.</param>
        public async Task<ControlCompletionCheck> ActAsync(CancellationToken cancellationToken)
        {
            try
            {
                ControlCompletionCheck controlCompletionCheck = null;

                if (await TransitionToNewStateAsync(SensingAgentState.ActiveObservation, cancellationToken))
                {
                    // observe data for the specified duration. the current method is run as a scheduled callback, so we're guaranteed
                    // to have some amount of background time. but watch out for background time expiration on iOS by monitoring the
                    // passed cancellation token.
                    try
                    {
                        SensusServiceHelper.Logger.Log("Sensing agent " + Id + " is observing data for " + ObservationDuration.Value + ".", LoggingLevel.Normal, GetType());

                        await Task.Delay(ObservationDuration.Value, cancellationToken);
                    }
                    // catch cancellation exception (e.g., due to background time expiration), but let other exceptions percolate back 
                    // up (they'll be caught and reported to app center).
                    catch (OperationCanceledException ex)
                    {
                        SensusServiceHelper.Logger.Log("Observation cancelled:  " + ex.Message, LoggingLevel.Normal, GetType());
                    }

                    if (ObservedDataMeetControlCriterion())
                    {
                        controlCompletionCheck = await BeginControlAsync(SensingAgentState.ActiveControl, cancellationToken);
                    }
                    else
                    {
                        await TransitionToNewStateAsync(SensingAgentState.Idle, cancellationToken);
                    }
                }

                return controlCompletionCheck;
            }
            catch (Exception ex)
            {
                // if anything goes wrong, ensure that we terminate sensing control and leave ourselves in the idle state.
                await EndControlAsync(cancellationToken);
                throw ex;
            }
        }

        private async Task<ControlCompletionCheck> BeginControlAsync(SensingAgentState controlState, CancellationToken cancellationToken)
        {
            ControlCompletionCheck controlCompletionCheck = null;

            if (await TransitionToNewStateAsync(controlState, cancellationToken))
            {
                controlCompletionCheck = new ControlCompletionCheck(async controlCompletionCheckCancellationToken =>
                {
                    // the current check is called when a protocol is shutting down, in which case we should end
                    // control and return to idle. in addition, the current check is called periodically while the
                    // protocol remains running. if the conrol criterion is not met, then also end control.
                    if (Protocol.State != ProtocolState.Running || !ObservedDataMeetControlCriterion())
                    {
                        await EndControlAsync(controlCompletionCheckCancellationToken);
                    }

                    return State;

                }, ControlCompletionCheckInterval, "Sensus would like to measure your environment. Please open this notification.", "Measuring environment. You may close this alert.");
            }

            return controlCompletionCheck;
        }

        private async Task EndControlAsync(CancellationToken cancellationToken)
        {
            try
            {
                await TransitionToNewStateAsync(SensingAgentState.EndingControl, cancellationToken);
            }
            finally
            {
                await TransitionToNewStateAsync(SensingAgentState.Idle, cancellationToken);
            }
        }

        /// <summary>
        /// The state machine for all <see cref="SensingAgent"/> classes.
        /// </summary>
        /// <returns><c>true</c>, if the <see cref="SensingAgent"/> transitioned into the new state, <c>false</c> otherwise. If the
        /// new state equals the current state, then <c>false</c> will be returned indicating no state change.</returns>
        /// <param name="newState">New state.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        private async Task<bool> TransitionToNewStateAsync(SensingAgentState newState, CancellationToken cancellationToken)
        {
            bool stateChanged = false;

            lock (_stateLocker)
            {
                if (_stateIsTransitioning)
                {
                    SensusServiceHelper.Logger.Log("State is already transitioning. Aborting transition request.", LoggingLevel.Normal, GetType());
                }
                else if (State == newState)
                {
                    SensusServiceHelper.Logger.Log("Continuing current state " + State + ".", LoggingLevel.Normal, GetType());
                }
                else
                {
                    bool transitionPermitted = false;

                    if (State == SensingAgentState.Idle)
                    {
                        transitionPermitted = newState == SensingAgentState.OpportunisticControl ||
                                              newState == SensingAgentState.ActiveObservation ||
                                              newState == SensingAgentState.EndingControl;
                    }
                    else if (State == SensingAgentState.OpportunisticControl)
                    {
                        transitionPermitted = newState == SensingAgentState.EndingControl;
                    }
                    else if (State == SensingAgentState.ActiveObservation)
                    {
                        transitionPermitted = newState == SensingAgentState.ActiveControl ||
                                              newState == SensingAgentState.Idle ||
                                              newState == SensingAgentState.EndingControl;
                    }
                    else if (State == SensingAgentState.ActiveControl)
                    {
                        transitionPermitted = newState == SensingAgentState.EndingControl;
                    }
                    else if (State == SensingAgentState.EndingControl)
                    {
                        transitionPermitted = newState == SensingAgentState.Idle;
                    }

                    if (transitionPermitted)
                    {
                        SensusServiceHelper.Logger.Log("Permitting transition from " + State + " to " + newState + ".", LoggingLevel.Normal, GetType());

                        // record the state change within the protocol's local data store
                        Protocol.WriteSensingAgentStateDatum(State, newState, StateDescription, cancellationToken);

                        // set new state
                        State = newState;
                        stateChanged = true;
                        _stateIsTransitioning = true;
                    }
                    else
                    {
                        SensusServiceHelper.Logger.Log("Prohibiting transition from " + State + " to " + newState + ".", LoggingLevel.Normal, GetType());
                    }
                }
            }

            if (stateChanged)
            {
                // call the concrete control methods. catch exceptions as we don't know who is implementing them.
                try
                {
                    if (State == SensingAgentState.OpportunisticControl)
                    {
                        await OnOpportunisticControlAsync(cancellationToken);
                    }
                    else if (State == SensingAgentState.ActiveControl)
                    {
                        await OnActiveControlAsync(cancellationToken);
                    }
                    else if (State == SensingAgentState.EndingControl)
                    {
                        await OnEndingControlAsync(cancellationToken);
                    }
                }
                catch (Exception ex)
                {
                    SensusServiceHelper.Logger.Log("Exception while calling concrete control method from state " + State + ":  " + ex.Message, LoggingLevel.Normal, GetType());
                    throw ex;
                }
                finally
                {
                    FireStateChangedEvent();
                    _stateIsTransitioning = false;
                }
            }

            return stateChanged;
        }

        /// <summary>
        /// Called when opportunistic control is starting.
        /// </summary>
        /// <returns>Task.</returns>
        /// <param name="cancellationToken">Cancellation token.</param>
        protected abstract Task OnOpportunisticControlAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Called when active control is starting.
        /// </summary>
        /// <returns>Task.</returns>
        /// <param name="cancellationToken">Cancellation token.</param>
        protected abstract Task OnActiveControlAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Called when control is ending, e.g., due to control completion, an exception when
        /// starting control, or other conditions that result in the end of control. The
        /// concrete implementation of this method should not assume that the agent is in any
        /// particular state when this method is called, as it can be called for several reasons.
        /// </summary>
        /// <returns>The ending control async.</returns>
        /// <param name="cancellationToken">Cancellation token.</param>
        protected abstract Task OnEndingControlAsync(CancellationToken cancellationToken);

        private void FireStateChangedEvent()
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(State)));
        }

        public override string ToString()
        {
            return Id + ":  " + Description;
        }
    }
}