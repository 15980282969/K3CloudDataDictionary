using K3CloudDataDictionary.Helpers;
using K3CloudDataDictionary.Models;
using System;
using System.Collections.ObjectModel;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Windows;

namespace K3CloudDataDictionary
{
    public partial class ConnectionDialog
    {
        public ConnectionInfo SelectedConnection { get; private set; }
        /// <summary>
        /// 当从本地数据页签连接时，指定要使用的本地数据文件路径
        /// </summary>
        public string SelectedLocalDbPath { get; private set; }

        private ObservableCollection<ConnectionInfo> _connections;
        private ObservableCollection<LocalDataFileInfo> _localDataFiles;
        private ConnectionInfo _currentConnection;
        private string _currentLocalDbPath;

        public ConnectionDialog(ConnectionInfo currentConnection = null, string currentLocalDbPath = null)
        {
            InitializeComponent();
            _currentConnection = currentConnection;
            _currentLocalDbPath = currentLocalDbPath;
            SQLiteHelper.EnsureDatabase();
            LoadConnections();
            LoadLocalDataFiles();
        }

        private void LoadConnections()
        {
            var list = SQLiteHelper.LoadAll();
            _connections = new ObservableCollection<ConnectionInfo>(list);

            // 标记当前连接
            if (_currentConnection != null)
            {
                var match = _connections.FirstOrDefault(c => c.Id == _currentConnection.Id);
                if (match != null)
                {
                    match.IsCurrent = true;
                    ConnectionList.SelectedItem = match;
                }
            }

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
                original.LocalDbFileName = editCopy.LocalDbFileName;

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
            // 如果在本地数据页签，尝试使用选中的本地数据
            if (MainTabControl.SelectedIndex == 1)
            {
                if (LocalDataList.SelectedItem is LocalDataFileInfo selectedFile)
                {
                    // 自动生成的文件：使用其关联的连接 + 本地数据路径
                    if (selectedFile.IsAutoGenerated && selectedFile.AssociatedConnectionId.HasValue)
                    {
                        var conn = _connections.FirstOrDefault(c => c.Id == selectedFile.AssociatedConnectionId.Value);
                        if (conn != null)
                        {
                            SelectedConnection = conn;
                            SelectedLocalDbPath = selectedFile.FilePath;
                            DialogResult = true;
                            return;
                        }
                    }
                    // 导入的文件：仅使用本地数据，不需要关联连接
                    else
                    {
                        SelectedConnection = null;
                        SelectedLocalDbPath = selectedFile.FilePath;
                        DialogResult = true;
                        return;
                    }
                }
                MessageBox.Show("请先选择一个本地数据文件", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

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
                original.LocalDbFileName = editCopy.LocalDbFileName;
                SQLiteHelper.Update(original);
                SQLiteHelper.SetDefault(original.Id);
                SelectedConnection = original;
                SelectedLocalDbPath = null;
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

        #region 本地数据管理

        private void LoadLocalDataFiles()
        {
            var files = SQLiteHelper.ScanLocalDataFiles();

            // 标记当前使用的本地数据
            if (!string.IsNullOrEmpty(_currentLocalDbPath))
            {
                foreach (var f in files)
                {
                    if (string.Equals(f.FilePath, _currentLocalDbPath, StringComparison.OrdinalIgnoreCase))
                    {
                        f.IsCurrent = true;
                        break;
                    }
                }
            }
            else if (_currentConnection != null)
            {
                var expectedPath = SQLiteHelper.GetLocalDbPath(_currentConnection);
                if (!string.IsNullOrEmpty(expectedPath))
                {
                    foreach (var f in files)
                    {
                        if (string.Equals(f.FilePath, expectedPath, StringComparison.OrdinalIgnoreCase))
                        {
                            f.IsCurrent = true;
                            break;
                        }
                    }
                }
            }

            _localDataFiles = new ObservableCollection<LocalDataFileInfo>(files);
            LocalDataList.ItemsSource = _localDataFiles;

            // 自动选中当前使用的本地数据
            var currentFile = files.FirstOrDefault(f => f.IsCurrent);
            if (currentFile != null)
                LocalDataList.SelectedItem = currentFile;
        }

        private void RefreshLocalData_Click(object sender, RoutedEventArgs e)
        {
            LoadLocalDataFiles();
            LocalDataDetailPanel.IsEnabled = false;
        }

        private void LocalDataList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (LocalDataList.SelectedItem is LocalDataFileInfo selected)
            {
                LocalDataDetailPanel.IsEnabled = true;
                LocalDataFileSizeText.Text = selected.DisplayFileSize;
                LocalDataLastModifiedText.Text = selected.LastModified.ToString("yyyy-MM-dd HH:mm:ss");

                // 根据是否自动生成显示不同的名称和关联信息
                if (selected.IsAutoGenerated)
                {
                    // 自动生成的文件：显示文件名（不可编辑），显示关联连接名
                    LocalDataFileNameText.Text = selected.FileName;
                    LocalDataFileNameText.Visibility = Visibility.Visible;
                    LocalDataFileNameEdit.Visibility = Visibility.Collapsed;
                    RenameLocalDataButton.Visibility = Visibility.Collapsed;

                    LocalDataConnectionText.Text = selected.AssociatedConnectionName ?? "";
                    LocalDataConnectionText.Visibility = Visibility.Visible;
                    LocalDataNoAssociationText.Visibility = Visibility.Collapsed;
                }
                else
                {
                    // 导入的文件：可编辑名称，无需关联
                    LocalDataFileNameText.Visibility = Visibility.Collapsed;
                    LocalDataFileNameEdit.Text = Path.GetFileNameWithoutExtension(selected.FileName);
                    LocalDataFileNameEdit.Visibility = Visibility.Visible;
                    RenameLocalDataButton.Visibility = Visibility.Visible;

                    LocalDataConnectionText.Visibility = Visibility.Collapsed;
                    LocalDataNoAssociationText.Visibility = Visibility.Visible;
                }

                // 读取数据概览
                LoadDataSummary(selected);
            }
            else
            {
                LocalDataDetailPanel.IsEnabled = false;
                LocalDataFileNameText.Text = "";
                LocalDataFileNameText.Visibility = Visibility.Visible;
                LocalDataFileNameEdit.Visibility = Visibility.Collapsed;
                RenameLocalDataButton.Visibility = Visibility.Collapsed;
                LocalDataFileSizeText.Text = "";
                LocalDataLastModifiedText.Text = "";
                LocalDataConnectionText.Visibility = Visibility.Collapsed;
                LocalDataNoAssociationText.Visibility = Visibility.Collapsed;
                LocalDataSummaryText.Text = "";
            }
        }

        private void LoadDataSummary(LocalDataFileInfo fileInfo)
        {
            try
            {
                using (var conn = new SQLiteConnection($"Data Source={fileInfo.FilePath};Version=3;"))
                {
                    conn.Open();

                    int formCount = 0, entityCount = 0, fieldCount = 0;

                    using (var cmd = new SQLiteCommand("SELECT COUNT(*) FROM T_FORM", conn))
                        formCount = Convert.ToInt32(cmd.ExecuteScalar());

                    using (var cmd = new SQLiteCommand("SELECT COUNT(*) FROM T_ENTITY", conn))
                        entityCount = Convert.ToInt32(cmd.ExecuteScalar());

                    using (var cmd = new SQLiteCommand("SELECT COUNT(*) FROM T_FIELD", conn))
                        fieldCount = Convert.ToInt32(cmd.ExecuteScalar());

                    LocalDataSummaryText.Text = $"表单：{formCount}，实体：{entityCount}，字段：{fieldCount}";
                }
            }
            catch
            {
                LocalDataSummaryText.Text = "无法读取数据（文件可能不是有效的元数据库）";
            }
        }

        private void ImportLocalData_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "选择要导入的数据库文件",
                Filter = "SQLite 数据库文件 (*.db)|*.db|所有文件 (*.*)|*.*",
                DefaultExt = ".db"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var importedFileName = SQLiteHelper.ImportLocalData(dialog.FileName);
                    LoadLocalDataFiles();

                    // 选中新导入的文件
                    var imported = _localDataFiles.FirstOrDefault(f => f.FileName == importedFileName);
                    if (imported != null)
                        LocalDataList.SelectedItem = imported;

                    MessageBox.Show($"文件已导入：{importedFileName}", "导入成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"导入失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void DeleteLocalData_Click(object sender, RoutedEventArgs e)
        {
            if (!(LocalDataList.SelectedItem is LocalDataFileInfo selected)) return;

            var result = MessageBox.Show(
                $"确定删除本地数据文件 \"{selected.FileName}\" 吗？\n此操作不可恢复！",
                "删除确认",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    SQLiteHelper.DeleteLocalData(selected.FilePath);
                    _localDataFiles.Remove(selected);
                    LocalDataDetailPanel.IsEnabled = false;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"删除失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void RenameLocalData_Click(object sender, RoutedEventArgs e)
        {
            if (!(LocalDataList.SelectedItem is LocalDataFileInfo selected)) return;
            if (selected.IsAutoGenerated) return; // 自动生成的不允许重命名

            var newName = LocalDataFileNameEdit.Text?.Trim();
            if (string.IsNullOrWhiteSpace(newName))
            {
                MessageBox.Show("名称不能为空", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 检查非法字符
            var invalidChars = Path.GetInvalidFileNameChars();
            if (newName.IndexOfAny(invalidChars) >= 0)
            {
                MessageBox.Show("名称包含非法字符", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var newPath = SQLiteHelper.RenameLocalData(selected.FilePath, newName);
                if (newPath == null)
                {
                    MessageBox.Show("该名称已存在，请使用其他名称", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 刷新列表并重新选中
                LoadLocalDataFiles();
                var newFileName = Path.GetFileName(newPath);
                var renamed = _localDataFiles.FirstOrDefault(f => f.FileName.Equals(newFileName, StringComparison.OrdinalIgnoreCase));
                if (renamed != null)
                    LocalDataList.SelectedItem = renamed;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"重命名失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ConnectWithLocalData_Click(object sender, RoutedEventArgs e)
        {
            if (!(LocalDataList.SelectedItem is LocalDataFileInfo selectedFile)) return;

            if (selectedFile.IsAutoGenerated && selectedFile.AssociatedConnectionId.HasValue)
            {
                // 自动生成的文件：使用关联的连接 + 本地数据路径
                var conn = _connections.FirstOrDefault(c => c.Id == selectedFile.AssociatedConnectionId.Value);
                if (conn != null)
                {
                    SelectedConnection = conn;
                    SelectedLocalDbPath = selectedFile.FilePath;
                    SQLiteHelper.SetDefault(conn.Id);
                    DialogResult = true;
                }
            }
            else
            {
                // 导入的文件：仅使用本地数据，不需要关联连接
                SelectedConnection = null;
                SelectedLocalDbPath = selectedFile.FilePath;
                DialogResult = true;
            }
        }

        #endregion
    }
}
