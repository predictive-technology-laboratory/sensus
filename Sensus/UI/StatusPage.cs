using Sensus.UI.Properties;
using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms;

namespace Sensus.UI
{
    public class StatusPage : ContentPage
    {
        public StatusPage(SensusServiceHelper service)
        {
            List<StackLayout> stacks = UiProperty.GetPropertyStacks(service);

            Content = new StackLayout
            {
                Orientation = StackOrientation.Vertical,
                VerticalOptions = LayoutOptions.FillAndExpand
            };

            foreach (StackLayout stack in stacks)
                (Content as StackLayout).Children.Add(stack);
        }
    }
}
