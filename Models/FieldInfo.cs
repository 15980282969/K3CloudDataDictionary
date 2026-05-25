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

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
