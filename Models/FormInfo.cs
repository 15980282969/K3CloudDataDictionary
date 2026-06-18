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
        private int _updateActionCount;
        private int _serviceRuleCount;
        private int _formOperationCount;

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
            set { _formPluginCount = value; OnPropertyChanged(); OnPropertyChanged(nameof(FormPluginCountDisplay)); }
        }

        public int ListPluginCount
        {
            get => _listPluginCount;
            set { _listPluginCount = value; OnPropertyChanged(); OnPropertyChanged(nameof(ListPluginCountDisplay)); }
        }

        public int BuilderPluginCount
        {
            get => _builderPluginCount;
            set { _builderPluginCount = value; OnPropertyChanged(); OnPropertyChanged(nameof(BuilderPluginCountDisplay)); }
        }

        public int UpdateActionCount
        {
            get => _updateActionCount;
            set { _updateActionCount = value; OnPropertyChanged(); OnPropertyChanged(nameof(UpdateActionCountDisplay)); }
        }

        public int ServiceRuleCount
        {
            get => _serviceRuleCount;
            set { _serviceRuleCount = value; OnPropertyChanged(); OnPropertyChanged(nameof(ServiceRuleCountDisplay)); }
        }

        public int FormOperationCount
        {
            get => _formOperationCount;
            set { _formOperationCount = value; OnPropertyChanged(); OnPropertyChanged(nameof(FormOperationCountDisplay)); }
        }

        public string FormPluginCountDisplay => _formPluginCount > 0 ? _formPluginCount.ToString() : "";
        public string ListPluginCountDisplay => _listPluginCount > 0 ? _listPluginCount.ToString() : "";
        public string BuilderPluginCountDisplay => _builderPluginCount > 0 ? _builderPluginCount.ToString() : "";
        public string UpdateActionCountDisplay => _updateActionCount > 0 ? _updateActionCount.ToString() : "";
        public string ServiceRuleCountDisplay => _serviceRuleCount > 0 ? _serviceRuleCount.ToString() : "";
        public string FormOperationCountDisplay => _formOperationCount > 0 ? _formOperationCount.ToString() : "";

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
