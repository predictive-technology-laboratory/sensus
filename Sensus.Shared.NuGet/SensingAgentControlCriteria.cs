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
using System.Linq;
using Sensus.Probes.Location;
using Sensus.Probes.Movement;

namespace Sensus
{
    public partial class SensingAgent
    {
        /// <summary>
        /// Checks whether device is near a surface, according to the most recent <see cref="IProximityDatum"/>.
        /// </summary>
        /// <returns><c>true</c>, if surface was neared, <c>false</c> otherwise.</returns>
        protected bool IsNearSurface()
        {
            lock (_typeData)
            {
                bool isNearSurface = false;

                List<IDatum> proximityData = GetObservedData<IProximityDatum>();

                if (proximityData.Count > 0)
                {
                    IProximityDatum mostRecentProximityDatum = proximityData.Last() as IProximityDatum;
                    isNearSurface = mostRecentProximityDatum.Distance < mostRecentProximityDatum.MaxDistance;
                }

                return isNearSurface;
            }
        }

        /// <summary>
        /// Checks whether the acceleration magnitude exceeds a threshold.
        /// </summary>
        /// <returns><c>true</c>, if average linear magnitude exceeds threshold, <c>false</c> otherwise.</returns>
        /// <param name="threshold">Threshold.</param>
        protected bool AverageLinearAccelerationMagnitudeExceedsThreshold(double threshold)
        {
            lock (_typeData)
            {
                bool almExceedsThreshold = false;

                List<IDatum> accelerometerData = GetObservedData<IAccelerometerDatum>();

                if (accelerometerData.Count > 0)
                {
                    double averageLinearMagnitude = accelerometerData.Cast<IAccelerometerDatum>()
                                                                     .Average(accelerometerDatum => Math.Sqrt(Math.Pow(accelerometerDatum.X, 2) +
                                                                                                              Math.Pow(accelerometerDatum.Y, 2) +
                                                                                                              Math.Pow(accelerometerDatum.Z, 2)));

                    // acceleration values include gravity. thus, a stationary device lying flat will register 1 
                    // on one of the axes. use absolute deviation from 1 as the criterion value with which to 
                    // compare the threshold.
                    almExceedsThreshold = Math.Abs(averageLinearMagnitude - 1) > threshold;
                }

                return almExceedsThreshold;
            }
        }
    }
}