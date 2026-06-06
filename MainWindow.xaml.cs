using K3CloudDataDictionary.Helpers;
using K3CloudDataDictionary.Models;
using K3CloudDataDictionary.ViewModels;
using K3CloudDataDictionary.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace K3CloudDataDictionary
{
    public partial class MainWindow
    {
        private ModuleTabItem _contextMenuTab;
        private readonly Dictionary<Type, Dictionary<string, PropertyInfo>> _propertyCache = new Dictionary<Type, Dictionary<string, PropertyInfo>>();
        private System.Windows.Threading.DispatcherTimer _filterDebounceTimer;
        private TextBox _pendingFilterTextBox;
        private int _isRefreshing; // 0=false, 1=true，用于 Interlocked 原子操作

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
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
                await vm.LoadTreeDataAsync();
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

        private async void MenuConnection_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ConnectionDialog();
            dialog.Owner = this;

            if (dialog.ShowDialog() == true && dialog.SelectedConnection != null)
            {
                var vm = DataContext as MainViewModel;
                if (vm != null)
                {
                    await vm.ApplyConnectionAsync(dialog.SelectedConnection);
                }
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

            if (Interlocked.CompareExchange(ref _isRefreshing, 1, 0) != 0) return;

            var result = MessageBox.Show(
                "刷新元数据将重建 T_FORM、T_ENTITY、T_ENTITYSPLIT、T_FIELD 表并重新提取所有元数据，是否继续？",
                "确认刷新",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
            {
                Interlocked.Exchange(ref _isRefreshing, 0);
                return;
            }

            RefreshProgressBar.Visibility = Visibility.Visible;
            RefreshProgressBar.Value = 0;
            vm.StatusText = "正在刷新元数据...";

            var connectionString = vm.CurrentConnection.ConnectionString;
            var localDbPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "metadata.db");

            Task.Run(() =>
            {
                try
                {
                    var context = new MetadataContext(connectionString);
                    var fidsWithoutExt = context.GetTargetFidsWithoutExtensions();
                    var fidsWithExt = context.GetTargetFidsWithExtensions();
                    var totalFids = fidsWithoutExt.Count + fidsWithExt.Count;

                    Dispatcher.Invoke(() =>
                    {
                        vm.StatusText = $"正在刷新元数据... 共 {totalFids} 个对象（无扩展 {fidsWithoutExt.Count}，有扩展 {fidsWithExt.Count}）";
                        RefreshProgressBar.Maximum = totalFids;
                    });

                    using (var sqliteWriter = new MetadataSqliteWriter(localDbPath))
                    {
                        Dispatcher.Invoke(() => vm.StatusText = "正在保存查找表...");
                        sqliteWriter.WriteLookupTables(connectionString);

                        int totalProcessed = 0;

                        // 第一阶段：处理无扩展的FID
                        totalProcessed = ProcessBatch(context, connectionString, sqliteWriter, fidsWithoutExt, totalProcessed, totalFids, vm, "无扩展");

                        // 第二阶段：处理有扩展的FID
                        ProcessBatch(context, connectionString, sqliteWriter, fidsWithExt, totalProcessed, totalFids, vm, "有扩展");
                    }

                    Dispatcher.Invoke(() =>
                    {
                        vm.OnRefreshCompletedAsync(localDbPath).ConfigureAwait(false);
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
                    Interlocked.Exchange(ref _isRefreshing, 0);
                }
            });
        }

        private void RefreshExtensionMetadata_Click(object sender, RoutedEventArgs e)
        {
            var vm = DataContext as MainViewModel;
            if (vm?.CurrentConnection == null || string.IsNullOrWhiteSpace(vm.CurrentConnection.ServerIp))
            {
                MessageBox.Show("请先连接远程数据库", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (!DbHelper.TestConnection(vm.CurrentConnection.ConnectionString, out string connError))
            {
                MessageBox.Show($"远程数据库连接失败：{connError}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (!vm.HasLocalData)
            {
                MessageBox.Show("请先执行\"重新获取元数据\"以建立本地数据", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (Interlocked.CompareExchange(ref _isRefreshing, 1, 0) != 0) return;

            var result = MessageBox.Show(
                "将仅重新获取存在扩展的元数据，是否继续？",
                "确认刷新",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
            {
                Interlocked.Exchange(ref _isRefreshing, 0);
                return;
            }

            RefreshProgressBar.Visibility = Visibility.Visible;
            RefreshProgressBar.Value = 0;
            vm.StatusText = "正在刷新扩展元数据...";

            var connectionString = vm.CurrentConnection.ConnectionString;
            var localDbPath = vm.LocalDbPath;

            Task.Run(() =>
            {
                try
                {
                    var context = new MetadataContext(connectionString);
                    var fidsWithExt = context.GetTargetFidsWithExtensions();

                    if (fidsWithExt.Count == 0)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            vm.StatusText = "未发现存在扩展的元数据";
                            RefreshProgressBar.Visibility = Visibility.Collapsed;
                            HandyControl.Controls.Growl.Info(new HandyControl.Data.GrowlInfo
                            {
                                Message = "未发现存在扩展的元数据",
                                WaitTime = 2
                            });
                        });
                        return;
                    }

                    Dispatcher.Invoke(() =>
                    {
                        vm.StatusText = $"正在刷新扩展元数据... 共 {fidsWithExt.Count} 个对象";
                        RefreshProgressBar.Maximum = fidsWithExt.Count;
                    });

                    using (var sqliteWriter = new MetadataSqliteWriter(localDbPath, false))
                    {
                        // 先删除存在扩展的旧数据
                        sqliteWriter.DeleteFormsByIdentifiers(fidsWithExt);

                        // 重新获取
                        ProcessBatch(context, connectionString, sqliteWriter, fidsWithExt, 0, fidsWithExt.Count, vm, "有扩展");
                    }

                    Dispatcher.Invoke(() =>
                    {
                        vm.OnRefreshCompletedAsync(localDbPath).ConfigureAwait(false);
                        RefreshProgressBar.Visibility = Visibility.Collapsed;
                        HandyControl.Controls.Growl.Success(new HandyControl.Data.GrowlInfo
                        {
                            Message = $"扩展元数据刷新完成，共 {fidsWithExt.Count} 个对象",
                            WaitTime = 2
                        });
                    });
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() =>
                    {
                        vm.StatusText = $"扩展元数据刷新失败：{ex.Message}";
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
                    Interlocked.Exchange(ref _isRefreshing, 0);
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

        private int ProcessBatch(MetadataContext context, string connectionString, MetadataSqliteWriter sqliteWriter, List<string> fids, int totalProcessed, int totalFids, MainViewModel vm, string phase)
        {
            const int batchSize = 50;
            Task<List<MetadataResult>> nextTask = null;

            for (int i = 0; i < fids.Count; i += batchSize)
            {
                var batchFids = fids.Skip(i).Take(batchSize).ToList();
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
                var nextBatchFids = fids.Skip(nextIndex).Take(batchSize).ToList();
                nextTask = nextBatchFids.Count > 0
                    ? Task.Run(() => MetadataExtractor.ExtractBatch(context, connectionString, nextBatchFids))
                    : null;

                foreach (var r in currentResults)
                {
                    sqliteWriter.Write(r);
                }

                sqliteWriter.Flush();

                totalProcessed += batchFids.Count;
                int captured = totalProcessed;
                Dispatcher.Invoke(() =>
                {
                    vm.StatusText = $"正在刷新元数据[{phase}]... {captured}/{totalFids} ({captured * 100 / totalFids}%)";
                    RefreshProgressBar.Value = captured;
                });
            }

            return totalProcessed;
        }

        private void DataGrid_Sorting(object sender, DataGridSortingEventArgs e)
        {
            var dataGrid = (DataGrid)sender;
            var view = CollectionViewSource.GetDefaultView(dataGrid.ItemsSource);
            if (view == null || view.SortDescriptions.Count == 0) return;

            var currentSort = view.SortDescriptions.FirstOrDefault(s => s.PropertyName == e.Column.SortMemberPath);
            if (currentSort.PropertyName == e.Column.SortMemberPath && currentSort.Direction == System.ComponentModel.ListSortDirection.Descending)
            {
                e.Handled = true;
                view.SortDescriptions.Clear();
                e.Column.SortDirection = null;
            }
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

        private async void FormDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left) return;

            var dep = (DependencyObject)e.OriginalSource;
            while (dep != null && !(dep is DataGridRow))
                dep = VisualTreeHelper.GetParent(dep);

            if (dep is DataGridRow row && row.Item is FormInfo form)
            {
                var vm = DataContext as MainViewModel;
                if (vm != null)
                {
                    await vm.OpenEntityTabAsync(form);
                }
            }
        }

        private async void EntityDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left) return;

            var dep = (DependencyObject)e.OriginalSource;
            while (dep != null && !(dep is DataGridRow))
                dep = VisualTreeHelper.GetParent(dep);

            if (dep is DataGridRow row && row.Item is FormEntityInfo entity)
            {
                var vm = DataContext as MainViewModel;
                if (vm != null)
                {
                    await vm.OpenFieldDetailTabAsync(entity);
                }
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
