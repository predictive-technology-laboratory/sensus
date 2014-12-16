using Newtonsoft.Json;
using SensusUI.UiProperties;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SensusService.DataStores.Remote
{
    public class PostgreSqlRemoteDataStore : RemoteDataStore
    {
        private string _host;
        private int _port;
        private string _database;
        private string _username;
        private string _password;

        [EntryStringUiProperty("Host:", true, 2)]
        public string Host
        {
            get { return _host; }
            set
            {
                if (!value.Equals(_host, StringComparison.Ordinal))
                {
                    _host = value;
                    OnPropertyChanged();
                }
            }
        }

        [EntryIntegerUiProperty("Port:", true, 3)]
        public int Port
        {
            get { return _port; }
            set
            {
                if (value != _port)
                {
                    _port = value;
                    OnPropertyChanged();
                }
            }
        }

        [EntryStringUiProperty("Database:", true, 4)]
        public string Database
        {
            get { return _database; }
            set
            {
                if (!value.Equals(_database, StringComparison.Ordinal))
                {
                    _database = value;
                    OnPropertyChanged();
                }
            }
        }

        [EntryStringUiProperty("Username:", true, 5)]
        public string Username
        {
            get { return _username; }
            set
            {
                if (!value.Equals(_username, StringComparison.Ordinal))
                {
                    _username = value;
                    OnPropertyChanged();
                }
            }
        }

        [EntryStringUiProperty("Password:", true, 6)]
        public string Password
        {
            get { return _password; }
            set
            {
                if (!value.Equals(_password, StringComparison.Ordinal))
                {
                    _password = value;
                    OnPropertyChanged();
                }
            }
        }

        protected override string DisplayName
        {
            get { return "PostgreSQL"; }
        }

        [JsonIgnore]
        public override bool CanClear
        {
            get { return true; }
        }

        public PostgreSqlRemoteDataStore()
        {
            _host = "";
            _port = 7367;
            _database = "sensus";
            _username = "";
            _password = "";
        }

        protected override ICollection<Datum> CommitData(ICollection<Datum> data)
        {
            throw new NotImplementedException();
        }

        public override void Clear()
        {
            // TODO:  Implement
        }

        public override void UploadProtocolReport(ProtocolReport report)
        {
            throw new NotImplementedException();
        }
    }
}
