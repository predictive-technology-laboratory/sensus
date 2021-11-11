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
using System.Collections.Generic;
using Xamarin.Forms;

namespace Sensus.UI.Inputs
{
	public class ReadOnlyTextInput : Input
	{
		public ReadOnlyTextInput()
		{

		}

		public override object Value
		{
			get
			{
				return Text;
			}
		}

		public override bool Enabled => true;

		public override string DefaultName => "Text";

		[EditorUiProperty("Text", true, 2, true)]
		public string Text { get; set; }

		[OnOffUiProperty("Text is HTML:", true, 3)]
		public bool IsTextHtml { get; set; }

		public override View GetView(int index)
		{
			if (base.GetView(index) == null)
			{
				Label label = CreateLabel(-1);

				Label textLabel = new Label()
				{
					StyleClass = new List<string> { "InputContent" },
					HorizontalOptions = LayoutOptions.FillAndExpand,
					Text = Text
				};

				if (IsTextHtml)
				{
					textLabel.TextType = TextType.Html;
				}

				StoreCompletionRecords = false;
				Complete = true;
				Required = false;

				base.SetView(new StackLayout
				{
					Orientation = StackOrientation.Vertical,
					Children = { label, textLabel }
				});
			}

			return base.GetView(index);
		}
	}
}
