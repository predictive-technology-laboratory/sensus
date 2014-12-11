using SensusService;
using SensusUI;
using System;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;

[assembly: ExportRenderer(typeof(TextCell), typeof(TextCellRendererFix))]
namespace SensusUI
{
    public class TextCellRendererFix : TextCellRenderer
    {
        protected override void OnCellPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs args)
        {
            try { base.OnCellPropertyChanged(sender, args); }
            catch (Exception) { }
        }
    }
}