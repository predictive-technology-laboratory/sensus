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
using System.Collections.ObjectModel;
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

        public static void Shuffle<T>(this Random random, Collection<T> array)
        {
            int indexToReplace = array.Count;

            while (indexToReplace > 1)
            {
                int replacementIndex = random.Next(indexToReplace--);

                T temp = array[indexToReplace];

                array[indexToReplace] = array[replacementIndex];

                array[replacementIndex] = temp;
            }
        }
    }
}
