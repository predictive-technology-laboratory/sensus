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

using Sensus.iOS.UI;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;

[assembly: ExportRenderer(typeof(Picker), typeof(iOSEPickerRenderer))]

namespace Sensus.iOS.UI
{
	public class iOSEPickerRenderer : PickerRenderer
	{
		private readonly iOSControlBottomLine _borderDrawer;

		public iOSEPickerRenderer()
		{
			_borderDrawer = new();
		}

		protected override void OnElementChanged(ElementChangedEventArgs<Picker> e)
		{
			base.OnElementChanged(e);

			if (e.NewElement is Picker picker)
			{
				_borderDrawer.Attach(Control, picker);
			}
			else
			{
				_borderDrawer.Detach();
			}
		}
	}
}
