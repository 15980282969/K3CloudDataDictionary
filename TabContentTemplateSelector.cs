using K3CloudDataDictionary.Models;
using System.Windows;
using System.Windows.Controls;

namespace K3CloudDataDictionary
{
    public class TabContentTemplateSelector : DataTemplateSelector
    {
        public DataTemplate FormTemplate { get; set; }
        public DataTemplate EntityTemplate { get; set; }
        public DataTemplate FieldTemplate { get; set; }
        public DataTemplate EnumTemplate { get; set; }
        public DataTemplate AllFieldsTemplate { get; set; }
        public DataTemplate BillTypeTemplate { get; set; }
        public DataTemplate AssistantDataTemplate { get; set; }
        public DataTemplate EntityServiceRuleTemplate { get; set; }
        public DataTemplate EntityServiceRuleDetailTemplate { get; set; }
        public DataTemplate PluginTemplate { get; set; }
        public DataTemplate FieldUpdateActionTemplate { get; set; }
        public DataTemplate FormOperationTemplate { get; set; }
        public DataTemplate ValidationTemplate { get; set; }
        public DataTemplate FormOperationPluginTemplate { get; set; }
        public DataTemplate FormOperationAppServiceTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is ModuleTabItem tab)
            {
                switch (tab.TabType)
                {
                    case TabType.Form: return FormTemplate;
                    case TabType.Entity: return EntityTemplate;
                    case TabType.Field: return FieldTemplate;
                    case TabType.Enum: return EnumTemplate;
                    case TabType.AllFields: return AllFieldsTemplate;
                    case TabType.BillType: return BillTypeTemplate;
                    case TabType.AssistantData: return AssistantDataTemplate;
                    case TabType.EntityServiceRule: return EntityServiceRuleTemplate;
                    case TabType.EntityServiceRuleDetail: return EntityServiceRuleDetailTemplate;
                    case TabType.Plugin: return PluginTemplate;
                    case TabType.FieldUpdateAction: return FieldUpdateActionTemplate;
                    case TabType.FormOperation: return FormOperationTemplate;
                    case TabType.Validation: return ValidationTemplate;
                    case TabType.FormOperationPlugin: return FormOperationPluginTemplate;
                    case TabType.FormOperationAppService: return FormOperationAppServiceTemplate;
                }
            }
            return null;
        }
    }
}
