using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace K3CloudDataDictionary.Models
{
    public class FormOperationDisplayItem : INotifyPropertyChanged
    {
        private string _operation;
        private string _operationName;
        private int _formOperationDbId;
        private int _validationCount;
        private int _servicePluginCount;
        private int _appServiceCount;

        public string Operation
        {
            get => _operation;
            set { _operation = value; OnPropertyChanged(); }
        }

        public string OperationName
        {
            get => _operationName;
            set { _operationName = value; OnPropertyChanged(); }
        }

        public int FormOperationDbId
        {
            get => _formOperationDbId;
            set { _formOperationDbId = value; OnPropertyChanged(); }
        }

        public int ValidationCount
        {
            get => _validationCount;
            set { _validationCount = value; OnPropertyChanged(); OnPropertyChanged(nameof(ValidationCountDisplay)); }
        }

        public int ServicePluginCount
        {
            get => _servicePluginCount;
            set { _servicePluginCount = value; OnPropertyChanged(); OnPropertyChanged(nameof(ServicePluginCountDisplay)); }
        }

        public int AppServiceCount
        {
            get => _appServiceCount;
            set { _appServiceCount = value; OnPropertyChanged(); OnPropertyChanged(nameof(AppServiceCountDisplay)); }
        }

        public string ValidationCountDisplay => _validationCount > 0 ? _validationCount.ToString() : "";
        public string ServicePluginCountDisplay => _servicePluginCount > 0 ? _servicePluginCount.ToString() : "";
        public string AppServiceCountDisplay => _appServiceCount > 0 ? _appServiceCount.ToString() : "";

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class ValidationDisplayItem : INotifyPropertyChanged
    {
        private string _errorMessage;
        private string _description;
        private string _isUsed;
        private string _operationName;
        private string _validationTypeName;

        public string ErrorMessage
        {
            get => _errorMessage;
            set { _errorMessage = value; OnPropertyChanged(); }
        }

        public string Description
        {
            get => _description;
            set { _description = value; OnPropertyChanged(); }
        }

        public string IsUsed
        {
            get => _isUsed;
            set { _isUsed = value; OnPropertyChanged(); }
        }

        public string OperationName
        {
            get => _operationName;
            set { _operationName = value; OnPropertyChanged(); }
        }

        public string ValidationTypeName
        {
            get => _validationTypeName;
            set { _validationTypeName = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class FormOperationPluginDisplayItem : INotifyPropertyChanged
    {
        private string _className;
        private string _isEnabled;
        private string _operationName;

        public string ClassName
        {
            get => _className;
            set { _className = value; OnPropertyChanged(); }
        }

        public string IsEnabled
        {
            get => _isEnabled;
            set { _isEnabled = value; OnPropertyChanged(); }
        }

        public string OperationName
        {
            get => _operationName;
            set { _operationName = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class FormOperationAppServiceDisplayItem : INotifyPropertyChanged
    {
        private string _description;
        private string _operationName;
        private string _isForbidden;

        public string Description
        {
            get => _description;
            set { _description = value; OnPropertyChanged(); }
        }

        public string OperationName
        {
            get => _operationName;
            set { _operationName = value; OnPropertyChanged(); }
        }

        public string IsForbidden
        {
            get => _isForbidden;
            set { _isForbidden = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
