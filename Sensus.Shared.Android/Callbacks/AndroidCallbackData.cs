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
using Android.Content;
using Sensus.Shared.Callbacks;

namespace Sensus.Shared.Android.Callbacks
{
    public class AndroidCallbackData: ICallbackData
    {
        #region Fields
        private readonly Intent _intent;
        #endregion

        #region Constructors
        public AndroidCallbackData(Intent intent)
        {
            _intent = intent;
        }
        #endregion

        #region Properties
        public NotificationType Type
        {
            get { return (NotificationType)Enum.Parse(typeof(NotificationType), _intent.GetStringExtra("NotficationType")); }
            set { _intent.PutExtra("NotficationType", value.ToString()); }
        }

        public string CallbackId
        {
            get { return _intent.GetStringExtra("CallbackId"); }
            set { _intent.PutExtra("CallbackId", value); }
        }

        public bool IsRepeating
        {
            get { return _intent.GetBooleanExtra("IsRepeating", false); }
            set { _intent.PutExtra("IsRepeating", value); }
        }

        public TimeSpan RepeatDelay
        {
            get { return new TimeSpan(_intent.GetLongExtra("RepeatDelay", 0)); }
            set { _intent.PutExtra("RepeatDelay", value.Ticks); }
        }

        public bool LagAllowed
        {
            get { return _intent.GetBooleanExtra("LagAllowed", false); }
            set { _intent.PutExtra("LagAllowed", value); }
        }
        #endregion
    }
}
