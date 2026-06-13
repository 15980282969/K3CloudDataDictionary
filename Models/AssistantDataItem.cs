using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace K3CloudDataDictionary.Models
{
    public class AssistantDataItem : INotifyPropertyChanged
    {
        private string _fid;
        private string _fnumber;
        private string _fname;
        private string _fentryId;
        private string _fentryNumber;
        private string _fdataValue;

        public string FId
        {
            get => _fid;
            set { _fid = value; OnPropertyChanged(); }
        }

        public string FNumber
        {
            get => _fnumber;
            set { _fnumber = value; OnPropertyChanged(); }
        }

        public string FName
        {
            get => _fname;
            set { _fname = value; OnPropertyChanged(); }
        }

        public string FEntryId
        {
            get => _fentryId;
            set { _fentryId = value; OnPropertyChanged(); }
        }

        public string FEntryNumber
        {
            get => _fentryNumber;
            set { _fentryNumber = value; OnPropertyChanged(); }
        }

        public string FDataValue
        {
            get => _fdataValue;
            set { _fdataValue = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
