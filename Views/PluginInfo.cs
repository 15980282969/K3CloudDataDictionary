namespace K3CloudDataDictionary.Views
{
    public class PluginInfo
    {
        public string Oid { get; set; } = "";
        public string Action { get; set; } = "";
        public string ClassName { get; set; } = "";
        public string OrderId { get; set; } = "";
        public string PluginType { get; set; } = ""; // FormPlugins / ListPlugins / WebFormBuilderPlugins
        public string ElementType { get; set; } = "";
        public string ElementStyle { get; set; } = "";
        public string IsEnabled { get; set; } = "";

        public PluginInfo Clone()
        {
            return new PluginInfo
            {
                Oid = Oid,
                Action = Action,
                ClassName = ClassName,
                OrderId = OrderId,
                PluginType = PluginType,
                ElementType = ElementType,
                ElementStyle = ElementStyle,
                IsEnabled = IsEnabled
            };
        }
    }
}
