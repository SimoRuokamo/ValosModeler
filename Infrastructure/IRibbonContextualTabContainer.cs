using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ValosModeler.Infrastructure
{
	public interface IRibbonContextualTabContainer
	{
		IEnumerable<System.Windows.Controls.Ribbon.RibbonTab> ContextualTabs { get; }
        System.Windows.Controls.Ribbon.RibbonContextualTabGroup ContextualTabGroup { get; }
	}
}
