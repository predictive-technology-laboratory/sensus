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
using SensusService.Probes.Apps;
using System.Collections.Generic;
using SensusService;
using System.Threading;
using Facebook.LoginKit;
using Facebook.CoreKit;
using Foundation;
using Xamarin.Forms;

namespace Sensus.iOS
{
    public class iOSFacebookProbe : FacebookProbe
    {
        private bool HasValidAccessToken
        {
            get { return AccessToken.CurrentAccessToken != null && AccessToken.CurrentAccessToken.ExpirationDate.SecondsSinceReferenceDate >= NSDate.Now.SecondsSinceReferenceDate; }
        }

        protected override void Initialize()
        {
            base.Initialize();

            if (HasValidAccessToken)
            {
                SensusServiceHelper.Get().Logger.Log("Already have valid Facebook access token. No need to initialize.", LoggingLevel.Normal, GetType());
                return;
            }

            ManualResetEvent loginWait = new ManualResetEvent(false);

            LoginManagerRequestTokenHandler loginResultHandler = new LoginManagerRequestTokenHandler((loginResult, error) =>
                {
                    if (error == null && loginResult.Token != null)
                    {
                        SensusServiceHelper.Get().Logger.Log("Facebook login succeeded.", SensusService.LoggingLevel.Normal, GetType());
                        AccessToken.CurrentAccessToken = loginResult.Token;
                    }
                    else
                    {
                        if (loginResult.IsCancelled)
                        {
                            SensusServiceHelper.Get().Logger.Log("Facebook login cancelled.", SensusService.LoggingLevel.Normal, GetType());
                            AccessToken.CurrentAccessToken = null;
                        }
                        else
                        {
                            SensusServiceHelper.Get().Logger.Log("Facebook login failed.", SensusService.LoggingLevel.Normal, GetType());
                            AccessToken.CurrentAccessToken = null;
                        }
                    }

                    loginWait.Set();
                });

            try
            {
                new LoginManager().LogInWithReadPermissions(GetRequiredPermissionNames(), loginResultHandler);
            }
            catch (Exception ex)
            {
                SensusServiceHelper.Get().Logger.Log("Error while initializing Facebook SDK and/or logging in:  " + ex.Message, LoggingLevel.Normal, GetType());
            }

            loginWait.WaitOne();

            if (!HasValidAccessToken)
            {
                string message = "Failed to obtain access token.";
                SensusServiceHelper.Get().Logger.Log(message, LoggingLevel.Normal, GetType());
                throw new Exception(message);
            }
        }

        protected override IEnumerable<Datum> Poll(CancellationToken cancellationToken)
        {
            return new Datum[0];
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
            {
                List<string> permissions = new List<string>();
                foreach (NSString permission in AccessToken.CurrentAccessToken.Permissions)
                    permissions.Add(permission.ToString());

                return permissions;
            }
            else
                return new string[0];
        }
    }
}

