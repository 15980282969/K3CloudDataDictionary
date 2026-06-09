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

        private TabContentTemplateSelector _templateSelector;
        private Dictionary<ModuleTabItem, FrameworkElement> _tabContentMap = new Dictionary<ModuleTabItem, FrameworkElement>();

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
                vm.OpenTabs.CollectionChanged += OpenTabs_CollectionChanged;
                vm.PropertyChanged += (s, args) =>
                {
                    if (args.PropertyName == nameof(MainViewModel.SelectedTab))
                        UpdateTabIsSelected();
                };
            }

            _templateSelector = FindResource("TabContentSelector") as TabContentTemplateSelector;

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

        private void OpenTabs_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (ModuleTabItem tab in e.NewItems)
                {
                    AddTabContent(tab);
                }
            }
            if (e.OldItems != null)
            {
                foreach (ModuleTabItem tab in e.OldItems)
                {
                    RemoveTabContent(tab);
                }
            }
            UpdateTabVisibility();
            RebuildTabHeaders();
        }

        private void RebuildTabHeaders()
        {
            if (TabListContainer == null) return;
            var vm = DataContext as MainViewModel;
            if (vm == null) return;

            TabListContainer.Children.Clear();
            TabListContainer.ColumnDefinitions.Clear();

            int count = vm.OpenTabs.Count;
            if (count == 0) return;

            // 每个标签固定宽度 180
            for (int i = 0; i < count; i++)
                TabListContainer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(180) });

            for (int i = 0; i < count; i++)
            {
                var tab = vm.OpenTabs[i];
                var card = CreateTabCard(tab);
                Grid.SetColumn(card, i);
                TabListContainer.Children.Add(card);
            }

            UpdateTabIsSelected();
            ScrollToSelectedTab();
        }

        private void ScrollToSelectedTab()
        {
            if (TabScrollViewer == null) return;
            Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Loaded, new Action(() =>
            {
                if (!(DataContext is MainViewModel vm) || vm.SelectedTab == null) return;
                for (int i = 0; i < vm.OpenTabs.Count; i++)
                {
                    if (vm.OpenTabs[i] == vm.SelectedTab)
                    {
                        var card = TabListContainer.Children[i] as FrameworkElement;
                        if (card != null)
                        {
                            double offset = card.TranslatePoint(new Point(0, 0), TabScrollViewer).X;
                            double viewportWidth = TabScrollViewer.ViewportWidth;
                            double itemWidth = card.ActualWidth;

                            if (offset < 0)
                                TabScrollViewer.ScrollToHorizontalOffset(TabScrollViewer.HorizontalOffset + offset - 10);
                            else if (offset + itemWidth > viewportWidth)
                                TabScrollViewer.ScrollToHorizontalOffset(TabScrollViewer.HorizontalOffset + (offset + itemWidth - viewportWidth) + 10);
                        }
                        break;
                    }
                }
            }));
        }

        private FrameworkElement CreateTabCard(ModuleTabItem tab)
        {
            // 外层：方角，#37495C 背景，右侧 2px 间隔
            var outerCard = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(0x37, 0x49, 0x5C)),
                Padding = new Thickness(0, 0, 2, 0),
                SnapsToDevicePixels = true,
                DataContext = tab
            };

            // 内层：圆角，白色/靛蓝背景
            var innerCard = new Border
            {
                Name = "TabCard",
                CornerRadius = new CornerRadius(6, 6, 0, 0),
                Padding = new Thickness(10, 1, 10, 1),
                SnapsToDevicePixels = true
            };

            // 内层选中/悬停样式
            var style = new Style(typeof(Border));
            style.Setters.Add(new Setter(BackgroundProperty, new SolidColorBrush(Color.FromRgb(0xF0, 0xF3, 0xF7))));
            var isSelectedTrigger = new DataTrigger { Binding = new Binding("IsSelected"), Value = true };
            isSelectedTrigger.Setters.Add(new Setter(BackgroundProperty, new SolidColorBrush(Color.FromRgb(0x32, 0x6C, 0xF3))));
            style.Triggers.Add(isSelectedTrigger);
            var isMouseOverTrigger = new MultiDataTrigger();
            isMouseOverTrigger.Conditions.Add(new Condition { Binding = new Binding("IsMouseOver"), Value = true });
            isMouseOverTrigger.Conditions.Add(new Condition { Binding = new Binding("IsSelected"), Value = false });
            isMouseOverTrigger.Setters.Add(new Setter(BackgroundProperty, new SolidColorBrush(Color.FromRgb(0xEE, 0xF2, 0xFF))));
            style.Triggers.Add(isMouseOverTrigger);
            innerCard.Style = style;

            var grid = new Grid { VerticalAlignment = VerticalAlignment.Stretch };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var text = new TextBlock
            {
                FontSize = 14,
                TextTrimming = TextTrimming.CharacterEllipsis,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            text.SetBinding(TextBlock.TextProperty, new Binding("Header"));

            var textStyle = new Style(typeof(TextBlock));
            textStyle.Setters.Add(new Setter(ForegroundProperty, new SolidColorBrush(Color.FromRgb(0x33, 0x41, 0x55))));
            textStyle.Setters.Add(new Setter(FontWeightProperty, FontWeights.Normal));
            var textSelectedTrigger = new DataTrigger { Binding = new Binding("IsSelected"), Value = true };
            textSelectedTrigger.Setters.Add(new Setter(ForegroundProperty, Brushes.White));
            textSelectedTrigger.Setters.Add(new Setter(FontWeightProperty, FontWeights.Bold));
            textStyle.Triggers.Add(textSelectedTrigger);
            var textHoverTrigger = new MultiDataTrigger();
            textHoverTrigger.Conditions.Add(new Condition { Binding = new Binding("IsMouseOver"), Value = true });
            textHoverTrigger.Conditions.Add(new Condition { Binding = new Binding("IsSelected"), Value = false });
            textHoverTrigger.Setters.Add(new Setter(FontWeightProperty, FontWeights.SemiBold));
            textStyle.Triggers.Add(textHoverTrigger);
            text.Style = textStyle;

            Grid.SetColumn(text, 0);
            grid.Children.Add(text);

            var closeButton = new Button
            {
                Width = 18,
                Height = 18,
                Margin = new Thickness(4, 0, -2, 0),
                Cursor = System.Windows.Input.Cursors.Hand,
                VerticalAlignment = VerticalAlignment.Center,
                Tag = tab
            };
            closeButton.Click += TabItemClose_Click;

            var closeStyle = new Style(typeof(Button));
            closeStyle.Setters.Add(new Setter(BackgroundProperty, Brushes.Transparent));
            closeStyle.Setters.Add(new Setter(BorderThicknessProperty, new Thickness(0)));
            closeStyle.Setters.Add(new Setter(VisibilityProperty, Visibility.Collapsed));
            var closeSelectedTrigger = new DataTrigger { Binding = new Binding("IsSelected"), Value = true };
            closeSelectedTrigger.Setters.Add(new Setter(VisibilityProperty, Visibility.Visible));
            closeStyle.Triggers.Add(closeSelectedTrigger);
            var closeMouseOverTrigger = new MultiDataTrigger();
            closeMouseOverTrigger.Conditions.Add(new Condition { Binding = new Binding("IsMouseOver"), Value = true });
            closeMouseOverTrigger.Conditions.Add(new Condition { Binding = new Binding("IsSelected"), Value = false });
            closeMouseOverTrigger.Setters.Add(new Setter(VisibilityProperty, Visibility.Visible));
            closeStyle.Triggers.Add(closeMouseOverTrigger);
            closeButton.Style = closeStyle;

            var closeControlTemplate = new ControlTemplate(typeof(Button));
            var closeBorder = new FrameworkElementFactory(typeof(Border), "CloseBtnBorder");
            closeBorder.SetValue(Border.BackgroundProperty, Brushes.Transparent);
            closeBorder.SetValue(Border.CornerRadiusProperty, new CornerRadius(4));
            closeBorder.SetValue(Border.WidthProperty, 18.0);
            closeBorder.SetValue(Border.HeightProperty, 18.0);
            var closeText = new FrameworkElementFactory(typeof(TextBlock), "CloseBtnText");
            closeText.SetValue(TextBlock.TextProperty, "✕");
            closeText.SetValue(TextBlock.FontSizeProperty, 10.0);
            closeText.SetValue(TextBlock.ForegroundProperty, new SolidColorBrush(Color.FromRgb(0x94, 0xA3, 0xB8)));
            closeText.SetValue(TextBlock.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            closeText.SetValue(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center);
            closeBorder.AppendChild(closeText);
            closeControlTemplate.VisualTree = closeBorder;
            var closeHoverTrigger = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
            closeHoverTrigger.Setters.Add(new Setter(Border.BackgroundProperty, new SolidColorBrush(Color.FromRgb(0xFE, 0xE2, 0xE2)), "CloseBtnBorder"));
            closeHoverTrigger.Setters.Add(new Setter(TextBlock.ForegroundProperty, new SolidColorBrush(Color.FromRgb(0xEF, 0x44, 0x44)), "CloseBtnText"));
            closeControlTemplate.Triggers.Add(closeHoverTrigger);
            closeButton.Template = closeControlTemplate;

            Grid.SetColumn(closeButton, 1);
            grid.Children.Add(closeButton);

            innerCard.Child = grid;
            outerCard.Child = innerCard;

            // 鼠标事件绑定到外层
            var vm = DataContext as MainViewModel;
            outerCard.MouseLeftButtonUp += (s, e) =>
            {
                if (vm != null) vm.SelectedTab = tab;
            };
            outerCard.MouseEnter += (s, e) => tab.IsMouseOver = true;
            outerCard.MouseLeave += (s, e) => tab.IsMouseOver = false;
            outerCard.MouseRightButtonUp += (s, e) =>
            {
                _contextMenuTab = tab;
                if (vm != null) vm.SelectedTab = tab;
                var menu = FindResource("TabItemContextMenu") as ContextMenu;
                if (menu != null)
                {
                    menu.PlacementTarget = outerCard;
                    menu.IsOpen = true;
                }
            };

            return outerCard;
        }

        private void AddTabContent(ModuleTabItem tab)
        {
            if (_templateSelector == null || TabContentPanel == null) return;

            var template = _templateSelector.SelectTemplate(tab, null);
            if (template == null) return;

            var content = template.LoadContent() as FrameworkElement;
            if (content == null) return;

            content.DataContext = tab;
            content.Visibility = Visibility.Collapsed;

            TabContentPanel.Children.Add(content);
            _tabContentMap[tab] = content;
        }

        private void RemoveTabContent(ModuleTabItem tab)
        {
            if (_tabContentMap.TryGetValue(tab, out var content))
            {
                TabContentPanel.Children.Remove(content);
                _tabContentMap.Remove(tab);
            }
        }

        private void UpdateTabVisibility()
        {
            var vm = DataContext as MainViewModel;
            if (vm == null) return;

            foreach (var kvp in _tabContentMap)
            {
                kvp.Value.Visibility = (kvp.Key == vm.SelectedTab) ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private void OnSelectedTabChanged()
        {
            UpdateTabVisibility();
            UpdateTabIsSelected();
        }

        private void UpdateTabIsSelected()
        {
            if (!(DataContext is MainViewModel vm)) return;
            foreach (var tab in vm.OpenTabs)
            {
                tab.IsSelected = (tab == vm.SelectedTab);
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

        private void SearchTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            var vm = DataContext as MainViewModel;
            if (vm != null) vm.IsSearchFocused = true;
        }

        private void SearchTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            var vm = DataContext as MainViewModel;
            if (vm != null) vm.IsSearchFocused = false;
        }

        private async void ShowAllFields_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as FrameworkElement;
            if (btn == null) return;

            // 向上查找 DataContext 为 ModuleTabItem 的元素
            var dep = btn as DependencyObject;
            while (dep != null && !(dep is FrameworkElement fe && fe.DataContext is ModuleTabItem))
                dep = VisualTreeHelper.GetParent(dep);

            if (dep is FrameworkElement element && element.DataContext is ModuleTabItem tab)
            {
                // 从 FormEntities 中获取 FormId
                if (tab.FormEntities.Count > 0)
                {
                    var firstEntity = tab.FormEntities[0];
                    var vm = DataContext as MainViewModel;
                    if (vm != null)
                    {
                        await vm.OpenAllFieldsTabAsync(firstEntity.FormId, firstEntity.FormName);
                    }
                }
            }
        }

        private void TabItemClose_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.Tag is ModuleTabItem tab)
            {
                var vm = DataContext as MainViewModel;
                vm?.CloseCurrentTabCommand.Execute(tab);
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
            if (view == null) return;

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

        private async void FieldDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left) return;

            var dep = (DependencyObject)e.OriginalSource;
            while (dep != null && !(dep is DataGridRow))
                dep = VisualTreeHelper.GetParent(dep);

            if (!(dep is DataGridRow dataRow)) return;
            var dataGrid = (DataGrid)sender;
            var rowItem = dataGrid.ItemContainerGenerator.ItemFromContainer(dataRow);
            if (!(rowItem is Models.FieldInfo field)) return;

            var vm = DataContext as MainViewModel;
            if (vm == null) return;

            // 根据行数据自动判断联查类型
            if (!string.IsNullOrWhiteSpace(field.LookUpObjectID))
            {
                await vm.OpenLookupEntityTabAsync(field);
            }
            else if (!string.IsNullOrWhiteSpace(field.EnumType))
            {
                await vm.OpenEnumDetailTabAsync(field);
            }
            else if (field.ElementTypeName == "单据类型")
            {
                var currentTab = vm.SelectedTab;
                if (currentTab != null && currentTab.ModuleId.StartsWith("field_"))
                {
                    var parts = currentTab.ModuleId.Split('_');
                    if (parts.Length >= 2)
                    {
                        var formId = parts[1];
                        var formIdentifier = vm.GetFormIdentifierByFormId(formId);
                        if (!string.IsNullOrWhiteSpace(formIdentifier))
                        {
                            await vm.OpenBillTypeTabAsync(formIdentifier);
                        }
                    }
                }
            }
        }

        private async void AllFieldsDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left) return;

            var dep = (DependencyObject)e.OriginalSource;
            while (dep != null && !(dep is DataGridRow))
                dep = VisualTreeHelper.GetParent(dep);

            if (!(dep is DataGridRow dataRow)) return;
            var dataGrid = (DataGrid)sender;
            var rowItem = dataGrid.ItemContainerGenerator.ItemFromContainer(dataRow);
            if (!(rowItem is Models.AllFieldInfo allField)) return;

            var vm = DataContext as MainViewModel;
            if (vm == null) return;

            // 根据行数据自动判断联查类型
            if (!string.IsNullOrWhiteSpace(allField.LookUpObjectID))
            {
                var fieldInfo = new Models.FieldInfo
                {
                    LookUpObjectID = allField.LookUpObjectID,
                    LookUpObjectDisplay = allField.LookUpObjectDisplay
                };
                await vm.OpenLookupEntityTabAsync(fieldInfo);
            }
            else if (!string.IsNullOrWhiteSpace(allField.EnumType))
            {
                var fieldInfo = new Models.FieldInfo
                {
                    EnumType = allField.EnumType,
                    EnumTypeDisplay = allField.EnumTypeDisplay
                };
                await vm.OpenEnumDetailTabAsync(fieldInfo);
            }
            else if (allField.ElementTypeName == "单据类型")
            {
                var currentTab = vm.SelectedTab;
                if (currentTab != null && currentTab.ModuleId.StartsWith("allfields_"))
                {
                    var formId = currentTab.ModuleId.Substring("allfields_".Length);
                    var formIdentifier = vm.GetFormIdentifierByFormId(formId);
                    if (!string.IsNullOrWhiteSpace(formIdentifier))
                    {
                        await vm.OpenBillTypeTabAsync(formIdentifier);
                    }
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

        private void TabListPopup_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            TabListButton.IsChecked = false;
            ScrollToSelectedTab();
        }

        private void ScrollLeftButton_Click(object sender, RoutedEventArgs e)
        {
            if (TabScrollViewer != null)
                TabScrollViewer.ScrollToHorizontalOffset(TabScrollViewer.HorizontalOffset - 150);
        }

        private void ScrollRightButton_Click(object sender, RoutedEventArgs e)
        {
            if (TabScrollViewer != null)
                TabScrollViewer.ScrollToHorizontalOffset(TabScrollViewer.HorizontalOffset + 150);
        }

        private void TabScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (TabScrollViewer == null) return;

            if (e.Delta > 0)
                TabScrollViewer.ScrollToHorizontalOffset(TabScrollViewer.HorizontalOffset - 150);
            else
                TabScrollViewer.ScrollToHorizontalOffset(TabScrollViewer.HorizontalOffset + 150);

            e.Handled = true;
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
