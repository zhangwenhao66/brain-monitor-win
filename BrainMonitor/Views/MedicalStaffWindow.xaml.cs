using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Linq; // Added for .Where() and .ToList()
using System; // Added for DateTime

namespace BrainMonitor.Views
{
    public class MedicalStaff
    {
        public string Name { get; set; } = string.Empty;
        public string StaffId { get; set; } = string.Empty;
        public string Account { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
    }

    // 全局医护人员管理
    public static class GlobalMedicalStaffManager
    {
        public static List<MedicalStaff> MedicalStaffList { get; set; } = new List<MedicalStaff>
        {
            new MedicalStaff { Name = "测试医生", StaffId = "001", Account = "1", Password = "1", Phone = "13800138001" },
            new MedicalStaff { Name = "张医生", StaffId = "002", Account = "doctor001", Password = "123456", Phone = "13800138002" },
            new MedicalStaff { Name = "李护士", StaffId = "003", Account = "nurse001", Password = "123456", Phone = "13800138003" }
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

    // 全局机构信息管理类
    public static class GlobalInstitutionManager
    {
        public static string CurrentInstitutionId { get; private set; } = "默认机构";
        public static string CurrentInstitutionName { get; private set; } = "默认机构名称";

        public static void SetCurrentInstitution(string institutionId, string institutionName = "")
        {
            CurrentInstitutionId = institutionId ?? "默认机构";
            CurrentInstitutionName = string.IsNullOrWhiteSpace(institutionName) ? institutionId : institutionName;
        }

        public static void ClearCurrentInstitution()
        {
            CurrentInstitutionId = "默认机构";
            CurrentInstitutionName = "默认机构名称";
        }
    }

    // 全局脑电波数据管理器
    public static class GlobalBrainwaveDataManager
    {
        private static readonly object dataLock = new object();
        private static List<double> latestBrainwaveData = new List<double>();
        private static DateTime lastDataUpdateTime = DateTime.MinValue;
        private static bool isDataCollectionActive = false;

        // 获取最新的脑电波数据
        public static double GetLatestBrainwaveData()
        {
            lock (dataLock)
            {
                if (latestBrainwaveData.Count > 0)
                {
                    // 返回最新的数据点
                    return latestBrainwaveData[latestBrainwaveData.Count - 1];
                }
                return double.MinValue; // 表示无数据
            }
        }

        // 获取所有最新的脑电波数据
        public static List<double> GetAllLatestBrainwaveData()
        {
            lock (dataLock)
            {
                return new List<double>(latestBrainwaveData);
            }
        }

        // 添加新的脑电波数据
        public static void AddBrainwaveData(double data)
        {
            lock (dataLock)
            {
                latestBrainwaveData.Add(data);
                lastDataUpdateTime = DateTime.Now;
                
                // 限制数据量，避免内存溢出
                if (latestBrainwaveData.Count > 1000)
                {
                    latestBrainwaveData.RemoveAt(0);
                }
            }
        }

        // 添加多个脑电波数据点
        public static void AddBrainwaveDataRange(IEnumerable<double> dataRange)
        {
            lock (dataLock)
            {
                latestBrainwaveData.AddRange(dataRange);
                lastDataUpdateTime = DateTime.Now;
                
                // 限制数据量，避免内存溢出
                if (latestBrainwaveData.Count > 1000)
                {
                    int removeCount = latestBrainwaveData.Count - 1000;
                    latestBrainwaveData.RemoveRange(0, removeCount);
                }
            }
        }

        // 清空数据
        public static void ClearData()
        {
            lock (dataLock)
            {
                latestBrainwaveData.Clear();
                lastDataUpdateTime = DateTime.MinValue;
            }
        }

        // 获取最后数据更新时间
        public static DateTime GetLastDataUpdateTime()
        {
            lock (dataLock)
            {
                return lastDataUpdateTime;
            }
        }

        // 检查是否有可用数据
        public static bool HasData()
        {
            lock (dataLock)
            {
                return latestBrainwaveData.Count > 0;
            }
        }

        // 设置数据采集状态
        public static void SetDataCollectionActive(bool active)
        {
            lock (dataLock)
            {
                isDataCollectionActive = active;
            }
        }

        // 检查数据采集是否活跃
        public static bool IsDataCollectionActive()
        {
            lock (dataLock)
            {
                return isDataCollectionActive;
            }
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
            // 为测试医生（账号1）添加测试者
            if (!StaffTesters.ContainsKey("1"))
            {
                StaffTesters["1"] = new List<Tester>
                {
                    new Tester { ID = "001", Name = "张三", Age = "25", Gender = "男", Phone = "13800138001" },
                    new Tester { ID = "002", Name = "李四", Age = "30", Gender = "女", Phone = "13800138002" },
                    new Tester { ID = "003", Name = "王五", Age = "45", Gender = "男", Phone = "13800138003" },
                    new Tester { ID = "004", Name = "赵六", Age = "35", Gender = "女", Phone = "13800138004" }
                };
            }

            // 为张医生添加一些测试者
            if (!StaffTesters.ContainsKey("doctor001"))
            {
                StaffTesters["doctor001"] = new List<Tester>
                {
                    new Tester { ID = "005", Name = "孙七", Age = "28", Gender = "男", Phone = "13800138005" },
                    new Tester { ID = "006", Name = "周八", Age = "32", Gender = "女", Phone = "13800138006" }
                };
            }

            // 为李护士添加一些测试者
            if (!StaffTesters.ContainsKey("nurse001"))
            {
                StaffTesters["nurse001"] = new List<Tester>
                {
                    new Tester { ID = "007", Name = "吴九", Age = "40", Gender = "男", Phone = "13800138007" }
                };
            }
        }
    }

    public partial class MedicalStaffWindow : Window
    {
        private Tester? selectedTester;

        public MedicalStaffWindow()
        {
            InitializeComponent();
            LoadSampleData();
            UpdateButtonStates();
            UpdateStaffInfo();
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

            // 以弹窗模式打开新增测试者界面
            var testerInfoWindow = new TesterInfoWindow();
            var result = testerInfoWindow.ShowDialog();
            
            if (result == true)
            {
                // 测试者添加成功，刷新测试者列表
                var currentTesters = GlobalTesterList.GetCurrentStaffTesters();
                TesterDataGrid.ItemsSource = null; // 先清空
                TesterDataGrid.ItemsSource = currentTesters; // 重新设置
                TesterDataGrid.Items.Refresh(); // 强制刷新
            }
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
            var staffLoginWindow = new StaffLoginWindow();
            var result = staffLoginWindow.ShowDialog();
            
            if (result == true)
            {
                // 登录成功，更新界面
                UpdateStaffInfo();
            }
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            // 直接执行退出登录，不显示确认弹窗
            GlobalMedicalStaffManager.Logout();
            UpdateStaffInfo();
        }
    }
}