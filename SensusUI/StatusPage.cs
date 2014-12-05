using SensusService;
using SensusUI.UiProperties;
using System.Collections.Generic;
using Xamarin.Forms;

namespace SensusUI
{
    public class StatusPage : ContentPage
    {
        public StatusPage(SensusServiceHelper service)
        {
            Title = "Status";

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
