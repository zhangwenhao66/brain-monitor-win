using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Threading.Tasks;
using BrainMonitor.Services;

namespace BrainMonitor.Views
{
    public partial class TestHistoryWindow : Window
    {
        private Tester? currentTester;

        public TestHistoryWindow(Tester tester)
        {
            InitializeComponent();
            currentTester = tester;
            LoadHistoryData();
        }

        private async void LoadHistoryData()
        {
            if (currentTester == null)
            {
                HistoryDataGrid.ItemsSource = new List<TestHistoryRecord>();
                return;
            }

            try
            {
                // 显示加载状态
                // 这里可以添加一个加载指示器
                
                // 从后端获取测试历史数据
                var historyRecords = await TestHistoryService.GetAllTestHistoryAsync(currentTester.ID);
                
                if (historyRecords != null && historyRecords.Count > 0)
                {
                    HistoryDataGrid.ItemsSource = historyRecords;
                }
                else
                {
                    // 如果没有历史记录，显示空列表
                    HistoryDataGrid.ItemsSource = new List<TestHistoryRecord>();
                }
            }
            catch (System.Exception ex)
            {
                // 记录错误日志
                System.Diagnostics.Debug.WriteLine($"加载测试历史失败: {ex.Message}");
                
                // 显示错误提示
                ModernMessageBoxWindow.Show($"加载测试历史失败: {ex.Message}", "错误", ModernMessageBoxWindow.MessageBoxType.Error);
                
                // 显示空列表
                HistoryDataGrid.ItemsSource = new List<TestHistoryRecord>();
            }
        }

        private void EnterTestButton_Click(object sender, RoutedEventArgs e)
        {
            var testWindow = new TestWindow(currentTester ?? new Tester());
            testWindow.Show();
            this.Close();
        }

        private void TesterGroupButton_Click(object sender, RoutedEventArgs e)
        {
            ModernMessageBoxWindow.Show("测试者分组功能", "功能", ModernMessageBoxWindow.MessageBoxType.Info);
        }

        private void RiskLevelButton_Click(object sender, RoutedEventArgs e)
        {
            ModernMessageBoxWindow.Show("风险等级功能", "功能", ModernMessageBoxWindow.MessageBoxType.Info);
        }

        private void MyProfileButton_Click(object sender, RoutedEventArgs e)
        {
            ModernMessageBoxWindow.Show("我的功能", "功能", ModernMessageBoxWindow.MessageBoxType.Info);
        }

        private void ReturnButton_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = new MedicalStaffWindow();
            mainWindow.Show();
            this.Close();
        }
    }
} 