using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace K3CloudDataDictionary.Models
{
    public class FormEntityInfo : INotifyPropertyChanged
    {
        private bool _isSelected;
        private string _formId;
        private string _entityId;
        private string _formIdentifier;
        private string _formName;
        private string _formModelType;
        private string _entityKey;
        private string _entityEntryName;
        private string _entityName;
        private string _entityTableName;
        private string _entityEntryPkFieldName;
        private string _entityElementTypeName;
        private int _serviceRuleCount;

        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; OnPropertyChanged(); }
        }

        public string FormId
        {
            get => _formId;
            set { _formId = value; OnPropertyChanged(); }
        }

        public string EntityId
        {
            get => _entityId;
            set { _entityId = value; OnPropertyChanged(); }
        }

        public string FormIdentifier
        {
            get => _formIdentifier;
            set { _formIdentifier = value; OnPropertyChanged(); }
        }

        public string FormName
        {
            get => _formName;
            set { _formName = value; OnPropertyChanged(); }
        }

        public string FormModelType
        {
            get => _formModelType;
            set { _formModelType = value; OnPropertyChanged(); }
        }

        public string EntityKey
        {
            get => _entityKey;
            set { _entityKey = value; OnPropertyChanged(); }
        }

        public string EntityEntryName
        {
            get => _entityEntryName;
            set { _entityEntryName = value; OnPropertyChanged(); }
        }

        public string EntityName
        {
            get => _entityName;
            set { _entityName = value; OnPropertyChanged(); }
        }

        public string EntityTableName
        {
            get => _entityTableName;
            set { _entityTableName = value; OnPropertyChanged(); }
        }

        public string EntityEntryPkFieldName
        {
            get => _entityEntryPkFieldName;
            set { _entityEntryPkFieldName = value; OnPropertyChanged(); }
        }

        public string EntityElementTypeName
        {
            get => _entityElementTypeName;
            set { _entityElementTypeName = value; OnPropertyChanged(); }
        }

        public int ServiceRuleCount
        {
            get => _serviceRuleCount;
            set { _serviceRuleCount = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
