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
using CoreAnimation;
using CoreGraphics;
using UIKit;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;
using XamarinApplication = Xamarin.Forms.Application;

namespace Sensus.iOS.UI
{
	public class iOSControlBottomLine
	{
		private readonly Color _focusedColor;
		private readonly Color _unfocusedColor;
		public UIView _uiView;
		public View _view;
		public CALayer _borderLayer;
		public bool _attached = false;

		private const int DEFAULT_THICKNESS = 1;
		private const int FOCUSED_THICKNESS = DEFAULT_THICKNESS + 1;
		private const int UNFOCUSED_THICKNESS = DEFAULT_THICKNESS;

		public iOSControlBottomLine()
		{
			XamarinApplication.Current.Resources.TryGetValue("AccentColor", out object focusedColor);
			XamarinApplication.Current.Resources.TryGetValue("LessDimmedColor", out object unfocusedColor);

			_focusedColor = focusedColor as Color? ?? Color.Default;
			_unfocusedColor = unfocusedColor as Color? ?? Color.Default;
		}

		public void OnSizeChanged(object sender, EventArgs e)
		{
			DrawBorder();
		}

		public void OnFocusChanged(object sender, FocusEventArgs e)
		{
			DrawBorder();
		}

		public void Attach(UIView uiView, View view)
		{
			_uiView = uiView;
			_view = view;

			view.SizeChanged += OnSizeChanged;
			view.Focused += OnFocusChanged;
			view.Unfocused += OnFocusChanged;
		}

		public void Detach()
		{
			_view.SizeChanged -= OnSizeChanged;
			_view.Focused -= OnFocusChanged;
			_view.Unfocused -= OnFocusChanged;
		}

		private void DrawBorder()
		{
			CGColor color;
			int thickness;

			if (_view.IsFocused)
			{
				color = _focusedColor.ToCGColor();

				thickness = FOCUSED_THICKNESS;
			}
			else
			{
				color = _unfocusedColor.ToCGColor();

				thickness = UNFOCUSED_THICKNESS;
			}

			if (_attached == false)
			{
				_borderLayer = new CALayer
				{
					MasksToBounds = true,
					Frame = new CGRect(0, _view.Bounds.Height - thickness, _view.Bounds.Width, thickness),
					BackgroundColor = color,
				};

				_uiView.Layer.AddSublayer(_borderLayer);

				_attached = true;
			}
			else
			{
				_borderLayer.Frame = new CGRect(0, _view.Bounds.Height - thickness, _view.Bounds.Width, thickness);
				_borderLayer.BackgroundColor = color;
			}
		}
	}
}
