using Epx.BIM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ValosModeler.Infrastructure;
using ValosModeler.Infrastructure.Events;

namespace ValosModeler.Views
{
	public class ViewMappings
	{
		public static void Initialize()
		{
#if !NETCoreOnly
			// data nodes
			DataNodeEventToViewMapper.Instance.Register<OpenView, DataModel>(typeof(Views.Model3DView.ModelView), typeof(Views.Model3DView.ModelViewViewModel));
			// views or controls
#endif
		}
	}
}
