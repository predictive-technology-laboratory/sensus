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
using Xamarin.UITest;
using Xamarin.UITest.Queries;

namespace Sensus.XTC.UiTests
{
    public class iOSTests : Tests
    {
        protected override IApp GetApp()
        {
            // simulator configs
            return ConfigureApp
                .iOS
                .AppBundle("/Users/jordanbuzzell/SensusWorkspace/sensus/Sensus.iOS/bin/iPhoneSimulator/Debug/device-builds/iphone9.1-10.3.1/SensusiOS.app")
                .DeviceIdentifier("D194D099-DF3D-4698-BED9-010A5F6F2F66")
                .StartApp();
        }

        protected override Func<AppQuery, AppQuery> GetStatusLinesQuery()
        {
            return c => c.Class("UITableViewLabel");
        }
    }
}