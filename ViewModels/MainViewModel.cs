using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
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
        private ConnectionInfo _currentConnection;
        private bool _isConnected;
        private string _statusText;
        private ObservableCollection<ModuleTabItem> _openTabs;
        private ModuleTabItem _selectedTab;
        private ICommand _closeCurrentTabCommand;
        private ICommand _closeLeftTabsCommand;
        private ICommand _closeRightTabsCommand;
        private ICommand _closeOtherTabsCommand;
        private bool _suppressCollectionChanged;
        private ModuleTabItem _recentlyRemovedTab;
        private int _recentlyRemovedIndex = -1;
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
            set { _searchText = value; OnPropertyChanged(); }
        }

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
            if (tab == null || !IsConnected)
            {
                if (!IsConnected) return;
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
            if (_suppressCollectionChanged) return;

            if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                if (e.OldItems?.Count == 1 && e.OldItems[0] is ModuleTabItem removedTab)
                {
                    _recentlyRemovedTab = removedTab;
                    _recentlyRemovedIndex = e.OldStartingIndex;
                }
                else
                {
                    _recentlyRemovedTab = null;
                    _recentlyRemovedIndex = -1;
                }

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
            else if (e.Action == NotifyCollectionChangedAction.Add)
            {
                if (e.NewStartingIndex == 0 && e.NewItems?.Count == 1 && e.NewItems[0] is ModuleTabItem addedTab)
                {
                    if (addedTab == _recentlyRemovedTab && _recentlyRemovedIndex > 0)
                    {
                        var targetIndex = _recentlyRemovedIndex;
                        _recentlyRemovedTab = null;
                        _recentlyRemovedIndex = -1;
                        Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            _suppressCollectionChanged = true;
                            OpenTabs.RemoveAt(0);
                            OpenTabs.Insert(targetIndex, addedTab);
                            _suppressCollectionChanged = false;
                            SelectedTab = addedTab;
                        }));
                        return;
                    }
                }
                _recentlyRemovedTab = null;
                _recentlyRemovedIndex = -1;
            }
            else if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                _recentlyRemovedTab = null;
                _recentlyRemovedIndex = -1;
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

        public void ApplyConnection(ConnectionInfo connection)
        {
            CurrentConnection = connection;
            IsConnected = true;

            LocalDbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "metadata.db");

            if (HasLocalData)
            {
                StatusText = $"已连接：{connection.DisplayName}（本地数据）";
                LoadTreeData();
            }
            else
            {
                StatusText = $"已连接：{connection.DisplayName} | 请刷新元数据";
            }
        }

        public void OnRefreshCompleted(string localDbPath)
        {
            LocalDbPath = localDbPath;
            OpenTabs.Clear();
            StatusText = $"已连接：{CurrentConnection?.DisplayName}（本地数据）";
            LoadTreeData();
        }

        private List<Dictionary<string, object>> ExecuteQuery(string sql)
        {
            var results = new List<Dictionary<string, object>>();
            using (var conn = new SQLiteConnection($"Data Source={LocalDbPath};Version=3;"))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand(sql, conn))
                {
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

        private void OnModuleSelected(ModuleTreeItem module)
        {
            if (module == null) return;
            module.IsExpanded = true;
            OpenTabForModule(module);
        }

        private void OpenTabForModule(ModuleTreeItem module)
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

            LoadFormData(tab, module.Id);

            OpenTabs.Add(tab);
            SelectedTab = tab;
        }

        public void OpenEntityTab(FormInfo form)
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

            LoadEntityData(tab, form.FormId);

            OpenTabs.Add(tab);
            SelectedTab = tab;
        }

        public void OpenFieldDetailTab(FormEntityInfo entity)
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
                Header = $"{entity.EntityName} - 字段",
                ModuleId = tabKey,
                TabType = TabType.Field
            };

            LoadFieldData(tab, entity.FormId, entity.EntityId);

            OpenTabs.Add(tab);
            SelectedTab = tab;
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

        public void LoadTreeData()
        {
            if (!HasLocalData) return;

            var nodeList = new List<ModuleTreeItem>();
            var nodeDict = new Dictionary<string, ModuleTreeItem>();

            try
            {
                LoadLevel1Nodes(nodeList, nodeDict);
                LoadLevel2Nodes(nodeList, nodeDict);
                LoadLevel3Nodes(nodeList, nodeDict);

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
            string sql = "SELECT 'T_' || FTOPCLASSID as id, FNAME as text, '0' as parentid FROM T_META_TOPCLASS_L WHERE FLOCALEID = 2052";
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
            string sql = "SELECT 'S_' || FID as id, FNAME as text, 'T_' || FTOPCLASSID as parentid FROM T_META_SUBSYSTEM";
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
            string sql = "SELECT DISTINCT FFORMIDENTIFIER as id, FNAME as text, 'S_' || FSUBSYSTEMID as parentid FROM T_FORM";
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

        private void LoadFormData(ModuleTabItem tab, string moduleId)
        {
            if (!HasLocalData) return;

            try
            {
                string sql = BuildFormQuery(moduleId, null, null);
                var rows = ExecuteQuery(sql);
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

        private void LoadEntityData(ModuleTabItem tab, string formId)
        {
            if (!HasLocalData) return;

            try
            {
                string sql = BuildEntityQuery(formId);
                var rows = ExecuteQuery(sql);
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

        private void LoadFieldData(ModuleTabItem tab, string formId, string entityId)
        {
            if (!HasLocalData) return;

            try
            {
                string sql = BuildFieldQuery(formId, entityId);
                var rows = ExecuteQuery(sql);
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
                        SplitDescription = row["FSPLITDESCRIPTION"]?.ToString() ?? ""
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

        private string BuildFormQuery(string moduleId, string searchText, string operatorValue)
        {
            var whereConditions = new List<string>();

            string sql = "SELECT DISTINCT a.FID as FFORMID, " +
                         "       a.FFORMIDENTIFIER as FFORMIDENTIFIER, " +
                         "       a.FNAME as FDJMC, " +
                         "       et.FNAME as FELEMENTTYPENAME, " +
                         "       sl.FNAME as FSUBSYSTEMNAME " +
                         "FROM T_FORM a " +
                         "LEFT JOIN T_MDL_ELEMENTTYPE_L et ON et.FID = a.FMODELTYPEID AND et.FLOCALEID = 2052 " +
                         "LEFT JOIN T_META_SUBSYSTEM sl ON sl.FID = a.FSUBSYSTEMID ";

            if (!string.IsNullOrEmpty(searchText))
            {
                string safeValue = searchText.Replace("'", "''");
                switch (operatorValue)
                {
                    case "LIKE":
                        whereConditions.Add($"(a.FFORMIDENTIFIER LIKE '%{safeValue}%' OR a.FNAME LIKE '%{safeValue}%')");
                        break;
                    case "LIKE_START":
                        whereConditions.Add($"(a.FFORMIDENTIFIER LIKE '{safeValue}%' OR a.FNAME LIKE '{safeValue}%')");
                        break;
                    case "LIKE_END":
                        whereConditions.Add($"(a.FFORMIDENTIFIER LIKE '%{safeValue}' OR a.FNAME LIKE '%{safeValue}')");
                        break;
                    default:
                        whereConditions.Add($"(a.FFORMIDENTIFIER = '{safeValue}' OR a.FNAME = '{safeValue}')");
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
                    whereConditions.Add($"a.FSUBSYSTEMID IN (SELECT FID FROM T_META_SUBSYSTEM WHERE FTOPCLASSID = '{topClassId}')");
                }
                else if (moduleId.StartsWith("S_"))
                {
                    string subSysId = moduleId.Substring(2);
                    whereConditions.Add($"a.FSUBSYSTEMID = '{subSysId}'");
                }
                else
                {
                    whereConditions.Add($"a.FFORMIDENTIFIER = '{moduleId}'");
                }
            }

            if (whereConditions.Count > 0)
                sql += " WHERE " + string.Join(" AND ", whereConditions) + " ";

            sql += "ORDER BY a.FID";
            return sql;
        }

        private string BuildEntityQuery(string formId)
        {
            return "SELECT a.FID as FFORMID, " +
                   "       b.FID as FENTITYID, " +
                   "       a.FFORMIDENTIFIER as FFORMIDENTIFIER, " +
                   "       a.FNAME as FDJMC, " +
                   "       a.FMODELTYPEID as FMODELTYPEID, " +
                   "       b.FKey as FKey, " +
                   "       b.FEntryName as FEntryName, " +
                   "       b.FName as FENTITYNAME, " +
                   "       b.FTableName as FTABLENAME, " +
                   "       b.FEntryPkFieldName as FENTRYPKFIELDNAME, " +
                   "       et.FNAME as FELEMENTTYPENAME, " +
                   "       b.FElementType as FELEMENTTYPE " +
                   "FROM T_FORM a " +
                   "INNER JOIN T_ENTITY b ON a.FID = b.FFORMID " +
                   "LEFT JOIN T_MDL_ELEMENTTYPE_L et ON et.FID = b.FElementType AND et.FLOCALEID = 2052 " +
                   $"WHERE a.FID = {formId} " +
                   "ORDER BY b.FID";
        }

        private string BuildFieldQuery(string formId, string entityId)
        {
            return "SELECT d.FKey as FKey, " +
                   "       d.FName as FName, " +
                   "       d.FFieldName as FFieldName, " +
                   "       d.FPropertyName as FPropertyName, " +
                   "       e.FNAME as FELEMENTTYPENAME, " +
                   "       c.FSUFFIX as FSUFFIX, " +
                   "       c.FDESCRIPTION as FSPLITDESCRIPTION " +
                   "FROM T_FORM a " +
                   "INNER JOIN T_ENTITY b ON a.FID = b.FFORMID " +
                   "INNER JOIN T_FIELD d ON b.FID = d.FENTITYID " +
                   "LEFT JOIN T_ENTITYSPLIT c ON c.FID = d.FENTITYSPLITID AND c.FFORMID = a.FID " +
                   "LEFT JOIN T_MDL_ELEMENTTYPE_L e ON e.FID = d.FElementType AND e.FLOCALEID = 2052 " +
                   $"WHERE a.FID = {formId} AND b.FID = {entityId} " +
                   "ORDER BY d.FID";
        }

        private void ExecuteSearch(object parameter)
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
                string sql = BuildFormQuery(null, SearchText, opValue);

                try
                {
                    var rows = ExecuteQuery(sql);
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
                SearchInTab();
            }
        }

        private void SearchInTab()
        {
            try
            {
                string opValue = SelectedOperator?.OperatorValue ?? "=";

                if (SelectedTab.TabType == TabType.Form)
                {
                    SelectedTab.Header = $"搜索: {SearchText}";
                    string sql = BuildFormQuery(SelectedTab.ModuleId, SearchText, opValue);
                    var rows = ExecuteQuery(sql);

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

    public class OperatorItem
    {
        public string DisplayName { get; set; }
        public string OperatorValue { get; set; }
    }

    public class RelayCommand : ICommand
    {
        private readonly Action<object> _execute;
        private readonly Func<object, bool> _canExecute;

        public RelayCommand(Action<object> execute, Func<object, bool> canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter) => _canExecute?.Invoke(parameter) ?? true;

        public void Execute(object parameter) => _execute(parameter);

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
    }
}
