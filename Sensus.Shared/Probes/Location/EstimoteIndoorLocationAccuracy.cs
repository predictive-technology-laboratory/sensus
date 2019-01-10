﻿// Copyright 2014 The Rector & Visitors of the University of Virginia
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

namespace Sensus.Probes.Location
{
    /// <summary>
    /// Estimote indoor location accuracy. Taken from [this page](https://developer.estimote.com/indoor/ios-tutorial/#start-location-updates).
    /// </summary>
    public enum EstimoteIndoorLocationAccuracy
    {
        /// <summary>
        /// Unknown accuracy
        /// </summary>
        Unknown,

        /// <summary>
        /// Very low accuracy
        /// </summary>
        VeryLow,

        /// <summary>
        /// Accurate within +/- 4.24 meters
        /// </summary>
        Low,

        /// <summary>
        /// Accurate within +/- 2.62 meters
        /// </summary>
        Medium,

        /// <summary>
        /// Accurate within +/- 1.62 meters
        /// </summary>
        High,

        /// <summary>
        /// Accurate within +/- 1.00 meter
        /// </summary>
        VeryHigh
    }
}