using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace K3CloudDataDictionary.Models
{
    public class FormInfo : INotifyPropertyChanged
    {
        private string _formId;
        private string _formIdentifier;
        private string _formName;
        private string _modelTypeName;
        private string _subSystemName;

        public string FormId
        {
            get => _formId;
            set { _formId = value; OnPropertyChanged(); }
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

        public string ModelTypeName
        {
            get => _modelTypeName;
            set { _modelTypeName = value; OnPropertyChanged(); }
        }

        public string SubSystemName
        {
            get => _subSystemName;
            set { _subSystemName = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
