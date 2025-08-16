/**************************************************************************
 *
 *  Copyright (C) 2022 -
 *  Shenzhen Shuimu AI Technology Co.LTD.  All rights reserved.           *
 *
 *************************************************************************/

#include "brnpro_sdk.h"
#include "brnpro_if.h"
#include "ble_device.h"

#if defined (_WIN32) || defined( _WIN64)
#include <Windows.h>
#include <conio.h>
#endif

#include <iostream>
#include <fstream>
#include <string>
#include <thread>
#include <mutex>

#define DIS_TEST       0
#define CONNECT_LIST   0
#define RECONNECT_DEMO  1

using namespace std;
using namespace jfbrnpro_if;

static std::thread backgroundThread;
static std::vector<ble_device> getConnectDev;
static std::thread reconThread;


mutex reconMutex;


struct reconnect_dev {
    int retry = 0;
    string mac;
};

static std::vector<reconnect_dev> reconnectDevList;

void test_thread();
void reconnect_thread();

std::atomic_bool bg_run{ true };
std::atomic_bool reconn_run{ true };

std::ofstream logfile;
std::ofstream rawFile[16];

static double start_timestamp = 0;

static void removeReconnectList(string& dev_mac)
{
    lock_guard<mutex> guard(reconMutex);

    bool found = false;

    if (!reconnectDevList.empty()) {
        for (auto iter{ begin(reconnectDevList) }; iter != end(reconnectDevList); ++iter) {
            reconnect_dev thisDevice = *iter;
            if (thisDevice.mac == dev_mac) {
                reconnectDevList.erase(iter);
                break;
            }
        }
    }

}


static void updateReconnectList(string &dev_mac)
{
    lock_guard<mutex> guard(reconMutex);

    bool found = false;

    if (!reconnectDevList.empty()) {
        for (auto iter{ begin(reconnectDevList) }; iter != end(reconnectDevList); ++iter) {
            reconnect_dev thisDevice = *iter;
            if (thisDevice.mac == dev_mac) {
                found = true;
                break;
            }
        }
    }

    if (found == false) {
        reconnect_dev new_dev;
        new_dev.mac = dev_mac;
        new_dev.retry = 0;
        
        reconnectDevList.push_back(new_dev);
    }

}


static const string getDeviceMacFromIndex(int idx)
{
    string ret;
    if (!getConnectDev.empty()) {
        for (auto iter{ cbegin(getConnectDev) }; iter != cend(getConnectDev); ++iter) {
            ble_device thisDevice = *iter;
            if (thisDevice.getConnectIndex() == idx) {
                ret = thisDevice.getDeviceMac();
                break;
            }
        }
    }
    return ret;
}

static double get_timestamp()
{
#define FILETIME_TO_UNIX 116444736000000000i64
    FILETIME ft;
    GetSystemTimePreciseAsFileTime(&ft);
    int64_t t = ((int64_t)ft.dwHighDateTime << 32L) | (int64_t)ft.dwLowDateTime;
    return (t - FILETIME_TO_UNIX) / (10.0 * 1000.0);
}

void resp_proc(void* user, int dev, uint8_t cmd, uint8_t* payload, int len)
{
    char buf[256]{ 0 };
    snprintf(buf, sizeof(buf), "[Cmd Resp] Dev=%d Cmd:%x len=%d \n", dev, cmd, len);
    printf("%s", buf);
    if (logfile.is_open()) {
        logfile << buf;
    }
}

void postData(void* user, int dev, uint8_t ele, uint8_t att, uint8_t med, uint8_t res, uint32_t psd[8])
{
    char buf[256]{ 0 };
    snprintf(buf, sizeof(buf), "[state] dev=%d ele=%d Att=%d med=%d \n", dev, ele, att, med);
    //printf("%s", buf);
    if (logfile.is_open()) {
        logfile << buf;
    }

}

void battInfo(void* user, int dev, uint32_t level, uint32_t vol)
{
    char buf[256]{ 0 };
    snprintf(buf, sizeof(buf), "[batinfo] dev=%d level=%d vol=%d \n", dev, level, vol);
    //printf("%s", buf);
    if (logfile.is_open()) {
        logfile << buf;
    }
}

void rawData(void* user, int dev, int chan, int* data, int len)
{
    char buf[256]{ 0 };
    snprintf(buf, sizeof(buf), "[rawdata] [%lld] dev=%d channel=%d len=%d \n", (int64_t)(get_timestamp() - start_timestamp),
        dev, chan, len);
    //printf("%s", buf);
    if (logfile.is_open()) {
        logfile << buf;
    }

    if (dev < 16) {
        if (rawFile[dev].is_open() == false) {
            string mac = getDeviceMacFromIndex(dev);
            string name = "rawData" + mac + ".csv";
            rawFile[dev].open(name);
        }
        if (rawFile[dev].is_open()) {
            if (chan == 0) {  //only record first channel data
                for (int i = 0; i < len; i++) {
                    rawFile[dev] << data[i] << "\n";
                }
            }

        }
    }

}


void eventFunc(void* user, uint32_t event, uint32_t param, void* param2)
{
    char buf[64]{ 0 };
    snprintf(buf, sizeof(buf), "[%lld] Event: %d\n", (int64_t)(get_timestamp() - start_timestamp), event);
    printf("%s", buf);
    if (logfile.is_open()) {
        logfile << buf;
    }
    if (event == (uint32_t)Event_devDisconnect) {

        printf("%s disconnected.\n", (char*)param2);
        string devMac = (char*)param2;
        ble_device checkDev(devMac);

        int state = static_cast <int>(brainpro_updateDeviceState(checkDev));
        snprintf(buf, sizeof(buf), "%s state: %d \n", (char*)param2, state);
        if (logfile.is_open()) {
            logfile << buf;
        }
        printf("%s", buf);
        updateReconnectList(devMac);
    }
    if (event == (uint32_t)Event_devConnected) {
        printf("%s connected.\n", (char*)param2);
        string devMac = (char*)param2;
        removeReconnectList(devMac);
        ble_device checkDev(devMac);
        int state = static_cast <int>(brainpro_updateDeviceState(checkDev));
        snprintf(buf, sizeof(buf), "%s state: %d \n", (char*)param2, state);
        if (logfile.is_open()) {
            logfile << buf;
        }
        printf("%s", buf);
    }
    if (event == (uint32_t)Event_dongleReboot) {
        printf("Dongle reboot!!!\n");
    }
}

static void device_config()
{
    brainpro_command(0, 0xEC);
    brainpro_command(0, 0xEF);
    brainpro_command(0, 0x04);
    uint8_t payload[3] = { 0x00, 0x00, 0x01 };
    brainpro_command(0, 0x2B, payload, 3);
    brainpro_command(0, 0xff);
}

int main()
{
    jfsdk_init(1);
    string sdkver = jfsdk_version();
    cout << "Build: " << __DATE__ << "," << __TIME__ << "\n";
    cout << "SDK version: " << sdkver << endl;
    bool start = false;

    start_timestamp = get_timestamp();

    string port = jfboard_checkPort();
    if (port.empty()) {
        cout << "Cannot find any JF dongles.\n";
        _getch();
        exit(0);
    }
    else {
        cout << "Found JF dongle on " << port << endl;
    }

    logfile.open("logfile.txt");

    brainpro_install_callback(postData, rawData, battInfo, resp_proc, eventFunc,nullptr);

    jfboard_connect(port);

#if CONNECT_LIST
    vector<ble_device> getDev;
    string test1_str[12] = {
        "F05ECD24F212",
        "F05ECD24F169",
        "F05ECD24F17A",
        "F05ECD24F152",
        "70B950482754",
        "F05ECD24F168",
        "70B950482764",
        "70B95048275F",
        "F05ECD24F16D",
        "F05ECD24F251",
        "F05ECD24F149",
        "70B950482763",

    };

    for (int i = 0; i < 12; i++) {
        ble_device cDevice(test1_str[i], 0);
        getDev.push_back(cDevice);
    }
#else
    jfboard_scan();
    vector<ble_device> getDev = jfboard_getScanDevices();
#endif

    cout << "dev_list size: " << getDev.size() << endl;
    if (logfile) {
        logfile << "Build: " << __DATE__ << "," << __TIME__ << "\n";
        logfile << "SDK version: " << sdkver << "\n";
        logfile << "dev_list size: " << getDev.size() << "\n";
    }

    for (int i = 0; i < getDev.size(); i++) {
        ble_device thisDevice = getDev.at(i);
        cout << i << ": " << thisDevice.getDeviceMac() << "," << thisDevice.getDeviceType() << " " << thisDevice.getDeviceName()
            << "\n";
        if (logfile) {
            logfile << i << ": " << thisDevice.getDeviceMac() << "," << thisDevice.getDeviceType() << " " <<
                thisDevice.getDeviceName()
                << "\n";
        }
    }


#if 1
    if (getDev.size()) {
        cout << "Start connecting ... \n";
        bool rc0 = brainpro_groupConnect(getDev);
        if (rc0) {
            cout << "Connect successfully.\n";
            if (logfile) {
                logfile << "Connect successfully.\n";
            }
            getConnectDev = jfboard_getDevices();
            for (int i = 0; i < getConnectDev.size(); i++) {
                ble_device thisDevice = getConnectDev.at(i);
                cout << thisDevice.getDeviceMac() << "," << thisDevice.getDeviceType() << " " << thisDevice.getDeviceName() << " " <<
                    thisDevice.getConnectIndex()
                    << "\n";
                if (logfile) {
                    logfile << thisDevice.getDeviceMac() << "," << thisDevice.getDeviceType() << " " << thisDevice.getDeviceName() << " " <<
                        thisDevice.getConnectIndex()
                        << "\n";
                }
            }

            device_config();
#if !DIS_TEST
            brainpro_start();
#endif
            start = true;
        }
        else {
            cout << "Failed to connect any devices.\n";
            if (logfile) {
                logfile << "Failed to connect any devices.\n";
            }
        }
        cout << "Done. " << endl;
    }
    else {
        cout << "No device found.\n";
    }
#else
#if 0
    //string constr = "200B16BEB8AC,0 AI70000036";
    string constr = "F05ECD24F316";
    ble_device conpro(constr, 0);
    cout << "Start connecting ... " << endl;
    bool rc = brainpro_connect(conpro); //Use this API if you only connect ONE device.
    if (rc) {
        cout << "Connect OK." << endl;
        device_config();
        brainpro_start();
    }
    else {
        cout << "Connect failed." << endl;
    }
#else

    string test1_str = "F05ECD24F149";
    ble_device m1Device(test1_str, 0);

    string test2_str = "F05ECD24F30A";
    ble_device m2Device(test2_str, 0);

    string test3_str = "F05ECD24F339";
    ble_device m3Device(test3_str, 0);


    vector<ble_device> conDevs;
    conDevs.push_back(m1Device);
    conDevs.push_back(m2Device);
    conDevs.push_back(m3Device);
    cout << "Start connecting " << test1_str << " " << test2_str << " " << test3_str << " " << "... \n";
    bool rc0 = brainpro_groupConnect(conDevs); // use this API if you need to connect more than one device
    // you may use "brainpro_groupAdd" API to connect more devices.
    if (rc0) {
        cout << "Connect successfully.\n";
        if (logfile) {
            logfile << "Connect successfully.\n";
        }
        getConnectDev = jfboard_getConnectedDevices();
        for (int i = 0; i < getConnectDev.size(); i++) {
            ble_device thisDevice = getConnectDev.at(i);
            cout << thisDevice.getDeviceMac() << "," << thisDevice.getDeviceType() << " " << thisDevice.getDeviceName() << " " <<
                thisDevice.getConnectIndex()
                << "\n";
            if (logfile) {
                logfile << thisDevice.getDeviceMac() << "," << thisDevice.getDeviceType() << " " << thisDevice.getDeviceName() << " " <<
                    thisDevice.getConnectIndex()
                    << "\n";
            }
        }

        device_config();
        brainpro_start();
    }
    else {
        cout << "Failed to connect any devices.\n";
        if (logfile) {
            logfile << "Failed to connect any devices.\n";
        }
    }
    cout << "Done. " << endl;


#endif
#endif

#if DIS_TEST
    bg_run = true;
    backgroundThread = thread(test_thread);
#endif

#if RECONNECT_DEMO
    if (start) {
        reconn_run = true;
        reconThread = thread(reconnect_thread);
    }
#endif

#if defined (_WIN32) || defined( _WIN64)
    cout << "Press any key to exit" << endl;
    _getch();
#endif

#if DIS_TEST
    bg_run = false;

    if (backgroundThread.joinable()) {
        backgroundThread.join();
    }
#endif

#if RECONNECT_DEMO
    reconn_run = false;

    if (reconThread.joinable()) {
        reconThread.join();
    }

#endif

    jfsdk_cleanup();
    logfile.close();
    for (int i = 0; i < 16; i++) {
        if (rawFile[i].is_open()) {
            rawFile[i].close();
        }
    }
    return 0;
}


void reconnect_thread()
{

    int wait = 0;
    while (reconn_run) {
        if ((!reconnectDevList.empty()) &&(wait <=0)) {
            lock_guard<mutex> guard(reconMutex);
            for (auto iter{ begin(reconnectDevList) }; iter != end(reconnectDevList); ++iter) {
                reconnect_dev dev = *iter;
                if (dev.mac.size() > 0) {
                    ble_device thisDevice(dev.mac);
                    cout << "Try to reconnect the device. " << dev.mac << (*iter).retry <<"\n";
                    if (logfile) {
                        logfile << "Try to reconnect the device. " << dev.mac <<" retry: " << (*iter).retry << "\n";
                    }
                    brainpro_groupAdd(thisDevice);
                    if (++(*iter).retry > 5) {
                        cout << "remove reconnect device." << "\n";
                        if (logfile) {
                            logfile << "remove reconnect device." << "\n";
                        }
                        reconnectDevList.erase(iter);
                        break;
                    }
                    jfsdk_sleep(20);
                    wait = 200;
                    break;
                }
            }
        }
        jfsdk_sleep(100);
        if (wait > 0) wait--;
    }

}


void test_thread()
{
    enum {
        STATE_IDLE = 0,
        STATE_DISC,
        STATE_WAIT1,
        STATE_RECONN,
        STATE_WAIT2,
        STATE_RETRY,
        STATE_LOOP,
    };

    int32_t bg_state = STATE_IDLE;
    int32_t counter = 0;
    brainpro_start();

    ble_device testDevice;


    int retry = 0;

    while (bg_run) {

        switch (bg_state) {
        case STATE_IDLE:
            if (counter > 300) {
                bg_state = STATE_DISC;
            }
            break;

        case STATE_DISC:
            if (getConnectDev.empty()) {
                bg_state = STATE_LOOP;
                break;
            }

            for (int i = 0; i < getConnectDev.size(); i++) {
                ble_device thisDevice = getConnectDev.at(i);
                //find the first connected device and disconnect it for test.
                if (thisDevice.getDeviceState() == ble_device::connected) {
                    cout << "Disconnect a test device. " << thisDevice.getDeviceMac() << "\n";
                    bool rc = brainpro_disconnect(thisDevice);
                    if (rc == true) {
                        testDevice = thisDevice;
                        break;
                    }
                }
            }
            counter = 0;
            bg_state = STATE_WAIT1;
            break;

        case STATE_WAIT1:
            if (counter > 300) {
                bg_state = STATE_RECONN;
            }
            break;

        case STATE_RECONN:
            cout << "Try to connect the test device. " << testDevice.getDeviceMac() << "\n";
            brainpro_groupAdd(testDevice);  //reconnect the test device with this API
            counter = 0;
            bg_state = STATE_WAIT2;
            break;

        case STATE_WAIT2:
            if (counter > 100) {
                counter = 0;
                bg_state = STATE_RETRY;
            }
            break;

        case STATE_RETRY:
            if (brainpro_updateDeviceState(testDevice) == ble_device::disconnected) {
                if (counter > 100) {
                    cout << "Connect retry " << retry << "\n";
                    brainpro_groupAdd(testDevice);  //retry if the previous connection was not successful.
                    retry++;
                    counter = 0;
                }
            }
            else {
                bg_state = STATE_LOOP;
            }
            if (retry > 20) {
                bg_state = STATE_LOOP;
            }
            break;

        case STATE_LOOP:
            break;

        }

        jfsdk_sleep(100);
        counter++;
    }


}