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
using CoreTelephony;

namespace Sensus.iOS
{
    public sealed class CallLogger
    {
        private static readonly CallLogger instance = new CallLogger();

        static CTCallCenter callCenter1;
        static CTCallCenter callCenter2;

        static List <CTCall> calls = new List<CTCall>();

        //Receives Call events
        private static void CallEvent (CTCall inCTCall)
        {

            CoreFoundation.DispatchQueue.MainQueue.DispatchSync (() =>
                {
                    calls.Add(inCTCall);
                });
        }

        // Explicit static constructor to tell C# compiler
        // not to mark type as beforefieldinit
        static CallLogger()
        {
            callCenter1 = new CTCallCenter ();
            callCenter1.CallEventHandler += CallEvent;
            callCenter2 = new CTCallCenter ();
            callCenter2.CallEventHandler += CallEvent;
        }

        public List<CTCall> CallLog(){

           return calls;
        }

        private CallLogger()
        {
        }

        public static CallLogger Instance
        {
            get
            {
                return instance;
            }
        }
    }
}

