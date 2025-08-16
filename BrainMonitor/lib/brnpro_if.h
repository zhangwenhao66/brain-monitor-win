/**************************************************************************
 *
 *  Copyright (C) 2022 -
 *  Shenzhen Shuimu AI Technology Co.LTD.  All rights reserved.           *
 *
 *************************************************************************/


#pragma once

#include <cstdint>

#include "ble_device.h"

#include <string>
#include <vector>
#include <queue>

namespace jfbrnpro_if
{

    enum {
        Event_devDisconnect = 1,
        Event_devConnected = 2,
        Event_dongleReboot = 3,
        Event_comThreadStart =4,
        Event_comThreadStop = 5,
    };

typedef void (*postDataOutputCB)(void* user, int dev, uint8_t ele, uint8_t att, uint8_t med, uint8_t res, uint32_t psd[8]);
typedef void (*rawDataOutputCB)(void* user, int dev, int chan, int* data, int len);
typedef void (*battInfoOutCB)(void* user, int dev, uint32_t level, uint32_t vol);
typedef void (*commandRespCB)(void* user, int dev, uint8_t cmd, uint8_t* payload, int len);
typedef void (*eventCB)(void* user, uint32_t event, uint32_t param, void *param2);

std::string jfboard_checkPort();
std::string jfboard_checkPort(const int uart_baudrate);
const char* jfboard_checkPortC();
bool jfboard_setBaudrate(int rate);
bool jfboard_connect(std::string& port, int baudrate = 2000000);
bool jfboard_connect(const char* port);
void jfboard_disconnect();
bool jfboard_scan();
std::vector <ble_device> jfboard_getScanDevices();
std::vector <ble_device> jfboard_getDevices();
int jfboard_getConnectedDevicesNum(void);

bool brainpro_connect(std::string& mac);
bool brainpro_connect(ble_device& dev);
bool brainpro_connect(const char* mac, int type);
bool brainpro_groupConnect(std::vector <ble_device> &devices, int timeout = 0);
bool brainpro_groupAdd(ble_device& devices);
bool brainpro_disconnect(ble_device& devices);
ble_device::dev_state brainpro_updateDeviceState(ble_device& devices);
bool brainpro_start();
bool brainpro_stop();
bool brainpro_exit();
bool brainpro_command(int dev, uint8_t cmd);
bool brainpro_command(int dev, uint8_t cmd, uint8_t* payload, uint8_t len);
void brainpro_install_callback(postDataOutputCB postOutput, rawDataOutputCB rawOutput, battInfoOutCB battInfo,
                               commandRespCB cmdResp, eventCB setEvent, void* userData);
std::string jfsdk_version();
void jfsdk_init(int arg);
void jfsdk_cleanup();
void jfsdk_sleep(int ms);

}
