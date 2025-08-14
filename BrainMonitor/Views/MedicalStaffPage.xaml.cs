using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace BrainMonitor.Views
{
    public partial class MedicalStaffPage : UserControl, INavigationAware
    {
        private Tester? selectedTester;

        public MedicalStaffPage()
        {
            InitializeComponent();
            LoadSampleData();
            UpdateStaffInfo();
        }

        public void OnNavigatedTo()
        {
            // 页面导航到时的处理
            UpdateStaffInfo();
        }

        public void OnNavigatedFrom()
        {
            // 页面离开时的处理
        }

        private void LoadSampleData()
        {
            // 加载示例数据
            TesterDataGrid.ItemsSource = GlobalTesterList.GetCurrentStaffTesters();
            UpdateButtonStates();
        }

        private void UpdateButtonStates()
        {
            EnterTestButton.IsEnabled = selectedTester != null;
            ViewHistoryButton.IsEnabled = selectedTester != null;
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            // 检查是否已登录
            if (GlobalMedicalStaffManager.CurrentLoggedInStaff == null)
            {
                MessageBox.Show("请先登录医护人员账号", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(SearchTextBox.Text))
            {
                // 如果搜索框为空，显示当前医护人员的所有测试者
                TesterDataGrid.ItemsSource = GlobalTesterList.GetCurrentStaffTesters();
                return;
            }

            // 获取搜索关键词
            string searchKeyword = SearchTextBox.Text.Trim().ToLower();
            
            // 过滤当前医护人员的测试者列表
            var currentTesters = GlobalTesterList.GetCurrentStaffTesters();
            var filteredTesters = currentTesters.Where(tester => 
                tester.ID.ToLower().Contains(searchKeyword) || 
                tester.Name.ToLower().Contains(searchKeyword)
            ).ToList();
            
            // 更新DataGrid显示过滤后的结果
            TesterDataGrid.ItemsSource = filteredTesters;
        }

        private void EnterTestButton_Click(object sender, RoutedEventArgs e)
        {
            if (selectedTester == null)
            {
                MessageBox.Show("请先选择一个测试者", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 导航到测试页面，传递选中的测试者
            NavigationManager.NavigateTo(new TestPage(selectedTester));
        }

        private void AddTesterButton_Click(object sender, RoutedEventArgs e)
        {
            // 检查是否已登录
            if (GlobalMedicalStaffManager.CurrentLoggedInStaff == null)
            {
                MessageBox.Show("请先登录医护人员账号", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 导航到新增测试者页面
            NavigationManager.NavigateTo(new TesterInfoPage());
        }

        private void TesterDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            selectedTester = TesterDataGrid.SelectedItem as Tester;
            UpdateButtonStates();
        }

        private void TesterDataGrid_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // 检查点击的是否是已选中的行
            var row = GetRowFromMousePosition(e.GetPosition(TesterDataGrid));
            if (row != null && row == TesterDataGrid.SelectedItem)
            {
                // 如果点击的是已选中的行，则取消选中
                TesterDataGrid.SelectedItem = null;
                e.Handled = true;
            }
        }

        private void TesterDataGrid_LostFocus(object sender, RoutedEventArgs e)
        {
            // 移除失去焦点时的取消选择逻辑
        }

        private void MainGrid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // 移除点击其他区域时的取消选择逻辑
        }

        private object? GetRowFromMousePosition(Point position)
        {
            var inputElement = TesterDataGrid.InputHitTest(position);
            if (inputElement is DependencyObject depObj)
            {
                while (depObj != null && !(depObj is DataGridRow))
                {
                    depObj = System.Windows.Media.VisualTreeHelper.GetParent(depObj);
                }
                if (depObj is DataGridRow row)
                {
                    return row.DataContext;
                }
            }
            return null;
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

        private void ViewHistoryButton_Click(object sender, RoutedEventArgs e)
        {
            if (selectedTester == null)
            {
                MessageBox.Show("请先选择一个测试者", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 导航到测试历史记录页面
            NavigationManager.NavigateTo(new TestHistoryPage(selectedTester));
        }

        // 医护人员相关方法
        private void UpdateStaffInfo()
        {
            if (GlobalMedicalStaffManager.CurrentLoggedInStaff != null)
            {
                // 已登录状态 - 隐藏大标题，只显示姓名和工号
                StaffNameText.Text = GlobalMedicalStaffManager.CurrentLoggedInStaff.Name;
                StaffIdText.Text = $"工号: {GlobalMedicalStaffManager.CurrentLoggedInStaff.StaffId}";
                StaffDepartmentText.Text = ""; // 不显示科室信息
                LogoutButton.Visibility = Visibility.Visible;
                StaffLoginRegisterButton.Visibility = Visibility.Collapsed;
                
                // 重新加载当前医护人员的测试者列表
                TesterDataGrid.ItemsSource = GlobalTesterList.GetCurrentStaffTesters();
            }
            else
            {
                // 未登录状态
                StaffNameText.Text = "未登录";
                StaffIdText.Text = "工号: --";
                StaffDepartmentText.Text = "";
                LogoutButton.Visibility = Visibility.Collapsed;
                StaffLoginRegisterButton.Visibility = Visibility.Visible;
                
                // 清空测试者列表
                TesterDataGrid.ItemsSource = new List<Tester>();
            }
        }

        // 注册/登录按钮事件处理
        private void StaffLoginRegisterButton_Click(object sender, RoutedEventArgs e)
        {
            // 导航到医护人员登录页面
            NavigationManager.NavigateTo(new StaffLoginPage());
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            // 直接执行退出登录，不显示确认弹窗
            GlobalMedicalStaffManager.Logout();
            UpdateStaffInfo();
        }
    }
}