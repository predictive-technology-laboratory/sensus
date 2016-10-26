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
using System.Threading;
using System.Reflection;
using System.Collections.Generic;
using Sensus.UI.UiProperties;
using Sensus.Anonymization;
using Sensus.Anonymization.Anonymizers;
using Syncfusion.SfChart.XForms;

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

        protected override void Initialize()
        {
            base.Initialize();

            if (GetRequiredPermissionNames().Length == 0)
                throw new NotSupportedException("No Facebook permissions requested. Will not start Facebook probe.");
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
                    requiredFacebookPermissions.Add(permission);
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
                if (grantedPermissions.Contains(permission.Name))
                {
                    if (permission.Edge == null)
                        userFields.Add(permission.Field);
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

            List<Tuple<string, List<string>>> edgeFieldQueries = new List<Tuple<string, List<string>>>();

            userFields.RemoveAll(s => string.IsNullOrWhiteSpace(s));
            if (userFields.Count > 0)
                edgeFieldQueries.Add(new Tuple<string, List<string>>(null, userFields.Distinct().ToList()));

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
            return null;
        }

        protected override ChartDataPoint GetChartDataPointFromDatum(Datum datum)
        {
            return null;
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