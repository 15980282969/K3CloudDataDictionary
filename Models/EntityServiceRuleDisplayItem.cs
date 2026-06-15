using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace K3CloudDataDictionary.Models
{
    public class EntityServiceRuleDisplayItem : INotifyPropertyChanged
    {
        private string _oid;
        private string _ruleId;
        private int _dbId;
        private string _description;
        private string _isEnabled;
        private string _preCondition;
        private string _preConditionDesc;
        private string _seq;
        private string _entityKey;
        private string _entityName;
        private string _whenTrueServices;
        private string _whenFalseServices;

        public string Oid
        {
            get => _oid;
            set { _oid = value; OnPropertyChanged(); }
        }

        public string RuleId
        {
            get => _ruleId;
            set { _ruleId = value; OnPropertyChanged(); }
        }

        public int DbId
        {
            get => _dbId;
            set { _dbId = value; OnPropertyChanged(); }
        }

        public string Description
        {
            get => _description;
            set { _description = value; OnPropertyChanged(); }
        }

        public string IsEnabled
        {
            get => _isEnabled;
            set { _isEnabled = value; OnPropertyChanged(); }
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

        public string Seq
        {
            get => _seq;
            set { _seq = value; OnPropertyChanged(); }
        }

        public string EntityKey
        {
            get => _entityKey;
            set { _entityKey = value; OnPropertyChanged(); }
        }

        public string EntityName
        {
            get => _entityName;
            set { _entityName = value; OnPropertyChanged(); }
        }

        public string WhenTrueServices
        {
            get => _whenTrueServices;
            set { _whenTrueServices = value; OnPropertyChanged(); }
        }

        public string WhenFalseServices
        {
            get => _whenFalseServices;
            set { _whenFalseServices = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
