// Copyright 2014 The Rector & Visitors of the University of Virginia
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

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
            Detail = new NavigationPage(new ContentPage());  
            IsPresented = true;
        }

        private void OnItemSelected(object sender, SelectedItemChangedEventArgs e)
        {
            SensusDetailPageItem selectedDetailPageItem = e.SelectedItem as SensusDetailPageItem;

            if (selectedDetailPageItem != null)
            {
                Detail = new NavigationPage((Page)Activator.CreateInstance(selectedDetailPageItem.TargetType));
                _masterPage.MasterPageItemsListView.SelectedItem = null;
                IsPresented = false;
            }
        }
    }
}