using Microsoft.WindowsAzure.MobileServices;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using System.Reflection;
using Sensus.Probes.Location;

namespace Sensus.DataStores.Remote
{
    public class AzureRemoteDataStore : RemoteDataStore
    {
        private MobileServiceClient _azureClient;
        private const string applicationURL = @"https://ptl-sensus.azure-mobile.net/";
        private const string applicationKey = @"uwCPohtQxDPNZHeRkHJjJXOmxQNscM81";

        protected override string DisplayName
        {
            get { return "Azure"; }
        }        

        public override Task StartAsync()
        {
            return Task.Run(async () =>
                {
                    _azureClient = new MobileServiceClient(applicationURL, applicationKey, null);

                    await base.StartAsync();
                });
        }

        protected override Task<ICollection<Datum>> CommitData(ICollection<Datum> data)
        {
            return Task.Run<ICollection<Datum>>(async () =>
                {
                    List<Datum> committedData = new List<Datum>();

                    foreach (Datum datum in data)
                    {
                        try
                        {
                            IMobileServiceTable<LocationDatum> table = _azureClient.GetTable<LocationDatum>();
                            await table.InsertAsync(datum as LocationDatum);
                            committedData.Add(datum);
                        }
                        catch (Exception ex) { if (App.LoggingLevel >= LoggingLevel.Normal) App.Get().SensusService.Log("Failed to insert datum into Azure table:  " + ex.Message); }
                    }

                    return committedData;
                });
        }
    }
}
