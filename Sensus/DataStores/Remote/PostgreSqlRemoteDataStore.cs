using Newtonsoft.Json;
using Sensus.UI.Properties;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sensus.DataStores.Remote
{
    public class PostgreSqlRemoteDataStore : RemoteDataStore
    {
        private string _host;
        private int _port;
        private string _database;
        private string _username;
        private string _password;

        [StringUiProperty("Host:", true)]
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

        [EntryIntegerUiProperty("Port:", true)]
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

        [StringUiProperty("Database:", true)]
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

        [StringUiProperty("Username:", true)]
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

        [StringUiProperty("Password:", true)]
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

        protected override Task<ICollection<Datum>> CommitData(ICollection<Datum> data)
        {
            throw new NotImplementedException();
        }

        public override void Clear()
        {
            // TODO:  Implement
        }
    }
}
