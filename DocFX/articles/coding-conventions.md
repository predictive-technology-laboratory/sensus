---
uid: coding_conventions
---

# Coding Conventions
The following snippet (full file [here](https://github.com/predictive-technology-laboratory/sensus/blob/develop/Sensus.Android/AndroidSensusServiceHelper.cs)) 
demonstrates many of the coding conventions within Sensus. These must be strictly adhered to.

The start of every source code file must contain the following copyright notice:

    //Copyright 2014 The Rector & Visitors of the University of Virginia
	//
	//Permission is hereby granted, free of charge, to any person obtaining a copy 
	//of this software and associated documentation files (the "Software"), to deal 
	//in the Software without restriction, including without limitation the rights
	//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell 
	//copies of the Software, and to permit persons to whom the Software is 
	//furnished to do so, subject to the following conditions:
	//
	//The above copyright notice and this permission notice shall be included in 
	//all copies or substantial portions of the Software.
	//
	//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
	//INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A 
	//PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT 
	//HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION 
	//OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE 
	//SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

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