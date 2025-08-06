using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Linq; // Added for .Where() and .ToList()

namespace BrainMonitor.Views
{
    public class MedicalStaff
    {
        public string Name { get; set; } = string.Empty;
        public string Account { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
    }

    // 全局医护人员管理
    public static class GlobalMedicalStaffManager
    {
        public static List<MedicalStaff> MedicalStaffList { get; set; } = new List<MedicalStaff>
        {
            new MedicalStaff { Name = "张医生", Account = "doctor001", Password = "123456", Phone = "13800138001" },
            new MedicalStaff { Name = "李护士", Account = "nurse001", Password = "123456", Phone = "13800138002" }
        };

        public static MedicalStaff? CurrentLoggedInStaff { get; set; } = null;

        public static bool Login(string account, string password)
        {
            var staff = MedicalStaffList.FirstOrDefault(s => s.Account == account && s.Password == password);
            if (staff != null)
            {
                CurrentLoggedInStaff = staff;
                return true;
            }
            return false;
        }

        public static bool Register(MedicalStaff newStaff)
        {
            // 检查账号是否已存在
            if (MedicalStaffList.Any(s => s.Account == newStaff.Account))
            {
                return false;
            }

            MedicalStaffList.Add(newStaff);
            return true;
        }

        public static void Logout()
        {
            CurrentLoggedInStaff = null;
        }
    }

    public class Tester
    {
        public string ID { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Age { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
    }

    // 全局测试者列表 - 与医护人员绑定
    public static class GlobalTesterList
    {
        // 每个医护人员的测试者列表
        public static Dictionary<string, List<Tester>> StaffTesters { get; set; } = new Dictionary<string, List<Tester>>();

        // 获取当前登录医护人员的测试者列表
        public static List<Tester> GetCurrentStaffTesters()
        {
            if (GlobalMedicalStaffManager.CurrentLoggedInStaff == null)
            {
                return new List<Tester>();
            }

            string staffAccount = GlobalMedicalStaffManager.CurrentLoggedInStaff.Account;
            if (!StaffTesters.ContainsKey(staffAccount))
            {
                // 如果该医护人员还没有测试者列表，创建一个空的
                StaffTesters[staffAccount] = new List<Tester>();
            }

            return StaffTesters[staffAccount];
        }

        // 为当前登录的医护人员添加测试者
        public static void AddTesterForCurrentStaff(Tester tester)
        {
            if (GlobalMedicalStaffManager.CurrentLoggedInStaff == null)
            {
                return;
            }

            string staffAccount = GlobalMedicalStaffManager.CurrentLoggedInStaff.Account;
            if (!StaffTesters.ContainsKey(staffAccount))
            {
                StaffTesters[staffAccount] = new List<Tester>();
            }

            StaffTesters[staffAccount].Add(tester);
        }

        // 初始化一些示例数据（可选）
        public static void InitializeSampleData()
        {
            // 为张医生添加一些测试者
            if (!StaffTesters.ContainsKey("doctor001"))
            {
                StaffTesters["doctor001"] = new List<Tester>
                {
                    new Tester { ID = "001", Name = "张三", Age = "25", Gender = "男", Phone = "13800138001" },
                    new Tester { ID = "002", Name = "李四", Age = "30", Gender = "女", Phone = "13800138002" }
                };
            }

            // 为李护士添加一些测试者
            if (!StaffTesters.ContainsKey("nurse001"))
            {
                StaffTesters["nurse001"] = new List<Tester>
                {
                    new Tester { ID = "003", Name = "王五", Age = "45", Gender = "男", Phone = "13800138003" }
                };
            }
        }
    }

    public partial class MedicalStaffWindow : Window
    {
        private Tester? selectedTester;
        private bool isLoginMode = true;

        public MedicalStaffWindow()
        {
            InitializeComponent();
            LoadSampleData();
            UpdateButtonStates();
            UpdateStaffInfo();
            // 确保登录注册界面的初始状态正确
            UpdateLoginUI();
        }

        private void LoadSampleData()
        {
            // 初始化示例数据
            GlobalTesterList.InitializeSampleData();
            
            // 根据当前登录的医护人员加载对应的测试者列表
            TesterDataGrid.ItemsSource = GlobalTesterList.GetCurrentStaffTesters();
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

            // 打开测试界面，传递选中的测试者
            var testWindow = new TestWindow(selectedTester);
            testWindow.Show();
            this.Close();
        }

        private void AddTesterButton_Click(object sender, RoutedEventArgs e)
        {
            // 检查是否已登录
            if (GlobalMedicalStaffManager.CurrentLoggedInStaff == null)
            {
                MessageBox.Show("请先登录医护人员账号", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 打开新增测试者界面
            var testerInfoWindow = new TesterInfoWindow();
            testerInfoWindow.Show();
            this.Close();
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

            // 打开测试历史记录界面
            var testHistoryWindow = new TestHistoryWindow(selectedTester);
            testHistoryWindow.Show();
            this.Close();
        }

        // 医护人员相关方法
        private void UpdateStaffInfo()
        {
            if (GlobalMedicalStaffManager.CurrentLoggedInStaff != null)
            {
                // 已登录状态
                StaffInfoTextBlock.Text = $"医护人员：{GlobalMedicalStaffManager.CurrentLoggedInStaff.Name}";
                StaffInfoTextBlock.Foreground = System.Windows.Media.Brushes.Green;
                LoginRegisterButton.Visibility = Visibility.Collapsed;
                LogoutButton.Visibility = Visibility.Visible;
                
                // 重新加载当前医护人员的测试者列表
                TesterDataGrid.ItemsSource = GlobalTesterList.GetCurrentStaffTesters();
            }
            else
            {
                // 未登录状态
                StaffInfoTextBlock.Text = "医护人员：未登录";
                StaffInfoTextBlock.Foreground = System.Windows.Media.Brushes.Gray;
                LoginRegisterButton.Visibility = Visibility.Visible;
                LogoutButton.Visibility = Visibility.Collapsed;
                
                // 清空测试者列表
                TesterDataGrid.ItemsSource = new List<Tester>();
            }
        }

        private void LoginRegisterButton_Click(object sender, RoutedEventArgs e)
        {
            // 显示登录注册面板
            LoginRegisterPanel.Visibility = Visibility.Visible;
            // 隐藏医护人员信息区域和其他面板
            HideOtherPanels();
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("确定要退出登录吗？", "确认退出", 
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                GlobalMedicalStaffManager.Logout();
                UpdateStaffInfo();
                // 移除退出成功的弹窗提示
            }
        }

        // 登录注册界面相关方法
        private void HideOtherPanels()
        {
            // 隐藏医护人员信息区域、项目介绍和测试者列表区域
            var staffInfoBorder = this.FindName("StaffInfoBorder") as Border;
            var projectIntroBorder = this.FindName("ProjectIntroBorder") as Border;
            var testerListBorder = this.FindName("TesterListBorder") as Border;
            var mainButtonsGrid = this.FindName("MainButtonsGrid") as Grid;
            
            if (staffInfoBorder != null) staffInfoBorder.Visibility = Visibility.Collapsed;
            if (projectIntroBorder != null) projectIntroBorder.Visibility = Visibility.Collapsed;
            if (testerListBorder != null) testerListBorder.Visibility = Visibility.Collapsed;
            if (mainButtonsGrid != null) mainButtonsGrid.Visibility = Visibility.Collapsed;
        }

        private void ShowAllPanels()
        {
            // 显示所有面板
            var staffInfoBorder = this.FindName("StaffInfoBorder") as Border;
            var projectIntroBorder = this.FindName("ProjectIntroBorder") as Border;
            var testerListBorder = this.FindName("TesterListBorder") as Border;
            var mainButtonsGrid = this.FindName("MainButtonsGrid") as Grid;
            
            if (staffInfoBorder != null) staffInfoBorder.Visibility = Visibility.Visible;
            if (projectIntroBorder != null) projectIntroBorder.Visibility = Visibility.Visible;
            if (testerListBorder != null) testerListBorder.Visibility = Visibility.Visible;
            if (mainButtonsGrid != null) mainButtonsGrid.Visibility = Visibility.Visible;
        }

        private void LoginRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            isLoginMode = true;
            UpdateLoginUI();
        }

        private void RegisterRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            isLoginMode = false;
            UpdateLoginUI();
        }

        private void UpdateLoginUI()
        {
            if (ActionButton == null || NameTextBox == null || PhoneTextBox == null || 
                NameLabel == null || PhoneLabel == null)
            {
                return;
            }

            if (isLoginMode)
            {
                ActionButton.Content = "登录";
                // 在登录模式下，隐藏姓名和手机号输入框
                NameLabel.Visibility = Visibility.Collapsed;
                NameTextBox.Visibility = Visibility.Collapsed;
                PhoneLabel.Visibility = Visibility.Collapsed;
                PhoneTextBox.Visibility = Visibility.Collapsed;
            }
            else
            {
                ActionButton.Content = "注册";
                // 在注册模式下，显示所有字段
                NameLabel.Visibility = Visibility.Visible;
                NameTextBox.Visibility = Visibility.Visible;
                PhoneLabel.Visibility = Visibility.Visible;
                PhoneTextBox.Visibility = Visibility.Visible;
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            // 隐藏登录注册面板
            LoginRegisterPanel.Visibility = Visibility.Collapsed;
            // 显示其他面板
            ShowAllPanels();
            // 清空输入框
            ClearLoginInputs();
        }

        private void ActionButton_Click(object sender, RoutedEventArgs e)
        {
            if (isLoginMode)
            {
                PerformLogin();
            }
            else
            {
                PerformRegister();
            }
        }

        private void PerformLogin()
        {
            // 添加空值检查
            if (AccountTextBox == null || PasswordBox == null)
            {
                MessageBox.Show("控件初始化失败", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string account = AccountTextBox.Text.Trim();
            string password = PasswordBox.Password;

            if (string.IsNullOrEmpty(account) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("请输入账号和密码", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (GlobalMedicalStaffManager.Login(account, password))
            {
                // 登录成功，直接跳转，不显示弹窗
                // 隐藏登录注册面板
                LoginRegisterPanel.Visibility = Visibility.Collapsed;
                // 显示其他面板
                ShowAllPanels();
                // 清空输入框
                ClearLoginInputs();
                // 更新界面状态
                UpdateStaffInfo();
            }
            else
            {
                MessageBox.Show("账号或密码错误", "登录失败", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PerformRegister()
        {
            // 添加空值检查
            if (NameTextBox == null || AccountTextBox == null || PasswordBox == null || PhoneTextBox == null)
            {
                MessageBox.Show("控件初始化失败", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string name = NameTextBox.Text.Trim();
            string account = AccountTextBox.Text.Trim();
            string password = PasswordBox.Password;
            string phone = PhoneTextBox.Text.Trim();

            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(account) || 
                string.IsNullOrEmpty(password) || string.IsNullOrEmpty(phone))
            {
                MessageBox.Show("请填写所有必填字段", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var newStaff = new MedicalStaff
            {
                Name = name,
                Account = account,
                Password = password,
                Phone = phone
            };

            if (GlobalMedicalStaffManager.Register(newStaff))
            {
                // 注册成功后直接登录
                if (GlobalMedicalStaffManager.Login(account, password))
                {
                    // 注册并登录成功，直接跳转，不显示弹窗
                    // 隐藏登录注册面板
                    LoginRegisterPanel.Visibility = Visibility.Collapsed;
                    // 显示其他面板
                    ShowAllPanels();
                    // 清空输入框
                    ClearLoginInputs();
                    // 更新界面状态
                    UpdateStaffInfo();
                }
            }
            else
            {
                MessageBox.Show("账号已存在，请使用其他账号", "注册失败", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ClearLoginInputs()
        {
            // 添加空值检查
            if (NameTextBox != null) NameTextBox.Text = "";
            if (AccountTextBox != null) AccountTextBox.Text = "";
            if (PasswordBox != null) PasswordBox.Password = "";
            if (PhoneTextBox != null) PhoneTextBox.Text = "";
        }
    }
} 