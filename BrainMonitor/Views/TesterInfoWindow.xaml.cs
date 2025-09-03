using System.Windows;
using System.Windows.Controls;

namespace BrainMirror.Views
{
    public partial class TesterInfoWindow : Window
    {
        public TesterInfoWindow()
        {
            InitializeComponent();
        }

        private void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            // 验证输入
            if (string.IsNullOrWhiteSpace(IdNameTextBox.Text))
            {
                ModernMessageBoxWindow.Show("请输入ID/姓名", "提示", ModernMessageBoxWindow.MessageBoxType.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(PhoneTextBox.Text))
            {
                ModernMessageBoxWindow.Show("请输入手机号", "提示", ModernMessageBoxWindow.MessageBoxType.Warning);
                return;
            }

            if (GenderComboBox.SelectedItem == null)
            {
                ModernMessageBoxWindow.Show("请选择性别", "提示", ModernMessageBoxWindow.MessageBoxType.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(AgeTextBox.Text))
            {
                ModernMessageBoxWindow.Show("请输入年龄", "提示", ModernMessageBoxWindow.MessageBoxType.Warning);
                return;
            }

            if (PrivacyCheckBox.IsChecked != true)
            {
                ModernMessageBoxWindow.Show("请同意数据隐私协议", "提示", ModernMessageBoxWindow.MessageBoxType.Warning);
                return;
            }

            // 获取性别选择
            var selectedGender = (GenderComboBox.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "";

            // 创建新的测试者信息
            var newTester = new Tester
            {
                ID = IdNameTextBox.Text.Split('/')[0].Trim(), // 取ID部分
                Name = IdNameTextBox.Text.Contains("/") ? IdNameTextBox.Text.Split('/')[1].Trim() : IdNameTextBox.Text,
                Age = AgeTextBox.Text,
                Gender = selectedGender,
                Phone = PhoneTextBox.Text
            };

            // 将新测试者信息添加到当前登录医护人员的测试者列表
            GlobalTesterList.AddTesterForCurrentStaff(newTester);

            // 设置对话框结果为成功并关闭窗口
            this.DialogResult = true;
            this.Close();
        }

        private void ReturnButton_Click(object sender, RoutedEventArgs e)
        {
            // 直接关闭弹窗
            this.Close();
        }
    }
}