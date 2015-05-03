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
using System.Reflection;
using SensusUI.UiProperties;
using System.Threading;
using SensusService.Anonymization;
using SensusService.Anonymization.Anonymizers;

namespace SensusService.Probes.Apps
{
    public abstract class FacebookProbe : PollingProbe
    {                     
        public sealed override Type DatumType
        {
            get
            {
                return typeof(FacebookDatum);
            }
        }

        protected override string DefaultDisplayName
        {
            get
            {
                return "Facebook";
            }
        }

        public override int DefaultPollingSleepDurationMS
        {
            get
            {
                return 60000;
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
                    requiredFacebookPermissions.Add(permission);
            }

            return requiredFacebookPermissions;
        }

        public ICollection<string> GetRequiredPermissionNames()
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
                        userFields.AddRange(permission.Fields);
                    else
                    {
                        List<string> fields;
                        if (!edgeFields.TryGetValue(permission.Edge, out fields))
                        {
                            fields = new List<string>();
                            edgeFields.Add(permission.Edge, fields);
                        }

                        fields.AddRange(permission.Fields);
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
                if (fields.Count > 0)
                    edgeFieldQueries.Add(new Tuple<string, List<string>>(edge, fields.Distinct().ToList()));
            }

            return edgeFieldQueries;
        }

        protected abstract ICollection<string> GetGrantedPermissions();
    }
}