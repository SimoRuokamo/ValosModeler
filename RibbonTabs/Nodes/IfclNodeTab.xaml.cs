using Enterprixe.WPF.Tools.Localization;
using Epx.BIM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls.Ribbon;
using System.Windows.Media;
using ValosModeler.Infrastructure;
using ValosModeler.ViewModels.Nodes;

namespace ValosModeler.RibbonTabs.Nodes
{
	/// <summary>
	/// Interaction logic for GeneralNodeTab.xaml
	/// </summary>
	public partial class IfcNodeTab : RibbonTab
	{
		Valos.Ifc.ValosIfcSpatialNode _ifcSpatialNode;
		public IfcNodeTab()
		{
			InitializeComponent();
		}
		internal IfcNodeTab(Valos.Ifc.ValosIfcSpatialNode node) : this()
		{
			_ifcSpatialNode = node;
			InfoText.Text = _ifcSpatialNode.DebugValuesString;
		}
	}



	public class IfcNodeTabContainer : IRibbonContextualTabContainer
	{
		Valos.Ifc.ValosIfcSpatialNode _ifcSpatialNode;

		public IfcNodeTabContainer(object tabObject)
		{
			_ifcSpatialNode=  tabObject as Valos.Ifc.ValosIfcSpatialNode;
		}

		public IEnumerable<RibbonTab> ContextualTabs
		{
			get
			{
				RibbonTab tab = null;
				List<RibbonTab> contextualTabs = new List<RibbonTab>();

				AvalonDock.DockingManager dockingManager = ViewModelBase.DockingManager;
				if(dockingManager == null) return contextualTabs;

				tab = new IfcNodeTab(_ifcSpatialNode);
				tab.Header = CultureManager.GetLocalizedString("Ifc Object");
				tab.ContextualTabGroupHeader = CultureManager.GetLocalizedString("Node");
				tab.IsSelected = true;
				tab.DataContext = new IfcNodeViewModel(_ifcSpatialNode as BaseDataNode, tab.IsSelected);

				if(tab != null)
					contextualTabs.Add(tab);

				return contextualTabs;
			}
		}
		public RibbonContextualTabGroup ContextualTabGroup
		{
			get
			{
				RibbonContextualTabGroup contextualTabGroup = new RibbonContextualTabGroup();
				contextualTabGroup.Header = CultureManager.GetLocalizedString("Node");
				contextualTabGroup.Background = Brushes.Crimson;

				string header = contextualTabGroup.Header.ToString();
				if(header.Length > 0)
				{
					contextualTabGroup.Visibility = System.Windows.Visibility.Visible;
				}

				return contextualTabGroup;
			}
		}
	}
}