using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace K3CloudDataDictionary.Models
{
    public class PluginDisplayItem : INotifyPropertyChanged
    {
        private string _pluginType;
        private string _className;
        private string _orderId;
        private string _elementType;
        private string _elementStyle;

        public string PluginType
        {
            get => _pluginType;
            set { _pluginType = value; OnPropertyChanged(); }
        }

        public string ClassName
        {
            get => _className;
            set { _className = value; OnPropertyChanged(); }
        }

        public string OrderId
        {
            get => _orderId;
            set { _orderId = value; OnPropertyChanged(); }
        }

        public string ElementType
        {
            get => _elementType;
            set { _elementType = value; OnPropertyChanged(); }
        }

        public string ElementStyle
        {
            get => _elementStyle;
            set { _elementStyle = value; OnPropertyChanged(); }
        }

        public string PluginTypeDisplay
        {
            get
            {
                switch (_pluginType)
                {
                    case "FormPlugins": return "表单插件";
                    case "ListPlugins": return "列表插件";
                    case "WebFormBuilderPlugins": return "构建插件";
                    default: return _pluginType ?? "";
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
