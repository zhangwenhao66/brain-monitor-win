using System;
using System.Runtime.InteropServices;
using System.Text;

namespace BrainMonitor.SDK
{
    // 设备信息结构体
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct DeviceInfo
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string Mac;
        
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string Name;
        
        public int Type;
        public int Index;
        public int State; // 0=disconnected, 1=connected
    }

    // 回调函数委托
    public delegate void RawDataCallback(int dev, int chan, IntPtr data, int len);
    public delegate void PostDataCallback(int dev, byte ele, byte att, byte med, byte res, IntPtr psd);
    public delegate void BattInfoCallback(int dev, uint level, uint vol);
    public delegate void EventCallback(uint eventType, uint param);

    public static class BrainMonitorSDK
    {
        private const string DllName = "BrainMonitorSDK.dll";
        
        // 检查DLL是否可用
        private static bool _dllAvailable = false;
        
        static BrainMonitorSDK()
        {
            try
            {
                // 尝试加载DLL
                var handle = LoadLibrary(DllName);
                _dllAvailable = handle != IntPtr.Zero;
                if (handle != IntPtr.Zero)
                {
                    FreeLibrary(handle);
                }
            }
            catch
            {
                _dllAvailable = false;
            }
        }
        
        public static bool IsDllAvailable => _dllAvailable;
        
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr LoadLibrary(string lpFileName);
        
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool FreeLibrary(IntPtr hModule);

        // SDK初始化和清理
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int SDK_Init();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void SDK_Cleanup();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr SDK_GetVersion();

        // 端口操作
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr SDK_CheckPort();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int SDK_ConnectPort([MarshalAs(UnmanagedType.LPStr)] string port);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void SDK_DisconnectPort();

        // 设备扫描和连接
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int SDK_ScanDevices();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int SDK_GetScanDevicesCount();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int SDK_GetScanDevice(int index, ref DeviceInfo device);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int SDK_ConnectDevice([MarshalAs(UnmanagedType.LPStr)] string mac, int type);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int SDK_DisconnectDevice([MarshalAs(UnmanagedType.LPStr)] string mac);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int SDK_GetConnectedDevicesCount();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int SDK_GetConnectedDevice(int index, ref DeviceInfo device);

        // 数据采集
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int SDK_StartDataCollection();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int SDK_StopDataCollection();

        // 回调函数设置
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void SDK_SetRawDataCallback(RawDataCallback callback);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void SDK_SetPostDataCallback(PostDataCallback callback);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void SDK_SetBattInfoCallback(BattInfoCallback callback);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void SDK_SetEventCallback(EventCallback callback);

        // 设备控制
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int SDK_SendCommand(int dev, byte cmd);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int SDK_SendCommandWithPayload(int dev, byte cmd, IntPtr payload, byte len);

        // 辅助方法
        public static string GetVersionString()
        {
            IntPtr ptr = SDK_GetVersion();
            return ptr != IntPtr.Zero ? Marshal.PtrToStringAnsi(ptr) : string.Empty;
        }

        public static string CheckPortString()
        {
            IntPtr ptr = SDK_CheckPort();
            return ptr != IntPtr.Zero ? Marshal.PtrToStringAnsi(ptr) : string.Empty;
        }
    }
}