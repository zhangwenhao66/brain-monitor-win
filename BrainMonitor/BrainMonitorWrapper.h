#pragma once

#ifdef BRAINMONITORWRAPPER_EXPORTS
#define BRAINMONITOR_API __declspec(dllexport)
#else
#define BRAINMONITOR_API __declspec(dllimport)
#endif

// 设备信息结构体 - C++版本
struct DeviceInfo {
    char mac[32];
    char name[64];
    int type;
    int index;
    int state; // 0=disconnected, 1=connected
};

#ifdef __cplusplus
extern "C" {
#endif

// 回调函数类型定义
typedef void (*RawDataCallback)(int dev, int chan, int* data, int len);
typedef void (*PostDataCallback)(int dev, unsigned char ele, unsigned char att, unsigned char med, unsigned char res, unsigned int psd[8]);
typedef void (*BattInfoCallback)(int dev, unsigned int level, unsigned int vol);
typedef void (*EventCallback)(unsigned int event, unsigned int param);

// SDK初始化和清理
BRAINMONITOR_API int SDK_Init();
BRAINMONITOR_API void SDK_Cleanup();
BRAINMONITOR_API const char* SDK_GetVersion();

// 端口操作
BRAINMONITOR_API const char* SDK_CheckPort();
BRAINMONITOR_API int SDK_ConnectPort(const char* port);
BRAINMONITOR_API void SDK_DisconnectPort();

// 设备扫描和连接
BRAINMONITOR_API int SDK_ScanDevices();
BRAINMONITOR_API int SDK_GetScanDevicesCount();
BRAINMONITOR_API int SDK_GetScanDevice(int index, DeviceInfo* device);
BRAINMONITOR_API int SDK_ConnectDevice(const char* mac, int type);
BRAINMONITOR_API int SDK_DisconnectDevice(const char* mac);
BRAINMONITOR_API int SDK_GetConnectedDevicesCount();
BRAINMONITOR_API int SDK_GetConnectedDevice(int index, DeviceInfo* device);

// 数据采集
BRAINMONITOR_API int SDK_StartDataCollection();
BRAINMONITOR_API int SDK_StopDataCollection();

// 回调函数设置
BRAINMONITOR_API void SDK_SetRawDataCallback(RawDataCallback callback);
BRAINMONITOR_API void SDK_SetPostDataCallback(PostDataCallback callback);
BRAINMONITOR_API void SDK_SetBattInfoCallback(BattInfoCallback callback);
BRAINMONITOR_API void SDK_SetEventCallback(EventCallback callback);

// 设备控制
BRAINMONITOR_API int SDK_SendCommand(int dev, unsigned char cmd);
BRAINMONITOR_API int SDK_SendCommandWithPayload(int dev, unsigned char cmd, unsigned char* payload, unsigned char len);

#ifdef __cplusplus
}
#endif