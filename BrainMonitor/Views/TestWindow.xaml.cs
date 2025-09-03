using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;

namespace BrainMirror.Views
{
    public partial class TestWindow : Window
    {
        public Tester CurrentTester { get; set; }

        public TestWindow(Tester tester)
        {
            InitializeComponent();
            CurrentTester = tester ?? new Tester(); // 确保不为null
            this.DataContext = this;
            
            // 初始化时禁用设备列表和连接按钮
            DeviceComboBox.IsEnabled = false;
            ConnectDeviceButton.IsEnabled = false;
        }

        public TestWindow() : this(new Tester()) { } // 创建默认Tester对象

        private void ScanDeviceButton_Click(object sender, RoutedEventArgs e)
        {
            // 模拟扫描设备
            ModernMessageBoxWindow.Show("正在扫描设备...", "设备扫描", ModernMessageBoxWindow.MessageBoxType.Info);
            
            // 清空并重新添加设备选项
            DeviceComboBox.Items.Clear();
            DeviceComboBox.Items.Add(new System.Windows.Controls.ComboBoxItem { Content = "脑电波检测设备001" });
            DeviceComboBox.Items.Add(new System.Windows.Controls.ComboBoxItem { Content = "脑电波检测设备002" });
            
            // 启用设备列表
            DeviceComboBox.IsEnabled = true;
            
            // 添加设备选择事件处理
            DeviceComboBox.SelectionChanged += DeviceComboBox_SelectionChanged;
            
            ModernMessageBoxWindow.Show("扫描完成，发现2个设备", "设备扫描", ModernMessageBoxWindow.MessageBoxType.Info);
        }

        private void DeviceComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            // 当用户选择了设备后，启用连接设备按钮
            if (DeviceComboBox.SelectedItem != null)
            {
                ConnectDeviceButton.IsEnabled = true;
            }
            else
            {
                ConnectDeviceButton.IsEnabled = false;
            }
        }

        private void ConnectDeviceButton_Click(object sender, RoutedEventArgs e)
        {
            if (PortComboBox.SelectedItem == null)
            {
                ModernMessageBoxWindow.Show("请选择端口", "提示", ModernMessageBoxWindow.MessageBoxType.Warning);
                return;
            }

            if (DeviceComboBox.SelectedItem == null)
            {
                ModernMessageBoxWindow.Show("请选择设备", "提示", ModernMessageBoxWindow.MessageBoxType.Warning);
                return;
            }

            // 模拟连接设备
            ModernMessageBoxWindow.Show("正在连接设备...", "设备连接", ModernMessageBoxWindow.MessageBoxType.Info);
            
            // 模拟开始显示脑电波
            StartBrainwaveSimulation();
            
            ModernMessageBoxWindow.Show("设备连接成功！", "设备连接", ModernMessageBoxWindow.MessageBoxType.Success);
        }

        private void StartBrainwaveSimulation()
        {
            // 清空画布
            BrainwaveCanvas.Children.Clear();
            
            // 添加模拟的脑电波线条
            var polyline = new Polyline
            {
                Stroke = Brushes.White,
                StrokeThickness = 2,
                Points = new PointCollection()
            };

            // 生成模拟的脑电波数据点
            for (int i = 0; i < 100; i++)
            {
                double x = i * 5;
                double y = 100 + 50 * System.Math.Sin(i * 0.2) + 20 * System.Math.Sin(i * 0.5);
                polyline.Points.Add(new System.Windows.Point(x, y));
            }

            BrainwaveCanvas.Children.Add(polyline);
        }

        private void GetReportButton_Click(object sender, RoutedEventArgs e)
        {
            // 验证量表输入
            if (string.IsNullOrWhiteSpace(MocaScoreTextBox.Text))
            {
                ModernMessageBoxWindow.Show("请输入MOCA量表得分", "提示", ModernMessageBoxWindow.MessageBoxType.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(MmseScoreTextBox.Text))
            {
                ModernMessageBoxWindow.Show("请输入MMSE量表得分", "提示", ModernMessageBoxWindow.MessageBoxType.Warning);
                return;
            }

            // 验证输入是否为数字
            if (!int.TryParse(MocaScoreTextBox.Text, out int mocaScore) || mocaScore < 0 || mocaScore > 30)
            {
                ModernMessageBoxWindow.Show("MOCA量表得分应为0-30之间的数字", "提示", ModernMessageBoxWindow.MessageBoxType.Warning);
                return;
            }

            if (!int.TryParse(MmseScoreTextBox.Text, out int mmseScore) || mmseScore < 0 || mmseScore > 30)
            {
                ModernMessageBoxWindow.Show("MMSE量表得分应为0-30之间的数字", "提示", ModernMessageBoxWindow.MessageBoxType.Warning);
                return;
            }

            // 打开报告界面
            var reportWindow = new ReportWindow();
            reportWindow.Show();
            this.Close();
        }

        private void ReturnButton_Click(object sender, RoutedEventArgs e)
        {
            // 返回医护人员操作界面
            var medicalStaffWindow = new MedicalStaffWindow();
            medicalStaffWindow.Show();
            this.Close();
        }
    }
}