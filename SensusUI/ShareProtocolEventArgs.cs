using SensusService;
using System;
using System.Collections.Generic;
using System.Text;

namespace SensusUI
{
    public class ShareProtocolEventArgs : EventArgs
    {
        private Protocol _protocol;
        private Protocol.ShareMethod _method;

        public Protocol Protocol
        {
            get { return _protocol; }
            set { _protocol = value; }
        }

        public Protocol.ShareMethod Method
        {
            get { return _method; }
            set { _method = value; }
        }

        public ShareProtocolEventArgs(Protocol protocol, Protocol.ShareMethod method)
        {
            _protocol = protocol;
            _method = method;
        }
    }
}
