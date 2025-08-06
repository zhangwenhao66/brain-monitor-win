using System.Windows;
using System.Windows.Controls;
using System.Linq;
using System.Windows.Media.Imaging;
using BrainMonitor.Views;

namespace BrainMonitor.Views
{
    public partial class InstitutionLoginWindow : Window
    {
        public InstitutionLoginWindow()
        {
            InitializeComponent();
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            // 获取当前显示的密码
            string password = PasswordBox.Visibility == Visibility.Visible ? PasswordBox.Password : PasswordTextBox.Text;
            
            // 简单的登录验证逻辑 - 允许任何非空输入
            if (string.IsNullOrWhiteSpace(InstitutionIdTextBox.Text))
            {
                MessageBox.Show("请输入机构ID", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("请输入密码", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!PrivacyCheckBox.IsChecked == true)
            {
                MessageBox.Show("请同意隐私协议", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 登录成功，打开医护人员操作界面
            var medicalStaffWindow = new MedicalStaffWindow();
            medicalStaffWindow.Show();
            this.Close();
        }

        private void TogglePasswordButton_Click(object sender, RoutedEventArgs e)
        {
            // 切换密码显示/隐藏
            if (PasswordBox.Visibility == Visibility.Visible)
            {
                // 显示密码 - 切换到TextBox
                PasswordTextBox.Text = PasswordBox.Password;
                PasswordBox.Visibility = Visibility.Collapsed;
                PasswordTextBox.Visibility = Visibility.Visible;
                TogglePasswordImage.Source = new System.Windows.Media.Imaging.BitmapImage(new Uri("/Assets/show.png", UriKind.Relative));
            }
            else
            {
                // 隐藏密码 - 切换到PasswordBox
                PasswordBox.Password = PasswordTextBox.Text;
                PasswordTextBox.Visibility = Visibility.Collapsed;
                PasswordBox.Visibility = Visibility.Visible;
                TogglePasswordImage.Source = new System.Windows.Media.Imaging.BitmapImage(new Uri("/Assets/hide.png", UriKind.Relative));
            }
        }
    }
} 