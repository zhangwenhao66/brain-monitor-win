using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace BrainMonitor.Views
{
    public partial class TestHistoryPage : UserControl, INavigationAware
    {
        private Tester? currentTester;

        public TestHistoryPage(Tester tester)
        {
            InitializeComponent();
            currentTester = tester;
            LoadHistoryData();
        }

        public void OnNavigatedTo()
        {
            // 页面导航到时的处理
        }

        public void OnNavigatedFrom()
        {
            // 页面离开时的处理
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
            // 导航到测试页面
            NavigationManager.NavigateTo(new TestPage(currentTester ?? new Tester()));
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
            // 返回到医护人员操作页面
            NavigationManager.NavigateTo(new MedicalStaffPage());
        }
    }
}