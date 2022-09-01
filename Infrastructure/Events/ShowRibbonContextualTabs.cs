using Epx.BIM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ValosModeler.Infrastructure.Events
{
    public class ShowRibbonContextualTabs
    {
        public const string MessageID = "ShowRibbonContextualTabs";

        public static void Publish(object tabObject)
        {
            ViewModelBase.MediatorStatic.NotifyColleagues<object>(typeof(ShowRibbonContextualTabs).Name, tabObject);
        }
    }
}
