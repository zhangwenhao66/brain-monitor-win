using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using BrainMonitor.SDK;
using System.Runtime.InteropServices;
using System.IO.Ports;

namespace BrainMonitor.Views
{
    public partial class TestPage : UserControl, INavigationAware
    {
        public Tester CurrentTester { get; set; }
        private double currentX = 0;
        private bool sdkInitialized = false;
        
        // 数据缓冲相关变量
        private Queue<int[]> dataBuffer = new Queue<int[]>();
        private DispatcherTimer displayTimer;
        private readonly object bufferLock = new object();
        private List<DeviceInfo> scannedDevices = new List<DeviceInfo>();
        private List<DeviceInfo> connectedDevices = new List<DeviceInfo>();
        
        // 时间坐标相关变量
        private DateTime startTime = DateTime.Now;
        private bool isDeviceConnected = false;
        private const double DISPLAY_WINDOW_SECONDS = 8.0; // 显示8秒窗口
        private DispatcherTimer? timeUpdateTimer; // 时间标签更新定时器
        
        // 回调函数引用，防止被垃圾回收
        private RawDataCallback? rawDataCallback;
        private PostDataCallback? postDataCallback;
        private BattInfoCallback? battInfoCallback;
        private EventCallback? eventCallback;
        
        // 数据接收统计变量
        private DateTime lastDataReceiveTime = DateTime.MinValue;
        private int totalDataPointsReceived = 0;
        private int dataReceiveCount = 0;
        private DateTime firstDataReceiveTime = DateTime.MinValue;
        
        // 显示更新频率控制
        private const int DISPLAY_UPDATE_INTERVAL_MS = 50; // 20Hz显示更新频率
        
        private void InitializeDisplayTimer()
        {
            displayTimer = new DispatcherTimer();
            displayTimer.Interval = TimeSpan.FromMilliseconds(DISPLAY_UPDATE_INTERVAL_MS);
            displayTimer.Tick += DisplayTimer_Tick;
        }
        
        private void DisplayTimer_Tick(object sender, EventArgs e)
        {
            // 处理缓冲队列中的数据
            List<int[]> dataToProcess = new List<int[]>();
            
            lock (bufferLock)
            {
                while (dataBuffer.Count > 0)
                {
                    dataToProcess.Add(dataBuffer.Dequeue());
                }
            }
            
            // 更新显示
            foreach (var data in dataToProcess)
            {
                UpdateBrainwaveDisplay(data);
            }
        }

        public TestPage(Tester tester)
        {
            InitializeComponent();
            CurrentTester = tester;
            DataContext = this;
            
            // 初始化时禁用设备列表和连接按钮
            DeviceComboBox.IsEnabled = false;
            ConnectDeviceButton.IsEnabled = false;
            
            // 初始化SDK
            InitializeSDK();
            
            // 初始化显示定时器
            InitializeDisplayTimer();
            
            // 页面加载完成后初始化画布
            this.Loaded += TestPage_Loaded;
        }
        
        private void StartTimeUpdateTimer()
        {
            // 停止现有的定时器
            StopTimeUpdateTimer();
            
            // 创建新的时间更新定时器
            timeUpdateTimer = new DispatcherTimer();
            timeUpdateTimer.Interval = TimeSpan.FromMilliseconds(100); // 每100毫秒更新一次
            timeUpdateTimer.Tick += (s, e) =>
            {
                if (isDeviceConnected)
                {
                    DrawTimeLabels();
                }
            };
            timeUpdateTimer.Start();
        }
        
        private void StopTimeUpdateTimer()
        {
            if (timeUpdateTimer != null)
            {
                timeUpdateTimer.Stop();
                timeUpdateTimer = null;
            }
        }
        
        private void DrawTimeLabels()
        {
            // 清除之前的时间标签
            XAxisLabelsCanvas.Children.Clear();
            
            double width = BrainwaveCanvas.ActualWidth;
            if (width <= 0) return;
            
            // 计算当前时间偏移
            double currentTimeOffset = isDeviceConnected ? 
                (DateTime.Now - startTime).TotalSeconds : 0;
            
            // 绘制时间刻度（每秒一个标签）
            for (int i = 0; i <= (int)DISPLAY_WINDOW_SECONDS; i++)
            {
                double timeInSeconds = -DISPLAY_WINDOW_SECONDS + i + currentTimeOffset;
                double x = (i / DISPLAY_WINDOW_SECONDS) * width;
                
                // 格式化时间显示
                string timeText;
                if (timeInSeconds < 0)
                {
                    TimeSpan negativeTime = TimeSpan.FromSeconds(-timeInSeconds);
                    timeText = $"-{negativeTime.Hours:00}:{negativeTime.Minutes:00}:{negativeTime.Seconds:00}";
                }
                else
                {
                    TimeSpan positiveTime = TimeSpan.FromSeconds(timeInSeconds);
                    timeText = $"{positiveTime.Hours:00}:{positiveTime.Minutes:00}:{positiveTime.Seconds:00}";
                }
                
                // 创建时间标签
                TextBlock timeLabel = new TextBlock
                {
                    Text = timeText,
                    FontSize = 10,
                    FontFamily = new FontFamily("Segoe UI"),
                    Foreground = new SolidColorBrush(Color.FromRgb(100, 100, 100)),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Tag = "time_label"
                };
                
                // 计算标签位置，确保不超出画布边界
                double labelWidth = 40; // 估算标签宽度
                double labelLeft = Math.Max(0, Math.Min(width - labelWidth, x - 25));
                Canvas.SetLeft(timeLabel, labelLeft);
                Canvas.SetTop(timeLabel, 5);
                XAxisLabelsCanvas.Children.Add(timeLabel);
                
                // 绘制时间刻度线
                Line tickLine = new Line
                {
                    X1 = x, Y1 = 0,
                    X2 = x, Y2 = 8,
                    Stroke = new SolidColorBrush(Color.FromRgb(150, 150, 150)),
                    StrokeThickness = 1,
                    Tag = "time_tick"
                };
                XAxisLabelsCanvas.Children.Add(tickLine);
            }
        }
        
        private void TestPage_Loaded(object sender, RoutedEventArgs e)
        {
            // 初始化画布显示栅格和绿色虚线
            InitializeCanvas();
            
            // 初始化端口列表
            RefreshPortList();
        }
        
        private void InitializeCanvas()
        {
            // 清空画布并设置背景
            BrainwaveCanvas.Children.Clear();
            BrainwaveCanvas.Background = new SolidColorBrush(Color.FromRgb(245, 245, 245)); // 浅灰色背景
            
            // 绘制网格和标准线
            DrawGridAndBaseline();
        }
        
        private void RefreshPortList()
        {
            try
            {
                // 获取当前选中的端口
                string selectedPort = PortComboBox.SelectedItem?.ToString();
                
                // 清空端口列表
                PortComboBox.Items.Clear();
                
                // 获取系统中所有可用的串口
                string[] availablePorts = SerialPort.GetPortNames();
                
                // 调试信息：显示找到的端口数量
                System.Diagnostics.Debug.WriteLine($"RefreshPortList: 找到 {availablePorts.Length} 个端口");
                
                if (availablePorts.Length > 0)
                {
                    // 添加可用端口到列表
                    foreach (string port in availablePorts)
                    {
                        PortComboBox.Items.Add(port);
                    }
                    
                    // 尝试恢复之前选中的端口
                    if (!string.IsNullOrEmpty(selectedPort) && PortComboBox.Items.Contains(selectedPort))
                    {
                        PortComboBox.SelectedItem = selectedPort;
                    }
                    else if (PortComboBox.Items.Count > 0)
                    {
                        // 默认选择第一个端口
                        PortComboBox.SelectedIndex = 0;
                    }
                }
                // 如果没有找到串口，保持列表为空
            }
            catch (Exception ex)
            {
                // 如果获取端口失败，显示错误信息
                PortComboBox.Items.Clear();
                PortComboBox.Items.Add($"获取端口失败: {ex.Message}");
                PortComboBox.SelectedIndex = 0;
                
                // 显示详细错误信息给用户
                MessageBox.Show($"获取串口列表失败：\n{ex.Message}\n\n可能的原因：\n1. 系统权限不足\n2. 串口驱动程序问题\n3. 系统资源不足", 
                    "端口获取错误", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        
        private void InitializeSDK()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("开始初始化SDK...");
                
                // 检查DLL是否可用
                if (!BrainMonitorSDK.IsDllAvailable)
                {
                    System.Diagnostics.Debug.WriteLine("DLL不可用");
                    MessageBox.Show("BrainMonitorSDK.dll不可用，无法使用设备功能", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                
                System.Diagnostics.Debug.WriteLine("DLL可用，调用SDK_Init...");
                int result = BrainMonitorSDK.SDK_Init();
                System.Diagnostics.Debug.WriteLine($"SDK_Init返回值: {result}");
                
                if (result == 1)
                {
                    sdkInitialized = true;
                    System.Diagnostics.Debug.WriteLine("SDK初始化成功");
                    
                    // 设置回调函数
                    rawDataCallback = OnRawDataReceived;
                    postDataCallback = OnPostDataReceived;
                    battInfoCallback = OnBatteryInfoReceived;
                    eventCallback = OnEventReceived;
                    
                    BrainMonitorSDK.SDK_SetRawDataCallback(rawDataCallback);
                    BrainMonitorSDK.SDK_SetPostDataCallback(postDataCallback);
                    BrainMonitorSDK.SDK_SetBattInfoCallback(battInfoCallback);
                    BrainMonitorSDK.SDK_SetEventCallback(eventCallback);
                    
                    // 检查端口
                    string port = BrainMonitorSDK.CheckPortString();
                    System.Diagnostics.Debug.WriteLine($"检测到端口: {port}");
                    if (!string.IsNullOrEmpty(port))
                    {
                        int connectResult = BrainMonitorSDK.SDK_ConnectPort(port);
                        System.Diagnostics.Debug.WriteLine($"端口连接结果: {connectResult}");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("SDK初始化失败");
                    MessageBox.Show($"SDK初始化失败，返回值: {result}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SDK初始化异常: {ex.Message}");
                MessageBox.Show($"SDK初始化异常: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void OnRawDataReceived(int dev, int chan, IntPtr data, int len)
        {
            try
            {
                DateTime currentTime = DateTime.Now;
                
                // 统计数据接收情况
                dataReceiveCount++;
                totalDataPointsReceived += len;
                
                if (firstDataReceiveTime == DateTime.MinValue)
                {
                    firstDataReceiveTime = currentTime;
                    lastDataReceiveTime = currentTime;
                }
                
                // 计算接收间隔
                double intervalMs = 0;
                if (lastDataReceiveTime != DateTime.MinValue)
                {
                    intervalMs = (currentTime - lastDataReceiveTime).TotalMilliseconds;
                }
                
                // 计算总体频率
                double totalElapsedSeconds = (currentTime - firstDataReceiveTime).TotalSeconds;
                double overallFrequency = totalElapsedSeconds > 0 ? totalDataPointsReceived / totalElapsedSeconds : 0;
                
                // 计算当前批次的等效频率（假设这批数据代表的时间间隔）
                double currentBatchFrequency = intervalMs > 0 ? (len * 1000.0 / intervalMs) : 0;
                
                // 详细日志输出
                System.Diagnostics.Debug.WriteLine($"=== 数据接收统计 ===");
                System.Diagnostics.Debug.WriteLine($"时间: {currentTime:HH:mm:ss.fff}");
                System.Diagnostics.Debug.WriteLine($"设备: {dev}, 通道: {chan}, 数据点数: {len}");
                System.Diagnostics.Debug.WriteLine($"接收间隔: {intervalMs:F1}ms");
                System.Diagnostics.Debug.WriteLine($"当前批次等效频率: {currentBatchFrequency:F1}Hz");
                System.Diagnostics.Debug.WriteLine($"总接收次数: {dataReceiveCount}");
                System.Diagnostics.Debug.WriteLine($"总数据点数: {totalDataPointsReceived}");
                System.Diagnostics.Debug.WriteLine($"总体平均频率: {overallFrequency:F1}Hz");
                
                // 将原始数据转换为可用的数据
                int[] rawData = new int[len];
                Marshal.Copy(data, rawData, 0, len);
                
                // 打印前几个数据点用于调试
                if (len > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"数据样本: {rawData[0]}, {(len > 1 ? rawData[1].ToString() : "N/A")}, {(len > 2 ? rawData[2].ToString() : "N/A")}");
                }
                System.Diagnostics.Debug.WriteLine($"===================");
                
                lastDataReceiveTime = currentTime;
                
                // 将数据放入缓冲队列
                lock (bufferLock)
                {
                    dataBuffer.Enqueue(rawData);
                    
                    // 限制缓冲队列大小，避免内存溢出
                    while (dataBuffer.Count > 100)
                    {
                        dataBuffer.Dequeue();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"数据处理异常: {ex.Message}");
            }
        }
        
        private void OnPostDataReceived(int dev, byte ele, byte att, byte med, byte res, IntPtr psd)
        {
            // 处理后的数据回调
        }
        
        private void OnBatteryInfoReceived(int dev, uint level, uint vol)
        {
            // 电池信息回调
            Dispatcher.BeginInvoke(() =>
            {
                // 可以在UI上显示电池信息
            });
        }
        
        private void OnEventReceived(uint eventType, uint param)
        {
            // 事件回调
            Dispatcher.BeginInvoke(() =>
            {
                // 处理设备事件
            });
        }
        
        private void UpdateBrainwaveDisplay(int[] rawData)
        {
            if (rawData == null || rawData.Length == 0) return;
            
            // 如果是第一次接收数据，设置设备连接状态和开始时间
            if (!isDeviceConnected)
            {
                isDeviceConnected = true;
                startTime = DateTime.Now;
            }
            
            // 使用真实数据更新显示
            double baselineY = BrainwaveCanvas.ActualHeight / 2;
            double canvasWidth = BrainwaveCanvas.ActualWidth;
            
            // 调整缩放因子以适应-400到400微伏的范围
            double microVoltRange = 800.0; // 总范围800微伏
            double scale = BrainwaveCanvas.ActualHeight / microVoltRange;
            
            // 使用实际的数据接收频率计算像素移动距离
            // 根据日志分析，平均频率约520Hz，每次接收112个数据点
            double actualSamplingRate = totalDataPointsReceived > 0 && dataReceiveCount > 0 ? 
                (totalDataPointsReceived / ((DateTime.Now - firstDataReceiveTime).TotalSeconds)) : 520.0;
            double pixelsPerDataPoint = canvasWidth / (DISPLAY_WINDOW_SECONDS * actualSamplingRate);
            double moveDistance = rawData.Length * pixelsPerDataPoint;
            MoveWaveformLeftByPixels(moveDistance);
            
            // 获取最后一个数据点的位置（用于连线）
            double lastX = canvasWidth;
            double lastY = baselineY;
            bool hasLastPoint = false;
            
            // 查找最右边的波形点
            foreach (UIElement element in BrainwaveCanvas.Children)
            {
                if (element is FrameworkElement fe && fe.Tag?.ToString() == "waveform")
                {
                    if (element is Line line && Canvas.GetLeft(line) + line.X2 > lastX - moveDistance)
                    {
                        lastX = line.X2;
                        lastY = line.Y2;
                        hasLastPoint = true;
                    }
                    else if (element is Ellipse ellipse)
                    {
                        double ellipseX = Canvas.GetLeft(ellipse) + ellipse.Width / 2;
                        if (ellipseX > lastX - moveDistance)
                        {
                            lastX = ellipseX;
                            lastY = Canvas.GetTop(ellipse) + ellipse.Height / 2;
                            hasLastPoint = true;
                        }
                    }
                }
            }
            
            // 逐点绘制新数据
            for (int i = 0; i < rawData.Length; i++)
            {
                // 将原始数据转换为微伏值
                double microVoltValue = rawData[i];
                
                // 计算Y坐标（向下为正，所以需要反转）
                double y = baselineY - (microVoltValue * scale);
                
                // 确保Y坐标在画布范围内
                y = Math.Max(10, Math.Min(BrainwaveCanvas.ActualHeight - 10, y));
                
                // 计算X坐标（新数据从右边开始）
                double x = canvasWidth - (rawData.Length - i) * pixelsPerDataPoint;
                
                // 绘制连线或点
                if (hasLastPoint || i > 0)
                {
                    double prevX = i == 0 ? lastX : canvasWidth - (rawData.Length - i + 1) * pixelsPerDataPoint;
                    double prevY = i == 0 ? lastY : baselineY - (rawData[i-1] * scale);
                    prevY = Math.Max(10, Math.Min(BrainwaveCanvas.ActualHeight - 10, prevY));
                    
                    Line line = new Line
                    {
                        X1 = prevX,
                        Y1 = prevY,
                        X2 = x,
                        Y2 = y,
                        Stroke = new SolidColorBrush(Color.FromRgb(0, 120, 215)),
                        StrokeThickness = 1.5,
                        Tag = "waveform"
                    };
                    BrainwaveCanvas.Children.Add(line);
                }
                else
                {
                    // 第一个点
                    Ellipse point = new Ellipse
                    {
                        Width = 2,
                        Height = 2,
                        Fill = new SolidColorBrush(Color.FromRgb(0, 120, 215)),
                        Tag = "waveform"
                    };
                    Canvas.SetLeft(point, x - 1);
                    Canvas.SetTop(point, y - 1);
                    BrainwaveCanvas.Children.Add(point);
                }
                
                hasLastPoint = true;
            }
            
            // 清理超出左边界的线条
            CleanupLeftBoundary();
            
            // 更新时间标签
            DrawTimeLabels();
        }

        private void ClearWaveformData()
        {
            // 只清除波形数据，保留栅格和基线
            var itemsToRemove = new List<UIElement>();
            
            foreach (UIElement element in BrainwaveCanvas.Children)
            {
                string tag = element.GetValue(FrameworkElement.TagProperty)?.ToString() ?? "";
                if (tag == "waveform")
                {
                    itemsToRemove.Add(element);
                }
            }
            
            foreach (var item in itemsToRemove)
            {
                BrainwaveCanvas.Children.Remove(item);
            }
        }
        
        private void ConfigureDevice()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("开始配置设备...");
                
                // 发送设备配置命令（参考demo中的device_config函数）
                BrainMonitorSDK.SDK_SendCommand(0, 0xEC);
                BrainMonitorSDK.SDK_SendCommand(0, 0xEF);
                BrainMonitorSDK.SDK_SendCommand(0, 0x04);
                
                // 发送带payload的命令
                byte[] payload = { 0x00, 0x00, 0x01 };
                IntPtr payloadPtr = Marshal.AllocHGlobal(payload.Length);
                Marshal.Copy(payload, 0, payloadPtr, payload.Length);
                BrainMonitorSDK.SDK_SendCommandWithPayload(0, 0x2B, payloadPtr, (byte)payload.Length);
                Marshal.FreeHGlobal(payloadPtr);
                
                BrainMonitorSDK.SDK_SendCommand(0, 0xFF);
                
                System.Diagnostics.Debug.WriteLine("设备配置完成");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"设备配置异常: {ex.Message}");
            }
        }

        public void OnNavigatedTo()
        {
            // 页面导航到时的处理
        }

        public void OnNavigatedFrom()
        {
            // 页面离开时的处理
            StopTimeUpdateTimer();
            
            // 停止显示定时器
            displayTimer?.Stop();
            
            // 清空缓冲队列
            lock (bufferLock)
            {
                dataBuffer.Clear();
            }
            
            // 页面离开时才真正清理SDK
            CleanupSDK();
        }
        
        private void CleanupSDK()
        {
            try
            {
                if (sdkInitialized)
                {
                    // 停止数据采集
                    BrainMonitorSDK.SDK_StopDataCollection();
                    
                    // 断开所有设备
                    foreach (var device in connectedDevices)
                    {
                        BrainMonitorSDK.SDK_DisconnectDevice(device.Mac);
                    }
                    connectedDevices.Clear();
                    
                    // 断开端口
                    BrainMonitorSDK.SDK_DisconnectPort();
                    
                    // 清理SDK
                    BrainMonitorSDK.SDK_Cleanup();
                    sdkInitialized = false;
                }
            }
            catch (Exception ex)
            {
                // 忽略清理过程中的异常
                System.Diagnostics.Debug.WriteLine($"SDK清理异常: {ex.Message}");
            }
        }

        private void ScanDeviceButton_Click(object sender, RoutedEventArgs e)
        {
            // 添加调试信息
            string debugInfo = $"DLL可用: {BrainMonitorSDK.IsDllAvailable}, SDK已初始化: {sdkInitialized}";
            System.Diagnostics.Debug.WriteLine(debugInfo);
            
            // 检查DLL是否可用，如果不可用则使用模拟模式
            if (!BrainMonitorSDK.IsDllAvailable || !sdkInitialized)
            {
                // 使用模拟设备扫描
                DeviceComboBox.Items.Clear();
                DeviceComboBox.Items.Add("模拟设备 1 (00:11:22:33:44:55)");
                DeviceComboBox.Items.Add("模拟设备 2 (AA:BB:CC:DD:EE:FF)");
                DeviceComboBox.IsEnabled = true;
                
                // 默认选中第一个设备
                if (DeviceComboBox.Items.Count > 0)
                {
                    DeviceComboBox.SelectedIndex = 0;
                }
                
                // 不再弹窗，直接在设备列表显示
                return;
            }
            
            try
            {
                // 确保SDK已初始化
                if (!sdkInitialized)
                {
                    MessageBox.Show("SDK未初始化，请先初始化SDK", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                
                // 确保端口连接状态正确
                string selectedPort = PortComboBox.SelectedItem?.ToString();
                if (string.IsNullOrEmpty(selectedPort))
                {
                    MessageBox.Show("请先选择一个端口", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                
                System.Diagnostics.Debug.WriteLine($"尝试使用端口: {selectedPort}");
                
                try
                {
                    // 检查端口是否已连接，如果没有连接则连接
                    int connectResult = BrainMonitorSDK.SDK_ConnectPort(selectedPort);
                    System.Diagnostics.Debug.WriteLine($"端口连接结果: {connectResult}");
                    
                    if (connectResult != 1)
                    {
                        // 如果连接失败，尝试先断开再重连
                        BrainMonitorSDK.SDK_DisconnectPort();
                        System.Threading.Thread.Sleep(100);
                        connectResult = BrainMonitorSDK.SDK_ConnectPort(selectedPort);
                        
                        if (connectResult != 1)
                        {
                            MessageBox.Show($"连接端口 {selectedPort} 失败，无法扫描设备\n连接结果: {connectResult}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                    }
                }
                catch (Exception portEx)
                {
                    MessageBox.Show($"端口连接异常: {portEx.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                
                // 使用SDK扫描设备
                int result = BrainMonitorSDK.SDK_ScanDevices();
                if (result == 1)
                {
                    // 获取扫描到的设备数量
                    int deviceCount = BrainMonitorSDK.SDK_GetScanDevicesCount();
                    
                    DeviceComboBox.Items.Clear();
                    scannedDevices.Clear();
                    
                    for (int i = 0; i < deviceCount; i++)
                    {
                        DeviceInfo device = new DeviceInfo();
                        if (BrainMonitorSDK.SDK_GetScanDevice(i, ref device) == 1)
                        {
                            scannedDevices.Add(device);
                            // 去掉设备名称前面的空格
                            string deviceName = device.Name?.Trim() ?? "未知设备";
                            string displayName = $"{deviceName} ({device.Mac})";
                            DeviceComboBox.Items.Add(displayName);
                        }
                    }
                    
                    // 启用设备列表
                    DeviceComboBox.IsEnabled = true;
                    
                    // 默认选中第一个设备
                    if (deviceCount > 0)
                    {
                        DeviceComboBox.SelectedIndex = 0;
                    }
                    
                    // 不再弹窗提示，直接在设备列表显示结果
                }
                else
                {
                    MessageBox.Show("设备扫描失败", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"扫描设备异常: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeviceComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // 当选择设备时，根据连接状态更新按钮状态
            if (!isDeviceConnected)
            {
                ConnectDeviceButton.IsEnabled = DeviceComboBox.SelectedItem != null;
            }
        }
        
        private void PortComboBox_DropDownOpened(object sender, EventArgs e)
        {
            // 每次打开下拉列表时刷新端口列表
            RefreshPortList();
        }

        private void ConnectDeviceButton_Click(object sender, RoutedEventArgs e)
        {
            if (DeviceComboBox.SelectedItem == null)
            {
                MessageBox.Show("请先选择一个设备", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            // 检查SDK是否可用
            if (!BrainMonitorSDK.IsDllAvailable || !sdkInitialized)
            {
                MessageBox.Show("SDK未初始化或不可用，无法连接设备", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            
            try
            {
                int selectedIndex = DeviceComboBox.SelectedIndex;
                if (selectedIndex >= 0 && selectedIndex < scannedDevices.Count)
                {
                    DeviceInfo selectedDevice = scannedDevices[selectedIndex];
                    
                    // 使用SDK连接设备
                    int result = BrainMonitorSDK.SDK_ConnectDevice(selectedDevice.Mac, selectedDevice.Type);
                    if (result == 1)
                    {
                        connectedDevices.Add(selectedDevice);
                        // 连接成功，不再弹窗提示
                        
                        // 设置设备连接状态
                        isDeviceConnected = true;
                        startTime = DateTime.Now;
                        
                        // 更新按钮状态
                        UpdateButtonStates(true);
                        
                        // 启动时间更新定时器
                        StartTimeUpdateTimer();
                        
                        // 开始数据采集
                        StartDataCollection();
                    }
                    else
                    {
                        MessageBox.Show("设备连接失败", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"连接设备异常: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void DisconnectDeviceButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 检查SDK是否可用
                if (!BrainMonitorSDK.IsDllAvailable || !sdkInitialized)
                {
                    MessageBox.Show("SDK未初始化或不可用", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                
                // 断开所有已连接的设备
                bool hasDisconnected = false;
                foreach (var device in connectedDevices.ToList())
                {
                    int result = BrainMonitorSDK.SDK_DisconnectDevice(device.Mac);
                    if (result == 1)
                    {
                        hasDisconnected = true;
                        connectedDevices.Remove(device);
                    }
                }
                
                if (hasDisconnected || connectedDevices.Count == 0)
                {
                    // 断开连接成功，不再弹窗提示
                    
                    // 停止数据采集
                    StopDataCollection();
                    
                    // 设置设备连接状态
                    isDeviceConnected = false;
                    
                    // 更新按钮状态
                    UpdateButtonStates(false);
                    
                    // 停止时间更新定时器
                    StopTimeUpdateTimer();
                    
                    // 清空设备列表，需要重新扫描
                    ClearDeviceList();
                }
                else
                {
                    MessageBox.Show("断开连接失败", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"断开连接异常: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void UpdateButtonStates(bool isConnected)
        {
            // 连接状态下：禁用连接按钮，启用断开连接按钮
            // 断开状态下：启用连接按钮（如果有选中设备），禁用断开连接按钮
            ConnectDeviceButton.IsEnabled = !isConnected && DeviceComboBox.SelectedItem != null;
            DisconnectDeviceButton.IsEnabled = isConnected;
            
            // 连接状态下禁用设备列表，断开状态下根据是否有设备来决定
            DeviceComboBox.IsEnabled = !isConnected && DeviceComboBox.Items.Count > 0;
        }
        
        private void ClearDeviceList()
        {
            // 清空设备列表
            DeviceComboBox.Items.Clear();
            DeviceComboBox.SelectedItem = null;
            DeviceComboBox.IsEnabled = false;
            scannedDevices.Clear();
        }

        private void StartDataCollection()
        {
            try
            {
                // 清除之前的波形数据，但保留栅格和基线
                ClearWaveformData();
                
                currentX = BrainwaveCanvas.ActualWidth; // 从右侧开始
                
                // 先进行设备配置（这是关键步骤！）
                ConfigureDevice();
                
                // 使用SDK开始数据采集
                int result = BrainMonitorSDK.SDK_StartDataCollection();
                if (result == 1)
                {
                    System.Diagnostics.Debug.WriteLine("数据采集已启动，等待回调数据...");
                    // 数据采集成功启动，数据将通过回调函数接收
                    // 启动显示定时器
                    displayTimer?.Start();
                }
                else
                {
                    MessageBox.Show("启动数据采集失败", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"启动数据采集异常: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        

        
        private void DrawGridAndBaseline()
        {
            double width = BrainwaveCanvas.ActualWidth;
            double height = BrainwaveCanvas.ActualHeight;
            
            // 清除之前的标签
            YAxisLabelsCanvas.Children.Clear();
            
            // 绘制网格线
            var gridBrush = new SolidColorBrush(Color.FromRgb(220, 220, 220));
            
            // 垂直网格线
            for (double x = 0; x <= width; x += 40)
            {
                Line gridLine = new Line
                {
                    X1 = x, Y1 = 0,
                    X2 = x, Y2 = height,
                    Stroke = gridBrush,
                    StrokeThickness = 0.5,
                    Tag = "grid"
                };
                BrainwaveCanvas.Children.Add(gridLine);
            }
            
            // 水平网格线和刻度标签
            double microVoltRange = 800.0; // -400到+400微伏
            double scale = height / microVoltRange;
            double centerY = height / 2;
            
            // 绘制主要刻度线（每100微伏一条）
            for (int voltage = -400; voltage <= 400; voltage += 100)
            {
                double y = centerY - (voltage * scale);
                
                if (y >= 0 && y <= height)
                {
                    // 主刻度线
                    Line gridLine = new Line
                    {
                        X1 = 0, Y1 = y,
                        X2 = width, Y2 = y,
                        Stroke = voltage == 0 ? new SolidColorBrush(Color.FromRgb(0, 150, 0)) : gridBrush,
                        StrokeThickness = voltage == 0 ? 2 : (voltage % 200 == 0 ? 1 : 0.5),
                        StrokeDashArray = voltage == 0 ? new DoubleCollection { 5, 3 } : null,
                        Tag = voltage == 0 ? "baseline" : "grid"
                    };
                    BrainwaveCanvas.Children.Add(gridLine);
                    
                    // 在左侧Canvas添加刻度标签
                    if (voltage % 200 == 0) // 每200微伏显示一个标签
                    {
                        TextBlock label = new TextBlock
                        {
                            Text = $"{voltage}μV",
                            FontSize = 10,
                            Foreground = new SolidColorBrush(Color.FromRgb(100, 100, 100)),
                            HorizontalAlignment = HorizontalAlignment.Right,
                            Tag = "scale_label"
                        };
                        Canvas.SetRight(label, 5); // 右对齐到左侧Canvas
                        
                        // 确保标签不会被遮挡，特别是顶部和底部的标签
                        double labelTop = Math.Max(0, Math.Min(YAxisLabelsCanvas.ActualHeight - 16, y - 8));
                        Canvas.SetTop(label, labelTop);
                        YAxisLabelsCanvas.Children.Add(label);
                    }
                }
            }
            
            // 绘制次要刻度线（每50微伏一条）
            for (int voltage = -350; voltage <= 350; voltage += 100)
            {
                double y = centerY - (voltage * scale);
                
                if (y >= 0 && y <= height)
                {
                    Line minorGridLine = new Line
                    {
                        X1 = 0, Y1 = y,
                        X2 = width, Y2 = y,
                        Stroke = new SolidColorBrush(Color.FromRgb(240, 240, 240)),
                        StrokeThickness = 0.3,
                        Tag = "grid"
                    };
                    BrainwaveCanvas.Children.Add(minorGridLine);
                }
            }
            
            // 绘制横坐标时间标签
            DrawTimeLabels();
        }
        
        private void MoveWaveformLeftByPixels(double moveDistance)
        {
            var elementsToRemove = new List<UIElement>();
            
            foreach (UIElement element in BrainwaveCanvas.Children)
            {
                if (element is FrameworkElement fe && fe.Tag?.ToString() == "waveform")
                {
                    if (element is Line line)
                    {
                        line.X1 -= moveDistance;
                        line.X2 -= moveDistance;
                        
                        // 标记完全移出左边界的线条
                        if (line.X2 < 0)
                        {
                            elementsToRemove.Add(element);
                        }
                    }
                    else if (element is Ellipse ellipse)
                    {
                        double currentLeft = Canvas.GetLeft(ellipse);
                        Canvas.SetLeft(ellipse, currentLeft - moveDistance);
                        
                        // 标记完全移出左边界的点
                        if (currentLeft - moveDistance + ellipse.Width < 0)
                        {
                            elementsToRemove.Add(element);
                        }
                    }
                }
            }
            
            // 删除移出边界的元素
            foreach (var element in elementsToRemove)
            {
                BrainwaveCanvas.Children.Remove(element);
            }
        }
        
        private void MoveWaveformLeft(int dataPointCount = 1)
        {
            if (!isDeviceConnected) return;
            
            double canvasWidth = BrainwaveCanvas.ActualWidth;
            if (canvasWidth <= 0) return;
            
            // 使用实际的数据接收频率计算移动距离
            double actualSamplingRate = totalDataPointsReceived > 0 && dataReceiveCount > 0 ? 
                (totalDataPointsReceived / ((DateTime.Now - firstDataReceiveTime).TotalSeconds)) : 520.0;
            double pixelsPerDataPoint = canvasWidth / (DISPLAY_WINDOW_SECONDS * actualSamplingRate);
            double moveDistance = dataPointCount * pixelsPerDataPoint;
            
            MoveWaveformLeftByPixels(moveDistance);
        }
        
        private double GetLastWaveformY()
        {
            double lastY = BrainwaveCanvas.ActualHeight / 2; // 默认中心线
            
            foreach (UIElement element in BrainwaveCanvas.Children)
            {
                if (element.GetValue(FrameworkElement.TagProperty)?.ToString() == "waveform" && element is Line line)
                {
                    if (line.X2 >= currentX - 10) // 找最近的线条
                    {
                        lastY = line.Y2;
                    }
                }
            }
            
            return lastY;
        }
        
        private void CleanupLeftBoundary()
        {
            var itemsToRemove = new List<UIElement>();
            
            foreach (UIElement element in BrainwaveCanvas.Children)
            {
                if (element.GetValue(FrameworkElement.TagProperty)?.ToString() == "waveform")
                {
                    if (element is Line line && line.X2 < -10)
                    {
                        itemsToRemove.Add(element);
                    }
                    else if (element is Ellipse ellipse && Canvas.GetLeft(ellipse) < -ellipse.Width - 10)
                    {
                        itemsToRemove.Add(element);
                    }
                }
            }
            
            foreach (var item in itemsToRemove)
            {
                BrainwaveCanvas.Children.Remove(item);
            }
        }

        private void StartTestButton_Click(object sender, RoutedEventArgs e)
        {
            // 开始测试的逻辑
            MessageBox.Show("开始脑电测试", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            
            // 这里可以添加具体的测试开始逻辑
            // 例如：开始数据采集、启动测试流程等
        }

        private void GetReportButton_Click(object sender, RoutedEventArgs e)
        {
            // 验证输入
            if (string.IsNullOrWhiteSpace(MocaScoreTextBox.Text) || string.IsNullOrWhiteSpace(MmseScoreTextBox.Text))
            {
                MessageBox.Show("请输入MOCA和MMSE评分", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!double.TryParse(MocaScoreTextBox.Text, out double mocaScore) || mocaScore < 0 || mocaScore > 30)
            {
                MessageBox.Show("MOCA评分必须是0-30之间的数字", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!double.TryParse(MmseScoreTextBox.Text, out double mmseScore) || mmseScore < 0 || mmseScore > 30)
            {
                MessageBox.Show("MMSE评分必须是0-30之间的数字", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 停止数据采集和脑电波模拟
            StopDataCollection();

            // 导航到报告页面
            NavigationManager.NavigateTo(new ReportPage(CurrentTester, mocaScore, mmseScore));
        }

        private void ReturnButton_Click(object sender, RoutedEventArgs e)
        {
            // 停止数据采集和脑电波模拟
            StopDataCollection();
            
            // 返回到医护人员操作页面
            NavigationManager.NavigateTo(new MedicalStaffPage());
        }
        
        private void StopDataCollection()
        {
            try
            {
                // 停止显示定时器
                displayTimer?.Stop();
                
                // 清空缓冲队列
                lock (bufferLock)
                {
                    dataBuffer.Clear();
                }
                
                if (sdkInitialized)
                {
                    // 停止SDK数据采集
                    BrainMonitorSDK.SDK_StopDataCollection();
                    
                    // 断开已连接的设备
                    foreach (var device in connectedDevices)
                    {
                        BrainMonitorSDK.SDK_DisconnectDevice(device.Mac);
                    }
                    connectedDevices.Clear();
                    
                    // 注意：这里不调用SDK_DisconnectPort()和SDK_Cleanup()
                    // 保持SDK初始化状态，以便重新扫描设备时能使用真实SDK
                }
            }
            catch (Exception ex)
            {
                // 忽略清理过程中的异常
            }
        }
    }
}