//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using SensusService.Probes.User.Health;
using HealthKit;
using Xamarin.Forms.Platform.iOS;
using SensusService;
using Newtonsoft.Json;

namespace Sensus.iOS.Probes.User.Health
{
    public class iOSHealthKitBodyFatPercentProbe : iOSHealthKitSamplingProbe
    {
        protected override string DefaultDisplayName
        {
            get
            {
                return "Body Fat (HealthKit)";
            }
        }

        public override Type DatumType
        {
            get
            {
                return typeof(BodyFatPercentDatum);
            }
        }

        public override int DefaultPollingSleepDurationMS
        {
            get
            {
                return int.MaxValue;
            }
        }

        public iOSHealthKitBodyFatPercentProbe()
            : base(HKObjectType.GetQuantityType(HKQuantityTypeIdentifierKey.BodyFatPercentage))
        {
        }

        protected override Datum ConvertSampleToDatum(HKSample sample)
        {
            HKQuantitySample quantitySample = sample as HKQuantitySample;

            if (quantitySample == null)
                return null;
            else
                return new HeightDatum(new DateTimeOffset(quantitySample.StartDate.ToDateTime()), quantitySample.Quantity.GetDoubleValue(HKUnit.Percent));
        }
    }
}

