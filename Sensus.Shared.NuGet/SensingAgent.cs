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

namespace Sensus
{
    /// <summary>
    /// An agent that observes data collected by the app and controls sensing parameters.
    /// </summary>
    public abstract class SensingAgent : INotifyPropertyChanged
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

        private Dictionary<Type, List<IDatum>> _typeData;
        private ISensusServiceHelper _sensusServiceHelper;
        private IProtocol _protocol;
        private SensingAgentState _state;

        private readonly object _stateLocker = new object();

        /// <summary>
        /// Gets the sensus service helper.
        /// </summary>
        /// <value>The sensus service helper.</value>
        protected ISensusServiceHelper SensusServiceHelper => _sensusServiceHelper;

        /// <summary>
        /// Gets the protocol.
        /// </summary>
        /// <value>The protocol.</value>
        protected IProtocol Protocol => _protocol;

        /// <summary>
        /// Current <see cref="SensingAgentState"/> of the <see cref="SensingAgent"/>.
        /// </summary>
        /// <value>The state.</value>
        public SensingAgentState State => _state;

        /// <summary>
        /// Unique identifier for the <see cref="SensingAgent"/>.
        /// </summary>
        /// <value>The identifier.</value>
        public abstract string Id { get; }

        /// <summary>
        /// Readable description for the <see cref="SensingAgent"/>.
        /// </summary>
        /// <value>The description.</value>
        public abstract string Description { get; }

        /// <summary>
        /// Interval of time between successive calls to <see cref="ActAsync(CancellationToken)"/>. If <c>null</c>
        /// is returned, then a repeating call to <see cref="ActAsync(CancellationToken)"/> will not be made
        /// and only calls to <see cref="ObserveAsync(IDatum)"/> will provide opportunities for the <see cref="SensingAgent"/>
        /// to control sensing parameters.
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
        /// is not <c>null</c>, then this value cannot be <c>null</c> either.
        /// </summary>
        /// <value>The observation interval.</value>
        public TimeSpan? ObservationDuration { get; set; }

        /// <summary>
        /// How much time between checks for control completion.
        /// </summary>
        /// <value>The control completion check interval.</value>
        public TimeSpan ControlCompletionCheckInterval { get; set; }

        protected SensingAgent()
        {
            _typeData = new Dictionary<Type, List<IDatum>>();

            ActionInterval = TimeSpan.FromSeconds(10);
            ObservationDuration = TimeSpan.FromSeconds(5);
            ControlCompletionCheckInterval = TimeSpan.FromSeconds(20);
        }

        /// <summary>
        /// Initializes the <see cref="SensingAgent"/>. This is called when the <see cref="IProtocol"/> associated with this
        /// <see cref="SensingAgent"/> is started.
        /// </summary>
        /// <param name="sensusServiceHelper">A reference to the service helper, which provides access to the app's core functionality.</param>
        /// <param name="protocol">A reference to the <see cref="IProtocol"/> associated with this <see cref="SensingAgent"/>.</param>
        public virtual async Task InitializeAsync(ISensusServiceHelper sensusServiceHelper, IProtocol protocol)
        {
            _sensusServiceHelper = sensusServiceHelper;
            _protocol = protocol;
            _state = SensingAgentState.Idle;

            // download the initial policy
            try
            {
                await _protocol.UpdateSensingAgentPolicyAsync(CancellationToken.None);
            }
            catch (Exception ex)
            {
                _sensusServiceHelper?.Logger.Log("Exception while downloading the policy:  " + ex.Message, LoggingLevel.Normal, GetType());
            }
        }

        /// <summary>
        /// Sets the control policy of the current <see cref="SensingAgent"/>. This method will be called in the following
        /// situations:
        /// 
        ///   * When a push notification arrives with a new policy.
        ///   * When the <see cref="SensingAgent"/> itself instructs the app to update the policy, through a call to
        ///     <see cref="IProtocol.UpdateSensingAgentPolicyAsync(System.Threading.CancellationToken)"/>.
        /// 
        /// In any case, the new policy will be passed to this method as a <see cref="JObject"/>.
        /// </summary>
        /// <param name="policy">Policy.</param>
        public virtual Task SetPolicyAsync(JObject policy)
        {
            ActionInterval = TimeSpan.Parse(policy.GetValue("action-interval").ToString());
            ObservationDuration = TimeSpan.Parse(policy.GetValue("observation-duration").ToString());
            ControlCompletionCheckInterval = TimeSpan.Parse(policy.GetValue("control-completion-check-interval").ToString());

            return Task.CompletedTask;
        }

        /// <summary>
        /// Instructs the current <see cref="SensingAgent"/> to observe data for <see cref="ObservationDuration"/>. Any 
        /// <see cref="IDatum"/> stored by the <see cref="IProtocol"/> associated with this <see cref="SensingAgent"/> 
        /// will be relayed to the <see cref="SensingAgent"/> via the <see cref="ObserveAsync(IDatum)"/> method.
        /// </summary>
        /// <returns>The async.</returns>
        /// <param name="cancellationToken">Cancellation token.</param>
        private async Task ObserveAsync(CancellationToken cancellationToken)
        {
            _sensusServiceHelper.Logger.Log("Sensing agent " + Id + " is observing data for " + ObservationDuration.Value + ".", LoggingLevel.Normal, GetType());

            // catch cancellation exception, but let other exceptions percolate back up (they'll be reported to app center).
            try
            {
                await Task.Delay(ObservationDuration.Value, cancellationToken);
            }
            catch (OperationCanceledException ex)
            {
                _sensusServiceHelper.Logger.Log("Observation cancelled:  " + ex.Message, LoggingLevel.Normal, GetType());
            }
        }

        /// <summary>
        /// Asks the agent to observe an <see cref="IDatum"/> object that was generated by Sensus.
        /// </summary>
        /// <returns>A <see cref="ControlCompletionCheck"/> to be configured upon return, or <c>null</c> for no such check.</returns>
        /// <param name="datum">Datum.</param>
        public async Task<ControlCompletionCheck> ObserveAsync(IDatum datum)
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

                // let the concrete implementation decide how to update the observed (e.g., trim to size, trim by time window, etc.)
                UpdateObservedData(_typeData);
            }

            // initiate opportunistic control if criterion is met for the datum type that was just observed. we 
            // don't want to check the criterion for each data type, as it causes newly observed data that do not
            // meet the control criterion to trigger control attempts for previously observed data that do meet
            // the control criterion.
            ControlCompletionCheck controlCompletionCheck = null;
            if (ObservedDataMeetControlCriterion(datum.GetType()))
            {
                try
                {
                    controlCompletionCheck = await BeginControlAsync(SensingAgentState.OpportunisticControl);
                }
                catch (Exception ex)
                {
                    await EndControl(CancellationToken.None);
                    throw ex;
                }
            }

            return controlCompletionCheck;
        }

        /// <summary>
        /// Updates the observed data.
        /// </summary>
        /// <param name="typeData">Observed data, by type.</param>
        protected abstract void UpdateObservedData(Dictionary<Type, List<IDatum>> typeData);

        /// <summary>
        /// Checks whether any type of observed data meets the control criterion.
        /// </summary>
        /// <returns><c>true</c>, if data meet control criterion, <c>false</c> otherwise.</returns>
        /// <param name="types">Types.</param>
        private bool ObservedDataMeetControlCriterion(params Type[] types)
        {
            lock (_typeData)
            {
                // if a null or empty type array was passed, then use all available types.
                if (types == null || types.Length == 0)
                {
                    types = _typeData.Keys.ToArray();
                }

                // check each type
                foreach (Type type in types)
                {
                    List<IDatum> observedData = GetObservedData(type);

                    if (ObservedDataMeetControlCriterion(observedData))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Checks whether a list of observed <see cref="IDatum"/> meet the control criterion.
        /// </summary>
        /// <returns><c>true</c>, if data meet control criterion was observeded, <c>false</c> otherwise.</returns>
        /// <param name="data">Data.</param>
        protected abstract bool ObservedDataMeetControlCriterion(List<IDatum> data);

        /// <summary>
        /// Gets the observed data, by type.
        /// </summary>
        /// <returns>The observed data.</returns>
        /// <param name="type">Type.</param>
        private List<IDatum> GetObservedData(Type type)
        {
            lock (_typeData)
            {
                _typeData.TryGetValue(type, out List<IDatum> data);
                return data;
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

                if (TransitionToNewState(SensingAgentState.ActiveObservation))
                {
                    // observe data for a window of time. the current method is run as a scheduled callback, so we're guaranteed
                    // to have some amount of background time. watch out for background time expiration on iOS by monitoring the
                    // passed cancellation token.
                    await ObserveAsync(cancellationToken);

                    if (ObservedDataMeetControlCriterion())
                    {
                        controlCompletionCheck = await BeginControlAsync(SensingAgentState.ActiveControl);
                    }
                    else
                    {
                        TransitionToNewState(SensingAgentState.Idle);
                    }
                }

                return controlCompletionCheck;
            }
            catch (Exception ex)
            {
                // if anything goes wrong, ensure that we terminate sensing control and leave ourselves in the idle state.
                await EndControl(cancellationToken);
                throw ex;
            }
        }

        private async Task<ControlCompletionCheck> BeginControlAsync(SensingAgentState controlState)
        {
            ControlCompletionCheck controlCompletionCheck = null;

            if (TransitionToNewState(controlState))
            {
                await _sensusServiceHelper.KeepDeviceAwakeAsync();

                controlCompletionCheck = new ControlCompletionCheck(async controlCompletionCheckCancellationToken =>
                {
                    // the current check is called when a protocol is shutting down, in which case we should end
                    // control and return to idle. in addition, the current check is called periodically while the
                    // protocol remains running. if the conrol criterion is not met, then also end control.
                    if (_protocol.State != ProtocolState.Running || !ObservedDataMeetControlCriterion())
                    {
                        await EndControl(controlCompletionCheckCancellationToken);
                    }

                    return State;

                }, ControlCompletionCheckInterval, "Sensus would like to measure your environment. Please open this notification.", "Measuring environment. You may close this alert.");
            }

            return controlCompletionCheck;
        }

        private async Task EndControl(CancellationToken cancellationToken)
        {
            try
            {
                if (TransitionToNewState(SensingAgentState.EndingControl))
                {
                    await _sensusServiceHelper.LetDeviceSleepAsync();
                }
            }
            finally
            {
                TransitionToNewState(SensingAgentState.Idle);
            }
        }

        /// <summary>
        /// The state machine for all <see cref="SensingAgent"/> classes.
        /// </summary>
        /// <returns><c>true</c>, if to new state was transitioned, <c>false</c> otherwise.</returns>
        /// <param name="newState">New state.</param>
        private bool TransitionToNewState(SensingAgentState newState)
        {
            lock (_stateLocker)
            {
                bool transitionToNewState = false;

                if (_state != newState)
                {
                    if (_state == SensingAgentState.Idle)
                    {
                        transitionToNewState = newState == SensingAgentState.OpportunisticControl ||
                                               newState == SensingAgentState.ActiveObservation ||
                                               newState == SensingAgentState.EndingControl;
                    }
                    else if (_state == SensingAgentState.OpportunisticControl)
                    {
                        transitionToNewState = newState == SensingAgentState.EndingControl;
                    }
                    else if (_state == SensingAgentState.ActiveObservation)
                    {
                        transitionToNewState = newState == SensingAgentState.ActiveControl ||
                                               newState == SensingAgentState.Idle ||
                                               newState == SensingAgentState.EndingControl;
                    }
                    else if (_state == SensingAgentState.ActiveControl)
                    {
                        transitionToNewState = newState == SensingAgentState.EndingControl;
                    }
                    else if (_state == SensingAgentState.EndingControl)
                    {
                        transitionToNewState = newState == SensingAgentState.Idle;
                    }
                }

                if (transitionToNewState)
                {
                    _sensusServiceHelper.Logger.Log("Sensing agent is transitioning from " + _state + " to " + newState + ".", LoggingLevel.Normal, GetType());
                    _state = newState;

                    FireStateChangedEvent();
                }
                else
                {
                    _sensusServiceHelper.Logger.Log("Sensing agent is prohibited from transitioning from " + _state + " to " + newState + ".", LoggingLevel.Normal, GetType());
                }

                return transitionToNewState;
            }
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