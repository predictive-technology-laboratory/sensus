//Copyright 2014 The Rector & Visitors of the University of Virginia
//
//Permission is hereby granted, free of charge, to any person obtaining a copy 
//of this software and associated documentation files (the "Software"), to deal 
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell 
//copies of the Software, and to permit persons to whom the Software is 
//furnished to do so, subject to the following conditions:
//
//The above copyright notice and this permission notice shall be included in 
//all copies or substantial portions of the Software.
//
//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
//INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A 
//PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT 
//HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION 
//OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE 
//SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using Sensus.Android;
using System;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;
using System.ComponentModel;

[assembly: ExportRenderer(typeof(TextCell), typeof(TextCellDisposedFix))]

namespace Sensus.Android
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
            try
            {
                base.OnCellPropertyChanged(sender, args);
            }
            catch (Exception ex)
            {
                SensusServiceHelper.Get().Logger.Log("Caught TextCell disposed exception:  " + ex, LoggingLevel.Normal, GetType());
            }
        }
    }
}
