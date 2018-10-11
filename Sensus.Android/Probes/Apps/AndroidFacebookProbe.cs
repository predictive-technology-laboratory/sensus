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
using System.Threading;
using Android.OS;
using Android.Runtime;
using Org.Json;
using Sensus.Probes.Apps;
using Xamarin.Facebook;
using Xamarin.Facebook.Login;
using System.Reflection;
using Sensus.Exceptions;
using System.Threading.Tasks;

namespace Sensus.Android.Probes.Apps
{
    /// <summary>
    /// Probes Facebook information. To generate key hashes:
    ///   * Debug:  keytool -exportcert -alias androiddebugkey -keystore ~/.local/share/Xamarin/Mono\ for\ Android/debug.keystore | openssl sha1 -binary | openssl base64
    ///   * Play store:  TODO
    /// </summary>
    public class AndroidFacebookProbe : FacebookProbe
    {
        private class FacebookCallback<TResult> : Java.Lang.Object, IFacebookCallback where TResult : Java.Lang.Object
        {
            public Action<TResult> HandleSuccess { get; set; }

            public Action HandleCancel { get; set; }

            public Action<FacebookException> HandleError { get; set; }

            public void OnSuccess(Java.Lang.Object result)
            {
                if (HandleSuccess != null)
                    HandleSuccess(result.JavaCast<TResult>());
            }

            public void OnCancel()
            {
                if (HandleCancel != null)
                    HandleCancel();
            }

            public void OnError(FacebookException error)
            {
                if (HandleError != null)
                    HandleError(error);
            }
        }

        private bool HasValidAccessToken
        {
            get { return FacebookSdk.IsInitialized && AccessToken.CurrentAccessToken != null && !AccessToken.CurrentAccessToken.IsExpired; }
        }

        private void ObtainAccessToken(string[] permissionNames)
        {
            lock (LoginLocker)
            {
                if (HasValidAccessToken)
                {
                    SensusServiceHelper.Get().Logger.Log("Facebook access token present.", LoggingLevel.Normal, GetType());
                }
                else
                {
                    ManualResetEvent loginWait = new ManualResetEvent(false);
                    bool loginCancelled = false;
                    string accessTokenError = null;

                    #region prompt user to log in from main activity
                    (SensusServiceHelper.Get() as AndroidSensusServiceHelper).RunActionUsingMainActivityAsync(mainActivity =>
                    {
                        try
                        {
                            FacebookCallback<LoginResult> loginCallback = new FacebookCallback<LoginResult>
                            {
                                HandleSuccess = loginResult =>
                                {
                                    SensusServiceHelper.Get().Logger.Log("Facebook login succeeded.", LoggingLevel.Normal, GetType());
                                    AccessToken.CurrentAccessToken = loginResult.AccessToken;
                                    loginWait.Set();
                                },

                                HandleCancel = () =>
                                {
                                    SensusServiceHelper.Get().Logger.Log("Facebook login cancelled.", LoggingLevel.Normal, GetType());
                                    AccessToken.CurrentAccessToken = null;
                                    loginCancelled = true;
                                    loginWait.Set();
                                },

                                HandleError = loginResult =>
                                {
                                    SensusServiceHelper.Get().Logger.Log("Facebook login failed.", LoggingLevel.Normal, GetType());
                                    AccessToken.CurrentAccessToken = null;
                                    loginWait.Set();
                                }
                            };

                            LoginManager.Instance.RegisterCallback(mainActivity.FacebookCallbackManager, loginCallback);
                            LoginManager.Instance.LogInWithReadPermissions(mainActivity, permissionNames);
                        }
                        catch (Exception ex)
                        {
                            accessTokenError = ex.Message;
                            loginWait.Set();
                        }

                    }, true, false);
                    #endregion

                    loginWait.WaitOne();

                    if (accessTokenError != null)
                    {
                        SensusServiceHelper.Get().Logger.Log("Error while initializing Facebook SDK and/or logging in:  " + accessTokenError, LoggingLevel.Normal, GetType());
                    }

                    // if the access token is still not valid after logging in, consider it a fail.
                    if (!HasValidAccessToken)
                    {
                        string message = "Failed to obtain access token.";
                        SensusServiceHelper.Get().Logger.Log(message, LoggingLevel.Normal, GetType());

                        // if the user cancelled the login, don't prompt them to log in again
                        if (loginCancelled)
                        {
                            throw new NotSupportedException(message + " User cancelled login.");
                        }
                        // if the user did not cancel the login, allow the login to be presented again when the health test is run
                        else
                        {
                            throw new Exception(message);
                        }
                    }
                }
            }
        }

        protected override async Task InitializeAsync()
        {
            await base.InitializeAsync();

            ObtainAccessToken(GetRequiredPermissionNames());
        }

        protected override Task<List<Datum>> PollAsync(CancellationToken cancellationToken)
        {
            List<Datum> data = new List<Datum>();

            if (HasValidAccessToken)
            {
                string[] missingPermissions = GetRequiredPermissionNames().Where(p => !AccessToken.CurrentAccessToken.Permissions.Contains(p)).ToArray();
                if (missingPermissions.Length > 0)
                {
                    ObtainAccessToken(missingPermissions);
                }
            }
            else
            {
                ObtainAccessToken(GetRequiredPermissionNames());
            }

            if (HasValidAccessToken)
            {
                GraphRequestBatch graphRequestBatch = new GraphRequestBatch();

                foreach (Tuple<string, List<string>> edgeFieldQuery in GetEdgeFieldQueries())
                {
                    Bundle parameters = new Bundle();

                    if (edgeFieldQuery.Item2.Count > 0)
                    {
                        parameters.PutString("fields", string.Concat(edgeFieldQuery.Item2.Select(field => field + ",")).Trim(','));
                    }

                    GraphRequest request = new GraphRequest(
                                               AccessToken.CurrentAccessToken,
                                               "/me" + (edgeFieldQuery.Item1 == null ? "" : "/" + edgeFieldQuery.Item1),
                                               parameters,
                                               HttpMethod.Get);

                    request.Version = "v2.8";

                    graphRequestBatch.Add(request);
                }

                if (graphRequestBatch.Size() == 0)
                {
                    throw new Exception("User has not granted any Facebook permissions.");
                }
                else
                {
                    foreach (GraphResponse response in graphRequestBatch.ExecuteAndWait())
                    {
                        if (response.Error == null)
                        {
                            FacebookDatum datum = new FacebookDatum(DateTimeOffset.UtcNow);

                            JSONObject responseJSON = response.JSONObject;
                            JSONArray jsonFields = responseJSON.Names();
                            bool valuesSet = false;
                            for (int i = 0; i < jsonFields.Length(); ++i)
                            {
                                string jsonField = jsonFields.GetString(i);

                                PropertyInfo property;
                                if (FacebookDatum.TryGetProperty(jsonField, out property))
                                {
                                    object value = null;

                                    if (property.PropertyType == typeof(string))
                                    {
                                        value = responseJSON.GetString(jsonField);
                                    }
                                    else if (property.PropertyType == typeof(bool?))
                                    {
                                        value = responseJSON.GetBoolean(jsonField);
                                    }
                                    else if (property.PropertyType == typeof(DateTimeOffset?))
                                    {
                                        value = DateTimeOffset.Parse(responseJSON.GetString(jsonField));
                                    }
                                    else if (property.PropertyType == typeof(List<string>))
                                    {
                                        List<string> values = new List<string>();
                                        JSONArray jsonValues = responseJSON.GetJSONArray(jsonField);
                                        for (int j = 0; j < jsonValues.Length(); ++j)
                                        {
                                            values.Add(jsonValues.GetString(j));
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
                                else if (jsonField != "data" && jsonField != "paging" && jsonField != "summary")
                                {
                                    SensusServiceHelper.Get().Logger.Log("Unrecognized JSON field in Facebook query response:  " + jsonField, LoggingLevel.Verbose, GetType());
                                }
                            }

                            if (valuesSet)
                            {
                                data.Add(datum);
                            }
                        }
                        else
                        {
                            throw new Exception("Error received while querying Facebook graph API:  " + response.Error.ErrorMessage);
                        }
                    }
                }
            }
            else
            {
                throw new Exception("Attempted to poll Facebook probe without a valid access token.");
            }

            return Task.FromResult(data);
        }

        public override async Task<HealthTestResult> TestHealthAsync(List<AnalyticsTrackedEvent> events)
        {
            HealthTestResult result = await base.TestHealthAsync(events);

            if (!HasValidAccessToken)
            {
                result = HealthTestResult.Restart;
            }

            return result;
        }

        protected override ICollection<string> GetGrantedPermissions()
        {
            if (HasValidAccessToken)
            {
                return AccessToken.CurrentAccessToken.Permissions;
            }
            else
            {
                return new string[0];
            }
        }
    }
}