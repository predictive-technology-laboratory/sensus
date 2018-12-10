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
using Sensus.Probes;
using System.Linq;

namespace Sensus.UI
{
    public class ParticipationReportDetailsPage : ContentPage
    {
        public ParticipationReportDetailsPage(Protocol protocol)
        {
            Title = "Participation Details";

            StackLayout contentLayout = new StackLayout
            {
                Orientation = StackOrientation.Vertical,
                VerticalOptions = LayoutOptions.StartAndExpand
            };

            foreach (Probe probe in protocol.Probes.Where(probe => probe.GetParticipation() != null).OrderBy(probe => probe.GetParticipation()))
            {
                double participation = probe.GetParticipation().GetValueOrDefault();

                contentLayout.Children.Add(new StackLayout
                {
                    Orientation = StackOrientation.Vertical,
                    VerticalOptions = LayoutOptions.FillAndExpand,
                    Children =
                    {
                        new Label
                        {
                            Text = probe.DisplayName + ":  " + Math.Round(participation * 100, 0) + "%",
                            FontSize = 20
                        },
                        new ProgressBar
                        {
                            Progress = participation
                        }
                    }
                });
            }

            Content = new ScrollView
            {
                Content = contentLayout
            };
        }
    }
}
