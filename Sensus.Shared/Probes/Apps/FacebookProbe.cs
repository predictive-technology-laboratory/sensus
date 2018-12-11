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
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using Sensus.Anonymization.Anonymizers;
using Syncfusion.SfChart.XForms;
using System.Threading.Tasks;

namespace Sensus.Probes.Apps
{
    /// <summary>
    /// Use the following command to generate the debug keyhash:
    /// 
    /// keytool -exportcert -keystore /Users/[username]/.local/share/Xamarin/Mono\ for\ Android/debug.keystore -alias androiddebugkey | openssl sha1 -binary | openssl base64
    /// 
    /// </summary>
    public abstract class FacebookProbe : PollingProbe
    {
        private readonly object _loginLocker = new object();

        protected object LoginLocker
        {
            get { return _loginLocker; }
        }

        public sealed override Type DatumType
        {
            get
            {
                return typeof(FacebookDatum);
            }
        }

        public sealed override string DisplayName
        {
            get
            {
                return "Facebook Profile";
            }
        }

        public override int DefaultPollingSleepDurationMS
        {
            get
            {
                return 60000 * 60 * 24; // once per day
            }
        }

        protected override async Task InitializeAsync()
        {
            await base.InitializeAsync();

            if (GetRequiredPermissionNames().Length == 0)
            {
                throw new NotSupportedException("No Facebook permissions requested. Will not start Facebook probe.");
            }
        }

        /// <summary>
        /// Gets the required Facebook permissions, as determined by the Facebook probe's configuration and
        /// anonymization setup.
        /// </summary>
        /// <returns>The required Facebook permissions.</returns>
        public ICollection<FacebookPermission> GetRequiredFacebookPermissions()
        {
            List<FacebookPermission> requiredFacebookPermissions = new List<FacebookPermission>();

            foreach (PropertyInfo facebookDatumProperty in typeof(FacebookDatum).GetProperties())
            {
                FacebookPermission permission = facebookDatumProperty.GetCustomAttribute<FacebookPermission>();
                Anonymizer propertyAnonymizer = Protocol.JsonAnonymizer.GetAnonymizer(facebookDatumProperty);
                if (permission != null && (propertyAnonymizer == null || !(propertyAnonymizer is ValueOmittingAnonymizer)))
                {
                    requiredFacebookPermissions.Add(permission);
                }
            }

            return requiredFacebookPermissions;
        }

        public string[] GetRequiredPermissionNames()
        {
            return GetRequiredFacebookPermissions().Select(permission => permission.Name).Distinct().ToArray();
        }

        public List<Tuple<string, List<string>>> GetEdgeFieldQueries()
        {
            List<string> userFields = new List<string>();
            Dictionary<string, List<string>> edgeFields = new Dictionary<string, List<string>>();

            ICollection<string> grantedPermissions = GetGrantedPermissions();

            // get query for all required permissions that have been granted
            foreach (FacebookPermission permission in GetRequiredFacebookPermissions())
            {
                if (grantedPermissions.Contains(permission.Name))
                {
                    if (permission.Edge == null)
                    {
                        userFields.Add(permission.Field);
                    }
                    else
                    {
                        List<string> fields;
                        if (!edgeFields.TryGetValue(permission.Edge, out fields))
                        {
                            fields = new List<string>();
                            edgeFields.Add(permission.Edge, fields);
                        }

                        fields.Add(permission.Field);
                    }
                }
            }

            List<Tuple<string, List<string>>> edgeFieldQueries = new List<Tuple<string, List<string>>>();

            userFields.RemoveAll(s => string.IsNullOrWhiteSpace(s));
            if (userFields.Count > 0)
            {
                edgeFieldQueries.Add(new Tuple<string, List<string>>(null, userFields.Distinct().ToList()));
            }

            foreach (string edge in edgeFields.Keys)
            {
                List<string> fields = edgeFields[edge];
                fields.RemoveAll(s => string.IsNullOrWhiteSpace(s));
                edgeFieldQueries.Add(new Tuple<string, List<string>>(edge, fields.Distinct().ToList()));
            }

            return edgeFieldQueries;
        }

        protected abstract ICollection<string> GetGrantedPermissions();

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
