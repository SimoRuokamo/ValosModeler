using Enterprixe.WPF.Tools.Localization;
using Epx.BIM.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Ribbon;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ValosModeler.Infrastructure;

namespace ValosModeler.RibbonTabs.Nodes
{
	/// <summary>
	/// Interaction logic for IPluginNodeTab.xaml
	/// </summary>
	public partial class IPluginNodeTab : RibbonTab
	{
		public IPluginNodeTab()
		{
			InitializeComponent();
		}
	}

	public class IPluginNodeTabContainer : IRibbonContextualTabContainer
	{
		IPluginNode _node;

		public IPluginNodeTabContainer(object tabObject)
		{
			_node = tabObject as IPluginNode;
		}

		public IEnumerable<RibbonTab> ContextualTabs
		{
			get
			{
				RibbonTab tab = null;
				List<RibbonTab> contextualTabs = new List<RibbonTab>();

				AvalonDock.DockingManager dockingManager = ViewModelBase.DockingManager;
				if (dockingManager == null) return contextualTabs;

				tab = new IPluginNodeTab();
				tab.Header = CultureManager.GetLocalizedString("Feature");
				tab.ContextualTabGroupHeader = CultureManager.GetLocalizedString("Node");
				tab.IsSelected = false;
				tab.DataContext = new ViewModels.Nodes.IPluginNodeViewModel(_node as IPluginNode, tab.IsSelected);

				if (tab != null)
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
				if (header.Length > 0)
				{
					contextualTabGroup.Visibility = System.Windows.Visibility.Visible;
				}

				return contextualTabGroup;
			}
		}
	}
}
