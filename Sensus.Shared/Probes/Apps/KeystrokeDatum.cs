using Sensus.Anonymization;
using Sensus.Anonymization.Anonymizers;
using Sensus.Probes.User.Scripts.ProbeTriggerProperties;
using System;

namespace Sensus.Probes.Apps
{
    public class KeystrokeDatum : Datum
    {
        private string _key;
        private string _app;

        public KeystrokeDatum(DateTimeOffset timestamp, string key, string app) : base(timestamp)
        {
            _key = key == null ? "" : key;
            _app = app == null ? "" : app;
        }

        public override string DisplayDetail
        {
            get
            {
                return "(Keystroke Data)";
            }
        }

        [StringProbeTriggerProperty]
        [Anonymizable(null, typeof(StringHashAnonymizer), false)]
        public string Key { get => _key; set => _key = value; }

        [StringProbeTriggerProperty]
        [Anonymizable(null, typeof(StringHashAnonymizer), false)]
        public string App { get => _app; set => _app = value; }

        public override object StringPlaceholderValue => throw new NotImplementedException();

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override string ToString()
        {
            return base.ToString() + Environment.NewLine +
                      "Key:  " + _key + Environment.NewLine +
                      "App:  " + _app + Environment.NewLine;
        }
    }
}
