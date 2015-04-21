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

        private class CustomProfileTracker : ProfileTracker
        {
            public Action<Profile, Profile> HandleCurrentProfileChanged { get; set; }

            protected override void OnCurrentProfileChanged(Profile oldProfile, Profile currentProfile)
            {
                if (HandleCurrentProfileChanged != null)
                    HandleCurrentProfileChanged(oldProfile, currentProfile);
            }
        }

        private ICallbackManager _callbackManager;
        private ProfileTracker _profileTracker;

        static readonly string [] PERMISSIONS = new string[] { };

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

            (AndroidSensusServiceHelper.Get() as AndroidSensusServiceHelper).GetMainActivityAsync(true, mainActivity =>
                {
                    LoginManager.Instance.LogInWithPublishPermissions(mainActivity, PERMISSIONS);

                    _profileTracker = new CustomProfileTracker
                    {
                        HandleCurrentProfileChanged = (oldProfile, currentProfile) =>
                        {
                            AndroidSensusServiceHelper.Get().Logger.Log("Facebook profile changed.", SensusService.LoggingLevel.Normal, GetType());
                        }
                    };
                });
        }

        protected override void StartListening()
        {
            _profileTracker.StartTracking();
        }

        protected override void StopListening()
        {
            _profileTracker.StopTracking();
        }
    }
}