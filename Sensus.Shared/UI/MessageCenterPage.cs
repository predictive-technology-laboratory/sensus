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
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace Sensus.UI
{
	public class MessageCenterPage : ContentPage
	{
		protected Grid _contentGrid;
		protected ListView _notificationList;

		private class TitleColorValueConverter : IValueConverter
		{
			private Color _defaultColor;

			public TitleColorValueConverter(Color defaultColor)
			{
				_defaultColor = defaultColor;
			}

			public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
			{
				if (value == null)
				{
					return Color.Red;
				}

				return _defaultColor;
			}

			public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
			{
				throw new NotImplementedException();
			}
		}

		public MessageCenterPage()
		{
			Title = "Message Center";

			_notificationList = new ListView(ListViewCachingStrategy.RecycleElement);

			_notificationList.SetBinding(IsVisibleProperty, new Binding("Count", converter: new ViewVisibleValueConverter(), converterParameter: false));  // don't show list when there are no surveys
			_notificationList.ItemTemplate = new DataTemplate(typeof(NotificationTextCell));
			_notificationList.ItemTemplate.SetBinding(TextCell.TextProperty, nameof(NotificationMessage.DisplayTitle));
			_notificationList.ItemTemplate.SetBinding(TextCell.TextColorProperty, new Binding(nameof(NotificationMessage.ViewedOn), converter: new TitleColorValueConverter(new TextCell().TextColor)));
			_notificationList.ItemTemplate.SetBinding(TextCell.DetailProperty, new Binding(nameof(NotificationMessage.ReceivedOn), stringFormat: "{0:g}"));
			_notificationList.ItemsSource = SensusServiceHelper.Get().Notifications;

			_notificationList.ItemTapped += ItemTapped;

			_contentGrid = new Grid
			{
				RowDefinitions = { new RowDefinition { Height = new GridLength(1, GridUnitType.Star) } },
				ColumnDefinitions = { new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) } },
				VerticalOptions = LayoutOptions.FillAndExpand
			};

			_contentGrid.Children.Add(_notificationList, 0, 0);

			Content = _contentGrid;

			// display an informative message when there are no surveys
			Label noSurveysLabel = new Label
			{
				Text = "You have no messages.",
				TextColor = Color.Accent,
				FontSize = 20,
				VerticalOptions = LayoutOptions.Center,
				HorizontalOptions = LayoutOptions.Center,
				BindingContext = SensusServiceHelper.Get().Notifications
			};

			noSurveysLabel.SetBinding(IsVisibleProperty, new Binding("Count", converter: new ViewVisibleValueConverter(), converterParameter: true));

			_contentGrid.Children.Add(noSurveysLabel, 0, 0);

			//SensusServiceHelper.Get().Notifications.Clear();

			//SensusServiceHelper.Get().Notifications.Add(new NotificationMessage { Title = "Test", Message = "This is a test notification", ReceivedOn = DateTime.Now.AddSeconds(-30) });
			//SensusServiceHelper.Get().Notifications.Add(new NotificationMessage { Title = "Test 2", Message = "This is another one.", ReceivedOn = DateTime.Now });
		}

		protected virtual async void ItemTapped(object sender, ItemTappedEventArgs args)
		{
			await Navigation.PushAsync(new MessagePage(_notificationList.SelectedItem as NotificationMessage, this));
		}

		public async Task ViewNotification(NotificationMessage notificationMessage)
		{
			await Navigation.PopAsync();

			await Navigation.PushAsync(new MessagePage(notificationMessage, this));
		}

		private class NotificationTextCell : TextCell
		{
			public NotificationTextCell()
			{
				//MenuItem deleteMenuItem = new MenuItem { Text = "Delete", IsDestructive = true };
				//deleteMenuItem.SetBinding(MenuItem.CommandParameterProperty, ".");

				//deleteMenuItem.Clicked += async (sender, e) =>
				//{

				//};

				//ContextActions.Add(deleteMenuItem);
			}
		}
	}
}
