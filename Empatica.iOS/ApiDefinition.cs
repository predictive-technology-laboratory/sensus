using System;
using EmpaLink-ios-0.7-full;
using Foundation;
using ObjCRuntime;

// @protocol EmpaticaDelegate <NSObject>
[Protocol, Model]
[BaseType (typeof(NSObject))]
interface EmpaticaDelegate
{
	// @required -(void)didUpdateBLEStatus:(BLEStatus)status;
	[Abstract]
	[Export ("didUpdateBLEStatus:")]
	void DidUpdateBLEStatus (BLEStatus status);

	// @required -(void)didDiscoverDevices:(NSArray *)devices;
	[Abstract]
	[Export ("didDiscoverDevices:")]
	[Verify (StronglyTypedNSArray)]
	void DidDiscoverDevices (NSObject[] devices);
}

// @interface EmpaticaAPI : NSObject
[BaseType (typeof(NSObject))]
interface EmpaticaAPI
{
	// +(void)authenticateWithAPIKey:(NSString *)key andCompletionHandler:(void (^)(BOOL, NSString *))handler;
	[Static]
	[Export ("authenticateWithAPIKey:andCompletionHandler:")]
	void AuthenticateWithAPIKey (string key, Action<bool, NSString> handler);

	// +(void)discoverDevicesWithDelegate:(id<EmpaticaDelegate>)empaticaDelegate;
	[Static]
	[Export ("discoverDevicesWithDelegate:")]
	void DiscoverDevicesWithDelegate (EmpaticaDelegate empaticaDelegate);

	// +(BLEStatus)status;
	[Static]
	[Export ("status")]
	[Verify (MethodToProperty)]
	BLEStatus Status { get; }

	// +(void)prepareForBackground;
	[Static]
	[Export ("prepareForBackground")]
	void PrepareForBackground ();

	// +(void)prepareForResume;
	[Static]
	[Export ("prepareForResume")]
	void PrepareForResume ();
}

// @protocol EmpaticaDeviceDelegate <NSObject>
[Protocol, Model]
[BaseType (typeof(NSObject))]
interface EmpaticaDeviceDelegate
{
	// @optional -(void)didUpdateDeviceStatus:(DeviceStatus)status forDevice:(EmpaticaDeviceManager *)device;
	[Export ("didUpdateDeviceStatus:forDevice:")]
	void DidUpdateDeviceStatus (DeviceStatus status, EmpaticaDeviceManager device);

	// @optional -(void)didReceiveTagAtTimestamp:(double)timestamp fromDevice:(EmpaticaDeviceManager *)device;
	[Export ("didReceiveTagAtTimestamp:fromDevice:")]
	void DidReceiveTagAtTimestamp (double timestamp, EmpaticaDeviceManager device);

	// @optional -(void)didReceiveGSR:(float)gsr withTimestamp:(double)timestamp fromDevice:(EmpaticaDeviceManager *)device;
	[Export ("didReceiveGSR:withTimestamp:fromDevice:")]
	void DidReceiveGSR (float gsr, double timestamp, EmpaticaDeviceManager device);

	// @optional -(void)didReceiveBVP:(float)bvp withTimestamp:(double)timestamp fromDevice:(EmpaticaDeviceManager *)device;
	[Export ("didReceiveBVP:withTimestamp:fromDevice:")]
	void DidReceiveBVP (float bvp, double timestamp, EmpaticaDeviceManager device);

	// @optional -(void)didReceiveTemperature:(float)temp withTimestamp:(double)timestamp fromDevice:(EmpaticaDeviceManager *)device;
	[Export ("didReceiveTemperature:withTimestamp:fromDevice:")]
	void DidReceiveTemperature (float temp, double timestamp, EmpaticaDeviceManager device);

	// @optional -(void)didReceiveAccelerationX:(char)x y:(char)y z:(char)z withTimestamp:(double)timestamp fromDevice:(EmpaticaDeviceManager *)device;
	[Export ("didReceiveAccelerationX:y:z:withTimestamp:fromDevice:")]
	void DidReceiveAccelerationX (sbyte x, sbyte y, sbyte z, double timestamp, EmpaticaDeviceManager device);

	// @optional -(void)didReceiveIBI:(float)ibi withTimestamp:(double)timestamp fromDevice:(EmpaticaDeviceManager *)device;
	[Export ("didReceiveIBI:withTimestamp:fromDevice:")]
	void DidReceiveIBI (float ibi, double timestamp, EmpaticaDeviceManager device);

	// @optional -(void)didReceiveBatteryLevel:(float)level withTimestamp:(double)timestamp fromDevice:(EmpaticaDeviceManager *)device;
	[Export ("didReceiveBatteryLevel:withTimestamp:fromDevice:")]
	void DidReceiveBatteryLevel (float level, double timestamp, EmpaticaDeviceManager device);
}

// @interface EmpaticaDeviceManager : NSObject
[BaseType (typeof(NSObject))]
interface EmpaticaDeviceManager
{
	// @property (nonatomic, strong) NSString * name;
	[Export ("name", ArgumentSemantic.Strong)]
	string Name { get; set; }

	// @property (readonly, assign, nonatomic) BOOL allowed;
	[Export ("allowed")]
	bool Allowed { get; }

	// @property (readonly, assign, nonatomic) DeviceStatus deviceStatus;
	[Export ("deviceStatus", ArgumentSemantic.Assign)]
	DeviceStatus DeviceStatus { get; }

	// -(void)connectWithDeviceDelegate:(id<EmpaticaDeviceDelegate>)deviceDelegate;
	[Export ("connectWithDeviceDelegate:")]
	void ConnectWithDeviceDelegate (EmpaticaDeviceDelegate deviceDelegate);

	// -(void)connectWithDeviceDelegate:(id<EmpaticaDeviceDelegate>)deviceDelegate andConnectionOptions:(NSArray *)connectionOptions;
	[Export ("connectWithDeviceDelegate:andConnectionOptions:")]
	[Verify (StronglyTypedNSArray)]
	void ConnectWithDeviceDelegate (EmpaticaDeviceDelegate deviceDelegate, NSObject[] connectionOptions);

	// -(void)disconnect;
	[Export ("disconnect")]
	void Disconnect ();
}
