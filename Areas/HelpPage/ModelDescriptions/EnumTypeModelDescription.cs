using System.Collections.ObjectModel;

namespace CampusLogicEvents.Web.Areas.HelpPage.ModelDescriptions
{
	public class EnumTypeModelDescription : ModelDescription
    {
        public EnumTypeModelDescription()
        {
            Values = new Collection<EnumValueDescription>();
        }

        public Collection<EnumValueDescription> Values { get; private set; }
    }
}