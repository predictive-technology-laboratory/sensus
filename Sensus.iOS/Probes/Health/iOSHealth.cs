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
using System.Linq;
using System.Collections.Generic;
using UIKit;
using Xamarin.Forms.Platform.iOS;
using Xamarin.Forms;
using SensusUI;
using SensusService;
using System.IO;
using SensusService.Probes;
using SensusService.Probes.Health;
using HealthKit;
using Foundation;
using System.Threading;
using System.Reflection;
namespace Sensus.iOS.Probes.Health
{
    public class iOSHealth : HealthProbe
    {
        private HKHealthStore healthKitStore = new HKHealthStore ();

        NSNumberFormatter numberFormatter;

        List<HKObjectType> HealthList = new List<HKObjectType>();

        HKObjectType[] myarray;

        bool flag= false;


        NSSet DataTypesToWrite {
            get {
                return NSSet.MakeNSObjectSet <HKObjectType> (new HKObjectType[] {

                });
            }
        }


        NSSet DataTypesToRead
        {

            get
            {
                Type t = typeof(HealthDatum);
                Console.WriteLine(t.Name);
                foreach (var prop in t.GetProperties()) //Gives us  the list of properties 
                {
                    //In order to recognize which ones are the health related properties
                    if (prop.Name == "Age" || prop.Name == "Height" || prop.Name == "Weight")
                    {
                        
                        if (Protocol.JsonAnonymizer.GetAnonymizer(prop).DisplayText != "Omit" && prop.Name == "Height")
                        {
                            HealthList.Add(HKQuantityType.GetQuantityType(HKQuantityTypeIdentifierKey.Height));
                        }

                        else if(Protocol.JsonAnonymizer.GetAnonymizer(prop).DisplayText != "Omit" && prop.Name == "Weight")
                        {
                            HealthList.Add(HKQuantityType.GetQuantityType(HKQuantityTypeIdentifierKey.BodyMass));
                        }
                        else if(Protocol.JsonAnonymizer.GetAnonymizer(prop).DisplayText != "Omit" && prop.Name == "Age")
                        {
                            HealthList.Add(HKCharacteristicType.GetCharacteristicType(HKCharacteristicTypeIdentifierKey.DateOfBirth));
                        }
                    }


                }
                //THe NSSet.MakeNSObjectSet takes only arrays
                myarray = HealthList.ToArray();

                return NSSet.MakeNSObjectSet <HKObjectType>(myarray);


            }
        }


        protected override void Initialize()
        {
            base.Initialize();

            //identify which fields the user has chosen only to request the authorization


            Xamarin.Forms.Device.BeginInvokeOnMainThread(() =>
                {
                    if (HKHealthStore.IsHealthDataAvailable)
                    {

                        healthKitStore.RequestAuthorizationToShare(DataTypesToWrite, DataTypesToRead, (success, error) =>
                            {
                                if (success == true)
                                {
                                    numberFormatter = new NSNumberFormatter();

                                    flag = true;

                                }


                            });
                    }
                });
        }

        private double UpdateUsersAge ()
        {
            NSError error;

            NSDate dateOfBirth = healthKitStore.GetDateOfBirth(out error);

            if (error != null) {
                return 0.0;
            }

            if (dateOfBirth == null)
                return 0.0;

            var now = NSDate.Now;

            NSDateComponents ageComponents = NSCalendar.CurrentCalendar.Components (NSCalendarUnit.Year, dateOfBirth, now,
                NSCalendarOptions.WrapCalendarComponents);

            nint usersAge = ageComponents.Year;
            return usersAge;
        }



        private double UpdateUsersHeight ()
        {
            var heightType = HKQuantityType.GetQuantityType (HKQuantityTypeIdentifierKey.Height);
            double usersHeight=0.0;
            FetchMostRecentData (heightType, (mostRecentQuantity, error) => {
                if (error != null) {
                    Console.WriteLine(usersHeight);
                    return ;
                }

               

                if (mostRecentQuantity != null) {
                    var heightUnit = HKUnit.Inch;
                    usersHeight = mostRecentQuantity.GetDoubleValue (heightUnit);
                }

                Console.WriteLine(usersHeight);

            });
            return usersHeight;
        }

        private double UpdateUsersWeight ()
        {
            var weightType = HKQuantityType.GetQuantityType (HKQuantityTypeIdentifierKey.BodyMass);
            double usersWeight = 0.0;
            FetchMostRecentData (weightType, (mostRecentQuantity, error) => {
                if (error != null) {
               
                    return;
                }

       

                if (mostRecentQuantity != null) {
                    var weightUnit = HKUnit.Pound;
                    usersWeight = mostRecentQuantity.GetDoubleValue (weightUnit);
                }


//                Console.WriteLine (new NSNumber (usersWeight));

            }
            );
            return usersWeight;
        }




        void FetchMostRecentData (HKQuantityType quantityType, Action <HKQuantity, NSError> completion)
        {
            var timeSortDescriptor = new NSSortDescriptor (HKSample.SortIdentifierEndDate, false);
            var query = new HKSampleQuery (quantityType, null, 1, new NSSortDescriptor[] { timeSortDescriptor },
                (HKSampleQuery resultQuery, HKSample[] results, NSError error) => {
                if (completion != null && error != null) {
                    completion (null, error);
                    return;
                }

                HKQuantity quantity = null;
                if (results.Length != 0) {
                    var quantitySample = (HKQuantitySample)results [results.Length - 1];
                    quantity = quantitySample.Quantity;
                }

                if (completion != null)
                    completion (quantity, error);
            });

            healthKitStore.ExecuteQuery(query);
        }

        protected override IEnumerable<Datum> Poll(CancellationToken cancellationToken)
        {
            if (flag == true)
            {
                //returns all the datatypes, the data types for which permissions are not granted, return a null value
                return new Datum[] { new HealthDatum(DateTimeOffset.UtcNow, UpdateUsersHeight(), UpdateUsersAge(), UpdateUsersWeight()) }; 

            }
            else
                return new Datum[]{};
        }



    }
}
