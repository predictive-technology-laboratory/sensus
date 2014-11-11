using System;
using System.Collections.Generic;
using System.Text;

namespace Sensus.Probes.Parameters
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public abstract class ProbeParameter : Attribute
    {
        private string _labelText;
        private bool _editable;

        public string LabelText
        {
            get { return _labelText; }
        }

        public bool Editable
        {
            get { return _editable; }
        }

        protected ProbeParameter(string labelText, bool editable)
        {
            _labelText = labelText;
            _editable = editable;
        }
    }
}
