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
using System.Linq;
using System.Reflection;
using System.Threading;
using Facebook.CoreKit;
using Facebook.LoginKit;
using Foundation;
using Sensus.Context;
using Sensus.Exceptions;
using Sensus.Probes.Apps;
using UIKit;

namespace Sensus.iOS.Probes.Apps
{
    public class iOSFacebookProbe : FacebookProbe
    {
        private bool HasValidAccessToken
        {
            get { return AccessToken.CurrentAccessToken != null && AccessToken.CurrentAccessToken.ExpirationDate.SecondsSinceReferenceDate >= NSDate.Now.SecondsSinceReferenceDate; }
        }

        private void ObtainAccessToken(string[] permissionNames)
        {
            if (!UIDevice.CurrentDevice.CheckSystemVersion(9, 0))
                throw new NotSupportedException("The Facebook probe is only available on iOS 9 and higher.");
            
            lock (LoginLocker)
            {
                if (HasValidAccessToken)
                {
                    SensusServiceHelper.Get().Logger.Log("Already have valid Facebook access token. No need to initialize.", LoggingLevel.Normal, GetType());
                    return;
                }

                ManualResetEvent loginWait = new ManualResetEvent(false);
                string loginErrorMessage = null;
                bool userCancelledLogin = false;

                SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(async () =>
                {
                    try
                    {
                        LoginManagerLoginResult loginResult = await new LoginManager().LogInWithReadPermissionsAsync(permissionNames, UIApplication.SharedApplication.KeyWindow.RootViewController);

                        if (loginResult == null)
                        {
                            loginErrorMessage = "No login result returned by login manager.";
                        }
                        else if (loginResult.IsCancelled)
                        {
                            loginErrorMessage = "User cancelled login.";
                            userCancelledLogin = true;
                        }
                        else
                        {
                            AccessToken.CurrentAccessToken = loginResult.Token;
                        }
                    }
                    catch (Exception ex)
                    {
                        loginErrorMessage = "Exception while logging in:  " + ex.Message;
                    }
                    finally
                    {
                        loginWait.Set();
                    }
                });

                loginWait.WaitOne();

                if (loginErrorMessage == null)
                    SensusServiceHelper.Get().Logger.Log("Facebook login succeeded.", LoggingLevel.Normal, GetType());
                else
                    SensusServiceHelper.Get().Logger.Log("Error while initializing Facebook SDK and/or logging in:  " + loginErrorMessage, LoggingLevel.Normal, GetType());

                if (!HasValidAccessToken)
                {
                    string message = "Failed to obtain access token.";
                    SensusServiceHelper.Get().Logger.Log(message, LoggingLevel.Normal, GetType());

                    // if the user cancelled the login, don't prompt them again
                    if (userCancelledLogin)
                        throw new NotSupportedException(message + " User cancelled login.");
                    // if the user did not cancel the login, allow the login to be presented again when the health test is run
                    else
                        throw new Exception(message);
                }
            }
        }

        protected override void Initialize()
        {
            base.Initialize();

            ObtainAccessToken(GetRequiredPermissionNames());
        }

        protected override IEnumerable<Datum> Poll(CancellationToken cancellationToken)
        {
            List<Datum> data = new List<Datum>();

            if (HasValidAccessToken)
            {
                string[] missingPermissions = GetRequiredPermissionNames().Where(p => !AccessToken.CurrentAccessToken.Permissions.Contains(p)).ToArray();
                if (missingPermissions.Length > 0)
                    ObtainAccessToken(missingPermissions);
            }
            else
                ObtainAccessToken(GetRequiredPermissionNames());

            if (HasValidAccessToken)
            {
                ManualResetEvent startWait = new ManualResetEvent(false);
                List<ManualResetEvent> responseWaits = new List<ManualResetEvent>();
                Exception exception = null;  // can't throw exception from within the UI thread -- it will crash the app. use this variable to check whether an exception did occur.

                SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(() =>
                {
                    try
                    {
                        GraphRequestConnection requestConnection = new GraphRequestConnection();

                        #region add requests to connection
                        foreach (Tuple<string, List<string>> edgeFieldQuery in GetEdgeFieldQueries())
                        {
                            NSDictionary parameters = null;
                            if (edgeFieldQuery.Item2.Count > 0)
                                parameters = new NSDictionary("fields", string.Concat(edgeFieldQuery.Item2.Select(field => field + ",")).Trim(','));

                            GraphRequest request = new GraphRequest(
                                                       "me" + (edgeFieldQuery.Item1 == null ? "" : "/" + edgeFieldQuery.Item1),
                                                       parameters,
                                                       AccessToken.CurrentAccessToken.TokenString,
                                                       "v2.8",
                                                       "GET");

                            ManualResetEvent responseWait = new ManualResetEvent(false);

                            try
                            {
                                requestConnection.AddRequest(request, (connection, result, error) =>
                                {
                                    try
                                    {
                                        if (error == null)
                                        {
                                            FacebookDatum datum = new FacebookDatum(DateTimeOffset.UtcNow);

                                            #region set datum properties
                                            NSDictionary resultDictionary = result as NSDictionary;
                                            bool valuesSet = false;
                                            foreach (string resultKey in resultDictionary.Keys.Select(k => k.ToString()))
                                            {
                                                PropertyInfo property;
                                                if (FacebookDatum.TryGetProperty(resultKey, out property))
                                                {
                                                    object value = null;

                                                    if (property.PropertyType == typeof(string))
                                                        value = resultDictionary[resultKey].ToString();
                                                    else if (property.PropertyType == typeof(bool?))
                                                    {
                                                        int parsedBool;
                                                        if (int.TryParse(resultDictionary[resultKey].ToString(), out parsedBool))
                                                            value = parsedBool == 1 ? true : false;
                                                    }
                                                    else if (property.PropertyType == typeof(DateTimeOffset?))
                                                    {
                                                        DateTimeOffset parsedDateTimeOffset;
                                                        if (DateTimeOffset.TryParse(resultDictionary[resultKey].ToString(), out parsedDateTimeOffset))
                                                            value = parsedDateTimeOffset;
                                                    }
                                                    else if (property.PropertyType == typeof(List<string>))
                                                    {
                                                        List<string> values = new List<string>();

                                                        NSArray resultArray = resultDictionary[resultKey] as NSArray;
                                                        for (nuint i = 0; i < resultArray.Count; ++i)
                                                        {
                                                            values.Add(resultArray.GetItem<NSObject>(i).ToString());
                                                        }

                                                        value = values;
                                                    }
                                                    else
                                                    {
                                                        throw SensusException.Report("Unrecognized FacebookDatum property type:  " + property.PropertyType);
                                                    }

                                                    if (value != null)
                                                    {
                                                        property.SetValue(datum, value);
                                                        valuesSet = true;
                                                    }
                                                }
                                                // there are several result keys that we don't yet handle. ignore these.
                                                else if (resultKey != "data" && resultKey != "paging" && resultKey != "summary")
                                                    SensusServiceHelper.Get().Logger.Log("Unrecognized key in Facebook result dictionary:  " + resultKey, LoggingLevel.Verbose, GetType());
                                            }
                                            #endregion

                                            if (valuesSet)
                                            {
                                                data.Add(datum);
                                            }
                                        }
                                        else
                                        {
                                            SensusServiceHelper.Get().Logger.Log("Error received while querying Facebook graph API:  " + error.Description, LoggingLevel.Normal, GetType());
                                        }

                                        SensusServiceHelper.Get().Logger.Log("Response for \"" + request.GraphPath + "\" has been processed.", LoggingLevel.Verbose, GetType());
                                    }
                                    catch (Exception ex)
                                    {
                                        SensusServiceHelper.Get().Logger.Log("Exception while processing response:  " + ex.Message, LoggingLevel.Normal, GetType());
                                    }
                                    finally
                                    {
                                        // ensure that the response wait is always set when processing the request response
                                        responseWait.Set();
                                    }
                                });

                                responseWaits.Add(responseWait);
                            }
                            catch (Exception ex)
                            {
                                SensusServiceHelper.Get().Logger.Log("Exception while adding request:  " + ex.Message, LoggingLevel.Normal, GetType());

                                // ensure that the response wait is always set when adding the request response
                                responseWait.Set();
                            }
                        }
                        #endregion

                        if (responseWaits.Count == 0)
                        {
                            exception = new Exception("Request connection contained zero requests.");
                        }
                        else
                        {
                            SensusServiceHelper.Get().Logger.Log("Starting request connection with " + responseWaits.Count + " requests.", LoggingLevel.Normal, GetType());
                            requestConnection.Start();
                        }

                        startWait.Set();
                    }
                    catch (Exception ex)
                    {
                        exception = new Exception("Error starting request connection:  " + ex.Message);

                        // if anything bad happened when starting the request, abort any response waits
                        foreach (ManualResetEvent responseWait in responseWaits)
                        {
                            responseWait.Set();
                        }

                        startWait.Set();
                    }
                });

                startWait.WaitOne();

                // wait for all responses to be processed
                foreach (ManualResetEvent responseWait in responseWaits)
                {
                    responseWait.WaitOne();
                }

                // if any exception occurred when running query, throw it now
                if (exception != null)
                {
                    throw exception;
                }
            }
            else
            {
                throw new Exception("Attempted to poll Facebook probe without a valid access token.");
            }

            return data;
        }

        public override bool TestHealth()
        {
            bool restart = base.TestHealth();

            if (!HasValidAccessToken)
            {
                restart = true;
            }

            return restart;
        }

        protected override ICollection<string> GetGrantedPermissions()
        {
            if (HasValidAccessToken)
            {
                List<string> permissions = new List<string>();
                foreach (NSString permission in AccessToken.CurrentAccessToken.Permissions)
                {
                    permissions.Add(permission.ToString());
                }

                return permissions;
            }
            else
            {
                return new string[0];
            }
        }
    }
}