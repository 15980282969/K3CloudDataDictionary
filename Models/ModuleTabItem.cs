using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace K3CloudDataDictionary.Models
{
    public enum TabType { Form, Entity, Field, Enum, AllFields, BillType, AssistantData, EntityServiceRule, EntityServiceRuleDetail, Plugin }

    public class ModuleTabItem : INotifyPropertyChanged
    {
        private string _header;
        private string _moduleId;
        private TabType _tabType;
        private ObservableCollection<FormInfo> _forms;
        private ObservableCollection<FormEntityInfo> _formEntities;
        private ObservableCollection<FieldInfo> _fields;
        private ObservableCollection<EnumItemInfo> _enumItems;
        private ObservableCollection<AllFieldInfo> _allFields;
        private ObservableCollection<BillTypeInfo> _billTypes;
        private ObservableCollection<AssistantDataItem> _assistantDataItems;
        private ObservableCollection<EntityServiceRuleDisplayItem> _entityServiceRules;
        private ObservableCollection<FormBusinessServiceDisplayItem> _allBusinessServices;
        private ObservableCollection<PluginDisplayItem> _plugins;

        public string Header
        {
            get => _header;
            set { _header = value; OnPropertyChanged(); }
        }

        public string ModuleId
        {
            get => _moduleId;
            set { _moduleId = value; OnPropertyChanged(); }
        }

        public TabType TabType
        {
            get => _tabType;
            set { _tabType = value; OnPropertyChanged(); }
        }

        public ObservableCollection<FormInfo> Forms
        {
            get => _forms;
            set { _forms = value; OnPropertyChanged(); }
        }

        public ObservableCollection<FormEntityInfo> FormEntities
        {
            get => _formEntities;
            set { _formEntities = value; OnPropertyChanged(); }
        }

        public ObservableCollection<FieldInfo> Fields
        {
            get => _fields;
            set { _fields = value; OnPropertyChanged(); }
        }

        public ObservableCollection<EnumItemInfo> EnumItems
        {
            get => _enumItems;
            set { _enumItems = value; OnPropertyChanged(); }
        }

        public ObservableCollection<AllFieldInfo> AllFields
        {
            get => _allFields;
            set { _allFields = value; OnPropertyChanged(); }
        }

        public ObservableCollection<BillTypeInfo> BillTypes
        {
            get => _billTypes;
            set { _billTypes = value; OnPropertyChanged(); }
        }

        public ObservableCollection<AssistantDataItem> AssistantDataItems
        {
            get => _assistantDataItems;
            set { _assistantDataItems = value; OnPropertyChanged(); }
        }

        public ObservableCollection<EntityServiceRuleDisplayItem> EntityServiceRules
        {
            get => _entityServiceRules;
            set { _entityServiceRules = value; OnPropertyChanged(); }
        }

        public ObservableCollection<FormBusinessServiceDisplayItem> AllBusinessServices
        {
            get => _allBusinessServices;
            set { _allBusinessServices = value; OnPropertyChanged(); }
        }

        public ObservableCollection<PluginDisplayItem> Plugins
        {
            get => _plugins;
            set { _plugins = value; OnPropertyChanged(); }
        }

        public bool IsFormTab => TabType == TabType.Form;
        public bool IsEntityTab => TabType == TabType.Entity;
        public bool IsFieldTab => TabType == TabType.Field;
        public bool IsEnumTab => TabType == TabType.Enum;
        public bool IsAllFieldsTab => TabType == TabType.AllFields;
        public bool IsBillTypeTab => TabType == TabType.BillType;
        public bool IsAssistantDataTab => TabType == TabType.AssistantData;
        public bool IsEntityServiceRuleTab => TabType == TabType.EntityServiceRule;
        public bool IsEntityServiceRuleDetailTab => TabType == TabType.EntityServiceRuleDetail;
        public bool IsPluginTab => TabType == TabType.Plugin;

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; OnPropertyChanged(); }
        }

        private bool _isMouseOver;
        public bool IsMouseOver
        {
            get => _isMouseOver;
            set { _isMouseOver = value; OnPropertyChanged(); }
        }

        public ModuleTabItem()
        {
            Forms = new ObservableCollection<FormInfo>();
            FormEntities = new ObservableCollection<FormEntityInfo>();
            Fields = new ObservableCollection<FieldInfo>();
            EnumItems = new ObservableCollection<EnumItemInfo>();
            AllFields = new ObservableCollection<AllFieldInfo>();
            BillTypes = new ObservableCollection<BillTypeInfo>();
            AssistantDataItems = new ObservableCollection<AssistantDataItem>();
            EntityServiceRules = new ObservableCollection<EntityServiceRuleDisplayItem>();
            AllBusinessServices = new ObservableCollection<FormBusinessServiceDisplayItem>();
            Plugins = new ObservableCollection<PluginDisplayItem>();
            TabType = TabType.Form;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
