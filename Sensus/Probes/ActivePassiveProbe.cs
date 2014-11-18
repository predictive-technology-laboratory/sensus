using Sensus.UI.Properties;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sensus.Probes
{
    public abstract class ActivePassiveProbe : PassiveProbe, IActiveProbe
    {
        private bool _passive;

        [BooleanUiProperty("Passive Mode:", true)]
        public virtual bool Passive
        {
            get { return _passive; }
            set
            {
                if (value != _passive)
                {
                    _passive = value;
                    OnPropertyChanged();

                    bool wasRunning = false;
                    if (Controller.Running)
                    {
                        if (Logger.Level >= LoggingLevel.Normal)
                            Logger.Log("Restarting " + Name + " as " + (_passive ? "passive" : "active") + " probe.");

                        Controller.StopAsync();
                        wasRunning = true;
                    }

                    Controller = _passive ? new PassiveProbeController(this) as ProbeController : new ActiveProbeController(this) as ProbeController;

                    if (wasRunning)
                        InitializeAndStartAsync();
                }
            }
        }

        public abstract Datum Poll();
    }
}
