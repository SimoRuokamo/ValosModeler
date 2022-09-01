using Epx.BIM;
using Epx.BIM.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ValosModeler.ViewModels.Nodes
{
	public class IPluginNodeViewModel : GeneralNodeViewModel
	{
		public IPluginNodeViewModel(IPluginNode node, bool isSelected)
			: base(node as BaseDataNode, isSelected)
		{
		}
	}
}
