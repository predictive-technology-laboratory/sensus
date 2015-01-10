using SensusService.Probes;
using Xamarin.Forms;
using System.Linq;
using SensusService.Probes.User;
using SensusService;
using System.Collections.Generic;

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

            List<Probe> enabledProbes = scriptProbe.Protocol.Probes.Where(p => p.Enabled).ToList();
            foreach (Probe enabledProbe in enabledProbes)
                probePicker.Items.Add(enabledProbe.GetType().FullName);

            contentLayout.Children.Add(probePicker);

            StackLayout triggerLayout = new StackLayout
            {
                Orientation = StackOrientation.Vertical,
                VerticalOptions = LayoutOptions.FillAndExpand
            };

            probePicker.SelectedIndexChanged += (o, e) =>
                {
                    triggerLayout.Children.Clear();

                    Datum datum = enabledProbes.Where(p => p.GetType().FullName == probePicker.Items[probePicker.SelectedIndex]).First().DatumType;

                    Label propertyLabel = new Label
                    {
                        Text = "Property:",
                        Font = Font.SystemFontOfSize(20)
                    };

                    Picker propertyPicker = new Picker();
                    

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
