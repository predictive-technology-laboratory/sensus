using Sensus.DataStores;
using Sensus.DataStores.Local;
using Sensus.DataStores.Remote;
using Sensus.UI.Properties;
using System;
using System.Collections.Generic;
using Xamarin.Forms;

namespace Sensus.UI
{
    public class DataStorePage : ContentPage
    {        
        public static event EventHandler OkTapped;

        public DataStorePage(ProtocolDataStoreEventArgs args)
        {
            BindingContext = args.DataStore;

            SetBinding(TitleProperty, new Binding("Name"));

            List<StackLayout> stacks = UiProperty.GetPropertyStacks(args.DataStore);

            Button clearButton = new Button
            {
                Text = "Clear",
                HorizontalOptions = LayoutOptions.Start,
                Font = Font.SystemFontOfSize(20),
                IsEnabled = args.DataStore.CanClear
            };

            clearButton.Clicked += async (o, e) =>
                {
                    if (await DisplayAlert("Clear data from " + args.DataStore.Name + "?", "This action cannot be undone.", "Clear", "Cancel"))
                        args.DataStore.Clear();
                };

            Button okayButton = new Button
            {
                Text = "OK",
                HorizontalOptions = LayoutOptions.FillAndExpand,
                Font = Font.SystemFontOfSize(20)
            };

            okayButton.Clicked += (o, e) =>
                {
                    if (args.Local)
                        args.Protocol.LocalDataStore = args.DataStore as LocalDataStore;
                    else
                        args.Protocol.RemoteDataStore = args.DataStore as RemoteDataStore;

                    OkTapped(o, e);
                };

            stacks.Add(new StackLayout
            {
                Orientation = StackOrientation.Horizontal,
                HorizontalOptions = LayoutOptions.FillAndExpand,
                Children = { clearButton, okayButton }
            });

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
