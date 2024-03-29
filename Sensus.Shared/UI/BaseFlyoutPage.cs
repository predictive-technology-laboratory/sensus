﻿// Copyright 2014 The Rector & Visitors of the University of Virginia
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
using Xamarin.Forms;

namespace Sensus.UI
{
	public class BaseFlyoutPage : FlyoutPage
	{
		public void InterruptScript(bool useReturnPage)
		{
			try
			{
				if (Detail is InputGroupPage withoutNavigationPage)
				{
					withoutNavigationPage.Interrupt(false, useReturnPage);
				}
				else if (Detail is NavigationPage navigationPage && navigationPage.CurrentPage is InputGroupPage withNavigationPage)
				{
					withNavigationPage.Interrupt(false, useReturnPage);
				}
			}
			catch (Exception e)
			{
				SensusServiceHelper.Get().Logger.Log($"Error interrupting script: {e.Message}", LoggingLevel.Normal, GetType());
			}
		}

		protected void RegisterNavigationEvents()
		{
			PropertyChanging += (o, e) =>
			{
				if (e.PropertyName == nameof(Detail))
				{
					InterruptScript(false);
				}
			};
		}
	}
}
