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

using System.Threading.Tasks;
using Android.Content;
using Xamarin.Essentials;
using Android.Provider;
using Android.App;
using System.Threading;
using Android.OS;
using XamarinApplication = Xamarin.Forms.Application;
using Object = Java.Lang.Object;
using Process = Android.OS.Process;
using Uri = Android.Net.Uri;
using System;
using Android;
using Android.Content.PM;

namespace Sensus.Android
{
	public static partial class AndroidPermissions
	{
		public class UsageStats : Permissions.BasePlatformPermission
		{
			private class AppOpsPermissionListener : Object, AppOpsManager.IOnOpChangedListener, Application.IActivityLifecycleCallbacks
			{
				private readonly ManualResetEventSlim _resetEvent;

				public AppOpsPermissionListener(ManualResetEventSlim resetEvent)
				{
					_resetEvent = resetEvent;
				}

				public bool PermissionChanged { get; private set; }

				public void OnActivityResumed(Activity activity)
				{
					if (_resetEvent.IsSet == false)
					{
						PermissionChanged = false;
					}

					_resetEvent.Set();
				}

				public void OnOpChanged(string op, string packageName)
				{
					if (_resetEvent.IsSet == false)
					{
						PermissionChanged = true;
					}

					_resetEvent.Set();
				}

				#region Unimplemented methods
				public void OnActivityCreated(Activity activity, global::Android.OS.Bundle savedInstanceState)
				{

				}

				public void OnActivityDestroyed(Activity activity)
				{

				}

				public void OnActivityPaused(Activity activity)
				{

				}

				public void OnActivitySaveInstanceState(Activity activity, global::Android.OS.Bundle outState)
				{

				}

				public void OnActivityStarted(Activity activity)
				{

				}

				public void OnActivityStopped(Activity activity)
				{

				}
				#endregion
			}

			public override (string androidPermission, bool isRuntime)[] RequiredPermissions => new[] { (Manifest.Permission.PackageUsageStats, false) };

			private AppOpsManagerMode CheckStatus(AppOpsManager appOpsManager)
			{
				return Build.VERSION.SdkInt switch
				{
#pragma warning disable CS0618 // Type or member is obsolete
					< BuildVersionCodes.R => appOpsManager?.NoteOpNoThrow(AppOpsManager.OpstrGetUsageStats, Process.MyUid(), Application.Context.PackageName),
#pragma warning restore CS0618 // Type or member is obsolete
					>= BuildVersionCodes.R => appOpsManager?.NoteOpNoThrow(AppOpsManager.OpstrGetUsageStats, Process.MyUid(), Application.Context.PackageName, null, null)
				} ?? AppOpsManagerMode.Ignored;
			}

			protected PermissionStatus CheckStatus()
			{
				if (AndroidSensusServiceHelper.AppOpsManager is AppOpsManager appOpsManager)
				{
					AppOpsManagerMode mode = CheckStatus(appOpsManager);

					if (mode == AppOpsManagerMode.Allowed)
					{
						return PermissionStatus.Granted;
					}

					return PermissionStatus.Denied;
				}

				return PermissionStatus.Unknown;
			}

			public override Task<PermissionStatus> CheckStatusAsync()
			{
				return Task.FromResult(CheckStatus());
			}

			public override bool ShouldShowRationale()
			{
				return true;
			}

			public async override Task<PermissionStatus> RequestAsync()
			{
				PermissionStatus status = PermissionStatus.Unknown;

				try
				{
					status = CheckStatus();

					if (status != PermissionStatus.Granted)
					{
						using ManualResetEventSlim resetEvent = new();
						using AppOpsPermissionListener listener = new(resetEvent);
						bool confirmDisable = true;

						while (status != PermissionStatus.Granted && confirmDisable == true)
						{
							await Task.Run(() =>
							{
								AndroidSensusServiceHelper.AppOpsManager.StartWatchingMode(AppOpsManager.OpstrGetUsageStats, Application.Context.PackageName, listener);

								if (Build.VERSION.SdkInt < BuildVersionCodes.Q)
								{
									Platform.CurrentActivity.Application.RegisterActivityLifecycleCallbacks(listener);
								}
								else
								{
									Platform.CurrentActivity.RegisterActivityLifecycleCallbacks(listener);
								}

								resetEvent.Reset();

								Intent usageSettings = new(Settings.ActionUsageAccessSettings, Uri.FromParts("package", Application.Context.PackageName, null));

								usageSettings.AddFlags(ActivityFlags.NewTask | ActivityFlags.ClearTask | ActivityFlags.NoHistory | ActivityFlags.NoUserAction);

								if (usageSettings.ResolveActivityInfo(Application.Context.PackageManager, PackageInfoFlags.MatchSystemOnly) != null)
								{
									Platform.CurrentActivity.StartActivity(usageSettings);
								}
								else
								{
									usageSettings = new(Settings.ActionUsageAccessSettings);

									ActivityInfo activity = usageSettings.ResolveActivityInfo(Application.Context.PackageManager, PackageInfoFlags.MatchSystemOnly);

									ComponentName component = new(activity.ApplicationInfo.PackageName, activity.Name);

									usageSettings.AddFlags(ActivityFlags.NewTask | ActivityFlags.ClearTask | ActivityFlags.NoHistory | ActivityFlags.NoUserAction);

									usageSettings.SetComponent(component);

									Platform.CurrentActivity.StartActivity(usageSettings);
								}

								resetEvent.Wait();

								AndroidSensusServiceHelper.AppOpsManager.StopWatchingMode(listener);

								if (Build.VERSION.SdkInt < BuildVersionCodes.Q)
								{
									Platform.CurrentActivity.Application.UnregisterActivityLifecycleCallbacks(listener);
								}
								else
								{
									Platform.CurrentActivity.UnregisterActivityLifecycleCallbacks(listener);
								}
							});

							status = CheckStatus();

							if (status != PermissionStatus.Granted && (listener.PermissionChanged == false))
							{
								confirmDisable = await XamarinApplication.Current.MainPage.DisplayAlert("Permission Request", "The Usage Stats permission for Sensus is not enabled. Are you sure you want to continue without enabling it?", "Enable", "Disable");
							}
						}
					}
				}
				catch (Exception e)
				{
					SensusServiceHelper.Get().Logger.Log($"Exception while obtaining {nameof(UsageStats)} permission: " + e.Message, LoggingLevel.Normal, GetType());
				}
				
				return status;
			}
		}
	}
}
