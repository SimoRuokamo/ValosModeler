using Epx.BIM;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ValosModeler.Infrastructure;
using ValosModeler.Views.Model3DView;


namespace ValosModeler.RibbonTabs
{
	public static class RibbonTabMappings
	{
		/// <summary>
		/// Register Event to View mappings for RibbonTabs.
		/// </summary>
		public static void Initialize()
		{
			DataNodeEventToViewMapper.Instance.Register<Infrastructure.Events.ShowRibbonContextualTabs, Tuple<Epx.BIM.Plugins.PluginTool, FeatureEngineViewModel>>(typeof(FeatureTabContainer));
			// this dictates the order of the tabs
			// 1. General
			DataNodeEventToViewMapper.Instance.Register<Infrastructure.Events.ShowRibbonContextualTabs, BaseDataNode>(typeof(Nodes.GeneralNodeTabContainer));
			// 2. Data node
			DataNodeEventToViewMapper.Instance.Register<Infrastructure.Events.ShowRibbonContextualTabs, Valos.Ifc.ValosIfcSpatialNode>(typeof(Nodes.IfcNodeTabContainer));
			// 3. Interfaces etc
			DataNodeEventToViewMapper.Instance.Register<Infrastructure.Events.ShowRibbonContextualTabs, Epx.BIM.Plugins.IPluginNode>(typeof(Nodes.IPluginNodeTabContainer));
		}
	}
}
