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

using HealthKit;
using Foundation;
using System;
using System.Threading;
using System.Collections.Generic;
using Sensus;
using Sensus.Probes.User.Health;
using Syncfusion.SfChart.XForms;
using System.Threading.Tasks;

namespace Sensus.iOS.Probes.User.Health
{
    public class iOSHealthKitFitzpatrickSkinTypeProbe : iOSHealthKitProbe
    {
        public sealed override string DisplayName
        {
            get
            {
                return "HealthKit Fitzpatrick Skin Type";
            }
        }

        public override Type DatumType
        {
            get
            {
                return typeof(FitzpatrickSkinTypeDatum);
            }
        }

        public override int DefaultPollingSleepDurationMS
        {
            get
            {
                return int.MaxValue;
            }
        }

        public iOSHealthKitFitzpatrickSkinTypeProbe()
            : base(HKCharacteristicType.Create(HKCharacteristicTypeIdentifier.FitzpatrickSkinType))
        {
        }

        protected override Task<List<Datum>> PollAsync(CancellationToken cancellationToken)
        {
            List<Datum> data = new List<Datum>();

            NSError error;
            HKFitzpatrickSkinTypeObject skinType = HealthStore.GetFitzpatrickSkinType(out error);

            if (error == null)
            {
                if (skinType.SkinType == HKFitzpatrickSkinType.I)
                {
                    data.Add(new FitzpatrickSkinTypeDatum(DateTimeOffset.Now, FitzpatrickSkinType.TypeI));
                }
                else if (skinType.SkinType == HKFitzpatrickSkinType.II)
                {
                    data.Add(new FitzpatrickSkinTypeDatum(DateTimeOffset.Now, FitzpatrickSkinType.TypeII));
                }
                else if (skinType.SkinType == HKFitzpatrickSkinType.III)
                {
                    data.Add(new FitzpatrickSkinTypeDatum(DateTimeOffset.Now, FitzpatrickSkinType.TypeIII));
                }
                else if (skinType.SkinType == HKFitzpatrickSkinType.IV)
                {
                    data.Add(new FitzpatrickSkinTypeDatum(DateTimeOffset.Now, FitzpatrickSkinType.TypeIV));
                }
                else if (skinType.SkinType == HKFitzpatrickSkinType.V)
                {
                    data.Add(new FitzpatrickSkinTypeDatum(DateTimeOffset.Now, FitzpatrickSkinType.TypeV));
                }
                else if (skinType.SkinType == HKFitzpatrickSkinType.VI)
                {
                    data.Add(new FitzpatrickSkinTypeDatum(DateTimeOffset.Now, FitzpatrickSkinType.TypeVI));
                }
                else if (skinType.SkinType == HKFitzpatrickSkinType.NotSet)
                {
                    data.Add(new FitzpatrickSkinTypeDatum(DateTimeOffset.Now, FitzpatrickSkinType.NotSet));
                }
                else
                {
                    throw new Exception("User has not provided -- or has not allowed access to -- their fitzpatrick skin type.");
                }
            }
            else
            {
                throw new Exception("Error reading Fitzpatrick skin type:  " + error.Description);
            }

            return Task.FromResult(data);
        }

        protected override ChartSeries GetChartSeries()
        {
            throw new NotImplementedException();
        }

        protected override ChartDataPoint GetChartDataPointFromDatum(Datum datum)
        {
            throw new NotImplementedException();
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
