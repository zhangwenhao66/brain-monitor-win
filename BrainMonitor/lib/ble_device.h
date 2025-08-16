
/**************************************************************************
 *
 *  Copyright (C) 2022 -
 *  Shenzhen Shuimu AI Technology Co.LTD.  All rights reserved.           *
 *
 *************************************************************************/


#pragma once

#include <string>

namespace jfbrnpro_if
{

	class ble_device
	{
	public:
		enum dev_state {
			disconnected = 0,
			connected = 1,
		};

	private:
		std::string m_mac;
		std::string m_name;
		int m_type = 0;
		int m_index = 0;
		dev_state m_state = disconnected;

	public:
		ble_device(std::string& mac, std::string& name, int type, int index = 0);
		ble_device(std::string& mac, int type, int index = 0);
		ble_device(std::string& line);
		ble_device();

		std::string getDeviceMac();
		std::string getDeviceName();
		int getDeviceType();
		int getConnectIndex();
		void setConnectIndex(int idx);
		void updateDeviceName(const std::string& name);
		dev_state getDeviceState();
		void setDeviceState(dev_state state);

	};

}