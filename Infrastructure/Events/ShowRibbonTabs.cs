using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ValosModeler.Infrastructure.Events
{
    public class ShowRibbonTabs
    {
        public const string MessageID = "ShowRibbonTabs";

        public static void Publish(object tabObject)
        {
            ViewModelBase.MediatorStatic.NotifyColleagues<object>(typeof(ShowRibbonTabs).Name, tabObject);
        }
    }
}
