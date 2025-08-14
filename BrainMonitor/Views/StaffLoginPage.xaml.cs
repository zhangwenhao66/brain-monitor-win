using System;
using System.Windows;
using System.Windows.Controls;

namespace BrainMonitor.Views
{
    public partial class StaffLoginPage : UserControl
    {
        public StaffLoginPage()
        {
            InitializeComponent();
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            // 获取登录信息
            string account = LoginAccountTextBox.Text.Trim();
            string password = LoginPasswordBox.Password;

            // 验证输入
            if (string.IsNullOrEmpty(account))
            {
                MessageBox.Show("请输入账号", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrEmpty(password))
            {
                MessageBox.Show("请输入密码", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 尝试登录
            bool loginSuccess = GlobalMedicalStaffManager.Login(account, password);
            
            if (loginSuccess)
            {
                // 登录成功，导航到医护人员操作页面
                NavigationManager.NavigateTo(new MedicalStaffPage());
            }
            else
            {
                MessageBox.Show("账号或密码错误，请重试", "登录失败", MessageBoxButton.OK, MessageBoxImage.Error);
                // 清空密码框
                LoginPasswordBox.Password = "";
            }
        }

        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            // 获取注册信息
            string name = RegisterNameTextBox.Text.Trim();
            string staffId = RegisterStaffIdTextBox.Text.Trim();
            string account = RegisterAccountTextBox.Text.Trim();
            string password = RegisterPasswordBox.Password;

            // 验证输入
            if (string.IsNullOrEmpty(name))
            {
                MessageBox.Show("请输入姓名", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrEmpty(staffId))
            {
                MessageBox.Show("请输入工号", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrEmpty(account))
            {
                MessageBox.Show("请输入账号", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrEmpty(password))
            {
                MessageBox.Show("请输入密码", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 创建新的医护人员对象
            var newStaff = new MedicalStaff
            {
                Name = name,
                StaffId = staffId,
                Account = account,
                Password = password,
                Phone = "" // 不再使用Phone字段存储科室信息
            };
            
            // 尝试注册
            bool registerSuccess = GlobalMedicalStaffManager.Register(newStaff);
            
            if (registerSuccess)
            {
                // 注册成功后直接登录
                GlobalMedicalStaffManager.Login(account, password);
                
                // 导航到医护人员操作页面
                NavigationManager.NavigateTo(new MedicalStaffPage());
            }
            else
            {
                MessageBox.Show("该账号已存在，请使用其他账号", "注册失败", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            // 返回到医护人员操作页面
            NavigationManager.NavigateTo(new MedicalStaffPage());
        }

        private void ClearRegisterInputs()
        {
            RegisterNameTextBox.Text = "";
            RegisterStaffIdTextBox.Text = "";
            RegisterAccountTextBox.Text = "";
            RegisterPasswordBox.Password = "";
        }

        private void ClearLoginInputs()
        {
            LoginAccountTextBox.Text = "";
            LoginPasswordBox.Password = "";
        }
    }
}