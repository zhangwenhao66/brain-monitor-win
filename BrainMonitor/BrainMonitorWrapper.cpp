#define BRAINMIRRORWRAPPER_EXPORTS
#include "brnpro_if.h"
#include "ble_device.h"
#include "BrainMonitorWrapper.h"
#include <vector>
#include <string>
#include <mutex>
#include <cstring>

using namespace jfbrnpro_if;

// Global variables
static std::vector<ble_device> g_scanDevices;
static std::vector<ble_device> g_connectedDevices;
static std::mutex g_deviceMutex;
static bool g_initialized = false;
static std::string g_currentPort;

// Callback function pointers
static RawDataCallback g_rawDataCallback = nullptr;
static PostDataCallback g_postDataCallback = nullptr;
static BattInfoCallback g_battInfoCallback = nullptr;
static EventCallback g_eventCallback = nullptr;

// Internal callback functions
void internal_rawDataCallback(void* user, int dev, int chan, int* data, int len) {
    if (g_rawDataCallback) {
        g_rawDataCallback(dev, chan, data, len);
    }
}

void internal_postDataCallback(void* user, int dev, uint8_t ele, uint8_t att, uint8_t med, uint8_t res, uint32_t psd[8]) {
    if (g_postDataCallback) {
        g_postDataCallback(dev, ele, att, med, res, psd);
    }
}

void internal_battInfoCallback(void* user, int dev, uint32_t level, uint32_t vol) {
    if (g_battInfoCallback) {
        g_battInfoCallback(dev, level, vol);
    }
}

void internal_respCallback(void* user, int dev, uint8_t cmd, uint8_t* payload, int len) {
    // Response callback handling
}

void internal_eventCallback(void* user, uint32_t event, uint32_t param, void* param2) {
    if (g_eventCallback) {
        g_eventCallback(event, param);
    }
}

// Helper functions
void convertToDeviceInfo(ble_device& device, DeviceInfo* info) {
    if (!info) return;
    
    // Copy device name
    std::string name = device.getDeviceName();
    strcpy_s(info->name, sizeof(info->name), name.c_str());
    
    // Copy MAC address
    std::string mac = device.getDeviceMac();
    strcpy_s(info->mac, sizeof(info->mac), mac.c_str());
    
    // Set other properties
    info->type = device.getDeviceType();
    info->index = device.getConnectIndex();
    info->state = static_cast<int>(device.getDeviceState());
}

// SDK API implementation
BRAINMIRROR_API int SDK_Init() {
    if (g_initialized) {
        return 1; // Already initialized
    }
    
    try {
        // Initialize SDK
        jfsdk_init(1);
        
        // Install callback functions
        brainpro_install_callback(
            internal_postDataCallback,
            internal_rawDataCallback,
            internal_battInfoCallback,
            internal_respCallback,
            internal_eventCallback,
            nullptr
        );
        
        g_initialized = true;
        return 1; // Success
    }
    catch (...) {
        return 0; // Failure
    }
}

BRAINMIRROR_API void SDK_Cleanup() {
    if (g_initialized) {
        jfsdk_cleanup();
        g_initialized = false;
    }
}

BRAINMIRROR_API const char* SDK_GetVersion() {
    static std::string version;
    try {
        version = jfsdk_version();
        return version.c_str();
    }
    catch (...) {
        return "Unknown";
    }
}

BRAINMIRROR_API const char* SDK_CheckPort() {
    static std::string port;
    try {
        port = jfboard_checkPort();
        return port.c_str();
    }
    catch (...) {
        return "";
    }
}

BRAINMIRROR_API int SDK_ConnectPort(const char* port) {
    if (!port) return 0;
    
    try {
        std::string portStr(port);
        // Use 2000000 baud rate to connect port
        bool result = jfboard_connect(portStr, 2000000);
        if (result) {
            g_currentPort = portStr;
        }
        return result ? 1 : 0;
    }
    catch (...) {
        return 0;
    }
}

BRAINMIRROR_API void SDK_DisconnectPort() {
    try {
        jfboard_disconnect();
        g_currentPort.clear();
    }
    catch (...) {
        // Ignore errors
    }
}

BRAINMIRROR_API int SDK_ScanDevices() {
    try {
        std::lock_guard<std::mutex> lock(g_deviceMutex);
        if (jfboard_scan()) {
            g_scanDevices = jfboard_getScanDevices();
            return 1;
        }
        return 0;
    }
    catch (...) {
        return 0;
    }
}

BRAINMIRROR_API int SDK_GetScanDevicesCount() {
    std::lock_guard<std::mutex> lock(g_deviceMutex);
    return static_cast<int>(g_scanDevices.size());
}

BRAINMIRROR_API int SDK_GetScanDevice(int index, DeviceInfo* device) {
    if (!device || index < 0) return 0;
    
    std::lock_guard<std::mutex> lock(g_deviceMutex);
    if (index >= static_cast<int>(g_scanDevices.size())) return 0;
    
    convertToDeviceInfo(g_scanDevices[index], device);
    return 1;
}

BRAINMIRROR_API int SDK_ConnectDevice(const char* mac, int type) {
    if (!mac) return 0;
    
    try {
        std::string macStr(mac);
        ble_device device(macStr, type);
        
        std::vector<ble_device> devices;
        devices.push_back(device);
        
        bool result = brainpro_groupConnect(devices);
        if (result) {
            std::lock_guard<std::mutex> lock(g_deviceMutex);
            g_connectedDevices.push_back(device);
        }
        return result ? 1 : 0;
    }
    catch (...) {
        return 0;
    }
}

BRAINMIRROR_API int SDK_DisconnectDevice(const char* mac) {
    if (!mac) return 0;
    
    try {
        std::lock_guard<std::mutex> lock(g_deviceMutex);
        
        for (auto it = g_connectedDevices.begin(); it != g_connectedDevices.end(); ++it) {
            if (it->getDeviceMac() == mac) {
                brainpro_disconnect(*it);
                g_connectedDevices.erase(it);
                return 1;
            }
        }
        return 0;
    }
    catch (...) {
        return 0;
    }
}

BRAINMIRROR_API int SDK_GetConnectedDevicesCount() {
    std::lock_guard<std::mutex> lock(g_deviceMutex);
    return static_cast<int>(g_connectedDevices.size());
}

BRAINMIRROR_API int SDK_GetConnectedDevice(int index, DeviceInfo* device) {
    if (!device || index < 0) return 0;
    
    std::lock_guard<std::mutex> lock(g_deviceMutex);
    if (index >= static_cast<int>(g_connectedDevices.size())) return 0;
    
    convertToDeviceInfo(g_connectedDevices[index], device);
    return 1;
}

BRAINMIRROR_API int SDK_StartDataCollection() {
    try {
        return brainpro_start() ? 1 : 0;
    }
    catch (...) {
        return 0;
    }
}

BRAINMIRROR_API int SDK_StopDataCollection() {
    try {
        return brainpro_stop() ? 1 : 0;
    }
    catch (...) {
        return 0;
    }
}

BRAINMIRROR_API void SDK_SetRawDataCallback(RawDataCallback callback) {
    g_rawDataCallback = callback;
}

BRAINMIRROR_API void SDK_SetPostDataCallback(PostDataCallback callback) {
    g_postDataCallback = callback;
}

BRAINMIRROR_API void SDK_SetBattInfoCallback(BattInfoCallback callback) {
    g_battInfoCallback = callback;
}

BRAINMIRROR_API void SDK_SetEventCallback(EventCallback callback) {
    g_eventCallback = callback;
}

BRAINMIRROR_API int SDK_SendCommand(int dev, unsigned char cmd) {
    try {
        return brainpro_command(dev, cmd) ? 1 : 0;
    }
    catch (...) {
        return 0;
    }
}

BRAINMIRROR_API int SDK_SendCommandWithPayload(int dev, unsigned char cmd, unsigned char* payload, unsigned char len) {
    try {
        return brainpro_command(dev, cmd, payload, len) ? 1 : 0;
    }
    catch (...) {
        return 0;
    }
}