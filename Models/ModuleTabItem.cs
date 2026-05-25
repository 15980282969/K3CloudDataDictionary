using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace K3CloudDataDictionary.Models
{
    public enum TabType { Form, Entity, Field }

    public class ModuleTabItem : INotifyPropertyChanged
    {
        private string _header;
        private string _moduleId;
        private TabType _tabType;
        private ObservableCollection<FormInfo> _forms;
        private ObservableCollection<FormEntityInfo> _formEntities;
        private ObservableCollection<FieldInfo> _fields;

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

        public bool IsFormTab => TabType == TabType.Form;
        public bool IsEntityTab => TabType == TabType.Entity;
        public bool IsFieldTab => TabType == TabType.Field;

        public ModuleTabItem()
        {
            Forms = new ObservableCollection<FormInfo>();
            FormEntities = new ObservableCollection<FormEntityInfo>();
            Fields = new ObservableCollection<FieldInfo>();
            TabType = TabType.Form;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
