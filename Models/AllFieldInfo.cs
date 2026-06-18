using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace K3CloudDataDictionary.Models
{
    public class AllFieldInfo : INotifyPropertyChanged
    {
        private string _formName;
        private string _entityName;
        private string _entityTableName;
        private string _key;
        private string _name;
        private string _fieldName;
        private string _propertyName;
        private string _elementTypeName;
        private string _lookUpObjectID;
        private string _enumType;
        private string _lookUpObjectDisplay;
        private string _enumTypeDisplay;
        private string _suffix;
        private string _splitDescription;
        private int _updateActionCount;
        private string _fieldDbId;

        public string FormName { get => _formName; set { _formName = value; OnPropertyChanged(); } }
        public string EntityName { get => _entityName; set { _entityName = value; OnPropertyChanged(); } }
        public string EntityTableName { get => _entityTableName; set { _entityTableName = value; OnPropertyChanged(); } }
        public string Key { get => _key; set { _key = value; OnPropertyChanged(); } }
        public string Name { get => _name; set { _name = value; OnPropertyChanged(); } }
        public string FieldName { get => _fieldName; set { _fieldName = value; OnPropertyChanged(); } }
        public string PropertyName { get => _propertyName; set { _propertyName = value; OnPropertyChanged(); } }
        public string ElementTypeName { get => _elementTypeName; set { _elementTypeName = value; OnPropertyChanged(); } }
        public string LookUpObjectID { get => _lookUpObjectID; set { _lookUpObjectID = value; OnPropertyChanged(); } }
        public string EnumType { get => _enumType; set { _enumType = value; OnPropertyChanged(); } }
        public string LookUpObjectDisplay { get => _lookUpObjectDisplay; set { _lookUpObjectDisplay = value; OnPropertyChanged(); } }
        public string EnumTypeDisplay { get => _enumTypeDisplay; set { _enumTypeDisplay = value; OnPropertyChanged(); } }
        public string Suffix { get => _suffix; set { _suffix = value; OnPropertyChanged(); } }
        public string SplitDescription { get => _splitDescription; set { _splitDescription = value; OnPropertyChanged(); } }
        public int UpdateActionCount { get => _updateActionCount; set { _updateActionCount = value; OnPropertyChanged(); OnPropertyChanged(nameof(UpdateActionCountDisplay)); } }
        public string UpdateActionCountDisplay => _updateActionCount > 0 ? _updateActionCount.ToString() : "";
        public string FieldDbId { get => _fieldDbId; set { _fieldDbId = value; OnPropertyChanged(); } }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
