using SensusService.Probes;
using SensusUI.UiProperties;
using Xamarin.Forms;

namespace SensusUI
{
    /// <summary>
    /// Displays properties for a single probe.
    /// </summary>
    public class ProbePage : ContentPage
    {
        public ProbePage(Probe probe)
        {
            BindingContext = probe;

            SetBinding(TitleProperty, new Binding("DisplayName"));

            Content = new StackLayout
            {
                VerticalOptions = LayoutOptions.FillAndExpand,
                Orientation = StackOrientation.Vertical,
            };

            foreach (StackLayout stack in UiProperty.GetPropertyStacks(probe))
                (Content as StackLayout).Children.Add(stack);
        }
    }
}
