---
uid: coding_conventions
---

# Coding Conventions
The following snippet (full file [here](https://github.com/predictive-technology-laboratory/sensus/blob/develop/Sensus.Android/AndroidSensusServiceHelper.cs)) 
demonstrates many of the coding conventions within Sensus. These must be strictly adhered to.

The start of every source code file must contain the following copyright notice:

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

Next is a blank line followed by the `using` declarations:

    
    using System;
    ...

Next are the `namespace` and `class` declarations (note the upper-camel-casing):

    namespace Sensus.Android
    {
        public class AndroidSensusServiceHelper : SensusServiceHelper
        {

Next are any `static` or `const` fields, which must be in all capital letters:

            public const string MAIN_ACTIVITY_WILL_BE_SET = "MAIN-ACTIVITY-WILL-BE-SET";
            ...

Next are any `private` members, which must be explicitly denoted as `private`, use lower-camel-casing, and be prefixed with an underscore:
       
            private AndroidSensusService _service;
            ...

Next are any properties, which must use upper-camel-casing and be identical to the private member except without the underscore:
            
            [JsonIgnore]
            public AndroidSensusService Service
            {
                get { return _service; }
            }
            ...

Next are any constructors:

            public AndroidSensusServiceHelper()
            {
                _mainActivityWait = new ManualResetEvent(false);   
            }

Next are any methods:

            protected override void InitializeXamarinInsights()
            {
                Insights.Initialize(XAMARIN_INSIGHTS_APP_KEY, Application.Context);  // can't reference _service here since this method is called from the base class constructor, before the service is set.
            }
            ... 
        }
    }

Some miscellaneous conventions and guidelines:
* Always reference private variables within a class rather than their properties.
* Use grammatically appropriate variable names. For example, a list of `string` objects representing names should be declared 
  as `List<string> names`, and the associated `foreach` statement should be `foreach(string name in names)`.
* Do not update packages while you work. Such changes will often create conflicts with other developers.
* Do not submit code that is commented out.