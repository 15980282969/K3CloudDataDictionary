namespace K3CloudDataDictionary.Views
{
    public class FieldUpdateActionInfo
    {
        public string Oid { get; set; } = "";
        public string Action { get; set; } = "";
        public string Id { get; set; } = "";
        public string ActionId { get; set; } = "";
        public string Description { get; set; } = "";
        public string Parameters { get; set; } = "";
        public string Seq { get; set; } = "";
        public string ServiceTypeName { get; set; } = ""; // XML元素名，如 FormBusinessService、GetPriceBusinessServiceMeta
        public string IsForbidden { get; set; } = "";
        public string PreCondition { get; set; } = "";
        public string PreConditionDesc { get; set; } = "";

        public FieldUpdateActionInfo Clone()
        {
            return new FieldUpdateActionInfo
            {
                Oid = Oid,
                Action = Action,
                Id = Id,
                ActionId = ActionId,
                Description = Description,
                Parameters = Parameters,
                Seq = Seq,
                ServiceTypeName = ServiceTypeName,
                IsForbidden = IsForbidden,
                PreCondition = PreCondition,
                PreConditionDesc = PreConditionDesc
            };
        }
    }
}
