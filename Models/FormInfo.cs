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
        private int _formPluginCount;
        private int _listPluginCount;
        private int _builderPluginCount;

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

        public int FormPluginCount
        {
            get => _formPluginCount;
            set { _formPluginCount = value; OnPropertyChanged(); }
        }

        public int ListPluginCount
        {
            get => _listPluginCount;
            set { _listPluginCount = value; OnPropertyChanged(); }
        }

        public int BuilderPluginCount
        {
            get => _builderPluginCount;
            set { _builderPluginCount = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
