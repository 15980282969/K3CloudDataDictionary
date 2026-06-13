using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace K3CloudDataDictionary.Models
{
    public class ConnectionInfo : INotifyPropertyChanged
    {
        private int _id;
        private string _name;
        private string _serverIp;
        private int _port;
        private string _userName;
        private string _password;
        private string _database;
        private bool _isDefault;
        private string _localDbFileName;

        public int Id
        {
            get => _id;
            set { _id = value; OnPropertyChanged(); }
        }

        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(); }
        }

        public string ServerIp
        {
            get => _serverIp;
            set { _serverIp = value; OnPropertyChanged(); }
        }

        public int Port
        {
            get => _port;
            set { _port = value; OnPropertyChanged(); }
        }

        public string UserName
        {
            get => _userName;
            set { _userName = value; OnPropertyChanged(); }
        }

        public string Password
        {
            get => _password;
            set { _password = value; OnPropertyChanged(); }
        }

        public string Database
        {
            get => _database;
            set { _database = value; OnPropertyChanged(); }
        }

        public bool IsDefault
        {
            get => _isDefault;
            set { _isDefault = value; OnPropertyChanged(); }
        }

        public string LocalDbFileName
        {
            get => _localDbFileName;
            set { _localDbFileName = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// 是否为当前正在使用的连接（仅用于 UI 显示，不持久化）
        /// </summary>
        public bool IsCurrent
        {
            get => _isCurrent;
            set { _isCurrent = value; OnPropertyChanged(); }
        }

        private bool _isCurrent;

        /// <summary>
        /// 实际使用的本地数据文件名：优先使用 LocalDbFileName，否则用数据库名生成
        /// </summary>
        public string EffectiveLocalDbFileName
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(_localDbFileName))
                    return _localDbFileName;
                if (!string.IsNullOrWhiteSpace(_database))
                    return $"{_database}.db";
                return "metadata.db";
            }
        }

        public string ConnectionString
        {
            get
            {
                return $"Server={ServerIp},{Port};Database={Database};User Id={UserName};Password={Password};";
            }
        }

        public string DisplayName
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(Name) && !string.IsNullOrWhiteSpace(Database))
                    return $"{Name} ({Database})";
                if (!string.IsNullOrWhiteSpace(Name))
                    return Name;
                if (!string.IsNullOrWhiteSpace(Database))
                    return $"{ServerIp},{Port} ({Database})";
                return $"{ServerIp},{Port}";
            }
        }

        public ConnectionInfo Clone()
        {
            return new ConnectionInfo
            {
                Id = this.Id,
                Name = this.Name,
                ServerIp = this.ServerIp,
                Port = this.Port,
                UserName = this.UserName,
                Password = this.Password,
                Database = this.Database,
                IsDefault = this.IsDefault,
                LocalDbFileName = this.LocalDbFileName
            };
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
