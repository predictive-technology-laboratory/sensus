using System;
using System.Collections.Generic;
using System.Text;

namespace SensusService.Probes.User
{
    public class Script
    {
        private string _path;
        private string _name;

        public string Path
        {
            get { return _path; }
        }

        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        public Script(string path)
        {
        }

        public void Run(Datum previous, Datum current)
        {
        }
    }
}
