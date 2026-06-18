using System.Collections.Generic;
using System.Linq;

namespace K3CloudDataDictionary.Views
{
    /// <summary>
    /// FormOperation 信息模型
    /// </summary>
    public class FormOperationInfo
    {
        public string Oid { get; set; } = "";
        public string Action { get; set; } = "";
        public string Id { get; set; } = "";
        public string Operation { get; set; } = "";
        public string OperationName { get; set; } = "";
        public List<ValidationInfo> Validations { get; set; } = new List<ValidationInfo>();
        public List<FormOperationPluginInfo> ServicePlugins { get; set; } = new List<FormOperationPluginInfo>();
        public List<FormOperationAppServiceInfo> AppBusinessServices { get; set; } = new List<FormOperationAppServiceInfo>();

        public FormOperationInfo Clone()
        {
            return new FormOperationInfo
            {
                Oid = Oid,
                Action = Action,
                Id = Id,
                Operation = Operation,
                OperationName = OperationName,
                Validations = Validations.Select(v => v.Clone()).ToList(),
                ServicePlugins = ServicePlugins.Select(p => p.Clone()).ToList(),
                AppBusinessServices = AppBusinessServices.Select(s => s.Clone()).ToList()
            };
        }
    }

    /// <summary>
    /// Validation 信息模型（ConditionValidation、MustInputValidation、DoNothingValidation 等）
    /// </summary>
    public class ValidationInfo
    {
        public string Oid { get; set; } = "";
        public string Action { get; set; } = "";
        public string Id { get; set; } = "";
        public string ValidationType { get; set; } = "";
        public string ValidationTypeName { get; set; } = "";
        public string ErrorMessage { get; set; } = "";
        public string Description { get; set; } = "";
        public string IsUsed { get; set; } = "";

        public ValidationInfo Clone()
        {
            return new ValidationInfo
            {
                Oid = Oid,
                Action = Action,
                Id = Id,
                ValidationType = ValidationType,
                ValidationTypeName = ValidationTypeName,
                ErrorMessage = ErrorMessage,
                Description = Description,
                IsUsed = IsUsed
            };
        }
    }

    /// <summary>
    /// FormOperation 下的 ServicePlugin 信息模型（与表单插件属性一致）
    /// </summary>
    public class FormOperationPluginInfo
    {
        public string Oid { get; set; } = "";
        public string Action { get; set; } = "";
        public string ClassName { get; set; } = "";
        public string OrderId { get; set; } = "";
        public string ElementType { get; set; } = "";
        public string ElementStyle { get; set; } = "";
        public string IsEnabled { get; set; } = "";

        public FormOperationPluginInfo Clone()
        {
            return new FormOperationPluginInfo
            {
                Oid = Oid,
                Action = Action,
                ClassName = ClassName,
                OrderId = OrderId,
                ElementType = ElementType,
                ElementStyle = ElementStyle,
                IsEnabled = IsEnabled
            };
        }
    }

    /// <summary>
    /// FormOperation 下的 AppBusinessService 信息模型
    /// </summary>
    public class FormOperationAppServiceInfo
    {
        public string Oid { get; set; } = "";
        public string Action { get; set; } = "";
        public string Id { get; set; } = "";
        public string ServiceTypeName { get; set; } = "";
        public string Description { get; set; } = "";
        public string IsForbidden { get; set; } = "";

        public FormOperationAppServiceInfo Clone()
        {
            return new FormOperationAppServiceInfo
            {
                Oid = Oid,
                Action = Action,
                Id = Id,
                ServiceTypeName = ServiceTypeName,
                Description = Description,
                IsForbidden = IsForbidden
            };
        }
    }
}
