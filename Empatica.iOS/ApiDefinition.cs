using System;
using Foundation;
using ObjCRuntime;

namespace Empatica.iOS
{
    // @protocol EmpaticaDelegate <NSObject>
    [Protocol, Model]
    [BaseType(typeof(NSObject))]
    interface EmpaticaDelegate
    {
        // @required -(void)didUpdateBLEStatus:(BLEStatus)status;
        [Abstract]
        [Export("didUpdateBLEStatus:")]
        void DidUpdateBLEStatus(BLEStatus status);

        // @required -(void)didDiscoverDevices:(NSArray *)devices;
        [Abstract]
        [Export("didDiscoverDevices:")]
        void DidDiscoverDevices(EmpaticaDeviceManager[] devices);
    }

    // @interface EmpaticaAPI : NSObject
    [BaseType(typeof(NSObject))]
    interface EmpaticaAPI
    {
        // +(void)authenticateWithAPIKey:(NSString *)key andCompletionHandler:(void (^)(BOOL, NSString *))handler;
        [Static]
        [Export("authenticateWithAPIKey:andCompletionHandler:")]
        void AuthenticateWithAPIKey(string key, Action<bool, NSString> handler);

        // +(void)discoverDevicesWithDelegate:(id<EmpaticaDelegate>)empaticaDelegate;
        [Static]
        [Export("discoverDevicesWithDelegate:")]
        void DiscoverDevices(EmpaticaDelegate empaticaListener);

        // +(BLEStatus)status;
        [Static]
        [Export("status")]
        BLEStatus Status { get; }

        // +(void)prepareForBackground;
        [Static]
        [Export("prepareForBackground")]
        void PrepareForBackground();

        // +(void)prepareForResume;
        [Static]
        [Export("prepareForResume")]
        void PrepareForResume();
    }

    // @protocol EmpaticaDeviceDelegate <NSObject>
    [Protocol, Model]
    [BaseType(typeof(NSObject))]
    interface EmpaticaDeviceDelegate
    {
        // @optional -(void)didUpdateDeviceStatus:(DeviceStatus)status forDevice:(EmpaticaDeviceManager *)device;
        [Export("didUpdateDeviceStatus:forDevice:")]
        void DidUpdateDeviceStatus(DeviceStatus status, EmpaticaDeviceManager device);

        // @optional -(void)didReceiveTagAtTimestamp:(double)timestamp fromDevice:(EmpaticaDeviceManager *)device;
        [Export("didReceiveTagAtTimestamp:fromDevice:")]
        void DidReceiveTagAtTimestamp(double timestamp, EmpaticaDeviceManager device);

        // @optional -(void)didReceiveGSR:(float)gsr withTimestamp:(double)timestamp fromDevice:(EmpaticaDeviceManager *)device;
        [Export("didReceiveGSR:withTimestamp:fromDevice:")]
        void DidReceiveGalvanicSkinResponse(float galvanicSkinResponse, double timestamp, EmpaticaDeviceManager device);

        // @optional -(void)didReceiveBVP:(float)bvp withTimestamp:(double)timestamp fromDevice:(EmpaticaDeviceManager *)device;
        [Export("didReceiveBVP:withTimestamp:fromDevice:")]
        void DidReceiveBloodVolumePulse(float bloodVolumePulse, double timestamp, EmpaticaDeviceManager device);

        // @optional -(void)didReceiveTemperature:(float)temp withTimestamp:(double)timestamp fromDevice:(EmpaticaDeviceManager *)device;
        [Export("didReceiveTemperature:withTimestamp:fromDevice:")]
        void DidReceiveTemperature(float temperature, double timestamp, EmpaticaDeviceManager device);

        // @optional -(void)didReceiveAccelerationX:(char)x y:(char)y z:(char)z withTimestamp:(double)timestamp fromDevice:(EmpaticaDeviceManager *)device;
        [Export("didReceiveAccelerationX:y:z:withTimestamp:fromDevice:")]
        void DidReceiveAcceleration(sbyte x, sbyte y, sbyte z, double timestamp, EmpaticaDeviceManager device);

        // @optional -(void)didReceiveIBI:(float)ibi withTimestamp:(double)timestamp fromDevice:(EmpaticaDeviceManager *)device;
        [Export("didReceiveIBI:withTimestamp:fromDevice:")]
        void DidReceiveInterBeatInterval(float interBeatInterval, double timestamp, EmpaticaDeviceManager device);

        // @optional -(void)didReceiveBatteryLevel:(float)level withTimestamp:(double)timestamp fromDevice:(EmpaticaDeviceManager *)device;
        [Export("didReceiveBatteryLevel:withTimestamp:fromDevice:")]
        void DidReceiveBatteryLevel(float level, double timestamp, EmpaticaDeviceManager device);
    }

    // @interface EmpaticaDeviceManager : NSObject
    [BaseType(typeof(NSObject))]
    interface EmpaticaDeviceManager
    {
        // @property (nonatomic, strong) NSString * name;
        [Export("name", ArgumentSemantic.Strong)]
        string Name { get; set; }

        // @property (readonly, assign, nonatomic) BOOL allowed;
        [Export("allowed")]
        bool Allowed { get; }

        // @property (readonly, assign, nonatomic) DeviceStatus deviceStatus;
        [Export("deviceStatus", ArgumentSemantic.Assign)]
        DeviceStatus DeviceStatus { get; }

        // -(void)connectWithDeviceDelegate:(id<EmpaticaDeviceDelegate>)deviceDelegate;
        [Export("connectWithDeviceDelegate:")]
        void ConnectWithDeviceListener(EmpaticaDeviceDelegate deviceListener);

        // -(void)connectWithDeviceDelegate:(id<EmpaticaDeviceDelegate>)deviceDelegate andConnectionOptions:(NSArray *)connectionOptions;
        [Export("connectWithDeviceDelegate:andConnectionOptions:")]
        void ConnectWithDeviceListener(EmpaticaDeviceDelegate deviceListener, NSObject[] connectionOptions);

        // -(void)disconnect;
        [Export("disconnect")]
        void Disconnect();
    }
}