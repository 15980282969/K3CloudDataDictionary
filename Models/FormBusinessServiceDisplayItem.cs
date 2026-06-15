using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace K3CloudDataDictionary.Models
{
    public class FormBusinessServiceDisplayItem : INotifyPropertyChanged
    {
        private string _serviceType;
        private string _serviceTypeName;
        private string _actionId;
        private string _description;
        private string _parameters;

        public string ServiceType
        {
            get => _serviceType;
            set { _serviceType = value; OnPropertyChanged(); }
        }

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

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
