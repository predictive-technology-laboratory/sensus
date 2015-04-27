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

        protected override void Initialize()
        {
            base.Initialize();

            FacebookSdk.SdkInitialize((AndroidSensusServiceHelper.Get() as AndroidSensusServiceHelper).Service);                  

            FacebookCallback<LoginResult> loginCallback = new FacebookCallback<LoginResult>
                {
                    HandleSuccess = result =>
                        {
                            SensusServiceHelper.Get().Logger.Log("Facebook login succeeded.", SensusService.LoggingLevel.Normal, GetType());
                            LoggedIn = true;
                        },

                    HandleCancel = () =>
                        {
                            SensusServiceHelper.Get().Logger.Log("Facebook login cancelled.", SensusService.LoggingLevel.Normal, GetType());
                            LoggedIn = false;
                        },

                    HandleError = error =>
                        {
                            SensusServiceHelper.Get().Logger.Log("Facebook login failed.", SensusService.LoggingLevel.Normal, GetType());
                            LoggedIn = false;
                        }
                };

            LoginManager.Instance.RegisterCallback(CallbackManagerFactory.Create(), loginCallback);

            // TODO:  Load access token.

            Login();
        }   

        private void Login()
        {
            if (AccessToken.CurrentAccessToken != null && !AccessToken.CurrentAccessToken.IsExpired)
                return;
            
            ManualResetEvent loginWait = new ManualResetEvent(false);

            (AndroidSensusServiceHelper.Get() as AndroidSensusServiceHelper).GetMainActivityAsync(true, mainActivity =>
                {
                    Xamarin.Forms.Device.BeginInvokeOnMainThread(() =>
                        {
                            LoginManager.Instance.LogInWithReadPermissions(mainActivity, GetEnabledPermissionNames());

                            // TODO:  save access token

                            loginWait.Set();
                        });
                });

            loginWait.WaitOne();
        }

        protected override IEnumerable<Datum> Poll(CancellationToken cancellationToken)
        {
            List<Datum> data = new List<Datum>();

            if (!LoggedIn)
                Login();

            if(LoggedIn)
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
            }
            else
            {
                SensusServiceHelper.Get().Logger.Log("Not logged into Facebook.", LoggingLevel.Normal, GetType());

                // TODO:  Log user in.
            }

            return data;
        }            
    }
}