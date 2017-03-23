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
using Plugin.Geolocator.Abstractions;

namespace Sensus
{
    public static class ExtensionMethods
    {
        public static Position ToGeolocationPosition(this Xamarin.Forms.Maps.Position position)
        {
            return new Position { Latitude = position.Latitude, Longitude = position.Longitude, Timestamp = DateTime.MinValue };
        }

        public static Xamarin.Forms.Maps.Position ToFormsPosition(this Position position)
        {
            return new Xamarin.Forms.Maps.Position(position.Latitude, position.Longitude);
        }
    }
}