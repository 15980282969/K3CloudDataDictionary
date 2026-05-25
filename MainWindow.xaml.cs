using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using K3CloudDataDictionary.Helpers;
using K3CloudDataDictionary.Models;
using K3CloudDataDictionary.ViewModels;
using K3CloudDataDictionary.Views;

namespace K3CloudDataDictionary
{
    public partial class MainWindow
    {
        private ModuleTabItem _contextMenuTab;
        private readonly Dictionary<Type, Dictionary<string, PropertyInfo>> _propertyCache = new Dictionary<Type, Dictionary<string, PropertyInfo>>();
        private System.Windows.Threading.DispatcherTimer _filterDebounceTimer;
        private TextBox _pendingFilterTextBox;

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var vm = DataContext as MainViewModel;
            if (vm != null)
            {
                vm.SelectedTabChanged += OnSelectedTabChanged;
            }

            var localDbPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "metadata.db");

            if (System.IO.File.Exists(localDbPath))
            {
                vm.LocalDbPath = localDbPath;
                vm.IsConnected = true;
                vm.StatusText = vm.CurrentConnection != null && !string.IsNullOrWhiteSpace(vm.CurrentConnection.ServerIp)
                    ? $"已连接：{vm.CurrentConnection.DisplayName}（本地数据）"
                    : "本地数据模式";
                vm.LoadTreeData();
            }
            else if (vm?.CurrentConnection != null && !string.IsNullOrWhiteSpace(vm.CurrentConnection.ServerIp))
            {
                vm.IsConnected = true;
                vm.StatusText = $"已连接：{vm.CurrentConnection.DisplayName} | 请刷新元数据";
            }
        }

        private void OnSelectedTabChanged()
        {
            if (MainTabControl != null && DataContext is MainViewModel vm && vm.SelectedTab != null)
            {
                Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Loaded, new Action(() =>
                {
                    var container = MainTabControl.ItemContainerGenerator.ContainerFromItem(vm.SelectedTab);
                    if (container is FrameworkElement fe)
                    {
                        fe.BringIntoView();
                    }
                }));
            }
        }

        private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (DataContext is MainViewModel vm && e.NewValue is ModuleTreeItem item)
            {
                vm.SelectedModule = item;
            }
        }

        private void ConnectionMenuButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.ContextMenu != null)
            {
                element.ContextMenu.PlacementTarget = element;
                element.ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
                element.ContextMenu.HorizontalOffset = 0;
                element.ContextMenu.VerticalOffset = 2;
                element.ContextMenu.IsOpen = true;
            }
        }

        private void MenuConnection_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ConnectionDialog();
            dialog.Owner = this;

            if (dialog.ShowDialog() == true && dialog.SelectedConnection != null)
            {
                var vm = DataContext as MainViewModel;
                vm?.ApplyConnection(dialog.SelectedConnection);
            }
            else
            {
                var vm = DataContext as MainViewModel;
                if (vm == null) return;

                var remaining = SQLiteHelper.LoadAll();
                if (remaining.Count == 0)
                {
                    vm.CurrentConnection = null;
                    vm.IsConnected = false;
                    vm.StatusText = "未连接 | 请添加连接";
                }
            }
        }

        private bool _isRefreshing;

        private void RefreshMetadata_Click(object sender, RoutedEventArgs e)
        {
            var vm = DataContext as MainViewModel;
            if (vm?.CurrentConnection == null || string.IsNullOrWhiteSpace(vm.CurrentConnection.ServerIp))
            {
                MessageBox.Show("请先连接远程数据库，刷新元数据需要有效的远程连接", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (!DbHelper.TestConnection(vm.CurrentConnection.ConnectionString, out string connError))
            {
                MessageBox.Show($"远程数据库连接失败：{connError}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (_isRefreshing) return;

            var result = MessageBox.Show(
                "刷新元数据将重建 T_FORM、T_ENTITY、T_ENTITYSPLIT、T_FIELD 表并重新提取所有元数据，是否继续？",
                "确认刷新",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            _isRefreshing = true;
            RefreshProgressBar.Visibility = Visibility.Visible;
            RefreshProgressBar.Value = 0;
            vm.StatusText = "正在刷新元数据...";

            var connectionString = vm.CurrentConnection.ConnectionString;
            var localDbPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "metadata.db");
            const int batchSize = 20;

            Task.Run(() =>
            {
                try
                {
                    var context = new MetadataContext(connectionString);
                    var allFids = context.GetTargetFids();
                    var totalFids = allFids.Count;

                    Dispatcher.Invoke(() =>
                    {
                        vm.StatusText = $"正在刷新元数据... 共 {totalFids} 个对象";
                        RefreshProgressBar.Maximum = totalFids;
                    });

                    using (var sqliteWriter = new MetadataSqliteWriter(localDbPath))
                    {
                        Dispatcher.Invoke(() => vm.StatusText = "正在保存查找表...");
                        sqliteWriter.WriteLookupTables(connectionString);

                        Task<List<MetadataResult>> nextTask = null;

                        for (int i = 0; i < allFids.Count; i += batchSize)
                        {
                            var batchFids = allFids.Skip(i).Take(batchSize).ToList();
                            if (batchFids.Count == 0) break;

                            List<MetadataResult> currentResults;
                            if (nextTask != null)
                            {
                                currentResults = nextTask.Result;
                            }
                            else
                            {
                                currentResults = MetadataExtractor.ExtractBatch(context, connectionString, batchFids);
                            }

                            int nextIndex = i + batchSize;
                            var nextBatchFids = allFids.Skip(nextIndex).Take(batchSize).ToList();
                            nextTask = nextBatchFids.Count > 0
                                ? Task.Run(() => MetadataExtractor.ExtractBatch(context, connectionString, nextBatchFids))
                                : null;

                            foreach (var r in currentResults)
                            {
                                sqliteWriter.Write(r);
                            }

                            sqliteWriter.Flush();

                            int processed = Math.Min(i + batchSize, totalFids);
                            Dispatcher.Invoke(() =>
                            {
                                vm.StatusText = $"正在刷新元数据... {processed}/{totalFids} ({processed * 100 / totalFids}%)";
                                RefreshProgressBar.Value = processed;
                            });
                        }
                    }

                    Dispatcher.Invoke(() =>
                    {
                        vm.OnRefreshCompleted(localDbPath);
                        RefreshProgressBar.Visibility = Visibility.Collapsed;
                        HandyControl.Controls.Growl.Success(new HandyControl.Data.GrowlInfo
                        {
                            Message = $"元数据刷新完成，共 {totalFids} 个对象",
                            WaitTime = 2
                        });
                    });
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() =>
                    {
                        vm.StatusText = $"元数据刷新失败：{ex.Message}";
                        RefreshProgressBar.Visibility = Visibility.Collapsed;
                        HandyControl.Controls.Growl.Error(new HandyControl.Data.GrowlInfo
                        {
                            Message = $"刷新失败：{ex.Message}",
                            WaitTime = 3
                        });
                    });
                }
                finally
                {
                    _isRefreshing = false;
                }
            });
        }

        private void SearchTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                var vm = DataContext as MainViewModel;
                vm?.SearchCommand.Execute(null);
            }
        }

        private void MainTabControl_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            var dep = (DependencyObject)e.OriginalSource;
            while (dep != null && !(dep is System.Windows.Controls.TabItem))
                dep = VisualTreeHelper.GetParent(dep);

            if (dep is System.Windows.Controls.TabItem tabItem)
            {
                var dataItem = MainTabControl.ItemContainerGenerator.ItemFromContainer(tabItem);
                if (dataItem is ModuleTabItem tab)
                {
                    _contextMenuTab = tab;
                    tabItem.IsSelected = true;
                    var menu = FindResource("TabItemContextMenu") as ContextMenu;
                    if (menu != null)
                    {
                        menu.PlacementTarget = tabItem;
                        menu.IsOpen = true;
                        e.Handled = true;
                    }
                }
            }
        }

        private void DataGrid_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            e.Row.Header = (e.Row.GetIndex() + 1).ToString();
        }

        private void DataGrid_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is DataGrid dataGrid)
            {
                var dep = (DependencyObject)e.OriginalSource;
                while (dep != null && !(dep is DataGridCell))
                    dep = VisualTreeHelper.GetParent(dep);

                if (dep is DataGridCell cell)
                {
                    dataGrid.CommitEdit();
                    dataGrid.UnselectAll();
                    dataGrid.CurrentCell = new DataGridCellInfo(cell);
                    cell.IsSelected = true;
                    cell.Focus();
                    e.Handled = true;
                    if (cell.Content is TextBlock textBlock)
                    {
                        Clipboard.SetText(textBlock.Text ?? "");
                        HandyControl.Controls.Growl.Success(new HandyControl.Data.GrowlInfo
                        {
                            Message = "已复制到剪贴板",
                            WaitTime = 1
                        });
                    }
                }
            }
        }

        private void FormDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left) return;

            var dep = (DependencyObject)e.OriginalSource;
            while (dep != null && !(dep is DataGridRow))
                dep = VisualTreeHelper.GetParent(dep);

            if (dep is DataGridRow row && row.Item is FormInfo form)
            {
                var vm = DataContext as MainViewModel;
                vm?.OpenEntityTab(form);
            }
        }

        private void EntityDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left) return;

            var dep = (DependencyObject)e.OriginalSource;
            while (dep != null && !(dep is DataGridRow))
                dep = VisualTreeHelper.GetParent(dep);

            if (dep is DataGridRow row && row.Item is FormEntityInfo entity)
            {
                var vm = DataContext as MainViewModel;
                vm?.OpenFieldDetailTab(entity);
            }
        }

        private void FilterTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!(sender is TextBox textBox)) return;
            _pendingFilterTextBox = textBox;

            if (_filterDebounceTimer == null)
            {
                _filterDebounceTimer = new System.Windows.Threading.DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(200)
                };
                _filterDebounceTimer.Tick += FilterDebounceTimer_Tick;
            }
            _filterDebounceTimer.Stop();
            _filterDebounceTimer.Start();
        }

        private void FilterDebounceTimer_Tick(object sender, EventArgs e)
        {
            _filterDebounceTimer.Stop();
            if (_pendingFilterTextBox == null) return;

            var textBox = _pendingFilterTextBox;
            _pendingFilterTextBox = null;

            DependencyObject parent = VisualTreeHelper.GetParent(textBox);
            while (parent != null && !(parent is DataGrid))
                parent = VisualTreeHelper.GetParent(parent);

            if (!(parent is DataGrid dataGrid)) return;

            var view = CollectionViewSource.GetDefaultView(dataGrid.ItemsSource);
            if (view == null) return;

            var filters = new Dictionary<string, string>();
            CollectFilters(dataGrid, filters);

            bool hasFilter = filters.Values.Any(v => !string.IsNullOrWhiteSpace(v));

            if (!hasFilter)
            {
                view.Filter = null;
                return;
            }

            view.Filter = item =>
            {
                var type = item.GetType();
                if (!_propertyCache.TryGetValue(type, out var props))
                {
                    props = new Dictionary<string, PropertyInfo>(StringComparer.OrdinalIgnoreCase);
                    foreach (var p in type.GetProperties())
                        props[p.Name] = p;
                    _propertyCache[type] = props;
                }

                foreach (var kvp in filters)
                {
                    if (string.IsNullOrWhiteSpace(kvp.Value)) continue;
                    if (!props.TryGetValue(kvp.Key, out var prop)) continue;
                    var value = prop.GetValue(item)?.ToString() ?? "";
                    if (value.IndexOf(kvp.Value, StringComparison.OrdinalIgnoreCase) < 0)
                        return false;
                }
                return true;
            };
        }

        private void CollectFilters(DependencyObject parent, Dictionary<string, string> filters)
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is TextBox tb && tb.Tag is string tag && !string.IsNullOrEmpty(tag))
                {
                    filters[tag] = tb.Text?.Trim() ?? "";
                }
                CollectFilters(child, filters);
            }
        }

        private void ContextMenu_CloseCurrent(object sender, RoutedEventArgs e)
        {
            if (_contextMenuTab != null)
            {
                var vm = DataContext as MainViewModel;
                vm?.CloseCurrentTabCommand.Execute(_contextMenuTab);
            }
        }

        private void ContextMenu_CloseLeft(object sender, RoutedEventArgs e)
        {
            if (_contextMenuTab != null)
            {
                var vm = DataContext as MainViewModel;
                vm?.CloseLeftTabsCommand.Execute(_contextMenuTab);
            }
        }

        private void ContextMenu_CloseRight(object sender, RoutedEventArgs e)
        {
            if (_contextMenuTab != null)
            {
                var vm = DataContext as MainViewModel;
                vm?.CloseRightTabsCommand.Execute(_contextMenuTab);
            }
        }

        private void ContextMenu_CloseOther(object sender, RoutedEventArgs e)
        {
            if (_contextMenuTab != null)
            {
                var vm = DataContext as MainViewModel;
                vm?.CloseOtherTabsCommand.Execute(_contextMenuTab);
            }
        }
    }
}
