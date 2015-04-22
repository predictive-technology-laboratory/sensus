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

namespace Sensus.Android.Probes.Communication
{
    public class AndroidListeningFacebookProbe : ListeningFacebookProbe
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

        private ICallbackManager _callbackManager;

        static readonly string [] PERMISSIONS = new string[] { "public_profile", "user_friends" };

        public AndroidListeningFacebookProbe()
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
                            LoginManager.Instance.LogInWithPublishPermissions(mainActivity, PERMISSIONS);
                            loginWait.Set();
                        });
                });

            loginWait.WaitOne();
        }

        protected override void StartListening()
        {
        }

        protected override void StopListening()
        {
        }
    }
}