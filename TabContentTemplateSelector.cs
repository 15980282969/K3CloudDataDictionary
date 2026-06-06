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

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is ModuleTabItem tab)
            {
                switch (tab.TabType)
                {
                    case TabType.Form: return FormTemplate;
                    case TabType.Entity: return EntityTemplate;
                    case TabType.Field: return FieldTemplate;
                }
            }
            return null;
        }
    }
}
