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

## Survey Agent Plug-Ins
Sensus supports a plug-in architecture for modules (or agents) that control the delivery of surveys.
This architecture is intended to support research into adaptive surveys by providing a simple interface
through which researchers can deploy agents that implement specific adaptation hypotheses. For example,
one might hypothesize that surveys for a specific user are best delivered at 3pm each day. To test this
hypothesis, the researcher can implement a simple survey agent that defers delivery of surveys until 3pm
each day. The resulting response rates across users will provide supporting or opposing information for
this hypothesis.

## Implementing and Deploying a Survey Agent Plug-In
Follow the steps below to implement and deploy a survey agent within your Sensus study.

1. Create a new .NET Standard Library project in Visual Studio. In Visual Studio for Mac, the following image
shows the correct selection:

![image](/sensus/images/survey-agent-project.png)

1. Add a NuGet reference to [the Sensus package](https://www.nuget.org/packages/Sensus).

1. Add a new class that implements the `IScriptProbeAgent` interface. Implement all
abstract methods.

1. Build the library project, and upload the resulting .dll to a web-accessible URL. A convenient
solution is to upload the .dll to a Dropbox directory and copy the sharing URL for the .dll file.

1. Generate a QR code that points to your .dll (e.g., using [QR Code Generator](https://www.qr-code-generator.com/)).
The content of the QR code must be exactly as shown below:

    survey-agent:URL
    
where URL is the web-accessible URL that points to your .dll file. If you are using Dropbox, then the URL 
will be similar to the one shown below (note the `dl=1` ending):

    https://www.dropbox.com/s/nxaatmzuufb5qbk/ExampleScriptProbeAgent.dll?dl=1
    
