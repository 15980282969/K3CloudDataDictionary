using System.Collections.ObjectModel;
using System.Windows;
using K3CloudDataDictionary.Helpers;
using K3CloudDataDictionary.Models;

namespace K3CloudDataDictionary
{
    public partial class ConnectionDialog
    {
        public ConnectionInfo SelectedConnection { get; private set; }
        private ObservableCollection<ConnectionInfo> _connections;

        public ConnectionDialog()
        {
            InitializeComponent();
            SQLiteHelper.EnsureDatabase();
            LoadConnections();
        }

        private void LoadConnections()
        {
            var list = SQLiteHelper.LoadAll();
            _connections = new ObservableCollection<ConnectionInfo>(list);
            ConnectionList.ItemsSource = _connections;
        }

        private void ConnectionList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (ConnectionList.SelectedItem is ConnectionInfo selected)
            {
                var editCopy = selected.Clone();
                EditPanel.DataContext = editCopy;
                EditPanel.IsEnabled = true;
                PasswordBox.Password = selected.Password;
            }
            else
            {
                EditPanel.DataContext = null;
                EditPanel.IsEnabled = false;
                PasswordBox.Password = "";
            }
        }

        private void AddConnection_Click(object sender, RoutedEventArgs e)
        {
            var newConn = new ConnectionInfo
            {
                Name = "新连接",
                ServerIp = "",
                Port = 1433,
                UserName = "",
                Password = "",
                Database = "",
                IsDefault = _connections.Count == 0
            };
            int id = SQLiteHelper.Save(newConn);
            newConn.Id = id;
            _connections.Add(newConn);
            ConnectionList.SelectedItem = newConn;
        }

        private void DeleteConnection_Click(object sender, RoutedEventArgs e)
        {
            if (ConnectionList.SelectedItem is ConnectionInfo selected)
            {
                var result = MessageBox.Show($"确定删除连接 \"{selected.DisplayName}\" 吗？", "删除确认",
                    MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    SQLiteHelper.Delete(selected.Id);
                    _connections.Remove(selected);
                    EditPanel.DataContext = null;
                    EditPanel.IsEnabled = false;
                    PasswordBox.Password = "";
                }
            }
        }

        private void SaveConnection_Click(object sender, RoutedEventArgs e)
        {
            if (!(EditPanel.DataContext is ConnectionInfo editCopy)) return;

            if (string.IsNullOrWhiteSpace(editCopy.ServerIp))
            {
                MessageBox.Show("请输入服务器IP", "验证", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (ConnectionList.SelectedItem is ConnectionInfo original)
            {
                original.Name = editCopy.Name;
                original.ServerIp = editCopy.ServerIp;
                original.Port = editCopy.Port;
                original.UserName = editCopy.UserName;
                original.Password = editCopy.Password;
                original.Database = editCopy.Database;
                original.IsDefault = editCopy.IsDefault;

                SQLiteHelper.Update(original);

                ConnectionList.ItemsSource = null;
                ConnectionList.ItemsSource = _connections;
                ConnectionList.SelectedItem = original;
            }
        }

        private void TestConnection_Click(object sender, RoutedEventArgs e)
        {
            if (!(EditPanel.DataContext is ConnectionInfo editCopy)) return;

            if (string.IsNullOrWhiteSpace(editCopy.ServerIp))
            {
                MessageBox.Show("请输入服务器IP", "验证", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (DbHelper.TestConnection(editCopy.ConnectionString, out string errorMessage))
            {
                MessageBox.Show("连接成功！", "测试连接", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show($"连接失败：{errorMessage}", "测试连接", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Connect_Click(object sender, RoutedEventArgs e)
        {
            if (!(EditPanel.DataContext is ConnectionInfo editCopy)) return;

            if (string.IsNullOrWhiteSpace(editCopy.ServerIp))
            {
                MessageBox.Show("请先选择或创建一个连接", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!DbHelper.TestConnection(editCopy.ConnectionString, out string errorMessage))
            {
                var result = MessageBox.Show(
                    $"连接测试失败：{errorMessage}\n\n是否仍然使用此连接？",
                    "连接失败",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);
                if (result != MessageBoxResult.Yes) return;
            }

            if (ConnectionList.SelectedItem is ConnectionInfo original)
            {
                original.Name = editCopy.Name;
                original.ServerIp = editCopy.ServerIp;
                original.Port = editCopy.Port;
                original.UserName = editCopy.UserName;
                original.Password = editCopy.Password;
                original.Database = editCopy.Database;
                original.IsDefault = true;
                SQLiteHelper.Update(original);
                SQLiteHelper.SetDefault(original.Id);
                SelectedConnection = original;
            }

            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (EditPanel?.DataContext is ConnectionInfo editCopy)
            {
                editCopy.Password = PasswordBox.Password;
            }
        }
    }
}
