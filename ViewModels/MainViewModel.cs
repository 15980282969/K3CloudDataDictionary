using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using K3CloudDataDictionary.Helpers;
using K3CloudDataDictionary.Models;

namespace K3CloudDataDictionary.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<ModuleTreeItem> _moduleTree;
        private ModuleTreeItem _selectedModule;
        private ObservableCollection<OperatorItem> _operators;
        private OperatorItem _selectedOperator;
        private string _searchText;
        private bool _isSearchFocused;
        private ConnectionInfo _currentConnection;
        private bool _isConnected;
        private string _statusText;
        private ObservableCollection<ModuleTabItem> _openTabs;
        private ModuleTabItem _selectedTab;
        private ICommand _closeCurrentTabCommand;
        private ICommand _closeLeftTabsCommand;
        private ICommand _closeRightTabsCommand;
        private ICommand _closeOtherTabsCommand;
        private bool _isClosingTab;
        private string _localDbPath;

        public ObservableCollection<ModuleTreeItem> ModuleTree
        {
            get => _moduleTree;
            set { _moduleTree = value; OnPropertyChanged(); }
        }

        public ModuleTreeItem SelectedModule
        {
            get => _selectedModule;
            set
            {
                if (_selectedModule != value)
                {
                    _selectedModule = value;
                    OnPropertyChanged();
                    OnModuleSelected(value);
                }
            }
        }

        public ObservableCollection<OperatorItem> Operators
        {
            get => _operators;
            set { _operators = value; OnPropertyChanged(); }
        }

        public OperatorItem SelectedOperator
        {
            get => _selectedOperator;
            set { _selectedOperator = value; OnPropertyChanged(); }
        }

        public string SearchText
        {
            get => _searchText;
            set { _searchText = value; OnPropertyChanged(); OnPropertyChanged(nameof(SearchPlaceholderVisible)); }
        }

        public bool IsSearchFocused
        {
            get => _isSearchFocused;
            set { _isSearchFocused = value; OnPropertyChanged(); OnPropertyChanged(nameof(SearchPlaceholderVisible)); }
        }

        public bool SearchPlaceholderVisible => string.IsNullOrEmpty(_searchText) && !_isSearchFocused;

        public ConnectionInfo CurrentConnection
        {
            get => _currentConnection;
            set { _currentConnection = value; OnPropertyChanged(); }
        }

        public bool IsConnected
        {
            get => _isConnected;
            set { _isConnected = value; OnPropertyChanged(); }
        }

        public string StatusText
        {
            get => _statusText;
            set { _statusText = value; OnPropertyChanged(); }
        }

        public ObservableCollection<ModuleTabItem> OpenTabs
        {
            get => _openTabs;
            set { _openTabs = value; OnPropertyChanged(); }
        }

        public ModuleTabItem SelectedTab
        {
            get => _selectedTab;
            set
            {
                if (_selectedTab != value)
                {
                    _selectedTab = value;
                    OnPropertyChanged();
                    SelectedTabChanged?.Invoke();
                    UpdateStatusForTab(value);
                }
            }
        }

        public string LocalDbPath
        {
            get => _localDbPath;
            set { _localDbPath = value; OnPropertyChanged(); }
        }

        public bool HasLocalData => !string.IsNullOrEmpty(LocalDbPath) && File.Exists(LocalDbPath);

        private void UpdateStatusForTab(ModuleTabItem tab)
        {
            if (tab == null)
            {
                if (IsConnected) StatusText = "本地数据";
                return;
            }

            switch (tab.TabType)
            {
                case TabType.Form:
                    StatusText = $"本地数据 | {tab.Header} - {tab.Forms.Count} 条表单";
                    break;
                case TabType.Entity:
                    StatusText = $"本地数据 | {tab.Header} - {tab.FormEntities.Count} 条实体";
                    break;
                case TabType.Field:
                    StatusText = $"本地数据 | {tab.Header} - {tab.Fields.Count} 条记录";
                    break;
                case TabType.Enum:
                    StatusText = $"本地数据 | {tab.Header} - {tab.EnumItems.Count} 条枚举项";
                    break;
                case TabType.AllFields:
                    StatusText = $"本地数据 | {tab.Header} - {tab.AllFields.Count} 条记录";
                    break;
                case TabType.BillType:
                    StatusText = $"本地数据 | {tab.Header} - {tab.BillTypes.Count} 条单据类型";
                    break;
            }
        }

        public event Action SelectedTabChanged;

        public ICommand CloseCurrentTabCommand
        {
            get { return _closeCurrentTabCommand ?? (_closeCurrentTabCommand = new RelayCommand(CloseCurrentTab)); }
        }

        public ICommand CloseLeftTabsCommand
        {
            get { return _closeLeftTabsCommand ?? (_closeLeftTabsCommand = new RelayCommand(CloseLeftTabs)); }
        }

        public ICommand CloseRightTabsCommand
        {
            get { return _closeRightTabsCommand ?? (_closeRightTabsCommand = new RelayCommand(CloseRightTabs)); }
        }

        public ICommand CloseOtherTabsCommand
        {
            get { return _closeOtherTabsCommand ?? (_closeOtherTabsCommand = new RelayCommand(CloseOtherTabs)); }
        }

        public ICommand SearchCommand { get; }

        public MainViewModel()
        {
            ModuleTree = new ObservableCollection<ModuleTreeItem>();
            Operators = new ObservableCollection<OperatorItem>
            {
                new OperatorItem { DisplayName = "等于", OperatorValue = "=" },
                new OperatorItem { DisplayName = "包含", OperatorValue = "LIKE" },
                new OperatorItem { DisplayName = "左包含", OperatorValue = "LIKE_START" },
                new OperatorItem { DisplayName = "右包含", OperatorValue = "LIKE_END" }
            };
            SelectedOperator = Operators[0];
            OpenTabs = new ObservableCollection<ModuleTabItem>();
            OpenTabs.CollectionChanged += OpenTabs_CollectionChanged;
            StatusText = "未连接";
            SearchCommand = new RelayCommand(ExecuteSearch);

            LoadSavedConnection();
        }

        private void OpenTabs_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Remove || e.Action == NotifyCollectionChangedAction.Reset)
            {
                if (!_isClosingTab)
                {
                    if (OpenTabs.Count > 0 && (SelectedTab == null || !OpenTabs.Contains(SelectedTab)))
                    {
                        SelectedTab = OpenTabs[OpenTabs.Count - 1];
                    }
                    else if (OpenTabs.Count == 0)
                    {
                        SelectedTab = null;
                    }
                }
            }
        }

        private void LoadSavedConnection()
        {
            SQLiteHelper.EnsureDatabase();
            var saved = SQLiteHelper.LoadDefault();
            if (saved != null && !string.IsNullOrWhiteSpace(saved.ServerIp))
            {
                CurrentConnection = saved;
                StatusText = "正在尝试自动连接...";
            }
        }

        public async Task ApplyConnectionAsync(ConnectionInfo connection)
        {
            CurrentConnection = connection;
            IsConnected = true;

            LocalDbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "metadata.db");

            if (HasLocalData)
            {
                StatusText = $"已连接：{connection.DisplayName}（本地数据）";
                await LoadTreeDataAsync();
            }
            else
            {
                StatusText = $"已连接：{connection.DisplayName} | 请刷新元数据";
            }
        }

        public async Task OnRefreshCompletedAsync(string localDbPath)
        {
            LocalDbPath = localDbPath;
            OpenTabs.Clear();
            StatusText = $"已连接：{CurrentConnection?.DisplayName}（本地数据）";
            await LoadTreeDataAsync();
        }

        private List<Dictionary<string, object>> ExecuteQuery(string sql, IEnumerable<SQLiteParameter> parameters = null)
        {
            var results = new List<Dictionary<string, object>>();
            using (var conn = new SQLiteConnection($"Data Source={LocalDbPath};Version=3;"))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    if (parameters != null)
                    {
                        foreach (var p in parameters)
                            cmd.Parameters.Add(p);
                    }
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var row = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                string colName = reader.GetName(i);
                                row[colName] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                            }
                            results.Add(row);
                        }
                    }
                }
            }
            return results;
        }

        private async void OnModuleSelected(ModuleTreeItem module)
        {
            if (module == null) return;
            module.IsExpanded = true;
            await OpenTabForModuleAsync(module);
        }

        private async Task OpenTabForModuleAsync(ModuleTreeItem module)
        {
            var existingTab = OpenTabs.FirstOrDefault(t => t.ModuleId == module.Id && t.TabType == TabType.Form);
            if (existingTab != null)
            {
                SelectedTab = existingTab;
                return;
            }

            var tab = new ModuleTabItem
            {
                Header = module.Text,
                ModuleId = module.Id,
                TabType = TabType.Form
            };

            await LoadFormDataAsync(tab, module.Id);

            OpenTabs.Add(tab);
            SelectedTab = tab;
        }

        public async Task OpenEntityTabAsync(FormInfo form)
        {
            if (form == null || !HasLocalData) return;

            string tabKey = $"entity_{form.FormId}";
            var existingTab = OpenTabs.FirstOrDefault(t => t.ModuleId == tabKey);
            if (existingTab != null)
            {
                SelectedTab = existingTab;
                return;
            }

            var tab = new ModuleTabItem
            {
                Header = $"{form.FormName}",
                ModuleId = tabKey,
                TabType = TabType.Entity
            };

            await LoadEntityDataAsync(tab, form.FormId);

            OpenTabs.Add(tab);
            SelectedTab = tab;
        }

        public async Task OpenFieldDetailTabAsync(FormEntityInfo entity)
        {
            if (entity == null || !HasLocalData) return;

            string tabKey = $"field_{entity.FormId}_{entity.EntityId}";
            var existingTab = OpenTabs.FirstOrDefault(t => t.ModuleId == tabKey);
            if (existingTab != null)
            {
                SelectedTab = existingTab;
                return;
            }

            var tab = new ModuleTabItem
            {
                Header = $"{entity.FormName} - {entity.EntityName}",
                ModuleId = tabKey,
                TabType = TabType.Field
            };

            await LoadFieldDataAsync(tab, entity.FormId, entity.EntityId);

            OpenTabs.Add(tab);
            SelectedTab = tab;
        }

        /// <summary>
        /// 双击引用对象列，通过 LookUpObjectID 查找 T_Meta_LookupClass.FFORMID 对应的表单实体
        /// </summary>
        public async Task OpenLookupEntityTabAsync(FieldInfo field)
        {
            if (field == null || !HasLocalData || string.IsNullOrWhiteSpace(field.LookUpObjectID)) return;

            // 通过 LookUpObjectID 查找对应的 FFORMID（表单标识）
            string lookupSql = @"SELECT FFORMID FROM T_Meta_LookupClass WHERE FID = @FID";
            var lookupRows = await Task.Run(() => ExecuteQuery(lookupSql, new[] { new SQLiteParameter("@FID", field.LookUpObjectID) }));
            if (lookupRows.Count == 0) return;

            var formIdentifier = lookupRows[0]["FFORMID"]?.ToString() ?? "";
            if (string.IsNullOrWhiteSpace(formIdentifier)) return;

            // 通过表单标识查找 T_FORM 的 FID
            string formSql = @"SELECT FID, FFORMIDENTIFIER, FNAME FROM T_FORM WHERE FFORMIDENTIFIER = @FormIdentifier";
            var formRows = await Task.Run(() => ExecuteQuery(formSql, new[] { new SQLiteParameter("@FormIdentifier", formIdentifier) }));
            if (formRows.Count == 0) return;

            var formId = formRows[0]["FID"]?.ToString() ?? "";
            var formName = formRows[0]["FNAME"]?.ToString() ?? formIdentifier;

            string tabKey = $"entity_{formId}";
            var existingTab = OpenTabs.FirstOrDefault(t => t.ModuleId == tabKey);
            if (existingTab != null)
            {
                SelectedTab = existingTab;
                return;
            }

            var tab = new ModuleTabItem
            {
                Header = formName,
                ModuleId = tabKey,
                TabType = TabType.Entity
            };

            await LoadEntityDataAsync(tab, formId);

            OpenTabs.Add(tab);
            SelectedTab = tab;
        }

        /// <summary>
        /// 双击枚举类型列，通过 EnumType 查找 T_META_FORMENUM 的枚举项
        /// </summary>
        public async Task OpenEnumDetailTabAsync(FieldInfo field)
        {
            if (field == null || !HasLocalData || string.IsNullOrWhiteSpace(field.EnumType)) return;

            string tabKey = $"enum_{field.EnumType}";
            var existingTab = OpenTabs.FirstOrDefault(t => t.ModuleId == tabKey);
            if (existingTab != null)
            {
                SelectedTab = existingTab;
                return;
            }

            var tab = new ModuleTabItem
            {
                Header = $"{field.EnumTypeDisplay}",
                ModuleId = tabKey,
                TabType = TabType.Enum
            };

            string sql = @"SELECT FVALUE, FCAPTION FROM T_META_FORMENUM WHERE FID = @FID ORDER BY FVALUE";
            var rows = await Task.Run(() => ExecuteQuery(sql, new[] { new SQLiteParameter("@FID", field.EnumType) }));
            foreach (var row in rows)
            {
                tab.EnumItems.Add(new EnumItemInfo
                {
                    FValue = row["FVALUE"]?.ToString() ?? "",
                    FCaption = row["FCAPTION"]?.ToString() ?? ""
                });
            }

            StatusText = $"本地数据 | {tab.Header} - {tab.EnumItems.Count} 条枚举项";

            OpenTabs.Add(tab);
            SelectedTab = tab;
        }

        /// <summary>
        /// 显示当前表单所有实体的所有字段
        /// </summary>
        public async Task OpenAllFieldsTabAsync(string formId, string formName)
        {
            if (!HasLocalData) return;

            string tabKey = $"allfields_{formId}";
            var existingTab = OpenTabs.FirstOrDefault(t => t.ModuleId == tabKey);
            if (existingTab != null)
            {
                SelectedTab = existingTab;
                return;
            }

            var tab = new ModuleTabItem
            {
                Header = $"{formName} - 所有字段",
                ModuleId = tabKey,
                TabType = TabType.AllFields
            };

            await LoadAllFieldsDataAsync(tab, formId);

            OpenTabs.Add(tab);
            SelectedTab = tab;
        }

        /// <summary>
        /// 双击元素类型=单据类型时，通过 FFORMIDENTIFIER 查找 T_BAS_BILLTYPE 中对应的单据类型
        /// </summary>
        public async Task OpenBillTypeTabAsync(string formIdentifier)
        {
            if (!HasLocalData || string.IsNullOrWhiteSpace(formIdentifier)) return;

            string tabKey = $"billtype_{formIdentifier}";
            var existingTab = OpenTabs.FirstOrDefault(t => t.ModuleId == tabKey);
            if (existingTab != null)
            {
                SelectedTab = existingTab;
                return;
            }

            // 查找表单名称
            string formName = formIdentifier;
            try
            {
                var rows = ExecuteQuery(@"SELECT FNAME FROM T_FORM WHERE FFORMIDENTIFIER = @FormIdentifier",
                    new[] { new SQLiteParameter("@FormIdentifier", formIdentifier) });
                if (rows.Count > 0 && !string.IsNullOrWhiteSpace(rows[0]["FNAME"]?.ToString()))
                    formName = rows[0]["FNAME"].ToString();
            }
            catch { }

            var tab = new ModuleTabItem
            {
                Header = $"{formName} - 单据类型",
                ModuleId = tabKey,
                TabType = TabType.BillType
            };

            await LoadBillTypeDataAsync(tab, formIdentifier);

            OpenTabs.Add(tab);
            SelectedTab = tab;
        }

        /// <summary>
        /// 通过表单数字ID查找表单标识（FFORMIDENTIFIER）
        /// </summary>
        public string GetFormIdentifierByFormId(string formId)
        {
            if (!HasLocalData || string.IsNullOrWhiteSpace(formId)) return null;
            try
            {
                var rows = ExecuteQuery(@"SELECT FFORMIDENTIFIER FROM T_FORM WHERE FID = @FormId", new[] { new SQLiteParameter("@FormId", formId) });
                if (rows.Count > 0) return rows[0]["FFORMIDENTIFIER"]?.ToString();
            }
            catch { }
            return null;
        }

        private async Task LoadBillTypeDataAsync(ModuleTabItem tab, string formIdentifier)
        {
            if (!HasLocalData) return;

            try
            {
                string sql = @"SELECT FBILLTYPEID, FBILLFORMID, FNUMBER, FNAME FROM T_BAS_BILLTYPE WHERE FBILLFORMID = @FormIdentifier ORDER BY FNUMBER";
                var rows = await Task.Run(() => ExecuteQuery(sql, new[] { new SQLiteParameter("@FormIdentifier", formIdentifier) }));
                foreach (var row in rows)
                {
                    tab.BillTypes.Add(new BillTypeInfo
                    {
                        BillTypeId = row["FBILLTYPEID"]?.ToString() ?? "",
                        BillFormId = row["FBILLFORMID"]?.ToString() ?? "",
                        Number = row["FNUMBER"]?.ToString() ?? "",
                        Name = row["FNAME"]?.ToString() ?? ""
                    });
                }

                StatusText = $"本地数据 | {tab.Header} - {tab.BillTypes.Count} 条单据类型";
            }
            catch (Exception ex)
            {
                StatusText = $"单据类型查询失败：{ex.Message}";
            }
        }

        private async Task LoadAllFieldsDataAsync(ModuleTabItem tab, string formId)
        {
            if (!HasLocalData) return;

            try
            {
                var (sql, parameters) = BuildAllFieldsQuery(formId);
                var rows = await Task.Run(() => ExecuteQuery(sql, parameters));
                foreach (var row in rows)
                {
                    tab.AllFields.Add(new AllFieldInfo
                    {
                        FormName = row["FDJMC"]?.ToString() ?? "",
                        EntityName = row["FENTITYNAME"]?.ToString() ?? "",
                        EntityTableName = row["FTABLENAME"]?.ToString() ?? "",
                        Key = row["FKey"]?.ToString() ?? "",
                        Name = row["FName"]?.ToString() ?? "",
                        FieldName = row["FFieldName"]?.ToString() ?? "",
                        PropertyName = row["FPropertyName"]?.ToString() ?? "",
                        ElementTypeName = row["FELEMENTTYPENAME"]?.ToString() ?? "",
                        LookUpObjectID = row["FLookUpObjectID"]?.ToString() ?? "",
                        EnumType = row["FEnumType"]?.ToString() ?? "",
                        LookUpObjectDisplay = row["FLookUpObjectDisplay"]?.ToString() ?? "",
                        EnumTypeDisplay = row["FEnumTypeDisplay"]?.ToString() ?? "",
                        Suffix = row["FSUFFIX"]?.ToString() ?? "",
                        SplitDescription = row["FSPLITDESCRIPTION"]?.ToString() ?? ""
                    });
                }

                StatusText = $"本地数据 | {tab.Header} - {tab.AllFields.Count} 条记录";
            }
            catch (Exception ex)
            {
                StatusText = $"所有字段查询失败：{ex.Message}";
            }
        }

        private (string sql, List<SQLiteParameter> parameters) BuildAllFieldsQuery(string formId)
        {
            string sql = @"
SELECT a.FNAME as FDJMC,
       b.FName as FENTITYNAME,
       b.FTableName as FTABLENAME,
       d.FKey as FKey,
       d.FName as FName,
       d.FFieldName as FFieldName,
       d.FPropertyName as FPropertyName,
       e.FNAME as FELEMENTTYPENAME,
       d.FLookUpObjectID as FLookUpObjectID,
       d.FEnumType as FEnumType,
       lk.FFORMID as FLookUpObjectDisplay,
       (SELECT FNAME FROM T_META_FORMENUM WHERE FID = d.FEnumType LIMIT 1) as FEnumTypeDisplay,
       c.FSUFFIX as FSUFFIX,
       c.FDESCRIPTION as FSPLITDESCRIPTION
FROM T_FORM a
INNER JOIN T_ENTITY b ON a.FID = b.FFORMID
INNER JOIN T_FIELD d ON b.FID = d.FENTITYID
LEFT JOIN T_ENTITYSPLIT c ON c.FID = d.FENTITYSPLITID AND c.FFORMID = a.FID
LEFT JOIN T_MDL_ELEMENTTYPE_L e ON e.FID = d.FElementType AND e.FLOCALEID = 2052
LEFT JOIN T_Meta_LookupClass lk ON lk.FID = d.FLookUpObjectID
WHERE a.FID = @FormId
ORDER BY b.FID, d.FID";
            var parameters = new List<SQLiteParameter>
            {
                new SQLiteParameter("@FormId", formId)
            };
            return (sql, parameters);
        }

        private void CloseCurrentTab(object parameter)
        {
            if (_isClosingTab) return;

            ModuleTabItem tabToClose = parameter as ModuleTabItem ?? SelectedTab;
            if (tabToClose == null) return;
            if (!OpenTabs.Contains(tabToClose)) return;

            _isClosingTab = true;
            try
            {
                int index = OpenTabs.IndexOf(tabToClose);
                OpenTabs.Remove(tabToClose);

                if (OpenTabs.Count > 0)
                {
                    int newIndex = Math.Min(index, OpenTabs.Count - 1);
                    if (SelectedTab == null || !OpenTabs.Contains(SelectedTab))
                    {
                        SelectedTab = OpenTabs[newIndex];
                    }
                }
                else
                {
                    SelectedTab = null;
                }
            }
            finally
            {
                Application.Current.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background,
                    new Action(() => { _isClosingTab = false; }));
            }
        }

        private void CloseLeftTabs(object parameter)
        {
            if (!(parameter is ModuleTabItem tab)) return;
            int index = OpenTabs.IndexOf(tab);
            var toRemove = OpenTabs.Take(index).ToList();
            foreach (var t in toRemove) OpenTabs.Remove(t);
            if (OpenTabs.Count > 0 && SelectedTab == null) SelectedTab = OpenTabs[0];
        }

        private void CloseRightTabs(object parameter)
        {
            if (!(parameter is ModuleTabItem tab)) return;
            int index = OpenTabs.IndexOf(tab);
            var toRemove = OpenTabs.Skip(index + 1).ToList();
            foreach (var t in toRemove) OpenTabs.Remove(t);
            if (OpenTabs.Count > 0 && SelectedTab == null) SelectedTab = OpenTabs[OpenTabs.Count - 1];
        }

        private void CloseOtherTabs(object parameter)
        {
            if (!(parameter is ModuleTabItem tab)) return;
            var toRemove = OpenTabs.Where(t => t != tab).ToList();
            foreach (var t in toRemove) OpenTabs.Remove(t);
            SelectedTab = tab;
        }

        public async Task LoadTreeDataAsync()
        {
            if (!HasLocalData) return;

            var nodeList = new List<ModuleTreeItem>();
            var nodeDict = new Dictionary<string, ModuleTreeItem>();

            try
            {
                await Task.Run(() =>
                {
                    LoadLevel1Nodes(nodeList, nodeDict);
                    LoadLevel2Nodes(nodeList, nodeDict);
                    LoadLevel3Nodes(nodeList, nodeDict);
                });

                ModuleTree.Clear();
                foreach (var node in nodeList) ModuleTree.Add(node);

                StatusText = $"本地数据 | 模块数：{ModuleTree.Count}";
            }
            catch (Exception ex)
            {
                StatusText = $"加载失败：{ex.Message}";
            }
        }

        private ModuleTreeItem EnsureOtherNode(List<ModuleTreeItem> nodeList, Dictionary<string, ModuleTreeItem> nodeDict, string otherId, string otherText)
        {
            if (!nodeDict.ContainsKey(otherId))
            {
                var otherNode = new ModuleTreeItem { Id = otherId, Text = otherText, ParentId = "0" };
                nodeDict[otherId] = otherNode;
                nodeList.Add(otherNode);
            }
            return nodeDict[otherId];
        }

        private void LoadLevel1Nodes(List<ModuleTreeItem> nodeList, Dictionary<string, ModuleTreeItem> nodeDict)
        {
            string sql = @"SELECT 'T_' || FTOPCLASSID as id, FNAME as text, '0' as parentid FROM T_META_TOPCLASS_L WHERE FLOCALEID = 2052";
            var rows = ExecuteQuery(sql);
            foreach (var row in rows)
            {
                string id = Convert.ToString(row["id"]);
                string text = Convert.ToString(row["text"]);
                var node = new ModuleTreeItem { Id = id, Text = text, ParentId = "0" };
                nodeDict[id] = node;
                nodeList.Add(node);
            }
        }

        private void LoadLevel2Nodes(List<ModuleTreeItem> nodeList, Dictionary<string, ModuleTreeItem> nodeDict)
        {
            string sql = @"SELECT 'S_' || FID as id, FNAME as text, 'T_' || FTOPCLASSID as parentid FROM T_META_SUBSYSTEM";
            var rows = ExecuteQuery(sql);
            foreach (var row in rows)
            {
                string id = Convert.ToString(row["id"]);
                string text = Convert.ToString(row["text"]);
                string parentId = Convert.ToString(row["parentid"]);
                var node = new ModuleTreeItem { Id = id, Text = text, ParentId = parentId };
                nodeDict[id] = node;
                if (nodeDict.ContainsKey(parentId))
                {
                    nodeDict[parentId].Children.Add(node);
                }
                else
                {
                    var otherNode = EnsureOtherNode(nodeList, nodeDict, "T_OTHER", "其他");
                    node.ParentId = otherNode.Id;
                    otherNode.Children.Add(node);
                }
            }
        }

        private void LoadLevel3Nodes(List<ModuleTreeItem> nodeList, Dictionary<string, ModuleTreeItem> nodeDict)
        {
            string sql = @"SELECT DISTINCT FFORMIDENTIFIER as id, FNAME as text, 'S_' || FSUBSYSTEMID as parentid FROM T_FORM";
            var rows = ExecuteQuery(sql);
            foreach (var row in rows)
            {
                string id = Convert.ToString(row["id"]);
                string text = Convert.ToString(row["text"]);
                string parentId = Convert.ToString(row["parentid"]);
                var node = new ModuleTreeItem { Id = id, Text = text, ParentId = parentId };
                nodeDict[id] = node;
                if (nodeDict.ContainsKey(parentId))
                {
                    nodeDict[parentId].Children.Add(node);
                }
                else
                {
                    var otherNode = EnsureOtherNode(nodeList, nodeDict, "S_OTHER", "其他");
                    node.ParentId = otherNode.Id;
                    otherNode.Children.Add(node);
                }
            }
        }

        private async Task LoadFormDataAsync(ModuleTabItem tab, string moduleId)
        {
            if (!HasLocalData) return;

            try
            {
                var (sql, parameters) = BuildFormQuery(moduleId, null, null);
                var rows = await Task.Run(() => ExecuteQuery(sql, parameters));
                foreach (var row in rows)
                {
                    tab.Forms.Add(new FormInfo
                    {
                        FormId = row["FFORMID"]?.ToString() ?? "",
                        FormIdentifier = row["FFORMIDENTIFIER"]?.ToString() ?? "",
                        FormName = row["FDJMC"]?.ToString() ?? "",
                        ModelTypeName = row["FELEMENTTYPENAME"]?.ToString() ?? "",
                        SubSystemName = row["FSUBSYSTEMNAME"]?.ToString() ?? ""
                    });
                }

                StatusText = $"本地数据 | {tab.Header} - {tab.Forms.Count} 条表单";
            }
            catch (Exception ex)
            {
                StatusText = $"查询失败：{ex.Message}";
            }
        }

        private async Task LoadEntityDataAsync(ModuleTabItem tab, string formId)
        {
            if (!HasLocalData) return;

            try
            {
                var (sql, parameters) = BuildEntityQuery(formId);
                var rows = await Task.Run(() => ExecuteQuery(sql, parameters));
                foreach (var row in rows)
                {
                    tab.FormEntities.Add(MapFormEntityInfo(row));
                }

                StatusText = $"本地数据 | {tab.Header} - {tab.FormEntities.Count} 条实体";
            }
            catch (Exception ex)
            {
                StatusText = $"实体查询失败：{ex.Message}";
            }
        }

        private async Task LoadFieldDataAsync(ModuleTabItem tab, string formId, string entityId)
        {
            if (!HasLocalData) return;

            try
            {
                var (sql, parameters) = BuildFieldQuery(formId, entityId);
                var rows = await Task.Run(() => ExecuteQuery(sql, parameters));
                foreach (var row in rows)
                {
                    tab.Fields.Add(new FieldInfo
                    {
                        Key = row["FKey"]?.ToString() ?? "",
                        Name = row["FName"]?.ToString() ?? "",
                        FieldName = row["FFieldName"]?.ToString() ?? "",
                        PropertyName = row["FPropertyName"]?.ToString() ?? "",
                        ElementTypeName = row["FELEMENTTYPENAME"]?.ToString() ?? "",
                        Suffix = row["FSUFFIX"]?.ToString() ?? "",
                        SplitDescription = row["FSPLITDESCRIPTION"]?.ToString() ?? "",
                        LookUpObjectID = row["FLookUpObjectID"]?.ToString() ?? "",
                        EnumType = row["FEnumType"]?.ToString() ?? "",
                        LookUpObjectDisplay = row["FLookUpObjectDisplay"]?.ToString() ?? "",
                        EnumTypeDisplay = row["FEnumTypeDisplay"]?.ToString() ?? ""
                    });
                }

                StatusText = $"本地数据 | {tab.Header} - {tab.Fields.Count} 条记录";
            }
            catch (Exception ex)
            {
                StatusText = $"字段查询失败：{ex.Message}";
            }
        }

        private FormEntityInfo MapFormEntityInfo(Dictionary<string, object> row)
        {
            return new FormEntityInfo
            {
                FormId = row["FFORMID"]?.ToString() ?? "",
                EntityId = row["FENTITYID"]?.ToString() ?? "",
                FormIdentifier = row["FFORMIDENTIFIER"]?.ToString() ?? "",
                FormName = row["FDJMC"]?.ToString() ?? "",
                FormModelType = row["FMODELTYPEID"]?.ToString() ?? "",
                EntityKey = row["FKey"]?.ToString() ?? "",
                EntityEntryName = row["FEntryName"]?.ToString() ?? "",
                EntityName = row["FENTITYNAME"]?.ToString() ?? "",
                EntityTableName = row["FTABLENAME"]?.ToString() ?? "",
                EntityEntryPkFieldName = row["FENTRYPKFIELDNAME"]?.ToString() ?? "",
                EntityElementTypeName = row["FELEMENTTYPENAME"]?.ToString() ?? ""
            };
        }

        private (string sql, List<SQLiteParameter> parameters) BuildFormQuery(string moduleId, string searchText, string operatorValue)
        {
            var whereConditions = new List<string>();
            var parameters = new List<SQLiteParameter>();

            bool needEntityJoin = !string.IsNullOrEmpty(searchText);
            string sql = @"
SELECT DISTINCT a.FID as FFORMID,
       a.FFORMIDENTIFIER as FFORMIDENTIFIER,
       a.FNAME as FDJMC,
       et.FNAME as FELEMENTTYPENAME,
       sl.FNAME as FSUBSYSTEMNAME
FROM T_FORM a
LEFT JOIN T_MDL_ELEMENTTYPE_L et ON et.FID = a.FMODELTYPEID AND et.FLOCALEID = 2052
LEFT JOIN T_META_SUBSYSTEM sl ON sl.FID = a.FSUBSYSTEMID"
+ (needEntityJoin ? @"
LEFT JOIN T_ENTITY ent ON ent.FFORMID = a.FID" : "");

            if (!string.IsNullOrEmpty(searchText))
            {
                switch (operatorValue)
                {
                    case "LIKE":
                        whereConditions.Add("(a.FFORMIDENTIFIER LIKE @SearchValue OR a.FNAME LIKE @SearchValue OR ent.FTableName LIKE @SearchValue)");
                        parameters.Add(new SQLiteParameter("@SearchValue", $"%{searchText}%"));
                        break;
                    case "LIKE_START":
                        whereConditions.Add("(a.FFORMIDENTIFIER LIKE @SearchValue OR a.FNAME LIKE @SearchValue OR ent.FTableName LIKE @SearchValue)");
                        parameters.Add(new SQLiteParameter("@SearchValue", $"{searchText}%"));
                        break;
                    case "LIKE_END":
                        whereConditions.Add("(a.FFORMIDENTIFIER LIKE @SearchValue OR a.FNAME LIKE @SearchValue OR ent.FTableName LIKE @SearchValue)");
                        parameters.Add(new SQLiteParameter("@SearchValue", $"%{searchText}"));
                        break;
                    default:
                        whereConditions.Add("(a.FFORMIDENTIFIER = @SearchValue OR a.FNAME = @SearchValue OR ent.FTableName = @SearchValue)");
                        parameters.Add(new SQLiteParameter("@SearchValue", searchText));
                        break;
                }
            }
            else if (!string.IsNullOrEmpty(moduleId))
            {
                if (moduleId == "T_OTHER" || moduleId == "S_OTHER")
                {
                    whereConditions.Add("(a.FSUBSYSTEMID NOT IN (SELECT FID FROM T_META_SUBSYSTEM) OR a.FSUBSYSTEMID IS NULL)");
                }
                else if (moduleId.StartsWith("T_"))
                {
                    string topClassId = moduleId.Substring(2);
                    whereConditions.Add("a.FSUBSYSTEMID IN (SELECT FID FROM T_META_SUBSYSTEM WHERE FTOPCLASSID = @TopClassId)");
                    parameters.Add(new SQLiteParameter("@TopClassId", topClassId));
                }
                else if (moduleId.StartsWith("S_"))
                {
                    string subSysId = moduleId.Substring(2);
                    whereConditions.Add("a.FSUBSYSTEMID = @SubSysId");
                    parameters.Add(new SQLiteParameter("@SubSysId", subSysId));
                }
                else
                {
                    whereConditions.Add("a.FFORMIDENTIFIER = @FormIdentifier");
                    parameters.Add(new SQLiteParameter("@FormIdentifier", moduleId));
                }
            }

            if (whereConditions.Count > 0)
                sql += " WHERE " + string.Join(" AND ", whereConditions) + " ";

            sql += "ORDER BY a.FID";
            return (sql, parameters);
        }

        private (string sql, List<SQLiteParameter> parameters) BuildEntityQuery(string formId)
        {
            string sql = @"
SELECT a.FID as FFORMID,
       b.FID as FENTITYID,
       a.FFORMIDENTIFIER as FFORMIDENTIFIER,
       a.FNAME as FDJMC,
       a.FMODELTYPEID as FMODELTYPEID,
       b.FKey as FKey,
       b.FEntryName as FEntryName,
       b.FName as FENTITYNAME,
       b.FTableName as FTABLENAME,
       b.FEntryPkFieldName as FENTRYPKFIELDNAME,
       et.FNAME as FELEMENTTYPENAME,
       b.FElementType as FELEMENTTYPE
FROM T_FORM a
INNER JOIN T_ENTITY b ON a.FID = b.FFORMID
LEFT JOIN T_MDL_ELEMENTTYPE_L et ON et.FID = b.FElementType AND et.FLOCALEID = 2052
WHERE a.FID = @FormId
ORDER BY b.FID";
            var parameters = new List<SQLiteParameter>
            {
                new SQLiteParameter("@FormId", formId)
            };
            return (sql, parameters);
        }

        private (string sql, List<SQLiteParameter> parameters) BuildFieldQuery(string formId, string entityId)
        {
            string sql = @"
SELECT d.FKey as FKey,
       d.FName as FName,
       d.FFieldName as FFieldName,
       d.FPropertyName as FPropertyName,
       e.FNAME as FELEMENTTYPENAME,
       c.FSUFFIX as FSUFFIX,
       c.FDESCRIPTION as FSPLITDESCRIPTION,
       d.FLookUpObjectID as FLookUpObjectID,
       d.FEnumType as FEnumType,
       lk.FFORMID as FLookUpObjectDisplay,
       (SELECT FNAME FROM T_META_FORMENUM WHERE FID = d.FEnumType LIMIT 1) as FEnumTypeDisplay
FROM T_FORM a
INNER JOIN T_ENTITY b ON a.FID = b.FFORMID
INNER JOIN T_FIELD d ON b.FID = d.FENTITYID
LEFT JOIN T_ENTITYSPLIT c ON c.FID = d.FENTITYSPLITID AND c.FFORMID = a.FID
LEFT JOIN T_MDL_ELEMENTTYPE_L e ON e.FID = d.FElementType AND e.FLOCALEID = 2052
LEFT JOIN T_Meta_LookupClass lk ON lk.FID = d.FLookUpObjectID
WHERE a.FID = @FormId AND b.FID = @EntityId
ORDER BY d.FID";
            var parameters = new List<SQLiteParameter>
            {
                new SQLiteParameter("@FormId", formId),
                new SQLiteParameter("@EntityId", entityId)
            };
            return (sql, parameters);
        }

        private async void ExecuteSearch(object parameter)
        {
            if (!HasLocalData) return;
            if (string.IsNullOrWhiteSpace(SearchText)) return;

            if (SelectedTab == null || SelectedTab.TabType != TabType.Form)
            {
                var searchTab = new ModuleTabItem
                {
                    Header = $"搜索: {SearchText}",
                    ModuleId = "SEARCH_" + Guid.NewGuid().ToString("N"),
                    TabType = TabType.Form
                };

                string opValue = SelectedOperator?.OperatorValue ?? "=";
                var (sql, queryParams) = BuildFormQuery(null, SearchText, opValue);

                try
                {
                    var rows = await Task.Run(() => ExecuteQuery(sql, queryParams));
                    foreach (var row in rows)
                    {
                        searchTab.Forms.Add(new FormInfo
                        {
                            FormId = row["FFORMID"]?.ToString() ?? "",
                            FormIdentifier = row["FFORMIDENTIFIER"]?.ToString() ?? "",
                            FormName = row["FDJMC"]?.ToString() ?? "",
                            ModelTypeName = row["FELEMENTTYPENAME"]?.ToString() ?? "",
                            SubSystemName = row["FSUBSYSTEMNAME"]?.ToString() ?? ""
                        });
                    }

                    OpenTabs.Add(searchTab);
                    SelectedTab = searchTab;
                    StatusText = $"本地数据 | 搜索结果：{searchTab.Forms.Count} 条表单";
                }
                catch (Exception ex)
                {
                    StatusText = $"查询失败：{ex.Message}";
                }
            }
            else
            {
                await SearchInTabAsync();
            }
        }

        private async Task SearchInTabAsync()
        {
            try
            {
                string opValue = SelectedOperator?.OperatorValue ?? "=";

                if (SelectedTab.TabType == TabType.Form)
                {
                    SelectedTab.Header = $"搜索: {SearchText}";
                    var (sql, queryParams) = BuildFormQuery(SelectedTab.ModuleId, SearchText, opValue);
                    var rows = await Task.Run(() => ExecuteQuery(sql, queryParams));

                    SelectedTab.Forms.Clear();
                    foreach (var row in rows)
                    {
                        SelectedTab.Forms.Add(new FormInfo
                        {
                            FormId = row["FFORMID"]?.ToString() ?? "",
                            FormIdentifier = row["FFORMIDENTIFIER"]?.ToString() ?? "",
                            FormName = row["FDJMC"]?.ToString() ?? "",
                            ModelTypeName = row["FELEMENTTYPENAME"]?.ToString() ?? "",
                            SubSystemName = row["FSUBSYSTEMNAME"]?.ToString() ?? ""
                        });
                    }

                    StatusText = $"本地数据 | 筛选结果：{SelectedTab.Forms.Count} 条表单";
                }
            }
            catch (Exception ex)
            {
                StatusText = $"查询失败：{ex.Message}";
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
