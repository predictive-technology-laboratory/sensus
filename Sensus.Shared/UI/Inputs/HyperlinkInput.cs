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

using Sensus.UI.UiProperties;
using Xamarin.Forms;

namespace Sensus.UI.Inputs
{
	public class HyperlinkInput : Input
	{
		public override object Value
		{
			get
			{
				return null;
			}
		}

		public override bool Enabled { get; set; }

		public override string DefaultName => "Hyperlink";

		[EntryStringUiProperty("Url", true, 5, true)]
		public string Url { get; set; }

		public override View GetView(int index)
		{
			if (base.GetView(index) == null)
			{
				Label label = CreateLabel(-1);
				
				label.TextDecorations = TextDecorations.Underline;

				TapGestureRecognizer gesture = new TapGestureRecognizer()
				{
					NumberOfTapsRequired = 1
				};

				gesture.Tapped += (s, e) =>
				{
					Device.OpenUri(new System.Uri(Url));

					Complete = true;
				};

				label.GestureRecognizers.Add(gesture);

				base.SetView(label);
			}

			return base.GetView(index);
		}
	}
}
