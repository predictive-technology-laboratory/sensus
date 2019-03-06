using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Sensus;
using Sensus.Probes.User.Scripts;
using Sensus.Probes.Location;
using Sensus.Probes.Movement;
using System.Threading;

namespace Sensus
{
    public class S1ScriptProbeAgent : IScriptProbeAgent
    {

        public string Description => "Adaptive Agent using RL with linear approximation Q-learning";
        public string Id => "RLLinearApproximationAgent";
        public TimeSpan? DeliveryInterval => TimeSpan.FromMinutes(5);
        public TimeSpan? DeliveryIntervalToleranceBefore => TimeSpan.Zero;
        public TimeSpan? DeliveryIntervalToleranceAfter => TimeSpan.Zero;
        public IProtocol _protocol;

        public string SurveyId = null;
        private ISensusServiceHelper _sensusServiceHelper;

        //properties for static strategy/policy parameters
        public string[] actions = new string[] { "Delivered", "Deferred" };
        public string[] stateFeatures = new string[] { "con_hourofday", "dis_currentPlace", "con_numAttemptsLeft" };
        public double alpha = 0.01;
        public double gamma = 0.2;
        public double lambda = 0.1;
        public double initEpsilon = 0.1;
        public double decayEpsilon = 0.999;
        public Dictionary<string, double> Rewards = new Dictionary<string, double>
        {
            { "Submitted", 1 },
            { "Deferred", 0},
            {"NotSubmitted", -1}
        };

        //properties for dynamic strategy/poicy parameters
        public Dictionary<string, double> parameters;
        public Dictionary<string, Dictionary<string, double>> qValue;

        //properties for experimental design
        public int numSurveys = 6;
        public int CurrentSurveyIndex = -1;
        public TimeSpan minIntervalBtwSurvey = TimeSpan.FromMinutes(10);
        public TimeSpan windowSize = TimeSpan.FromMinutes(30);
        public TimeSpan surveyWindowSize = TimeSpan.FromHours(1);
        public TimeSpan startTime = TimeSpan.FromHours(9);
        public TimeSpan endTime = TimeSpan.FromHours(21);
        public List<TimeSpan> surveyDecisionPoints = new List<TimeSpan>();

        //properties for storing the datum
        public List<ILocationDatum> locationDatum = new List<ILocationDatum>();
        public List<IActivityDatum> activityDatum = new List<IActivityDatum>();
        public List<IAccelerometerDatum> accelerometerDatum = new List<IAccelerometerDatum>();

        //the state
        public List<Place> semanticPlaces = new List<Place>();
        public Dictionary<string, double> state = new Dictionary<string, double>();
        public int numAttemptsLeft;

        public void InitiateDecisionPoints()
        {
            Random random = new Random();
            TimeSpan start = new TimeSpan();
            TimeSpan end = new TimeSpan();

            for (var i = 0; i < numSurveys; i++)
            {
                start = startTime + TimeSpan.FromTicks(surveyWindowSize.Ticks * i);
                end = start + TimeSpan.FromTicks(surveyWindowSize.Ticks * (i + 1)) - windowSize - minIntervalBtwSurvey;
                if (TimeSpan.Compare(end, endTime) == 1) end = endTime;
                int spanMinutes = (int)((end - start).TotalMinutes);
                int mins = random.Next(spanMinutes);
                surveyDecisionPoints.Add(start.Add(TimeSpan.FromMinutes(mins)));
            }
        }

        private static double DegreesToRadians(double degrees)
        {
            return degrees * Math.PI / 180;
        }

        public static double CalculateDist(double long1, double lat1, double long2, double lat2)
        {
            double phi1 = DegreesToRadians(lat2);
            double phi2 = DegreesToRadians(lat1);
            double dphi = DegreesToRadians(lat1 - lat2);
            double dlambda = DegreesToRadians(long1 - long2);
            double a = Math.Pow(Math.Sin(dphi / 2), 2) + Math.Cos(phi1) * Math.Cos(phi2) *
            Math.Pow(Math.Sin(dlambda / 2), 2);
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return 6371 * c * 1000;
        }

        public void UpdateState(DateTimeOffset t)
        {
            //extract the features
            int hourofday = t.Hour;

            //the current location
            Place currentPlace = new Place("unknown");
            double currentPlaceLongitude = locationDatum[locationDatum.Count - 1].Longitude;
            double currentPlaceLatitude = locationDatum[locationDatum.Count - 1].Latitude;
            for (var i = 0; i < semanticPlaces.Count; i++)
            {
                double dist = CalculateDist(currentPlaceLatitude, currentPlaceLongitude,
                    semanticPlaces[i].Longitude, semanticPlaces[i].Latitude);
                if (dist <= parameters["eps_cl"])
                {
                    currentPlace = semanticPlaces[i];
                    break;
                }
            }

            //add them to the state property if the key is not there and fill in the value
            //if the key is already in the state property, then just fill in the value
            //hourofday, currentPlace, numAttemptsLeft
            List<string>  updatedKeys = new List<string>();
            string currentPlace_key = "dis_currentPlace_" + currentPlace.ID;
            if (state.ContainsKey(currentPlace_key))
            {
                state[currentPlace_key] = 1;
            }
            else
            {
                state.Add(currentPlace_key, 1);
            }
            updatedKeys.Add(currentPlace_key);

            if (state.ContainsKey("con_hourofday"))
            {
                state["con_hourofday"] = hourofday;
            }
            else
            {
                state.Add("con_hourofday", hourofday);
            }
            updatedKeys.Add("con_hourofday");

            if (state.ContainsKey("con_numAttemptsLeft"))
            {
                state["con_numAttemptsLeft"] = numAttemptsLeft;
            }
            else
            {
                state.Add("con_numAttemptsLeft", numAttemptsLeft);
            }
            updatedKeys.Add("con_numAttemptsLeft");

            //reset all non-updated keys to 0
            foreach(string k in state.Keys.ToArray())
            {
                if (!updatedKeys.Contains(k))
                {
                    state[k] = 0;
                }
            }

        }

        public bool ChooseAction()
        {
            //qValue
            //public Dictionary<string, Dictionary<string, double>> qValue;
            //public string[] actions = new string[] { "Delivered", "Deferred" };
            double max_action_values = -100000;
            int max_action_values_index = 0;
            int counter = 0;
            foreach(string a in actions)
            {
                Dictionary<string, double> coefs = qValue[a];
                double ev = 0;
                foreach(string k in coefs.Keys.ToArray())
                {
                    ev += state[k] * coefs[k];
                }
                if (max_action_values < ev)
                {
                    max_action_values = ev;
                    max_action_values_index = counter;
                }
                counter++;
            }

            string chosen_action = actions[max_action_values_index];

            bool delivery = false;
            if (chosen_action == "Deliveded")
            {
                delivery = true;
            }

            return (delivery);
        }


        public Task SetPolicyAsync(string strategyJSON)
        {

            //read in the json object
            JObject strategyObject = JObject.Parse(strategyJSON);

            //read in the parameters
            parameters = strategyObject["params"].ToObject<Dictionary<string, double>>();

            //read in the semantic places
            JArray placeIdsJArray = (JArray)strategyObject["semanticPlaces"]["placeId"];
            string[] placeIds = placeIdsJArray.ToObject<string[]>();
            JArray longitudesArray = (JArray)strategyObject["semanticPlaces"]["cl.Longitude"];
            double[] longitudes = longitudesArray.ToObject<double[]>();
            JArray latitudesArray = (JArray)strategyObject["semanticPlaces"]["cl.Latitude"];
            double[] latitudes = latitudesArray.ToObject<double[]>();
            for (var i = 0; i <= placeIds.Length; i++)
            {
                semanticPlaces.Add(new Place(placeIds[i], longitudes[i], latitudes[i]));
            }

            //read in the qValues
            foreach (string a in actions)
            {
                Dictionary<string, double> q = strategyObject[a].ToObject<Dictionary<string, double>>();
                qValue.Add(a, q);
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Observe the specified datum.
        /// </summary>
        /// <param name="datum">Datum.</param>
        public Task ObserveAsync(IDatum datum)
        {
            DateTimeOffset t = datum.Timestamp;  //does timezone matter in the timestamp of the datum?
            if (datum is ILocationDatum)
            {
                locationDatum.Add(datum as ILocationDatum);
            }
            if (locationDatum.Count > 0)
            {
                IDatum firstLocationDatum = locationDatum[0];
                while (TimeSpan.Compare(t - firstLocationDatum.Timestamp, TimeSpan.FromHours(2)) == 1)
                {
                    locationDatum.RemoveAt(0);
                    firstLocationDatum = locationDatum[0];
                }
            }

            if (datum is IAccelerometerDatum)
            {
                accelerometerDatum.Add(datum as IAccelerometerDatum);
            }
            if (accelerometerDatum.Count > 0)
            {
                IDatum firstAccelerometerDatum = accelerometerDatum[0];
                while (TimeSpan.Compare(t - firstAccelerometerDatum.Timestamp, TimeSpan.FromHours(2)) == 1)
                {
                    accelerometerDatum.RemoveAt(0);
                    firstAccelerometerDatum = accelerometerDatum[0];
                }

            }

            if (datum is IActivityDatum)
            {
                activityDatum.Add(datum as IActivityDatum);
            }
            if (activityDatum.Count > 0)
            {
                IDatum firstActivityDatum = activityDatum[0];
                while (TimeSpan.Compare(t - firstActivityDatum.Timestamp, TimeSpan.FromHours(2)) == 1)
                {
                    activityDatum.RemoveAt(0);
                    firstActivityDatum = activityDatum[0];
                }
            }

            UpdateState(t);

            return Task.CompletedTask;
        }

        /// <summary>
        /// Checks whether or not to deliver/defer a survey.
        /// </summary>
        /// <returns>Deliver/defer decision.</returns>
        /// <param name="script">Script.</param>
        public async Task<Tuple<bool, DateTimeOffset?>> DeliverSurveyNowAsync(IScript script)
        {
            //initiate the decision points and check which one is the next survey
            TimeSpan t = DateTime.Now.TimeOfDay;
            DateTimeOffset? deferralTime = null;
            bool delivery;

            ////initiate the policy parameters
            //if (parameters.Count == 0)
            //{

            //}

            if (surveyDecisionPoints.Count == 0)
            {
                InitiateDecisionPoints();
                numAttemptsLeft = (int)(windowSize.Ticks / DeliveryInterval.Value.Ticks);
                for (var j = 0; j < surveyDecisionPoints.Count; j++)
                {
                    if (TimeSpan.Compare(surveyDecisionPoints[j], t) == 1)
                    {
                        CurrentSurveyIndex = j;
                        break;
                    }
                }
            }

            //
            if (TimeSpan.Compare(t, endTime) == 1)
            {
                surveyDecisionPoints.Clear();
                CurrentSurveyIndex = -1;
                SurveyId = null;
                numAttemptsLeft = (int)(windowSize.Ticks / DeliveryInterval.Value.Ticks);
                deferralTime = null;
                delivery = false;
            }
            else
            {
                //the first encounter of a survey
                if (SurveyId == null)
                {
                    SurveyId = script.Id;
                    //when the current time point 
                    if (TimeSpan.Compare(surveyDecisionPoints[CurrentSurveyIndex] - DeliveryInterval.Value, t) == 1)
                    {
                        deferralTime = DateTime.Now + (surveyDecisionPoints[CurrentSurveyIndex] - t);
                        delivery = false;
                    }
                    else if (TimeSpan.Compare(surveyDecisionPoints[CurrentSurveyIndex] - DeliveryInterval.Value, t) == -1 &
                    TimeSpan.Compare(surveyDecisionPoints[CurrentSurveyIndex] + DeliveryInterval.Value, t) == 1)
                    {
                        delivery = ChooseAction();
                        if (delivery)
                        {
                            deferralTime = null;
                            if (CurrentSurveyIndex < numSurveys - 1)
                            {
                                CurrentSurveyIndex++;
                                SurveyId = null;
                                numAttemptsLeft = (int)(windowSize.Ticks / DeliveryInterval.Value.Ticks);
                            }
                        }
                        else
                        {
                            surveyDecisionPoints[CurrentSurveyIndex] += DeliveryInterval.Value;
                            deferralTime = DateTime.Now + (surveyDecisionPoints[CurrentSurveyIndex] - t);
                            numAttemptsLeft -= 1;
                        }

                    }
                    else
                    {
                        delivery = ChooseAction();
                        if (delivery)
                        {
                            deferralTime = null;
                            if (CurrentSurveyIndex < numSurveys - 1)
                            {
                                CurrentSurveyIndex++;
                                SurveyId = null;
                                numAttemptsLeft = (int)(windowSize.Ticks / DeliveryInterval.Value.Ticks);
                            }
                        }
                        else
                        {
                            int count = 0;
                            while (TimeSpan.Compare(surveyDecisionPoints[CurrentSurveyIndex], t) == -1)
                            {
                                surveyDecisionPoints[CurrentSurveyIndex] += DeliveryInterval.Value;
                                count++;
                            }
                            numAttemptsLeft -= (count - 1);
                            deferralTime = DateTime.Now + (surveyDecisionPoints[CurrentSurveyIndex] - t);

                        }
                    }

                }
                //subsequent encounters of the same survey
                else
                {
                    if (SurveyId == script.Id)
                    {
                        if (numAttemptsLeft > 0)
                        {
                            delivery = ChooseAction();
                            if (delivery)
                            {
                                deferralTime = null;
                                if (CurrentSurveyIndex < numSurveys - 1)
                                {
                                    CurrentSurveyIndex++;
                                    SurveyId = null;
                                    numAttemptsLeft = (int)(windowSize.Ticks / DeliveryInterval.Value.Ticks);
                                }
                                else
                                {
                                    deferralTime = null;
                                    delivery = false;
                                }
                            }
                            else
                            {
                                surveyDecisionPoints[CurrentSurveyIndex] += DeliveryInterval.Value;
                                deferralTime = DateTime.Now + (surveyDecisionPoints[CurrentSurveyIndex] - t);
                                numAttemptsLeft -= 1;
                            }
                        }
                        else
                        {
                            delivery = false;
                            deferralTime = null;
                            CurrentSurveyIndex++;
                            SurveyId = null;
                            numAttemptsLeft = (int)(windowSize.Ticks / DeliveryInterval.Value.Ticks);
                        }

                    }
                    else
                    {
                        delivery = false;
                        deferralTime = null;
                    }

                }
            }

            await (_sensusServiceHelper?.FlashNotificationAsync("Delivery decision:  " + delivery) ?? Task.CompletedTask);

            return (new Tuple<bool, DateTimeOffset?>(delivery, deferralTime));
        }

        /// <summary>
        /// Observe the specified script and state.
        /// </summary>
        /// <param name="script">Script.</param>
        /// <param name="state">State.</param>
        public Task ObserveAsync(IScript script, ScriptState state)
        {
            if(script.Id == SurveyId)
            {


            }


            return Task.CompletedTask;
        }

        /// <summary>
        /// Returns a <see cref="T:System.String"/> that represents the current <see cref="T:ExampleScriptProbeAgent.ExampleAdaptiveScriptProbeAgent"/>.
        /// </summary>
        /// <returns>A <see cref="T:System.String"/> that represents the current <see cref="T:ExampleScriptProbeAgent.ExampleAdaptiveScriptProbeAgent"/>.</returns>
        public override string ToString()
        {
            return Id + ":  " + Description;
        }


        public async Task InitializeAsync(ISensusServiceHelper sensusServiceHelper, IProtocol protocol)
        {
            _sensusServiceHelper = sensusServiceHelper;
            _protocol = protocol;

            // download the initial policy
            try
            {
                await _protocol.UpdateScriptAgentPolicyAsync(CancellationToken.None);
            }
            catch (Exception ex)
            {
                _sensusServiceHelper?.Logger.Log("Exception while downloading the policy:  " + ex.Message, LoggingLevel.Normal, GetType());
            }


            _sensusServiceHelper?.Logger.Log("Agent has been initialized.", LoggingLevel.Normal, GetType());
        }

    }
}
