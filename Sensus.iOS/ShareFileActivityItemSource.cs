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
using UIKit;
using Foundation;

namespace Sensus.iOS
{
    public class ShareFileActivityItemSource : UIActivityItemSource
    {        
        private string _path;
        private string _subject;

        public ShareFileActivityItemSource(string path, string subject)
        {            
            _path = path;
            _subject = subject;
        }

        public override NSObject GetPlaceholderData(UIActivityViewController activityViewController)
        {
            return new NSString(_subject);
        }

        public override string GetSubjectForActivity(UIActivityViewController activityViewController, Foundation.NSString activityType)
        {
            return _subject;
        }

        public override NSObject GetItemForActivity(UIActivityViewController activityViewController, Foundation.NSString activityType)
        {
            return NSData.FromUrl(NSUrl.FromFilename(_path));
        }

        public override string GetDataTypeIdentifierForActivity(UIActivityViewController activityViewController, NSString activityType)
        {
            return "edu.virginia.sie.ptl.sensus.protocol";
        }
    }
}