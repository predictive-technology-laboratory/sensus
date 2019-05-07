---
uid: adaptive_surveys
---

# Adaptive Surveys
A primary use case for Sensus is the deployment of surveys according to schedules or in response
to data coming off [Probes](xref:Sensus.Probes.Probe). This is acheived through the use of the 
<xref:Sensus.Probes.User.Scripts.ScriptProbe>. The deployment of such surveys through Android and 
iOS devices is an active area of research. When should a survey be deployed? How should it be 
configured to attract the user's attention? Answers to these questions depend on the goal of the 
survey and the personal characteristics and contexts of each user.

## Android

### Survey Agent Plug-Ins
On Android, Sensus supports a plug-in architecture for modules (or agents) that control the delivery of surveys.
This architecture is intended to support research into adaptive surveys by providing a simple interface
through which researchers can deploy agents that implement specific adaptation hypotheses. For example,
one might hypothesize that surveys for a specific user are best delivered at 3pm each day. To test this
hypothesis, the researcher can implement a simple survey agent that defers delivery of surveys until 3pm
each day. The resulting response rates across users will provide supporting or opposing information for
this hypothesis.

### Implementing and Deploying a Survey Agent Plug-In
Follow the steps below to implement and deploy a survey agent within your Sensus study.

1. Create a new Android Class Library project in Visual Studio. In Visual Studio for Mac, the following image
shows the correct selection:

![image](/sensus/images/survey-agent-project.png)

1. Add a NuGet reference to [the Sensus package](https://www.nuget.org/packages/Sensus).

1. Add a new class that implements the <xref:Sensus.Probes.User.Scripts.IScriptProbeAgent> interface. Implement all
interface methods.

1. Build the library project, and upload the resulting .dll to a web-accessible URL. A convenient
solution is to upload the .dll to a Dropbox directory and copy the sharing URL for the .dll file.

1. Generate a QR code that points to your .dll (e.g., using [QR Code Generator](https://www.qr-code-generator.com/)).
The content of the QR code must be exactly as shown below:
```
survey-agent:URL
```
where URL is the web-accessible URL that points to your .dll file. If you are using Dropbox, then the QR code
content will be similar to the one shown below (note the `dl=1` ending of the URL, and note that the following 
URL is only an example -- it is not actually valid):
```
survey-agent:https://www.dropbox.com/s/dlaksdjfasfasdf/ScriptProbeAgent.dll?dl=1
```

1. In the protocol settings for your <xref:Sensus.Probes.User.Scripts.ScriptProbe>, tap "Set Agent" and scan
your QR code. Sensus will fetch your .dll file and extract any agent definitions contained therein. Select
your desired agent.

1. Continue with [configuration](xref:protocol_creation) and [distribution](xref:protocol_distribution)
of your protocol.

### Example Survey Agents
See the following implementations for example agents:

* [Random](xref:ExampleScriptProbeAgent.ExampleRandomScriptProbeAgent) (code [here](https://github.com/predictive-technology-laboratory/sensus/blob/develop/ExampleScriptProbeAgent.Shared/ExampleRandomScriptProbeAgent.cs)):  A 
survey agent that randomly decides whether or not to deliver surveys.

* [Adaptive](xref:ExampleScriptProbeAgent.ExampleAdaptiveScriptProbeAgent) (code [here](https://github.com/predictive-technology-laboratory/sensus/blob/develop/ExampleScriptProbeAgent.Shared/ExampleAdaptiveScriptProbeAgent.cs)):  A 
survey agent that attempts to adapt to the user by increasing and decreasing the likelihood of survey delivery based on experience.

## iOS

In contrast with Android, iOS does not allow apps to load code (e.g., from the above .dll assembly) at
run time. Thus, adaptive survey agents are more limited on iOS compared with Android. Here are the options:

* The app comes with two example survey agents; however, these are simply for demonstration and are unlikely to work
well in practice. Nonetheless, the examples are:

  * [Random](xref:ExampleScriptProbeAgent.ExampleRandomScriptProbeAgent) (code [here](https://github.com/predictive-technology-laboratory/sensus/blob/develop/ExampleScriptProbeAgent.Shared/ExampleRandomScriptProbeAgent.cs)):  A 
survey agent that randomly decides whether or not to deliver surveys.

  * [Adaptive](xref:ExampleScriptProbeAgent.ExampleAdaptiveScriptProbeAgent) (code [here](https://github.com/predictive-technology-laboratory/sensus/blob/develop/ExampleScriptProbeAgent.Shared/ExampleAdaptiveScriptProbeAgent.cs)):  A 
survey agent that attempts to adapt to the user by increasing and decreasing the likelihood of survey delivery based on experience.

  You can select either of these agents when configuring the <xref:Sensus.Probes.User.Scripts.ScriptProbe>.

* You can [redeploy](xref:redeploying) Sensus as your own app, to which you can add your own agent implementations.

* You can implement your own agent implementations following the instructions above for Android and email 
our team (uva.ptl@gmail.com) to include them in a future release.

## Testing and Debugging

Regardless of whether your survey agent targets Android or iOS, there are a few ways to test and debug it:

* Write to the log file:  See the code for the example agents above. You will see calls that write to the log file. Use similar
calls in your code to write information about the behavior of your agent to the log. Run your agent for a while in the app and
then share the log file with yourself from within the app. Note that the size of the log file is limited, so you might not be 
able to view the entire log history of your agent.

* Flash notifications on the screen:  On Android, you can flash notifications on the screen as shown in the example code. These
messages will appear for a short duration.

* Run your agent in the debugger:  By far the most useful approach is to [configure a development system](xref:dev_config) and
run Sensus in the debugger with your survey agent. You will need to add your agent code to the Sensus app projects in order to 
step through it in the debugger.

