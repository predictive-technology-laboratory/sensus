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

using Sensus.Android;
using System;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;
using System.ComponentModel;
using Sensus.Shared.Android;

[assembly: ExportRenderer(typeof(TextCell), typeof(TextCellDisposedFix))]

namespace Sensus.Shared.Android
{
    /// <summary>
    /// When using ListViews with ObservableCollections, we're getting ObjectDisposedExceptions because the Pages
    /// holding the ListViews get disposed but the ObservableCollection continues to be modified and fire update
    /// events to the (disposed) Page and ListView. Catch these exceptions here.
    /// </summary>
    public class TextCellDisposedFix : TextCellRenderer
    {
        protected override void OnCellPropertyChanged(object sender, PropertyChangedEventArgs args)
        {
            try { base.OnCellPropertyChanged(sender, args); }
            catch (Exception) { }
        }
    }
}