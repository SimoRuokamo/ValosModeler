using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ValosModeler.Infrastructure
{
    public interface IRibbonTabContainer
    {
        IEnumerable<System.Windows.Controls.Ribbon.RibbonTab> Tabs { get; }
    }
}
