using System;
using System.Collections.Generic;
using System.Text;

namespace Sensus.Models
{
    public class Account
    {
        public string participantId { get; set; }
        public string password { get; set; }
        public string protocolURL { get; set; }
        public string protocolId { get; set; }
    }
}
