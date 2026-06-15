using System.Collections.Generic;

namespace K3CloudDataDictionary.Views
{
    public class EntityServiceRuleInfo
    {
        public string Oid { get; set; } = "";
        public string Action { get; set; } = "";
        public string Id { get; set; } = "";
        public string Description { get; set; } = "";
        public string IsEnabled { get; set; } = "";
        public string PreCondition { get; set; } = "";
        public string PreConditionDesc { get; set; } = "";
        public string Seq { get; set; } = "";
        public string EntityKey { get; set; } = "";
        public List<FormBusinessServiceInfo> WhenTrueServices { get; set; } = new List<FormBusinessServiceInfo>();
        public List<FormBusinessServiceInfo> WhenFalseServices { get; set; } = new List<FormBusinessServiceInfo>();

        public EntityServiceRuleInfo Clone()
        {
            return new EntityServiceRuleInfo
            {
                Oid = Oid,
                Action = Action,
                Id = Id,
                Description = Description,
                IsEnabled = IsEnabled,
                PreCondition = PreCondition,
                PreConditionDesc = PreConditionDesc,
                Seq = Seq,
                EntityKey = EntityKey,
                WhenTrueServices = new List<FormBusinessServiceInfo>(WhenTrueServices),
                WhenFalseServices = new List<FormBusinessServiceInfo>(WhenFalseServices)
            };
        }
    }

    public class FormBusinessServiceInfo
    {
        public string Oid { get; set; } = "";
        public string Action { get; set; } = "";
        public string Id { get; set; } = "";
        public string ActionId { get; set; } = "";
        public string Description { get; set; } = "";
        public string Parameters { get; set; } = "";
        public string ParentRuleId { get; set; } = "";
        public string ServiceType { get; set; } = ""; // WhenTrue / WhenFalse
        public string ServiceTypeName { get; set; } = ""; // XML元素名，如 FormBusinessService、GetPriceBusinessServiceMeta

        public FormBusinessServiceInfo Clone()
        {
            return new FormBusinessServiceInfo
            {
                Oid = Oid,
                Action = Action,
                Id = Id,
                ActionId = ActionId,
                Description = Description,
                Parameters = Parameters,
                ParentRuleId = ParentRuleId,
                ServiceType = ServiceType,
                ServiceTypeName = ServiceTypeName
            };
        }
    }
}
