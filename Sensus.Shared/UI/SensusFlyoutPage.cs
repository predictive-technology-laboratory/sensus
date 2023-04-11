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
using Sensus.Exceptions;
using Xamarin.Forms;

namespace Sensus.UI
{
	public class SensusFlyoutPage : BaseFlyoutPage
	{
		private SensusDetailPage _flyoutPage;

		public SensusFlyoutPage()
		{
			_flyoutPage = new SensusDetailPage();

			_flyoutPage.MenuItemListView.ItemSelected += (sender, e) =>
			{
				try
				{
					SensusDetailPageItem selectedDetailPageItem = e.SelectedItem as SensusDetailPageItem;

					if (selectedDetailPageItem != null)
					{
						if (selectedDetailPageItem.TargetType == null)
						{
							selectedDetailPageItem.Action?.Invoke();
						}
						else
						{
							Detail = new NavigationPage((Page)Activator.CreateInstance(selectedDetailPageItem.TargetType));
							IsPresented = false;
						}

						_flyoutPage.MenuItemListView.SelectedItem = null;
					}
				}
				catch (Exception ex)
				{
					SensusException.Report("Exception while handling detail menu item selection:  " + ex.Message, ex);
				}
			};

			Flyout = _flyoutPage;

			// the SensusServiceHelper is not yet loaded when this page is constructed. as a result, we cannot assign the 
			// ProtocolsPage to the Detail property. instead, just assign a blank content page and show the user the master
			// detail list. by the time the user selects from the list, the service helper will be available and the protocols
			// page will be ready to go.
			Detail = new NavigationPage(new ContentPage
			{
				Content = new Label
				{
					Text = "Welcome to Sensus." + Environment.NewLine + "Please select from the menu above.",
					FontSize = 30,
					HorizontalOptions = LayoutOptions.CenterAndExpand,
					VerticalOptions = LayoutOptions.CenterAndExpand,
					VerticalTextAlignment = TextAlignment.Center,
					HorizontalTextAlignment = TextAlignment.Center
				}
			});

			IsPresented = true;

			RegisterNavigationEvents();
		}
	}
}