using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Threading.Tasks;
using BrainMonitor.Services;

namespace BrainMonitor.Views
{
    public partial class MedicalStaffPage : UserControl, INavigationAware
    {
        private Tester? selectedTester;
        private bool shouldRefreshOnLoad = false;

        public MedicalStaffPage()
        {
            InitializeComponent();
            LoadSampleData();
            UpdateStaffInfo();
            
            // 检查机构登录状态
            if (GlobalInstitutionManager.CurrentInstitutionDbId <= 0)
            {
                // 如果未登录机构，显示提示信息并导航到机构登录页面
                ModernMessageBoxWindow.Show("请先登录机构", "提示", ModernMessageBoxWindow.MessageBoxType.Warning);
                NavigationManager.NavigateTo(new InstitutionLoginPage());
            }
        }

        public void OnNavigatedTo()
        {
            // 页面导航到时的处理
            UpdateStaffInfo();
            
            // 检查机构登录状态
            if (GlobalInstitutionManager.CurrentInstitutionDbId <= 0)
            {
                // 如果未登录机构，显示提示信息并导航到机构登录页面
                ModernMessageBoxWindow.Show("请先登录机构", "提示", ModernMessageBoxWindow.MessageBoxType.Warning);
                NavigationManager.NavigateTo(new InstitutionLoginPage());
                return;
            }

            // 如果需要刷新数据，则重新加载测试者列表
            if (shouldRefreshOnLoad)
            {
                shouldRefreshOnLoad = false; // 重置标志
                LoadSampleData();
            }
        }

        public void OnNavigatedFrom()
        {
            // 页面离开时的处理
        }

        /// <summary>
        /// 设置页面加载时是否需要刷新数据
        /// </summary>
        /// <param name="shouldRefresh">是否需要刷新</param>
        public void SetRefreshFlag(bool shouldRefresh)
        {
            shouldRefreshOnLoad = shouldRefresh;
        }

        private async void LoadSampleData()
        {
            try
            {
                // 检查是否已登录医护人员和机构
                if (GlobalMedicalStaffManager.CurrentLoggedInStaff == null || GlobalInstitutionManager.CurrentInstitutionDbId <= 0)
                {
                    TesterDataGrid.ItemsSource = new List<TesterInfo>();
                    UpdateButtonStates();
                    return;
                }

                // 从后端获取测试者列表
                var testers = await TesterService.GetAllTestersAsync(
                    GlobalMedicalStaffManager.CurrentLoggedInStaff.Id,
                    GlobalInstitutionManager.CurrentInstitutionDbId
                );

                if (testers != null && testers.Count > 0)
                {
                    // 将TesterInfo转换为Tester对象以保持兼容性
                    var convertedTesters = testers.Select(t => new Tester
                    {
                        ID = t.TesterId,
                        Name = t.Name,
                        Age = t.Age,
                        Gender = t.Gender,
                        Phone = t.Phone
                    }).ToList();

                    TesterDataGrid.ItemsSource = convertedTesters;
                }
                else
                {
                    TesterDataGrid.ItemsSource = new List<Tester>();
                }
            }
            catch (System.Exception ex)
            {
                TesterDataGrid.ItemsSource = new List<Tester>();
            }

            UpdateButtonStates();
        }

        private void UpdateButtonStates()
        {
            EnterTestButton.IsEnabled = selectedTester != null;
            ViewHistoryButton.IsEnabled = selectedTester != null;
        }

        private async void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            // 检查是否已登录
            if (GlobalMedicalStaffManager.CurrentLoggedInStaff == null)
            {
                ModernMessageBoxWindow.Show("请先登录医护人员账号", "提示", ModernMessageBoxWindow.MessageBoxType.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(SearchTextBox.Text))
            {
                // 如果搜索框为空，重新加载所有测试者
                LoadSampleData();
                return;
            }

            try
            {
                // 获取搜索关键词
                string searchKeyword = SearchTextBox.Text.Trim().ToLower();
                
                // 从后端获取所有测试者，然后在本地过滤
                var allTesters = await TesterService.GetAllTestersAsync(
                    GlobalMedicalStaffManager.CurrentLoggedInStaff.Id,
                    GlobalInstitutionManager.CurrentInstitutionDbId
                );

                if (allTesters != null && allTesters.Count > 0)
                {
                    // 过滤测试者列表
                    var filteredTesters = allTesters.Where(tester => 
                        tester.TesterId.ToLower().Contains(searchKeyword) || 
                        tester.Name.ToLower().Contains(searchKeyword)
                    ).ToList();

                    // 转换为Tester对象
                    var convertedTesters = filteredTesters.Select(t => new Tester
                    {
                        ID = t.TesterId,
                        Name = t.Name,
                        Age = t.Age,
                        Gender = t.Gender,
                        Phone = t.Phone
                    }).ToList();

                    TesterDataGrid.ItemsSource = convertedTesters;
                }
                else
                {
                    TesterDataGrid.ItemsSource = new List<Tester>();
                }
            }
            catch (System.Exception ex)
            {
                ModernMessageBoxWindow.Show($"搜索失败: {ex.Message}", "错误", ModernMessageBoxWindow.MessageBoxType.Error);
            }
        }

        private void EnterTestButton_Click(object sender, RoutedEventArgs e)
        {
            if (selectedTester == null)
            {
                ModernMessageBoxWindow.Show("请先选择一个测试者", "提示", ModernMessageBoxWindow.MessageBoxType.Warning);
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
                ModernMessageBoxWindow.Show("请先登录医护人员账号", "提示", ModernMessageBoxWindow.MessageBoxType.Warning);
                return;
            }

            if (GlobalInstitutionManager.CurrentInstitutionDbId <= 0)
            {
                ModernMessageBoxWindow.Show("请先登录机构", "提示", ModernMessageBoxWindow.MessageBoxType.Warning);
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

        private void ViewHistoryButton_Click(object sender, RoutedEventArgs e)
        {
            if (selectedTester == null)
            {
                ModernMessageBoxWindow.Show("请先选择一个测试者", "提示", ModernMessageBoxWindow.MessageBoxType.Warning);
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
                // 已登录状态 - 显示姓名、工号和机构信息
                StaffNameText.Text = GlobalMedicalStaffManager.CurrentLoggedInStaff.Name;
                StaffIdText.Text = $"工号: {GlobalMedicalStaffManager.CurrentLoggedInStaff.StaffId}";
                
                // 显示机构信息
                if (GlobalInstitutionManager.CurrentInstitutionDbId > 0)
                {
                    StaffDepartmentText.Text = $"机构: {GlobalInstitutionManager.CurrentInstitutionName}";
                    StaffDepartmentText.Visibility = Visibility.Visible;
                }
                else
                {
                    StaffDepartmentText.Text = "";
                    StaffDepartmentText.Visibility = Visibility.Collapsed;
                }
                
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

        private void InstitutionLogoutButton_Click(object sender, RoutedEventArgs e)
        {
            // 退出机构登录时，同时退出医护人员登录
            GlobalMedicalStaffManager.Logout();
            
            // 清除机构信息
            GlobalInstitutionManager.ClearCurrentInstitution();
            
            // 跳转回机构登录界面
            NavigationManager.NavigateTo(new InstitutionLoginPage());
        }
    }
}