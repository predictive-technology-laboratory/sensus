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
using System.Linq;
using System.Collections.Generic;

using Foundation;
using UIKit;
using MonoTouch.NUnit.UI;
using Sensus.iOS.Tests.SetUp;
using Sensus.Tests.Classes;

namespace Sensus.iOS.Tests
{
    // The UIApplicationDelegate for the application. This class is responsible for launching the 
    // User Interface of the application, as well as listening (and optionally responding) to 
    // application events from iOS.
    [Register("UnitTestAppDelegate")]
    public partial class UnitTestAppDelegate : UIApplicationDelegate
    {
        private class NonterminatingTouchRunner : TouchRunner
        {
            private UIWindow _window;
            private LogSaver _logSaver;

            public NonterminatingTouchRunner(UIWindow window, LogSaver logSaver)
                : base(window)
            {
                _window = window;
                _logSaver = logSaver;
            }

            protected override void TerminateWithSuccess()
            {
                _window.BeginInvokeOnMainThread(() =>
                {
                    UITextView textView = new UITextView
                    {
                        AccessibilityLabel = "sensus-test-log",
                        Text = _logSaver.Log.ToString().Trim()
                    };

                    UIScrollView scrollView = new UIScrollView();
                    scrollView.Add(textView);

                    _window.RootViewController.View = textView;
                });
            }
        }

        private LogSaver _logSaver = new LogSaver();
        private UIWindow _window;
        private TouchRunner _runner;

        //
        // This method is invoked when the application has loaded and is ready to run. In this 
        // method you should instantiate the window, load the UI into it and then make the window
        // visible.
        //
        // You have 17 seconds to return from this method, or iOS will terminate your application.
        //
        public override bool FinishedLaunching(UIApplication app, NSDictionary options)
        {
            Console.SetOut(_logSaver);
            SetUpFixture.SetUp();

            // create a new window instance based on the screen size
            _window = new UIWindow(UIScreen.MainScreen.Bounds);
            _runner = new NonterminatingTouchRunner(_window, _logSaver);

            // register every tests included in the main application/assembly
            _runner.Add(System.Reflection.Assembly.GetExecutingAssembly());
            _runner.AutoStart = true;
            _runner.TerminateAfterExecution = true;

            _window.RootViewController = new UINavigationController(_runner.GetViewController());

            // make the window visible
            _window.MakeKeyAndVisible();

            #if ENABLE_TEST_CLOUD
            Xamarin.Calabash.Start();
            #endif           

            return true;
        }
    }
}
