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
	public class GeneralNodeTabContainer : Infrastructure.IRibbonContextualTabContainer
	{
		object _tabObject;

		public GeneralNodeTabContainer(object tabObject)
		{
			_tabObject = tabObject;
		}

		public IEnumerable<RibbonTab> ContextualTabs
		{
			get
			{
				RibbonTab tab = null;
				List<RibbonTab> contextualTabs = new List<RibbonTab>();

				AvalonDock.DockingManager dockingManager = ViewModelBase.DockingManager;
				if (dockingManager == null) return contextualTabs;

				tab = new GeneralNodeTab();
				tab.Header = CultureManager.GetLocalizedString("Properties");
				tab.ContextualTabGroupHeader = CultureManager.GetLocalizedString("Node");
				tab.IsSelected = true;
				tab.DataContext = new GeneralNodeViewModel(_tabObject as BaseDataNode, tab.IsSelected);

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
