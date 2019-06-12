namespace Sensus.Adaptation
{
    public interface ISensingAgentStateDatum
    {
        SensingAgentState PreviousState { get; set; }
        SensingAgentState CurrentState { get; set; }
    }
}