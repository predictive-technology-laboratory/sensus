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
using SensusService.Probes.Communication;
using Xamarin.Facebook;
using Xamarin.Facebook.Login;
using Android.Runtime;
using Android.App;
using System.Threading;
using System.Collections.Generic;
using SensusService;
using Android.OS;
using Org.Json;

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

        private class JsonCallbackHandler : Java.Lang.Object, GraphRequest.ICallback
        {
            private Action<GraphResponse> _callback;

            public JsonCallbackHandler(Action<GraphResponse> callback)
            {
                _callback = callback;
            }

            public void OnCompleted(GraphResponse response)
            {
                if (_callback != null)
                    _callback(response);
            }
        }

        private ICallbackManager _callbackManager;

        public AndroidFacebookProbe()
        {
        }

        protected override void Initialize()
        {
            base.Initialize();

            FacebookSdk.SdkInitialize((AndroidSensusServiceHelper.Get() as AndroidSensusServiceHelper).Service);

            _callbackManager = CallbackManagerFactory.Create();

            FacebookCallback<LoginResult> loginCallback = new FacebookCallback<LoginResult>
            {
                HandleSuccess = result =>
                {
                    AndroidSensusServiceHelper.Get().Logger.Log("Facebook login succeeded.", SensusService.LoggingLevel.Normal, GetType());
                },
                    
                HandleCancel = () =>
                {
                    AndroidSensusServiceHelper.Get().Logger.Log("Facebook login cancelled.", SensusService.LoggingLevel.Normal, GetType());
                },
                    
                HandleError = error =>
                {
                    AndroidSensusServiceHelper.Get().Logger.Log("Facebook login failed.", SensusService.LoggingLevel.Normal, GetType());
                }
            };

            LoginManager.Instance.RegisterCallback(_callbackManager, loginCallback);

            ManualResetEvent loginWait = new ManualResetEvent(false);

            (AndroidSensusServiceHelper.Get() as AndroidSensusServiceHelper).GetMainActivityAsync(true, mainActivity =>
                {
                    Xamarin.Forms.Device.BeginInvokeOnMainThread(() =>
                        {
                            LoginManager.Instance.LogInWithReadPermissions(mainActivity, GetEnabledPermissions());
                            loginWait.Set();
                        });
                });

            loginWait.WaitOne();
        }            

        protected override IEnumerable<Datum> Poll(CancellationToken cancellationToken)
        {
            /*GraphRequest request = new GraphRequest(AccessToken.CurrentAccessToken, new JsonCallbackHandler(response =>
                    {
                    }));
            
            Bundle parameters = new Bundle();
            parameters.PutString("fields", "id,name,link");
            request.Parameters = parameters;
            request.ExecuteAsync();*/

            return null;
        }            
    }
}