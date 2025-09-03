#pragma once

#ifdef BRAINMIRRORWRAPPER_EXPORTS
#define BRAINMIRROR_API __declspec(dllexport)
#else
#define BRAINMIRROR_API __declspec(dllimport)
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
BRAINMIRROR_API int SDK_Init();
BRAINMIRROR_API void SDK_Cleanup();
BRAINMIRROR_API const char* SDK_GetVersion();

// 端口操作
BRAINMIRROR_API const char* SDK_CheckPort();
BRAINMIRROR_API int SDK_ConnectPort(const char* port);
BRAINMIRROR_API void SDK_DisconnectPort();

// 设备扫描和连接
BRAINMIRROR_API int SDK_ScanDevices();
BRAINMIRROR_API int SDK_GetScanDevicesCount();
BRAINMIRROR_API int SDK_GetScanDevice(int index, DeviceInfo* device);
BRAINMIRROR_API int SDK_ConnectDevice(const char* mac, int type);
BRAINMIRROR_API int SDK_DisconnectDevice(const char* mac);
BRAINMIRROR_API int SDK_GetConnectedDevicesCount();
BRAINMIRROR_API int SDK_GetConnectedDevice(int index, DeviceInfo* device);

// 数据采集
BRAINMIRROR_API int SDK_StartDataCollection();
BRAINMIRROR_API int SDK_StopDataCollection();

// 回调函数设置
BRAINMIRROR_API void SDK_SetRawDataCallback(RawDataCallback callback);
BRAINMIRROR_API void SDK_SetPostDataCallback(PostDataCallback callback);
BRAINMIRROR_API void SDK_SetBattInfoCallback(BattInfoCallback callback);
BRAINMIRROR_API void SDK_SetEventCallback(EventCallback callback);

// 设备控制
BRAINMIRROR_API int SDK_SendCommand(int dev, unsigned char cmd);
BRAINMIRROR_API int SDK_SendCommandWithPayload(int dev, unsigned char cmd, unsigned char* payload, unsigned char len);

#ifdef __cplusplus
}
#endif