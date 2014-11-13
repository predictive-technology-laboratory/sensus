using Sensus.DataStores;
using Sensus.DataStores.Local;
using Sensus.DataStores.Remote;
using Sensus.UI.Properties;
using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms;

namespace Sensus.UI
{
    public class DataStorePage : ContentPage
    {
        public DataStorePage(DataStore dataStore, Protocol protocol, bool local)
        {
            BindingContext = dataStore;

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
            nameEntry.BindingContext = dataStore;
            nameEntry.SetBinding(Entry.TextProperty, "Name");

            stacks.Add(new StackLayout
            {
                HorizontalOptions = LayoutOptions.StartAndExpand,
                Orientation = StackOrientation.Horizontal,
                Children = { nameLabel, nameEntry }
            });
            #endregion

            stacks.AddRange(UiProperty.GetPropertyStacks(dataStore));

            #region cancel / okay
            Button cancelButton = new Button
            {
                Text = "Cancel"
            };

            cancelButton.Clicked += async (o, e) =>
                {
                    await Navigation.PopAsync();
                };

            Button okayButton = new Button
            {
                Text = "OK"
            };

            okayButton.Clicked += async (o, e) =>
                {
                    if (local)
                        protocol.LocalDataStore = dataStore as LocalDataStore;
                    else
                        protocol.RemoteDataStore = dataStore as RemoteDataStore;

                    await Navigation.PopAsync();  // back to data stores page

                    MessagingCenter.Send<DataStorePage, object>(this, "NewLocalDataStore", null);
                };

            stacks.Add(new StackLayout
            {
                HorizontalOptions = LayoutOptions.FillAndExpand,
                Orientation = StackOrientation.Horizontal,
                Children = { cancelButton, okayButton }
            });
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
