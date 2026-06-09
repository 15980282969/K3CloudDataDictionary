using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace K3CloudDataDictionary.Models
{
    public class EnumItemInfo : INotifyPropertyChanged
    {
        private string _fvalue;
        private string _fcaption;

        public string FValue
        {
            get => _fvalue;
            set { _fvalue = value; OnPropertyChanged(); }
        }

        public string FCaption
        {
            get => _fcaption;
            set { _fcaption = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
