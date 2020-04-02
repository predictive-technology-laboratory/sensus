using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Foundation;
using Sensus.Callbacks;
using Sensus.Context;
using Sensus.iOS.Notifications.UNUserNotifications;
using Sensus.Probes.User.Scripts;
using UserNotifications;

namespace Sensus.iOS.Callbacks
{
	public class iOSTimerCallbackScheduler : iOSCallbackScheduler
	{
		private Dictionary<string, NSTimer> _timers;

		public iOSTimerCallbackScheduler()
		{
			_timers = new Dictionary<string, NSTimer>();
		}

		public override List<string> CallbackIds
		{
			get
			{
				List<string> callbackIds;

				lock (_timers)
				{
					callbackIds = _timers.Keys.ToList();
				}

				return callbackIds;
			}
		}

		protected override void CancelLocalInvocation(ScheduledCallback callback)
		{
			SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(() =>
			{
				lock (_timers)
				{
					if (_timers.TryGetValue(callback.Id, out NSTimer timer))
					{
						timer.Invalidate();

						_timers.Remove(callback.Id);
					}
				}
			});
		}

		protected override async Task RequestLocalInvocationAsync(ScheduledCallback callback)
		{
			if (callback.NextExecution != null)
			{
				if (_timers.TryGetValue(callback.Id, out NSTimer existingTimer) == false || existingTimer.TimeInterval != callback.RepeatDelay?.TotalSeconds)
				{
					CancelLocalInvocation(callback);

					SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(() =>
					{
						NSTimer timer = new NSTimer((NSDate)callback.NextExecution.Value, callback.RepeatDelay ?? TimeSpan.Zero, async t =>
						{
							await RaiseCallbackAsync(callback, callback.InvocationId);
						}, callback.RepeatDelay.HasValue);
						/*{
							Tolerance = callback.RepeatDelay.Value.TotalMilliseconds * .10
						};*/

						NSRunLoop.Main.AddTimer(timer, NSRunLoopMode.Default);

						lock (_timers)
						{
							_timers[callback.Id] = timer;
						}

					});
				}
			}

			await Task.CompletedTask;
		}

		public async Task RequestNotificationsAsync(bool gpsIsRunning)
		{
			UNUserNotificationNotifier notifier = SensusContext.Current.Notifier as UNUserNotificationNotifier;

			foreach (string id in CallbackIds)
			{
				if (TryGetCallback(id) is ScheduledCallback callback)
				{
					using (NSMutableDictionary callbackInfo = GetCallbackInfo(callback))
					{
						if (callbackInfo != null)
						{
							if (callback.Silent == false)
							{
								await notifier.IssueNotificationAsync(callback.Protocol?.Name ?? "Alert", callback.UserNotificationMessage, callback.Id, true, callback.Protocol, null, callback.NotificationUserResponseAction, callback.NotificationUserResponseMessage, callback.NextExecution.Value, callbackInfo);
							}
							else
							{
								await RequestRemoteInvocationAsync(callback);
							}
						}
					}
				}
			}
		}

		public async Task CancelNotificationsAsync()
		{
			UNUserNotificationNotifier notifier = SensusContext.Current.Notifier as UNUserNotificationNotifier;

			foreach (string id in CallbackIds)
			{
				if (TryGetCallback(id) is ScheduledCallback callback)
				{
					notifier.CancelNotification(callback.Id);

					await CancelRemoteInvocationAsync(callback);
				}
			}
		}

		protected override Task ReissueSilentNotificationAsync(string id)
		{
			throw new NotImplementedException();
		}

		public override void CancelSilentNotifications()
		{
			throw new NotImplementedException();
		}
	}
}
