using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace K3CloudDataDictionary.Models
{
    public class FieldInfo : INotifyPropertyChanged
    {
        private string _key;
        private string _name;
        private string _fieldName;
        private string _propertyName;
        private string _elementTypeName;
        private string _suffix;
        private string _splitDescription;
        private string _lookUpObjectID;
        private string _enumType;
        private string _lookUpObjectDisplay;
        private string _enumTypeDisplay;

        public string Key
        {
            get => _key;
            set { _key = value; OnPropertyChanged(); }
        }

        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(); }
        }

        public string FieldName
        {
            get => _fieldName;
            set { _fieldName = value; OnPropertyChanged(); }
        }

        public string PropertyName
        {
            get => _propertyName;
            set { _propertyName = value; OnPropertyChanged(); }
        }

        public string ElementTypeName
        {
            get => _elementTypeName;
            set { _elementTypeName = value; OnPropertyChanged(); }
        }

        public string Suffix
        {
            get => _suffix;
            set { _suffix = value; OnPropertyChanged(); }
        }

        public string SplitDescription
        {
            get => _splitDescription;
            set { _splitDescription = value; OnPropertyChanged(); }
        }

        public string LookUpObjectID
        {
            get => _lookUpObjectID;
            set { _lookUpObjectID = value; OnPropertyChanged(); }
        }

        public string EnumType
        {
            get => _enumType;
            set { _enumType = value; OnPropertyChanged(); }
        }

        public string LookUpObjectDisplay
        {
            get => _lookUpObjectDisplay;
            set { _lookUpObjectDisplay = value; OnPropertyChanged(); }
        }

        public string EnumTypeDisplay
        {
            get => _enumTypeDisplay;
            set { _enumTypeDisplay = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
