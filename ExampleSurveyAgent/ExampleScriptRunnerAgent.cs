using System;
using System.Threading.Tasks;
using Sensus.Probes.User.Scripts;

namespace ExampleSurveyAgent
{
    /// <summary>
    /// Example script runner agent. Executes a random policy in which surveys are delivered
    /// randomly based on a selected probability.
    /// </summary>
    public class ExampleScriptRunnerAgent : ScriptRunnerAgent
    {
        /// <summary>
        /// The delivery probability.
        /// </summary>
        /// <value>The delivery probability.</value>
        public double DeliveryProbability { get; set; } = 0.5;

        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        public override string Name { get => "Random (p = " + DeliveryProbability + ")"; }

        public override Task<bool> ShouldDeliverSurvey()
        {
            Random random = new Random();
            return Task.FromResult(random.NextDouble() <= DeliveryProbability);
        }
    }
}