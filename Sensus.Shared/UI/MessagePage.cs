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

using Sensus.Notifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Xamarin.Forms;

namespace Sensus.UI
{
	public class MessagePage : ContentPage
	{
		public const string VIEW_EVENT = "View";
		public const string DELETE_EVENT = "Delete";

		public MessagePage(NotificationMessage notificationMessage, MessageCenterPage parent)
		{
			Title = notificationMessage.Title;

			ToolbarItems.Add(new ToolbarItem("Delete", null, async () =>
			{
				if (await DisplayAlert("Confirm", "Are you sure you want to delete this message?", "Yes", "No"))
				{
					SensusServiceHelper.Get().Notifications.Remove(notificationMessage);

					notificationMessage.Protocol.LocalDataStore.WriteDatum(new MessageEventDatum(notificationMessage.Id, notificationMessage.ProtocolId, DELETE_EVENT, DateTimeOffset.UtcNow), CancellationToken.None);

					await Navigation.PopAsync();
				}
			}));

			StackLayout buttonLayout = new StackLayout
			{
				Orientation = StackOrientation.Horizontal
			};

			Button previousButton = new Button
			{
				Text = "Previous",
				HorizontalOptions = LayoutOptions.FillAndExpand
			};

			Button nextButton = new Button
			{
				Text = "Next",
				HorizontalOptions = LayoutOptions.FillAndExpand
			};

			buttonLayout.Children.Add(previousButton);
			buttonLayout.Children.Add(nextButton);

			if (IsFirst(notificationMessage))
			{
				previousButton.IsEnabled = false;
			}
			else
			{
				previousButton.Clicked += async (s, e) =>
				{
					await parent.ViewNotification(GetOtherNotification(notificationMessage, -1));
				};
			}

			if (IsLast(notificationMessage))
			{
				nextButton.IsEnabled = false;
			}
			else
			{
				nextButton.Clicked += async (s, e) =>
				{
					await parent.ViewNotification(GetOtherNotification(notificationMessage, 1));
				};
			}

			ScrollView scrollView = new ScrollView
			{
				VerticalOptions = LayoutOptions.FillAndExpand,
				Content = new Label { Text = notificationMessage.Message }
			};

			Frame frame = new Frame
			{
				Content = scrollView,
				BorderColor = Color.Accent,
				BackgroundColor = Color.Transparent,
				VerticalOptions = LayoutOptions.FillAndExpand,
				HasShadow = false,
				Padding = new Thickness(10)
			};

			StackLayout layout = new StackLayout
			{
				Orientation = StackOrientation.Vertical
			};

			layout.Children.Add(frame);
			layout.Children.Add(buttonLayout);

			Content = layout;

			notificationMessage.Protocol.LocalDataStore.WriteDatum(new MessageEventDatum(notificationMessage.Id, notificationMessage.ProtocolId, VIEW_EVENT, DateTimeOffset.UtcNow), CancellationToken.None);
			notificationMessage.SetAsViewed();
		}

		private static NotificationMessage GetOtherNotification(NotificationMessage notificationMessage, int offset)
		{
			List<NotificationMessage> notifications = SensusServiceHelper.Get().Notifications.ToList();

			int index = notifications.IndexOf(notificationMessage) + offset;

			if (index > -1 && index < notifications.Count)
			{
				return notifications[index];
			}

			return null;
		}

		private static bool IsFirst(NotificationMessage notificationMessage)
		{
			return SensusServiceHelper.Get().Notifications.First() == notificationMessage;
		}

		private static bool IsLast(NotificationMessage notificationMessage)
		{
			return SensusServiceHelper.Get().Notifications.Last() == notificationMessage;
		}

	}
}
