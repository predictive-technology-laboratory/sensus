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
        public static event EventHandler CancelTapped;
        public static event EventHandler OkTapped;

        public DataStorePage(DataStore dataStore, Protocol protocol, bool local)
        {
            BindingContext = dataStore;

            SetBinding(TitleProperty, new Binding("Name"));

            List<StackLayout> stacks = UiProperty.GetPropertyStacks(dataStore);

            #region cancel / okay
            Button cancelButton = new Button
            {
                Text = "Cancel"
            };

            cancelButton.Clicked += (o, e) =>
                {
                    CancelTapped(o, e);
                };

            Button okayButton = new Button
            {
                Text = "OK"
            };

            okayButton.Clicked += (o, e) =>
                {
                    if (local)
                        protocol.LocalDataStore = dataStore as LocalDataStore;
                    else
                        protocol.RemoteDataStore = dataStore as RemoteDataStore;

                    OkTapped(o, e);
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
