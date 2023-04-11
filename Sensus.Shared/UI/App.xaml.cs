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

using Xamarin.Forms;

namespace Sensus.UI
{
	public partial class App : Application
	{
		public Page FlyoutPage
		{
			get { return (MainPage as FlyoutPage).Flyout; }
		}

		public Page DetailPage
		{
			get
			{
				return (MainPage as FlyoutPage).Detail;
			}
			set
			{
				if (value != null)
				{
					value.Parent = null;
				}

				if (MainPage is FlyoutPage flyoutPage)
				{
					flyoutPage.Detail = value;
				}
			}
		}

		public App()
		{
			InitializeComponent();

			MainPage = new SensusFlyoutPage();
		}

		protected override void OnStart()
		{
			base.OnStart();
		}

		public static void InterruptScript()
		{
			if (Current is App app)
			{
				if (app.MainPage is BaseFlyoutPage flyoutPage)
				{
					flyoutPage.InterruptScript(true);
				}
			}
		}
	}
}
