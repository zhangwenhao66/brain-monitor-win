using System.Windows;

namespace BrainMonitor.Views
{
    public partial class ReportWindow : Window
    {
        public ReportWindow()
        {
            InitializeComponent();
        }

        private void ReturnButton_Click(object sender, RoutedEventArgs e)
        {
            // 返回测试界面
            var testWindow = new TestWindow();
            testWindow.Show();
            this.Close();
        }
    }
} 