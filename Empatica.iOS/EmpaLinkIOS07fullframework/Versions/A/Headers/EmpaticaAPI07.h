#ifdef __IPHONE_OS_VERSION_MAX_ALLOWED
    #import <CoreBluetooth/CoreBluetooth.h>
#else
//    #import <IOBluetooth/IOBluetooth.h>
#endif

#import <Foundation/Foundation.h>

//-----------EmpaticaAPI------------------------------
typedef enum {
    kBLEStatusNotAvailable,
    kBLEStatusReady,
    kBLEStatusScanning
} BLEStatus;

@protocol EmpaticaDelegate<NSObject>

- (void)didUpdateBLEStatus:(BLEStatus)status;
- (void)didDiscoverDevices:(NSArray *)devices;

@end

@interface EmpaticaAPI : NSObject

+ (void)authenticateWithAPIKey:(NSString *)key andCompletionHandler:(void (^)(BOOL success, NSString *description))handler;
+ (void)discoverDevicesWithDelegate:(id<EmpaticaDelegate>)empaticaDelegate;

+ (BLEStatus)status;

+ (void)prepareForBackground;
+ (void)prepareForResume;

@end
// --------------------------------------------------


//-----------EmpaticaDeviceManager-------------------
@class EmpaticaDevice;
@class EmpaticaDeviceManager;

typedef enum {
    kDeviceStatusDisconnected,
    kDeviceStatusConnecting,
    kDeviceStatusConnected,
    kDeviceStatusFailedToConnect,
    kDeviceStatusDisconnecting
} DeviceStatus;

@protocol EmpaticaDeviceDelegate<NSObject>

@optional
- (void)didUpdateDeviceStatus:(DeviceStatus)status forDevice:(EmpaticaDeviceManager *)device;

- (void)didReceiveTagAtTimestamp:(double)timestamp fromDevice:(EmpaticaDeviceManager *)device;

- (void)didReceiveGSR:(float)gsr                            withTimestamp:(double)timestamp fromDevice:(EmpaticaDeviceManager *)device;
- (void)didReceiveBVP:(float)bvp                            withTimestamp:(double)timestamp fromDevice:(EmpaticaDeviceManager *)device;
- (void)didReceiveTemperature:(float)temp                   withTimestamp:(double)timestamp fromDevice:(EmpaticaDeviceManager *)device;
- (void)didReceiveAccelerationX:(char)x y:(char)y z:(char)z withTimestamp:(double)timestamp fromDevice:(EmpaticaDeviceManager *)device;
- (void)didReceiveIBI:(float)ibi                            withTimestamp:(double)timestamp fromDevice:(EmpaticaDeviceManager *)device;
- (void)didReceiveBatteryLevel:(float)level                 withTimestamp:(double)timestamp fromDevice:(EmpaticaDeviceManager *)device;

@end

@interface EmpaticaDeviceManager : NSObject

@property (nonatomic, strong) NSString *name;
@property (nonatomic, assign, readonly) BOOL allowed;
@property (nonatomic, assign, readonly) DeviceStatus deviceStatus;

- (void)connectWithDeviceDelegate:(id<EmpaticaDeviceDelegate>)deviceDelegate;
- (void)connectWithDeviceDelegate:(id<EmpaticaDeviceDelegate>)deviceDelegate andConnectionOptions:(NSArray *)connectionOptions;
- (void)disconnect;

@end
// --------------------------------------------------
