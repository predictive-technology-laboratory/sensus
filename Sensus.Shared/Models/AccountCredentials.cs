using System;
using System.Collections.Generic;
using System.Text;

namespace Sensus.Models
{
    public class AccountCredentials
    {
        public string accessKeyId { get; set; }
        public string secretAccessKey { get; set; }
        public string protocolURL { get; set; }
        public string expiration { get; set; }
        public string cmk { get; set; }

        public DateTimeOffset expirationDateTime
        {
            get
            {
                var rVal = DateTimeOffset.MinValue;
                if (string.IsNullOrWhiteSpace(expiration) == false && long.TryParse(expiration, out long milliseconds))
                {
                    rVal = DateTime.SpecifyKind(new DateTime(1970, 1, 1), DateTimeKind.Utc).AddMilliseconds(milliseconds);
                }
                return rVal;
            }
        }

        public bool IsExpired
        {
            get
            {
                return DateTimeOffset.UtcNow > expirationDateTime;
            }
        }
    }
}
