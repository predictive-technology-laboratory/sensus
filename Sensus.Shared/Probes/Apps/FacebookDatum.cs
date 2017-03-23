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
using System.Reflection;
using System.Collections.Generic;
using Sensus.Anonymization;
using Sensus.Anonymization.Anonymizers;

namespace Sensus.Probes.Apps
{
    public class FacebookDatum : Datum
    {
        private static Dictionary<string, PropertyInfo> JSON_FIELD_DATUM_PROPERTY;

        static FacebookDatum()
        {
            JSON_FIELD_DATUM_PROPERTY = new Dictionary<string, PropertyInfo>();

            foreach (PropertyInfo property in typeof(FacebookDatum).GetProperties())
            {
                FacebookPermission permission = property.GetCustomAttribute<FacebookPermission>();
                if (permission != null)
                    JSON_FIELD_DATUM_PROPERTY.Add(permission.Edge ?? permission.Field, property);
            }
        }

        public static bool TryGetProperty(string jsonField, out PropertyInfo property)
        {
            return JSON_FIELD_DATUM_PROPERTY.TryGetValue(jsonField, out property);
        }

        // below are the various permissions, the fields/edges that they provide access to, and the anonymization
        // options that sensus will provide. the list is taken from the following URL:
        //
        //   https://developers.facebook.com/docs/facebook-login/permissions/v2.3#reference

        // user object fields
        [Anonymizable("Age Range:", typeof(StringHashAnonymizer), false)]
        [FacebookPermission("public_profile", null, "age_range")]
        public string AgeRange { get; set; }

        [Anonymizable("First Name:", typeof(StringHashAnonymizer), false)]
        [FacebookPermission("public_profile", null, "first_name")]
        public string FirstName { get; set; }

        [Anonymizable(null, typeof(StringHashAnonymizer), false)]
        [FacebookPermission("public_profile", null, "gender")]
        public string Gender { get; set; }

        [Anonymizable("User ID:", typeof(StringHashAnonymizer), false)]
        [FacebookPermission("public_profile", null, "id")]
        public string UserId { get; set; }

        [Anonymizable("Last Name:", typeof(StringHashAnonymizer), false)]
        [FacebookPermission("public_profile", null, "last_name")]
        public string LastName { get; set; }

        [Anonymizable("Link to Timeline:", typeof(StringHashAnonymizer), false)]
        [FacebookPermission("public_profile", null, "link")]
        public string TimelineLink { get; set; }

        [Anonymizable(null, typeof(StringHashAnonymizer), false)]
        [FacebookPermission("public_profile", null, "locale")]
        public string Locale { get; set; }

        [Anonymizable("Full Name:", typeof(StringHashAnonymizer), false)]
        [FacebookPermission("public_profile", null, "name")]
        public string FullName { get; set; }

        [Anonymizable(null, typeof(StringHashAnonymizer), false)]
        [FacebookPermission("public_profile", null, "timezone")]
        public string Timezone { get; set; }

        [Anonymizable("Time of Last Update:", typeof(DateTimeOffsetTimelineAnonymizer), false)]
        [FacebookPermission("public_profile", null, "updated_time")]
        public DateTimeOffset? UpdatedTime { get; set; }

        [Anonymizable("Whether User is Verified:", null, false)]
        [FacebookPermission("public_profile", null, "verified")]
        public bool? Verified { get; set; }

        [Anonymizable("Email Address:", typeof(StringHashAnonymizer), false)]
        [FacebookPermission("email", null, "email")]
        public string Email { get; set; }

        [Anonymizable("About:", typeof(StringHashAnonymizer), false)]
        [FacebookPermission("user_about_me", null, "about")]
        public string About { get; set; }

        [Anonymizable(null, typeof(StringHashAnonymizer), false)]
        [FacebookPermission("user_education_history", null, "education")]
        public List<string> Education { get; set; }

        [Anonymizable(null, typeof(StringHashAnonymizer), false)]
        [FacebookPermission("user_hometown", null, "hometown")]
        public string Hometown { get; set; }

        [Anonymizable(null, typeof(StringHashAnonymizer), false)]
        [FacebookPermission("user_location", null, "location")]
        public string Location { get; set; }

        [Anonymizable("Relationship Status:", typeof(StringHashAnonymizer), false)]
        [FacebookPermission("user_relationships", null, "relationship_status")]
        public string RelationshipStatus { get; set; }

        [Anonymizable("Significant Other:", typeof(StringHashAnonymizer), false)]
        [FacebookPermission("user_relationships", null, "significant_other")]
        public string SignificantOther { get; set; }

        [Anonymizable(null, typeof(StringHashAnonymizer), false)]
        [FacebookPermission("user_religion_politics", null, "religion")]
        public string Religion { get; set; }

        [Anonymizable(null, typeof(StringHashAnonymizer), false)]
        [FacebookPermission("user_religion_politics", null, "political")]
        public string Politics { get; set; }

        [Anonymizable(null, typeof(StringHashAnonymizer), false)]
        [FacebookPermission("user_website", null, "website")]
        public string Website { get; set; }

        [Anonymizable("Employment History:", typeof(StringHashAnonymizer), false)]
        [FacebookPermission("user_work_history", null, "work")]
        public List<string> Employment { get; set; }

        // user edges
        [Anonymizable(null, typeof(StringHashAnonymizer), false)]
        [FacebookPermission("user_friends", "friends", "summary=total_count")]
        public List<string> Friends { get; set; }

        [Anonymizable("Books Read:", typeof(StringHashAnonymizer), false)]
        [FacebookPermission("user_actions.books", "book.reads", "book")]
        public List<string> Books { get; set; }

        [Anonymizable(null, typeof(StringHashAnonymizer), false)]
        [FacebookPermission("user_actions.fitness", "fitness.runs", "course")]
        public List<string> Runs { get; set; }

        [Anonymizable(null, typeof(StringHashAnonymizer), false)]
        [FacebookPermission("user_actions.fitness", "fitness.walks", "course")]
        public List<string> Walks { get; set; }

        [Anonymizable("Bike Rides:", typeof(StringHashAnonymizer), false)]
        [FacebookPermission("user_actions.fitness", "fitness.bikes", "course")]
        public List<string> Bikes { get; set; }

        [Anonymizable("Songs Listened To:", typeof(StringHashAnonymizer), false)]
        [FacebookPermission("user_actions.music", "music.listens", "song")]
        public List<string> Songs { get; set; }

        [Anonymizable(null, typeof(StringHashAnonymizer), false)]
        [FacebookPermission("user_actions.music", "music.playlists", "playlist")]
        public List<string> Playlists { get; set; }

        [Anonymizable("News Read:", typeof(StringHashAnonymizer), false)]
        [FacebookPermission("user_actions.news", "news.reads", "article")]
        public List<string> NewsReads { get; set; }

        [Anonymizable("News Published:", typeof(StringHashAnonymizer), false)]
        [FacebookPermission("user_actions.news", "news.publishes", "article")]
        public List<string> NewsPublishes { get; set; }

        [Anonymizable("Videos Watched:", typeof(StringHashAnonymizer), false)]
        [FacebookPermission("user_actions.video", "video.watches", "video")]
        public List<string> VideosWatched { get; set; }

        [Anonymizable("Video Ratings:", typeof(StringHashAnonymizer), false)]
        [FacebookPermission("user_actions.video", "video.rates", "movie")]
        public List<string> VideoRatings { get; set; }

        [Anonymizable("Video Wish List:", typeof(StringHashAnonymizer), false)]
        [FacebookPermission("user_actions.video", "video.wants_to_watch", "movie")]
        public List<string> VideoWishList { get; set; }

        [Anonymizable(null, typeof(StringHashAnonymizer), false)]
        [FacebookPermission("user_events", "events", "id")]
        public List<string> Events { get; set; }

        [Anonymizable(null, typeof(StringHashAnonymizer), false)]
        [FacebookPermission("user_likes", "likes", "created_time")]
        public List<string> Likes { get; set; }

        [Anonymizable("Captions of Posted Photos:", typeof(StringHashAnonymizer), false)]
        [FacebookPermission("user_photos", "photos", "id")]
        public List<string> PhotoCaptions { get; set; }

        [Anonymizable(null, typeof(StringHashAnonymizer), false)]
        [FacebookPermission("user_posts", "posts", "id")]
        public List<string> Posts { get; set; }

        [Anonymizable("Tagged Places:", typeof(StringHashAnonymizer), false)]
        [FacebookPermission("user_tagged_places", "tagged_places", "id")]
        public List<string> TaggedPlaces { get; set; }

        [Anonymizable("Titles of Posted Videos:", typeof(StringHashAnonymizer), false)]
        [FacebookPermission("user_videos", "videos", "id")]
        public List<string> VideoTitles { get; set; }

        public override string DisplayDetail
        {
            get
            {
                return "(Facebook Data)";
            }
        }

        public FacebookDatum(DateTimeOffset timestamp)
            : base(timestamp)
        {
        }
    }
}