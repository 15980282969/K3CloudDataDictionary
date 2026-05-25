using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace K3CloudDataDictionary.Models
{
    public class Entity : INotifyPropertyChanged
    {
        private bool _isSelected;
        private string _elementType;
        private string _name;
        private string _ormEntityName;
        private string _tableName;
        private string _primaryKey;
        private string _splitTable;
        private string _remark;

        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; OnPropertyChanged(); }
        }

        public string ElementType
        {
            get => _elementType;
            set { _elementType = value; OnPropertyChanged(); }
        }

        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(); }
        }

        public string OrmEntityName
        {
            get => _ormEntityName;
            set { _ormEntityName = value; OnPropertyChanged(); }
        }

        public string TableName
        {
            get => _tableName;
            set { _tableName = value; OnPropertyChanged(); }
        }

        public string PrimaryKey
        {
            get => _primaryKey;
            set { _primaryKey = value; OnPropertyChanged(); }
        }

        public string SplitTable
        {
            get => _splitTable;
            set { _splitTable = value; OnPropertyChanged(); }
        }

        public string Remark
        {
            get => _remark;
            set { _remark = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
