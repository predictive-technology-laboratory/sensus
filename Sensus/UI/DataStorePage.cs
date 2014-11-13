using Sensus.DataStores;
using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms;

namespace Sensus.UI
{
    public class DataStorePage : ContentPage
    {
        public DataStorePage(DataStore dataStore)
        {
            Title = dataStore.Name;
        }
    }
}
