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
using System.Linq;

namespace Sensus.Android.Probes.Communication
{
    public class AndroidFacebookProbe : FacebookProbe
    {
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

        protected override void Initialize()
        {
            base.Initialize();

            FacebookSdk.SdkInitialize((AndroidSensusServiceHelper.Get() as AndroidSensusServiceHelper).Service);

            _callbackManager = CallbackManagerFactory.Create();

            LoginManager.Instance.RegisterCallback(_callbackManager, LoginCallback);

            ManualResetEvent loginWait = new ManualResetEvent(false);

            (AndroidSensusServiceHelper.Get() as AndroidSensusServiceHelper).GetMainActivityAsync(true, mainActivity =>
                {
                    Xamarin.Forms.Device.BeginInvokeOnMainThread(() =>
                        {
                            LoginManager.Instance.LogInWithReadPermissions(mainActivity, GetEnabledPermissionNames());
                            loginWait.Set();
                        });
                });

            loginWait.WaitOne();
        }          

        protected override GraphRequestBatch GetGraphRequestBatch(Action<GraphResponse> responseHandler)
        {          
            GraphRequestBatch requestBatch = new GraphRequestBatch();

            foreach (Tuple<string, List<string>> edgeFieldQuery in GetEdgeFieldQueries())
            {

                Bundle parameters = new Bundle();
                parameters.PutString("fields", string.Concat(edgeFieldQuery.Item2.Select(field => field + ",")).Trim(','));

                GraphRequest request = new GraphRequest(
                                           AccessToken.CurrentAccessToken,
                                           "/me" + (edgeFieldQuery.Item1 == null ? "" : "/" + edgeFieldQuery.Item1),
                                           parameters,
                                           new JsonCallbackHandler(responseHandler));

                requestBatch.Add(request);
            }

            return requestBatch;
        }
    }
}