using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace K3CloudDataDictionary.Models
{
    public class BillTypeInfo : INotifyPropertyChanged
    {
        private string _billTypeId;
        private string _billFormId;
        private string _number;
        private string _name;

        public string BillTypeId
        {
            get => _billTypeId;
            set { _billTypeId = value; OnPropertyChanged(); }
        }

        public string BillFormId
        {
            get => _billFormId;
            set { _billFormId = value; OnPropertyChanged(); }
        }

        public string Number
        {
            get => _number;
            set { _number = value; OnPropertyChanged(); }
        }

        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
