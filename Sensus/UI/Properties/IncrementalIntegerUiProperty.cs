using System;
using System.Collections.Generic;
using System.Text;

namespace Sensus.UI.Properties
{
    public abstract class IncrementalIntegerUiProperty : UiProperty
    {
        private int _minimum;
        private int _maximum;
        private int _increment;

        public int Minimum
        {
            get { return _minimum; }
        }

        public int Maximum
        {
            get { return _maximum; }
        }

        public int Increment
        {
            get { return _increment; }
        }

        public IncrementalIntegerUiProperty(int minimum, int maximum, int increment, string labelText, bool editable)
            : base(labelText, editable)
        {
            _minimum = minimum;
            _maximum = maximum;
            _increment = increment;
        }
    }
}
