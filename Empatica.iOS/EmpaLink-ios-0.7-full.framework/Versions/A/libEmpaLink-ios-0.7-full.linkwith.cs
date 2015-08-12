using System;
using ObjCRuntime;

[assembly: LinkWith ("libEmpaLink-ios-0.7-full.a", LinkTarget.ArmV7 | LinkTarget.Simulator | LinkTarget.Simulator64 | LinkTarget.Arm64, SmartLink = true, ForceLoad = true)]
