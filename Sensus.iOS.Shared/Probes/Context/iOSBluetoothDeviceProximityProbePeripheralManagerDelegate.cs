﻿//Copyright 2014 The Rector & Visitors of the University of Virginia
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
using CoreBluetooth;
using Foundation;

namespace Sensus.iOS.Probes.Context
{
    /// <summary>
    /// iOS BLE peripheral (advertiser/server) delegate class. Receives events related to BLE advertising and  
    /// characteristic reading.
    /// </summary>
    public class iOSBluetoothDeviceProximityProbePeripheralManagerDelegate : CBPeripheralManagerDelegate
    {
        private CBMutableService _service;
        private CBMutableCharacteristic _characteristic;
        private iOSBluetoothDeviceProximityProbe _probe;

        public iOSBluetoothDeviceProximityProbePeripheralManagerDelegate(CBMutableService service, CBMutableCharacteristic characteristic, iOSBluetoothDeviceProximityProbe probe)
        {           
            _service = service;
            _characteristic = characteristic;
            _probe = probe;
        }

        public override void StateUpdated(CBPeripheralManager peripheral)
        {
            if (peripheral.State == CBPeripheralManagerState.PoweredOn && _probe.Running)
            {
                try
                {
                    // if the user powered BLE off/on, the peripheral will already have the service from before (exception will be thrown
                    // on the next line and caught), and the peripheral will already be advertising the service. note that the 
                    // CBPeripheralManager.Advertising property will still show false after the user's off/on setting because we haven't 
                    // called StartAdvertising ourselves:  https://developer.apple.com/documentation/corebluetooth/cbperipheralmanager/1393291-isadvertising
                    peripheral.AddService(_service);
                }
                catch (Exception ex)
                {
                    SensusServiceHelper.Get().Logger.Log("Exception while adding service:  " + ex, LoggingLevel.Normal, GetType());
                }
            }
        }

        public override void ServiceAdded(CBPeripheralManager peripheral, CBService service, NSError error)
        {
            if (error == null)
            {
                SensusServiceHelper.Get().Logger.Log("Added service.", LoggingLevel.Normal, GetType());

                try
                {
                    peripheral.StartAdvertising(new NSDictionary(CBAdvertisement.DataServiceUUIDsKey, NSArray.FromObjects(_service.UUID)));
                }
                catch (Exception ex)
                {
                    SensusServiceHelper.Get().Logger.Log("Exception while starting advertising:  " + ex.Message, LoggingLevel.Normal, GetType());
                }
            }
            else
            {
                SensusServiceHelper.Get().Logger.Log("Error adding service:  " + error, LoggingLevel.Normal, GetType());
            }
        }

        public override void AdvertisingStarted(CBPeripheralManager peripheral, NSError error)
        {
            if (error == null)
            {
                SensusServiceHelper.Get().Logger.Log("Advertising started.", LoggingLevel.Normal, GetType());
            }
            else
            {
                SensusServiceHelper.Get().Logger.Log("Error starting advertising:  " + error, LoggingLevel.Normal, GetType());
            }
        }

        public override void ReadRequestReceived(CBPeripheralManager peripheral, CBATTRequest request)
        {
            try
            {
                if (request.Characteristic.Service.UUID.Equals(_service.UUID) && request.Characteristic.UUID.Equals(_characteristic.UUID))
                {
                    // fill in the characteristic value for the request and return it to the central
                    request.Value = _characteristic.Value;
                    peripheral.RespondToRequest(request, CBATTError.Success);
                }
                else
                {
                    peripheral.RespondToRequest(request, CBATTError.RequestNotSupported);
                }
            }
            catch (Exception ex)
            {
                SensusServiceHelper.Get().Logger.Log("Exception while servicing read request:  " + ex.Message, LoggingLevel.Normal, GetType());
            }
        }

        public override void WillRestoreState(CBPeripheralManager peripheral, NSDictionary dict)
        {
            SensusServiceHelper.Get().Logger.Log("Will restore state.", LoggingLevel.Normal, GetType());
        }

        public override void WriteRequestsReceived(CBPeripheralManager peripheral, CBATTRequest[] requests)
        {          
            SensusServiceHelper.Get().Logger.Log("Write requests received.", LoggingLevel.Normal, GetType());
        }

        public override void ReadyToUpdateSubscribers(CBPeripheralManager peripheral)
        {
            SensusServiceHelper.Get().Logger.Log("Ready to update subscribers.", LoggingLevel.Normal, GetType());
        }

        public override void CharacteristicSubscribed(CBPeripheralManager peripheral, CBCentral central, CBCharacteristic characteristic)
        {
            SensusServiceHelper.Get().Logger.Log("Characteristic subscribed.", LoggingLevel.Normal, GetType());
        }

        public override void CharacteristicUnsubscribed(CBPeripheralManager peripheral, CBCentral central, CBCharacteristic characteristic)
        {
            SensusServiceHelper.Get().Logger.Log("Characteristic unsubscribed.", LoggingLevel.Normal, GetType());
        }
    }
}
