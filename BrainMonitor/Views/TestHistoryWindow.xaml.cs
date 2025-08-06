using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace BrainMonitor.Views
{
    public class TestHistoryRecord
    {
        public string Date { get; set; } = string.Empty;
        public string Result { get; set; } = string.Empty;
    }

    public partial class TestHistoryWindow : Window
    {
        private Tester? currentTester;

        public TestHistoryWindow(Tester tester)
        {
            InitializeComponent();
            currentTester = tester;
            LoadHistoryData();
        }

        private void LoadHistoryData()
        {
            // 模拟历史记录数据
            var historyRecords = new List<TestHistoryRecord>
            {
                new TestHistoryRecord { Date = "2023-01-01", Result = "0.42" },
                new TestHistoryRecord { Date = "2023-02-01", Result = "0.39" },
                new TestHistoryRecord { Date = "2023-03-01", Result = "0.37" },
                new TestHistoryRecord { Date = "2023-04-01", Result = "0.35" }
            };
            HistoryDataGrid.ItemsSource = historyRecords;
        }

        private void EnterTestButton_Click(object sender, RoutedEventArgs e)
        {
            var testWindow = new TestWindow(currentTester ?? new Tester());
            testWindow.Show();
            this.Close();
        }

        private void TesterGroupButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("测试者分组功能", "功能", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void RiskLevelButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("风险等级功能", "功能", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void MyProfileButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("我的功能", "功能", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ReturnButton_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = new MedicalStaffWindow();
            mainWindow.Show();
            this.Close();
        }
    }
} 