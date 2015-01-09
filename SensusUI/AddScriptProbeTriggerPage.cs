using SensusService.Probes;
using Xamarin.Forms;
using System.Linq;

namespace SensusUI
{
    public class AddScriptProbeTriggerPage : ContentPage
    {
        public AddScriptProbeTriggerPage(IScriptProbe scriptProbe)
        {
            StackLayout contentLayout = new StackLayout
            {
                Orientation = StackOrientation.Vertical,
                VerticalOptions = LayoutOptions.FillAndExpand
            };

            Picker probePicker = new Picker();

            foreach (Probe enabledProbe in scriptProbe.Protocol.Probes.Where(p => p.Enabled))
                probePicker.Items.Add(enabledProbe.DisplayName);

            contentLayout.Children.Add(probePicker);

            StackLayout triggerLayout = new StackLayout
            {
                Orientation = StackOrientation.Vertical,
                VerticalOptions = LayoutOptions.FillAndExpand
            };

            probePicker.SelectedIndexChanged += (o, e) =>
                {
                    triggerLayout.Children.Clear();

                    Label conditionLabel = new Label
                    {
                        Text = "Condition:",
                        Font = Font.SystemFontOfSize(20)
                    };

                    Picker conditionPicker = new Picker();

                    triggerLayout.Children.Add(new StackLayout
                    {
                        Orientation = StackOrientation.Horizontal,
                        HorizontalOptions = LayoutOptions.FillAndExpand,
                        Children = { conditionLabel, conditionPicker }
                    });
                };

            contentLayout.Children.Add(triggerLayout);

            Content = contentLayout;
        }
    }
}
