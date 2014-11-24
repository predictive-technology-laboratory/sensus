using Sensus.UI.Properties;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Sensus.Probes
{
    [Serializable]
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

                    ProbeController newController = _passive ? new PassiveProbeController(this) as ProbeController : new ActiveProbeController(this) as ProbeController;

                    if (Controller.Running)
                    {
                        if (App.LoggingLevel >= LoggingLevel.Normal)
                            App.Get().SensusService.Log("Restarting " + Name + " as " + (_passive ? "passive" : "active") + " probe.");

                        Controller.StopAsync().ContinueWith(t =>
                            {
                                Controller = newController;
                                InitializeAndStartAsync();
                            });
                    }
                    else
                        Controller = newController;
                }
            }
        }

        public abstract Datum Poll();
    }
}
