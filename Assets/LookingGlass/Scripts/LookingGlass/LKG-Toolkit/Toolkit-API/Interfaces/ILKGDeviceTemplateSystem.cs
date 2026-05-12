using System.Collections.Generic;
using LookingGlass.Toolkit;

namespace LookingGlass.Toolkit {
    public interface ILKGDeviceTemplateSystem {
        public LKGDeviceTemplate GetDefaultTemplate() => GetTemplate(LKGDeviceTypeExtensions.GetDefault());
        public LKGDeviceTemplate GetTemplate(LKGDeviceType deviceType);
        public IEnumerable<LKGDeviceTemplate> GetAllTemplates();
    }
}
