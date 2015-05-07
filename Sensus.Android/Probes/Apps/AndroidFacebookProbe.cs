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
using Android.App;
using Android.OS;
using Android.Runtime;
using Org.Json;
using SensusService;
using SensusService.Probes.Apps;
using Xamarin.Facebook;
using Xamarin.Facebook.Login;
using Android.Provider;
using Android.Content;
using System.Reflection;
using SensusService.Exceptions;

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

        private ManualResetEvent _loginWait;

        public AndroidFacebookProbe()
        {
            _loginWait = new ManualResetEvent(false);
        }

        private bool HasValidAccessToken
        {
            get { return FacebookSdk.IsInitialized && AccessToken.CurrentAccessToken != null && !AccessToken.CurrentAccessToken.IsExpired; }
        }

        protected override void Initialize()
        {
            base.Initialize();

            if (HasValidAccessToken)
            {
                SensusServiceHelper.Get().Logger.Log("Already have valid Facebook access token. No need to initialize.", LoggingLevel.Normal, GetType());
                return;
            }

            _loginWait.Reset();
            string accessTokenError = null;

            (AndroidSensusServiceHelper.Get() as AndroidSensusServiceHelper).GetMainActivityAsync(true, mainActivity => mainActivity.RunOnUiThread(() =>
                    {
                        try
                        {                            
                            if (FacebookSdk.IsInitialized)
                                SensusServiceHelper.Get().Logger.Log("Facebook SDK is already initialized.", LoggingLevel.Normal, GetType());
                            else
                            {
                                SensusServiceHelper.Get().Logger.Log("Initializing Facebook SDK.", LoggingLevel.Normal, GetType());
                                FacebookSdk.SdkInitialize(mainActivity);
                                Thread.Sleep(5000);  // give sdk intialization a few seconds to read the access token

                                FacebookCallback<LoginResult> loginCallback = new FacebookCallback<LoginResult>
                                {
                                    HandleSuccess = loginResult =>
                                    {
                                        AndroidSensusServiceHelper.Get().Logger.Log("Facebook login succeeded.", SensusService.LoggingLevel.Normal, GetType());
                                        AccessToken.CurrentAccessToken = loginResult.AccessToken;
                                        _loginWait.Set();
                                    },

                                    HandleCancel = () =>
                                    {
                                        AndroidSensusServiceHelper.Get().Logger.Log("Facebook login cancelled.", SensusService.LoggingLevel.Normal, GetType());
                                        AccessToken.CurrentAccessToken = null;
                                        _loginWait.Set();
                                    },

                                    HandleError = loginResult =>
                                    {
                                        AndroidSensusServiceHelper.Get().Logger.Log("Facebook login failed.", SensusService.LoggingLevel.Normal, GetType());
                                        AccessToken.CurrentAccessToken = null;
                                        _loginWait.Set();
                                    }
                                };

                                LoginManager.Instance.RegisterCallback(mainActivity.FacebookCallbackManager, loginCallback);
                            }

                            // if the access token was read from cache, we're done.
                            if (HasValidAccessToken)
                            {
                                SensusServiceHelper.Get().Logger.Log("Facebook access token read from cache.", LoggingLevel.Normal, GetType());
                                _loginWait.Set();
                            }
                            // prompt user to log in with all enabled permissions
                            else
                                LoginManager.Instance.LogInWithReadPermissions(mainActivity, GetRequiredPermissionNames());
                        }
                        catch (Exception ex)
                        {
                            accessTokenError = ex.Message;
                        }

                    }));

            if (accessTokenError != null)
                SensusServiceHelper.Get().Logger.Log("Error while initializing Facebook SDK and/or logging in:  " + accessTokenError, LoggingLevel.Normal, GetType());

            _loginWait.WaitOne();

            if (!HasValidAccessToken)
            {
                string message = "Failed to obtain access token.";
                SensusServiceHelper.Get().Logger.Log(message, LoggingLevel.Normal, GetType());
                throw new Exception(message);
            }
        }

        protected override IEnumerable<Datum> Poll(CancellationToken cancellationToken)
        {
            List<Datum> data = new List<Datum>();

            if (HasValidAccessToken)
            {
                // prompt user for any missing permissions
                ICollection<string> missingPermissions = GetRequiredPermissionNames().Where(p => !AccessToken.CurrentAccessToken.Permissions.Contains(p)).ToArray();
                if (missingPermissions.Count > 0)
                {
                    _loginWait.Reset();

                    (AndroidSensusServiceHelper.Get() as AndroidSensusServiceHelper).GetMainActivityAsync(true, mainActivity => mainActivity.RunOnUiThread(() =>
                            {
                                LoginManager.Instance.LogInWithReadPermissions(mainActivity, missingPermissions);
                            }));

                    _loginWait.WaitOne();
                }

                GraphRequestBatch graphRequestBatch = new GraphRequestBatch();

                foreach (Tuple<string, List<string>> edgeFieldQuery in GetEdgeFieldQueries())
                {
                    Bundle parameters = new Bundle();

                    if (edgeFieldQuery.Item2.Count > 0)
                        parameters.PutString("fields", string.Concat(edgeFieldQuery.Item2.Select(field => field + ",")).Trim(','));

                    GraphRequest request = new GraphRequest(
                                               AccessToken.CurrentAccessToken,
                                               "/me" + (edgeFieldQuery.Item1 == null ? "" : "/" + edgeFieldQuery.Item1),
                                               parameters,
                                               HttpMethod.Get);

                    graphRequestBatch.Add(request);
                }

                if (graphRequestBatch.Size() == 0)
                    SensusServiceHelper.Get().Logger.Log("Facebook request batch contained zero requests.", LoggingLevel.Normal, GetType());
                else
                    foreach (GraphResponse response in graphRequestBatch.ExecuteAndWait())
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
                                        value = responseJSON.GetString(jsonField);
                                    else if (property.PropertyType == typeof(bool))
                                        value = responseJSON.GetBoolean(jsonField);
                                    else if (property.PropertyType == typeof(DateTimeOffset))
                                        value = DateTimeOffset.Parse(responseJSON.GetString(jsonField));
                                    else if (property.PropertyType == typeof(List<string>))
                                    {
                                        List<string> values = new List<string>();
                                        JSONArray jsonValues = responseJSON.GetJSONArray(jsonField);
                                        for (int j = 0; j < jsonValues.Length(); ++j)
                                            values.Add(jsonValues.GetString(j));

                                        value = values;
                                    }
                                    else
                                        throw new SensusException("Unrecognized FacebookDatum property type:  " + property.PropertyType.ToString());

                                    if (value != null)
                                    {
                                        property.SetValue(datum, value);
                                        valuesSet = true;
                                    }
                                }
                                else
                                    SensusServiceHelper.Get().Logger.Log("Unrecognized JSON field in Facebook query response:  " + jsonField, LoggingLevel.Verbose, GetType());
                            }

                            if (valuesSet)
                                data.Add(datum);
                        }
                        else
                            SensusServiceHelper.Get().Logger.Log("Error received while querying Facebook graph API:  " + response.Error.ErrorMessage, LoggingLevel.Normal, GetType());
            }
            else
                SensusServiceHelper.Get().Logger.Log("Attempted to poll Facebook probe without a valid access token.", LoggingLevel.Normal, GetType());

            return data;
        }

        public override bool TestHealth(ref string error, ref string warning, ref string misc)
        {
            bool restart = base.TestHealth(ref error, ref warning, ref misc);

            if (!HasValidAccessToken)
                restart = true;

            return restart;
        }

        protected override ICollection<string> GetGrantedPermissions()
        {
            if (HasValidAccessToken)
                return AccessToken.CurrentAccessToken.Permissions;
            else
                return new string[0];
        }
    }
}