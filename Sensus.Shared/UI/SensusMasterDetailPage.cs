//Copyright 2014 The Rector & Visitors of the University of Virginia
//
//Permission is hereby granted, free of charge, to any person obtaining a copy 
//of this software and associated documentation files (the "Software"), to deal 
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell 
//copies of the Software, and to permit persons to whom the Software is 
//furnished to do so, subject to the following conditions:
//
//The above copyright notice and this permission notice shall be included in 
//all copies or substantial portions of the Software.
//
//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
//INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A 
//PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT 
//HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION 
//OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE 
//SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System;
using Xamarin.Forms;

namespace Sensus.UI
{
    public class SensusMasterDetailPage : MasterDetailPage
    {
        private SensusMasterPage _masterPage;

        public SensusMasterDetailPage()
        {
            _masterPage = new SensusMasterPage();
            _masterPage.MasterPageItemsListView.ItemSelected += OnItemSelected;

            Master = _masterPage;

            // the SensusServiceHelper is not yet loaded when this page is constructed. as a result, we cannot assign the 
            // ProtocolsPage to the Detail property. instead, just assign a blank content page and show the user the master
            // detail list. by the time the user selects from the list, the service helper will be available and the protocols
            // page will be ready to go.
            Detail = new NavigationPage(new ContentPage
            {
                Content = new Label
                {
                    Text = "Welcome to Sensus." + Environment.NewLine + "Please select from the menu above.",
                    FontSize = 30,
                    HorizontalOptions = LayoutOptions.CenterAndExpand,
                    VerticalOptions = LayoutOptions.CenterAndExpand,
                    VerticalTextAlignment = TextAlignment.Center,
                    HorizontalTextAlignment = TextAlignment.Center
                }
            });

            IsPresented = true;
        }

        private void OnItemSelected(object sender, SelectedItemChangedEventArgs e)
        {
            SensusDetailPageItem selectedDetailPageItem = e.SelectedItem as SensusDetailPageItem;

            if (selectedDetailPageItem != null)
            {
                if (selectedDetailPageItem.TargetType == null)
                {
                    selectedDetailPageItem.Action?.Invoke();
                }
                else
                {
                    Detail = new NavigationPage((Page)Activator.CreateInstance(selectedDetailPageItem.TargetType));
                    IsPresented = false;
                }

                _masterPage.MasterPageItemsListView.SelectedItem = null;
            }
        }
    }
}
