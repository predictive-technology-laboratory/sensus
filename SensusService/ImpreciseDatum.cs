using System;

namespace SensusService
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
            set { _accuracy = value; }
        }

        protected ImpreciseDatum(int probeId, DateTimeOffset timestamp, double accuracy)
            : base(probeId, timestamp)
        {
            _accuracy = accuracy;
        }

        public override string ToString()
        {
            return base.ToString() + Environment.NewLine +
                   "Imprecise:  true";
        }
    }
}
