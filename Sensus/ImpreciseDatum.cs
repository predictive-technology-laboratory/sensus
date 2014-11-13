using System;
using System.Collections.Generic;
using System.Text;

namespace Sensus
{
    /// <summary>
    /// Represents a Datum that could be imprecisely measured.
    /// </summary>
    public abstract class ImpreciseDatum : Datum
    {
        private double _accuracy;

        /// <summary>
        /// Precision of the measurement associated with this Datum.
        /// </summary>
        public double Accuracy
        {
            get { return _accuracy; }
        }

        protected ImpreciseDatum(int probeId, DateTimeOffset timestamp, double accuracy)
            : base(probeId, timestamp)
        {
            _accuracy = accuracy;
        }
    }
}
