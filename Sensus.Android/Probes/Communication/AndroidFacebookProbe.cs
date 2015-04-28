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
using SensusService.Probes.Communication;
using Xamarin.Facebook;
using Xamarin.Facebook.Login;
using Android.Provider;

namespace Sensus.Android.Probes.Communication
{
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

        private bool LoggedIn
        {
            get { return FacebookSdk.IsInitialized && AccessToken.CurrentAccessToken != null && !AccessToken.CurrentAccessToken.IsExpired; }
        }

        protected override void Initialize()
        {
            base.Initialize();

            LogIn();
        }

        public bool LogIn()
        {
            if (LoggedIn)
                return true;

            ManualResetEvent accessTokenWait = new ManualResetEvent(false);
            string accessTokenError = null;

            (AndroidSensusServiceHelper.Get() as AndroidSensusServiceHelper).GetMainActivityAsync(true, mainActivity =>
                {
                    try
                    {                        
                        if (!FacebookSdk.IsInitialized)
                        {
                            SensusServiceHelper.Get().Logger.Log("Facebook SDK is not initialized. Initializing it.", LoggingLevel.Normal, GetType());
                            FacebookSdk.SdkInitialize(mainActivity);
                        }

                        // the above sdk initialization will load the access token if one has been cached
                        if (LoggedIn)
                            accessTokenWait.Set();
                        // have the user log in again
                        else
                        {
                            FacebookCallback<LoginResult> loginCallback = new FacebookCallback<LoginResult>
                                {
                                    HandleSuccess = loginResult =>
                                        {
                                            AndroidSensusServiceHelper.Get().Logger.Log("Facebook login succeeded.", SensusService.LoggingLevel.Normal, GetType());
                                            AccessToken.CurrentAccessToken = loginResult.AccessToken;
                                            accessTokenWait.Set();
                                        },

                                    HandleCancel = () =>
                                        {
                                            AndroidSensusServiceHelper.Get().Logger.Log("Facebook login cancelled.", SensusService.LoggingLevel.Normal, GetType());
                                            AccessToken.CurrentAccessToken = null;
                                            accessTokenWait.Set();
                                        },

                                    HandleError = loginResult =>
                                        {
                                            AndroidSensusServiceHelper.Get().Logger.Log("Facebook login failed.", SensusService.LoggingLevel.Normal, GetType());
                                            AccessToken.CurrentAccessToken = null;
                                            accessTokenWait.Set();
                                        }
                                };

                            LoginManager.Instance.RegisterCallback(mainActivity.FacebookCallbackManager, loginCallback);
                            LoginManager.Instance.LogInWithReadPermissions(mainActivity, GetEnabledPermissionNames());
                        }
                    }
                    catch (Exception ex)
                    {
                        accessTokenError = ex.Message;
                    }
                });

            if (accessTokenError != null)
                SensusServiceHelper.Get().Logger.Log("Error while initializing Facebook SDK and/or logging in:  " + accessTokenError, LoggingLevel.Normal, GetType());

            accessTokenWait.WaitOne();

            if (!LoggedIn)
                SensusServiceHelper.Get().Logger.Log("Failed to log in to Facebook.", LoggingLevel.Normal, GetType());

            return LoggedIn;
        }

        protected override IEnumerable<Datum> Poll(CancellationToken cancellationToken)
        {
            List<Datum> data = new List<Datum>();

            /*if (ValidAccessToken || Login())
            {
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
                        data.Add(new FacebookDatum(DateTimeOffset.UtcNow, response.JSONObject.ToString()));
            }*/

            return data;
        }            
    }
}