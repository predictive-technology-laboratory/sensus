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
        public TimeSpan? ActiveObservationDuration { get; set; }

        /// <summary>
        /// How much time between checks for control completion.
        /// </summary>
        /// <value>The control completion check interval.</value>
        public TimeSpan ControlCompletionCheckInterval { get; set; }

        /// <summary>
        /// Maximum number of observed <see cref="IDatum"/> readings to retain for state estimation. Can be
        /// <c>null</c> to place no limit on the number of retained readings; however, either
        /// <see cref="MaxObservedDataCount"/> or <see cref="MaxObservedDataAge"/> should be enabled, or
        /// readings will be retained indefinitely, likely exhausting memory over time. This setting applies
        /// to all <see cref="IDatum"/> regardless of type. As different types of <see cref="IDatum"/> have 
        /// dramatically different data rates (e.g., <see cref="Probes.Movement.IAccelerometerDatum"/> versus
        /// <see cref="Probes.Movement.IActivityDatum"/>), this should be set sufficiently high to avoid
        /// data loss.
        /// </summary>
        /// <value>The max observed data count.</value>
        public int? MaxObservedDataCount { get; set; }

        /// <summary>
        /// Maximum age of observed <see cref="IDatum"/> readings to retain for state estimation. Can be
        /// <c>null</c> to place no limit on the age of retained readings; however, either
        /// <see cref="MaxObservedDataCount"/> or <see cref="MaxObservedDataAge"/> should be enabled, or
        /// readings will be retained indefinitely, likely exhausting memory over time. This setting applies
        /// to all <see cref="IDatum"/> regardless of type. As different types of <see cref="IDatum"/> have 
        /// dramatically different data rates (e.g., <see cref="Probes.Movement.IAccelerometerDatum"/> versus
        /// <see cref="Probes.Movement.IActivityDatum"/>), this should be set sufficiently high to avoid
        /// data loss.
        /// </summary>
        /// <value>The max observed data age.</value>
        public TimeSpan? MaxObservedDataAge { get; set; }

        /// <summary>
        /// A human-readable description of the <see cref="SensingAgent"/>'s current state. Used to track
        /// the state trajectory of the <see cref="SensingAgent"/> and to tag each <see cref="IDatum"/>
        /// produced while the <see cref="SensingAgent"/> is exerting sensing control.
        /// </summary>
        /// <value>The state description.</value>
        public string StateDescription { get; protected set; }

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
            ActiveObservationDuration = observationDuration;
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
        /// In any case, the new policy will be passed to this method as a <see cref="JObject"/>.
        /// </summary>
        /// <param name="policy">Policy.</param>
        public async Task SetPolicyAsync(JObject policy)
        {
            Id = policy["id"].ToString();
            Description = policy["description"].ToString();
            ActionInterval = TimeSpan.Parse(policy["action-interval"].ToString());
            ActiveObservationDuration = TimeSpan.Parse(policy["active-observation-duration"].ToString());
            ControlCompletionCheckInterval = TimeSpan.Parse(policy["control-completion-check-interval"].ToString());

            JObject observedData = policy["observed-data"] as JObject;
            MaxObservedDataCount = observedData["max-count"].ToObject<int?>();
            MaxObservedDataAge = observedData["max-age"].ToObject<TimeSpan?>();

            await ProtectedSetPolicyAsync(policy);

            // save policy within app state (agent itself is not serialized)
            Protocol.AgentPolicy = policy;
            await SensusServiceHelper.SaveAsync();
        }

        protected abstract Task ProtectedSetPolicyAsync(JObject policy);

        /// <summary>
        /// Asks the agent to observe an <see cref="IDatum"/> object that was generated by Sensus, either during
        /// <see cref="SensingAgentState.OpportunisticObservation"/> or during <see cref="SensingAgentState.ActiveObservation"/>.
        /// </summary>
        /// <returns>A <see cref="ControlCompletionCheck"/> to be configured upon return, or <c>null</c> for no such check.</returns>
        /// <param name="datum">Datum.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public async Task<ControlCompletionCheck> ObserveAsync(IDatum datum, CancellationToken cancellationToken)
        {
            // certain probes (e.g., ios activity polling) return a null datum to signal that polling occurred
            // but no data were returned. ignore any such null readings when observing.
            if (datum == null)
            {
                return null;
            }

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

            // run opportunistic observation and control if warranted
            ControlCompletionCheck opportunisticControlCompletionCheck = null;
            try
            {
                // the current method is called at high rates during normal operation (e.g., when observing the 
                // accelerometer). we want to avoid flooding the local data store with an agent-state datum for
                // each call, so don't write them here.
                if (await TransitionToNewStateAsync(SensingAgentState.OpportunisticObservation, false, cancellationToken))
                {
                    if (ObservedDataMeetControlCriterion())
                    {
                        opportunisticControlCompletionCheck = await BeginControlAsync(SensingAgentState.OpportunisticControl, cancellationToken);
                    }
                    else
                    {
                        await TransitionToNewStateAsync(SensingAgentState.Idle, false, cancellationToken);
                    }
                }
            }
            catch (Exception ex)
            {
                await ReturnToIdle(cancellationToken);
                throw ex;
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

                // trim collection by age. we can't assume that the data are sorted, so 
                // we'll need to scan the entire collection.
                if (MaxObservedDataAge != null)
                {
                    for (int i = 0; i < data.Count;)
                    {
                        TimeSpan currentAge = DateTimeOffset.UtcNow - data[i].Timestamp;

                        if (currentAge > MaxObservedDataAge.Value)
                        {
                            data.RemoveAt(i);
                        }
                        else
                        {
                            ++i;
                        }
                    }
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
                // catch any exceptions, as we don't know who will implement the method.
                try
                {
                    return ObservedDataMeetControlCriterion(_typeData);
                }
                catch (Exception ex)
                {
                    SensusServiceHelper.Logger.Log("Exception while checking observed data against control criterion:  " + ex.Message, LoggingLevel.Normal, GetType());
                    return false;
                }
            }
        }

        /// <summary>
        /// Checks whether the observed data meet a control criterion. Will be called during <see cref="SensingAgentState.OpportunisticObservation"/> 
        /// and <see cref="SensingAgentState.ActiveObservation"/> to check whether the observed data meet the control criterion, as well as during
        /// <see cref="SensingAgentState.OpportunisticControl"/> and <see cref="SensingAgentState.ActiveControl"/> to check whether the observed
        /// data continue to meet a control criterion and thus require control to continue.
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

                if (await TransitionToNewStateAsync(SensingAgentState.ActiveObservation, true, cancellationToken))
                {
                    // observe data for the specified duration. the current method is run as a scheduled callback, so we're guaranteed
                    // to have some amount of background time. but watch out for background time expiration on iOS by monitoring the
                    // passed cancellation token. an exception will be thrown if it expires, and it will be caught to return the agent 
                    // to idle.
                    SensusServiceHelper.Logger.Log("Sensing agent " + Id + " is actively observing data for " + ActiveObservationDuration.Value + ".", LoggingLevel.Normal, GetType());
                    await Task.Delay(ActiveObservationDuration.Value, cancellationToken);

                    // check criterion and begin control if warranted
                    if (ObservedDataMeetControlCriterion())
                    {
                        controlCompletionCheck = await BeginControlAsync(SensingAgentState.ActiveControl, cancellationToken);
                    }
                    else
                    {
                        await TransitionToNewStateAsync(SensingAgentState.Idle, true, cancellationToken);
                    }
                }

                return controlCompletionCheck;
            }
            catch (Exception ex)
            {
                await ReturnToIdle(cancellationToken);
                throw ex;
            }
        }

        private async Task<ControlCompletionCheck> BeginControlAsync(SensingAgentState controlState, CancellationToken cancellationToken)
        {
            // this is a convenience method for beginning both active and opportunistic control. control state must be one of these two.
            if (controlState != SensingAgentState.ActiveControl && controlState != SensingAgentState.OpportunisticControl)
            {
                throw new Exception("Unrecognized control state:  " + controlState);
            }

            ControlCompletionCheck controlCompletionCheck = null;

            if (await TransitionToNewStateAsync(controlState, true, cancellationToken))
            {
                controlCompletionCheck = new ControlCompletionCheck(async controlCompletionCheckCancellationToken =>
                {
                    // the current check is called when a protocol is shutting down, and periodically while the
                    // protocol remains running. as long as the protocol is running and the observed data meet
                    // the control criterion, continue with sensing control; otherwise, end control and return
                    // to idle.
                    if (Protocol.State == ProtocolState.Running && ObservedDataMeetControlCriterion())
                    {
                        SensusServiceHelper.Logger.Log("Continuing sensing control in state:  " + StateDescription, LoggingLevel.Normal, GetType());
                    }
                    else
                    {
                        await ReturnToIdle(controlCompletionCheckCancellationToken);
                    }

                    return State;

                }, ControlCompletionCheckInterval, "Sensus would like to measure your environment. Please open this notification.", "Measuring environment. You may close this alert.");

                SensusServiceHelper.Logger.Log("Established sensing control in state:  " + StateDescription, LoggingLevel.Normal, GetType());
            }

            return controlCompletionCheck;
        }

        /// <summary>
        /// Returns the <see cref="SensingAgent"/> to <see cref="SensingAgentState.Idle"/>, regardless of the state
        /// it is currently in. Used primarily to ensure that the <see cref="SensingAgent"/> does not become wedged
        /// in a <see cref="SensingAgentState"/> from which it does not emerge.
        /// </summary>
        /// <returns>Task.</returns>
        /// <param name="cancellationToken">Cancellation token.</param>
        private async Task ReturnToIdle(CancellationToken cancellationToken)
        {
            // all states are either (1) idle; (2) states that may transition directly to idle (e.g., opportunistic
            // or active observation); or (3) states that may transition directly to the ending control state, which 
            // may transition directly to idle. therefore, attempting to transition to ending control followed by 
            // transitioning to idle will guarantee that we end up idle. see the state diagram in the documentation.
            try
            {
                await TransitionToNewStateAsync(SensingAgentState.EndingControl, true, cancellationToken);
            }
            finally
            {
                await TransitionToNewStateAsync(SensingAgentState.Idle, true, cancellationToken);
            }
        }

        /// <summary>
        /// The state machine for all <see cref="SensingAgent"/> classes.
        /// </summary>
        /// <returns><c>true</c>, if the <see cref="SensingAgent"/> transitioned into the new state, <c>false</c> otherwise. If the
        /// new state equals the current state, then <c>false</c> will be returned indicating no state change.</returns>
        /// <param name="newState">New state.</param>
        /// <param name="writeSensingAgentStateDatum">Whether to write a <see cref="ISensingAgentStateDatum"/> to the local data store
        /// to record the transition, if made.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        private async Task<bool> TransitionToNewStateAsync(SensingAgentState newState, bool writeSensingAgentStateDatum, CancellationToken cancellationToken)
        {
            bool stateChanged = false;
            SensingAgentState? previousState = null;

            lock (_stateLocker)
            {
                if (!_stateIsTransitioning && newState != State)
                {
                    bool transitionPermitted = false;

                    // the state machine...
                    if (State == SensingAgentState.Idle)
                    {
                        transitionPermitted = newState == SensingAgentState.OpportunisticObservation ||
                                              newState == SensingAgentState.ActiveObservation;
                    }
                    else if (State == SensingAgentState.OpportunisticObservation)
                    {
                        transitionPermitted = newState == SensingAgentState.Idle ||
                                              newState == SensingAgentState.OpportunisticControl;
                    }
                    else if (State == SensingAgentState.OpportunisticControl)
                    {
                        transitionPermitted = newState == SensingAgentState.EndingControl;
                    }
                    else if (State == SensingAgentState.ActiveObservation)
                    {
                        transitionPermitted = newState == SensingAgentState.Idle ||
                                              newState == SensingAgentState.ActiveControl;
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
                        previousState = State;
                        State = newState;
                        stateChanged = true;
                        _stateIsTransitioning = true;
                    }
                }
            }

            if (stateChanged)
            {
                // notify the concrete class. catch exceptions as we don't know who is implementing this method.
                try
                {
                    await OnStateChangedAsync(previousState.Value, State, cancellationToken);

                    if (writeSensingAgentStateDatum)
                    {
                        // record the state change within the protocol's local data store. this needs to come after the 
                        // call to the concrete class implementation of OnStateChangedAsync, as the state description might
                        // be updated in that call, and we'd like to use the new description here.
                        Protocol.WriteSensingAgentStateDatum(previousState.Value, State, StateDescription, cancellationToken);
                    }
                }
                catch (Exception ex)
                {
                    SensusServiceHelper.Logger.Log("Exception while notifying concrete class of change from state " + previousState.Value + " to state " + State + ":  " + ex.Message, LoggingLevel.Normal, GetType());
                    throw ex;
                }
                finally
                {
                    _stateIsTransitioning = false;

                    FireStateChangedEvent();
                }
            }

            return stateChanged;
        }

        protected virtual Task OnStateChangedAsync(SensingAgentState previousState, SensingAgentState currentState, CancellationToken cancellationToken)
        {
            StateDescription = previousState + " --> " + currentState;

            return Task.CompletedTask;
        }

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