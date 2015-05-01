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

namespace SensusService.Probes.Apps
{
    public abstract class FacebookProbe : PollingProbe
    {             
        // below are the various permission settings and the fields/edges that they provide access to. the
        // list is taken from https://developers.facebook.com/docs/facebook-login/permissions/v2.3#reference

        // user fields
        [OnOffUiProperty("Public Profile:", true, 20)]
        [FacebookPermission("public_profile", new string[0], new string[] { "id", "name", "first_name", "last_name", "age_range", "link", "gender", "locale", "timezone", "updated_time", "verified" })]
        public bool PublicProfile { get; set; }

        [OnOffUiProperty(null, true, 20)]
        [FacebookPermission("email", new string[0], "email" )]
        public bool Email { get; set; }

        [OnOffUiProperty("Bio:", true, 20)]
        [FacebookPermission("user_about_me", new string[0], "bio")]
        public bool AboutMe { get; set; }

        [OnOffUiProperty(null, true, 20)]
        [FacebookPermission("user_education_history", new string[0], "education")]
        public bool Education { get; set; }

        [OnOffUiProperty(null, true, 20)]
        [FacebookPermission("user_hometown", new string[0], "hometown")]
        public bool Hometown { get; set; }

        [OnOffUiProperty(null, true, 20)]
        [FacebookPermission("user_location", new string[0], "location")]
        public bool Location { get; set; }

        [OnOffUiProperty(null, true, 20)]
        [FacebookPermission("user_relationships", new string[0], new string[] { "relationship_status", "significant_other" })]
        public bool Relationships { get; set; }

        [OnOffUiProperty(null, true, 20)]
        [FacebookPermission("user_religion_politics", new string[0], new string[] { "religion" })]
        public bool Religion { get; set; }

        [OnOffUiProperty(null, true, 20)]
        [FacebookPermission("user_religion_politics", new string[0], new string[] { "political" })]
        public bool Politics { get; set; }

        [OnOffUiProperty(null, true, 20)]
        [FacebookPermission("user_website", new string[0], "website")]
        public bool Website { get; set; }

        [OnOffUiProperty(null, true, 20)]
        [FacebookPermission("user_work_history", new string[0], "work")]
        public bool Employment { get; set; }

        // user edges
        [OnOffUiProperty(null, true, 20)]
        [FacebookPermission("user_friends", "friends", new string[0])]
        public bool Friends { get; set; }

        [OnOffUiProperty(null, true, 20)]
        [FacebookPermission("user_actions.books", "book.reads", new string[0])]
        public bool Books { get; set; }

        [OnOffUiProperty(null, true, 20)]
        [FacebookPermission("user_actions.fitness", new string[] { "fitness.runs", "fitness.walks", "fitness.bikes" }, new string[0])]
        public bool Fitness { get; set; }

        [OnOffUiProperty(null, true, 20)]
        [FacebookPermission("user_actions.music", new string[] { "music.listens", "music.playlists" }, new string[0])]
        public bool Music { get; set; }

        [OnOffUiProperty(null, true, 20)]
        [FacebookPermission("user_actions.news", new string[] { "news.reads", "news.publishes" }, new string[0])]
        public bool News { get; set; }

        [OnOffUiProperty("Video Viewing", true, 20)]
        [FacebookPermission("user_actions.video", new string[] { "video.watches", "video.rates", "video.wants_to_watch" }, new string[0])]
        public bool VideoActions { get; set; }

        [OnOffUiProperty(null, true, 20)]
        [FacebookPermission("user_events", "events", new string[0])]
        public bool Events { get; set; }

        [OnOffUiProperty(null, true, 20)]
        [FacebookPermission("user_games_activity", "games", new string[0])]
        public bool Games { get; set; }

        [OnOffUiProperty(null, true, 20)]
        [FacebookPermission("user_groups", "groups", new string[0])]
        public bool Groups { get; set; }

        [OnOffUiProperty(null, true, 20)]
        [FacebookPermission("user_likes", "likes", new string[0])]
        public bool Likes { get; set; }

        [OnOffUiProperty(null, true, 20)]
        [FacebookPermission("user_photos", "photos", new string[0])]
        public bool Photos { get; set; }

        [OnOffUiProperty(null, true, 20)]
        [FacebookPermission("user_posts", "photos", new string[0])]
        public bool Posts { get; set; }

        [OnOffUiProperty(null, true, 20)]
        [FacebookPermission("user_status", "statuses", new string[0])]
        public bool Status { get; set; }

        [OnOffUiProperty(null, true, 20)]
        [FacebookPermission("user_tagged_places", "tagged_places", new string[0])]
        public bool Places { get; set; }

        [OnOffUiProperty(null, true, 20)]
        [FacebookPermission("user_videos", "videos", new string[0])]
        public bool Videos { get; set; }

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

        public ICollection<FacebookPermission> GetEnabledFacebookPermissions()
        {
            List<FacebookPermission> enabledFacebookPermissions = new List<FacebookPermission>();

            foreach (PropertyInfo property in GetType().GetProperties())
            {
                FacebookPermission permissionAttribute = property.GetCustomAttribute<FacebookPermission>();
                if (permissionAttribute != null && (bool)property.GetValue(this))
                    enabledFacebookPermissions.Add(permissionAttribute);
            }

            return enabledFacebookPermissions;
        }

        public ICollection<string> GetEnabledPermissionNames()
        {
            return GetEnabledFacebookPermissions().Select(permission => permission.Name).Distinct().ToArray();
        }   

        public List<Tuple<string, List<string>>> GetEdgeFieldQueries()
        {
            List<string> userFields = new List<string>();
            Dictionary<string, List<string>> edgeFields = new Dictionary<string, List<string>>();

            ICollection<string> grantedPermissions = GetGrantedPermissions();

            // get query for all enabled permissions that have been granted
            foreach (FacebookPermission enabledPermission in GetEnabledFacebookPermissions())
                if (grantedPermissions.Contains(enabledPermission.Name))
                {
                    if (enabledPermission.Edges.Length == 0)
                        userFields.AddRange(enabledPermission.Fields);
                    else
                        foreach (string edge in enabledPermission.Edges)
                        {
                            List<string> fields;
                            if (!edgeFields.TryGetValue(edge, out fields))
                            {
                                fields = new List<string>();
                                edgeFields.Add(edge, fields);
                            }

                            fields.AddRange(enabledPermission.Fields);
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