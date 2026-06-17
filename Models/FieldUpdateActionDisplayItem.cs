using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace K3CloudDataDictionary.Models
{
    public class FieldUpdateActionDisplayItem : INotifyPropertyChanged
    {
        private string _serviceTypeName;
        private string _actionId;
        private string _description;
        private string _parameters;
        private string _seq;
        private string _isForbidden;
        private string _preCondition;
        private string _preConditionDesc;
        private string _fieldName;
        private string _fieldDisplayName;

        public string ServiceTypeName
        {
            get => _serviceTypeName;
            set { _serviceTypeName = value; OnPropertyChanged(); }
        }

        public string ActionId
        {
            get => _actionId;
            set { _actionId = value; OnPropertyChanged(); }
        }

        public string Description
        {
            get => _description;
            set { _description = value; OnPropertyChanged(); }
        }

        public string Parameters
        {
            get => _parameters;
            set { _parameters = value; OnPropertyChanged(); }
        }

        public string Seq
        {
            get => _seq;
            set { _seq = value; OnPropertyChanged(); }
        }

        public string IsForbidden
        {
            get => _isForbidden;
            set { _isForbidden = value; OnPropertyChanged(); }
        }

        public string PreCondition
        {
            get => _preCondition;
            set { _preCondition = value; OnPropertyChanged(); }
        }

        public string PreConditionDesc
        {
            get => _preConditionDesc;
            set { _preConditionDesc = value; OnPropertyChanged(); }
        }

        public string FieldName
        {
            get => _fieldName;
            set { _fieldName = value; OnPropertyChanged(); }
        }

        public string FieldDisplayName
        {
            get => _fieldDisplayName;
            set { _fieldDisplayName = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
