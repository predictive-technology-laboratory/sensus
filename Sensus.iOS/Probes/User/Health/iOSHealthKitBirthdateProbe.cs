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
using System.Collections.Generic;
using SensusService;
using System.Threading;
using HealthKit;
using Foundation;
using Xamarin.Forms.Platform.iOS;
using Newtonsoft.Json;
using Syncfusion.SfChart.XForms;

namespace Sensus.iOS.Probes.User.Health
{
    public class iOSHealthKitBirthdateProbe : iOSHealthKitProbe
    {
        public sealed override string DisplayName
        {
            get
            {
                return "HealthKit Birthdate";
            }
        }

        public override Type DatumType
        {
            get
            {
                return typeof(BirthdateDatum);
            }
        }

        public override int DefaultPollingSleepDurationMS
        {
            get
            {
                return int.MaxValue;
            }
        }

        public iOSHealthKitBirthdateProbe()
            : base(HKObjectType.GetCharacteristicType(HKCharacteristicTypeIdentifierKey.DateOfBirth))
        {
        }

        protected override IEnumerable<Datum> Poll(CancellationToken cancellationToken)
        {
            List<Datum> data = new List<Datum>();

            NSError error;
            NSDate dateOfBirth = HealthStore.GetDateOfBirth(out error);

            if (error == null)
            {
                if (dateOfBirth == null)
                    throw new Exception("User has not provided -- or has not allowed access to -- their date of birth.");
                else
                    data.Add(new BirthdateDatum(DateTimeOffset.Now, new DateTimeOffset(dateOfBirth.ToDateTime())));
            }
            else
                throw new Exception("Error reading date of birth:  " + error.Description);
                
            return data;
        }

        protected override ChartSeries GetChartSeries()
        {
            return null;
        }

        protected override ChartDataPoint GetChartDataPointFromDatum(Datum datum)
        {
            return null;
        }

        protected override ChartAxis GetChartPrimaryAxis()
        {
            throw new NotImplementedException();
        }

        protected override RangeAxisBase GetChartSecondaryAxis()
        {
            throw new NotImplementedException();
        }
    }
}