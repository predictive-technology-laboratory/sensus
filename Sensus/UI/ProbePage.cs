using Sensus.Probes;
using Sensus.UI.Properties;
using System.Collections.Generic;
using Xamarin.Forms;

namespace Sensus.UI
{
    /// <summary>
    /// Displays properties for a single probe.
    /// </summary>
    public class ProbePage : ContentPage
    {
        public ProbePage(Probe probe)
        {
            BindingContext = probe;

            SetBinding(TitleProperty, new Binding("Name"));

            List<StackLayout> stacks = new List<StackLayout>();

            #region name
            Label nameLabel = new Label
            {
                Text = "Name:  ",
                HorizontalOptions = LayoutOptions.FillAndExpand,
                Font = Font.SystemFontOfSize(20)
            };

            Entry nameEntry = new Entry();
            nameEntry.BindingContext = probe;
            nameEntry.SetBinding(Entry.TextProperty, "Name");

            stacks.Add(new StackLayout
            {
                HorizontalOptions = LayoutOptions.StartAndExpand,
                Orientation = StackOrientation.Horizontal,
                Children = { nameLabel, nameEntry }
            });
            #endregion

            #region status
            Label statusLabel = new Label
            {
                Text = "Status:  ",
                HorizontalOptions = LayoutOptions.FillAndExpand,
                Font = Font.SystemFontOfSize(20)
            };

            Switch statusSwitch = new Switch();
            statusSwitch.BindingContext = probe;
            statusSwitch.SetBinding(Switch.IsToggledProperty, "Enabled");

            stacks.Add(new StackLayout
            {
                HorizontalOptions = LayoutOptions.StartAndExpand,
                Orientation = StackOrientation.Horizontal,
                Children = { statusLabel, statusSwitch }
            });
            #endregion

            #region parameters
            stacks.AddRange(UiProperty.GetPropertyStacks(probe));
            #endregion

            Content = new StackLayout
            {
                VerticalOptions = LayoutOptions.FillAndExpand,
                Orientation = StackOrientation.Vertical,
            };

            foreach (StackLayout stack in stacks)
                (Content as StackLayout).Children.Add(stack);
        }
    }
}
