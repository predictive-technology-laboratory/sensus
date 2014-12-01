﻿using Microsoft.WindowsAzure.MobileServices;
using Sensus.Exceptions;
using Sensus.Probes.Location;
using Sensus.UI.Properties;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sensus.DataStores.Remote
{
    public class AzureRemoteDataStore : RemoteDataStore
    {
        private MobileServiceClient _client;
        private string _url;
        private string _key;

        private IMobileServiceTable<AltitudeDatum> _altitudeTable;
        private IMobileServiceTable<CompassDatum> _compassTable;
        private IMobileServiceTable<LocationDatum> _locationTable;

        [StringUiProperty("URL:", true)]
        public string URL
        {
            get { return _url; }
            set
            {
                if (!value.Equals(_url, StringComparison.Ordinal))
                {
                    _url = value;
                    OnPropertyChanged();
                }
            }
        }

        [StringUiProperty("Key:", true)]
        public string Key
        {
            get { return _key; }
            set
            {
                if (!value.Equals(_key, StringComparison.Ordinal))
                {
                    _key = value;
                    OnPropertyChanged();
                }
            }
        }

        protected override string DisplayName
        {
            get { return "Azure"; }
        }        

        public override Task StartAsync()
        {
            return Task.Run(async () =>
                {
                    _client = new MobileServiceClient(_url, _key);

                    _altitudeTable = _client.GetTable<AltitudeDatum>();
                    _compassTable = _client.GetTable<CompassDatum>();
                    _locationTable = _client.GetTable<LocationDatum>();

                    await base.StartAsync();
                });
        }

        protected override Task<ICollection<Datum>> CommitData(ICollection<Datum> data)
        {
            return Task.Run<ICollection<Datum>>(async () =>
                {
                    List<Datum> committedData = new List<Datum>();

                    DateTime start = DateTime.Now;

                    foreach (Datum datum in data)
                    {
                        try
                        {
                            if (datum is AltitudeDatum)
                                await _altitudeTable.InsertAsync(datum as AltitudeDatum);
                            else if (datum is CompassDatum)
                                await _compassTable.InsertAsync(datum as CompassDatum);
                            else if (datum is LocationDatum)
                                await _locationTable.InsertAsync(datum as LocationDatum);
                            else
                                throw new DataStoreException("Unrecognized Azure table:  " + datum.GetType().FullName);

                            committedData.Add(datum);
                        }
                        catch (Exception ex) { if (App.LoggingLevel >= LoggingLevel.Normal) App.Get().SensusService.Log("Failed to insert datum into Azure table:  " + ex.Message); }
                    }

                    if (App.LoggingLevel >= LoggingLevel.Verbose)
                        App.Get().SensusService.Log("Committed " + committedData.Count + " data items to Azure tables in " + (DateTime.Now - start).TotalSeconds + " seconds.");

                    return committedData;
                });
        }

        public override Task StopAsync()
        {
            return Task.Run(async () =>
                {
                    await base.StopAsync();

                    _client.Dispose();
                });
        }

        public override void Clear()
        {
            // TODO:  Implement
        }
    }
}
