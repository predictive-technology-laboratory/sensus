using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Xamarin.UITest;
using Xamarin.UITest.Queries;

namespace Sensus.UITests
{
    [TestFixture(Platform.Android)]
    [TestFixture(Platform.iOS)]
    public class SensusTests
    {
        IApp app;
        Platform platform;

        /// <summary>
        /// this is our xamarin test cloud APIkey.
        /// It could be found in the xamarin test cloud account.
        /// Each team will have a different APIkey. The one below is from the sensus team.
        /// </summary>
        const string _APIkey = "team_API_key";

        public SensusTests(Platform platform)
        {
            this.platform = platform;
        }

        /// <summary>
        ///   In some cases UITest will not be able to resolve the path to the Android SDK.
        ///   Set this to your local Android SDK path.
        /// </summary>
        public static readonly string PathToAndroidSdk = "/Users/lihuacai/Library/Developer/Xamarin/android-sdk-macosx";

        /// <summary>
        ///   Before each test is run we calculate the path to the AppBundle and
        ///   the APK.
        /// </summary>
        public string PathToIPA { get; private set;}
        public string PathToAPK { get; private set;}

        [TestFixtureSetUp]
        public void TestFixtureSetup()
        {
            //these paths also need to be updated based on your environment;
            PathToIPA = "../../../Sensus.iOS/bin/iPhoneSimulator/Debug/Sensus.iOS.app";
            PathToAPK = "../../../Sensus.Android/bin/Debug/edu.virginia.sie.ptl.sensus.apk";
        }

        /// <summary>
        ///   This method checks to make sure that UITest can find the Android SDK if it is not in
        ///   a standard location.
        /// </summary>
        /// <remarks>
        ///   This method is only used if the PathToAndroidSDK is set.
        /// </remarks>
        void CheckAndroidHomeEnvironmentVariable()
        {
            if (string.IsNullOrWhiteSpace(PathToAndroidSdk))
            {
                return;
            }
            string androidHome = Environment.GetEnvironmentVariable("ANDROID_HOME");
            if (string.IsNullOrWhiteSpace(androidHome))
            {
                Environment.SetEnvironmentVariable("ANDROID_HOME", PathToAndroidSdk);
            }
        }
            
        // <summary>
        ///   This will initialize the IApp to the Android application.
        /// </summary>
        void ConfigureAndroidApp()
        {
            // If there is a problem finding the Android SDK, this method can help.
            // CheckAndroidHomeEnvironmentVariable();

            if (TestEnvironment.Platform.Equals(TestPlatform.Local))
            {
                app = ConfigureApp.Android
                    .ApkFile(PathToAPK)
                    .ApiKey(_APIkey)
                    .EnableLocalScreenshots()
                    .StartApp();
            }
            else
            {
                app = ConfigureApp
                    .Android
                    .StartApp();
            }
        }

        /// <summary>
        ///   This will initialize IApp to the iOS application.
        ///   If you have setup an iOS device for testing instead of using iOS simulator, 
        ///   you will need to copy your own device identifier.
        /// </summary>
        void ConfigureiOSApp()
        {
            if (TestEnvironment.Platform.Equals(TestPlatform.Local))
            {
                app = ConfigureApp.iOS
                    .EnableLocalScreenshots()
                    //.DeviceIdentifier("f8c67472f88efb1985c2f5e73698d6bb36988f5d")
                    //.AppBundle("com.xamarin.calabash.example.creditcardvalidation")
                    .AppBundle(PathToIPA)
                    .StartApp();
            }
            else
            {
                app = ConfigureApp
                    .iOS
                    .StartApp();
            }
        }

        void ConfigureTest(Platform platform)
        {
            switch (platform)
            {
                case Platform.Android:
                    ConfigureAndroidApp();
                    break;
                case Platform.iOS:
                    ConfigureiOSApp();
                    break;
            }
        }
            
        [SetUp]
        public void BeforeEachTest()
        {
            ConfigureTest(platform);
        }

        /// <summary>
        /// simple example test. create similar test like below as needed.
        /// </summary>
        [Test]
        public void AppLaunches()
        {
            app.Repl();
            app.Screenshot("First screen.");
        }
            
    }
}

