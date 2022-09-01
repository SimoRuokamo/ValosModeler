using Enterprixe.ValosUITools.Features;
using Enterprixe.WPF.Tools.Localization;
using Epx.BIM.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls.Ribbon;
using System.Windows.Media;
using ValosModeler.Infrastructure;
using ValosModeler.Views.Model3DView;

namespace ValosModeler.RibbonTabs
{
	public class FeatureTabContainer : Infrastructure.IRibbonContextualTabContainer
	{
		object _tabObject;

		public FeatureTabContainer(object tabObject)
		{
			_tabObject = tabObject;
		}

		PluginTool ActiveFeature => (_tabObject as Tuple<PluginTool, FeatureEngineViewModel>).Item1 as PluginTool;

		public IEnumerable<RibbonTab> ContextualTabs
		{
			get
			{
				RibbonTab tab = null;
				List<RibbonTab> contextualTabs = new List<RibbonTab>();

				AvalonDock.DockingManager dockingManager = ViewModelBase.DockingManager;
				if (dockingManager == null) return contextualTabs;

				tab = new FeatureTab();
				tab.Header = CultureManager.GetLocalizedString("Properties");
				tab.ContextualTabGroupHeader = CultureManager.GetLocalizedString("Feature");
				tab.IsSelected = true;
				tab.DataContext = (_tabObject as Tuple<PluginTool, FeatureEngineViewModel>).Item2;
				tab.Items.Add(ActivePluginRibbonGroup);
				var ecg = tab.FindName("EndCommandRibbonGroup");//Set end command group last
				if (ecg != null)
				{
					tab.Items.Remove(ecg);
					tab.Items.Add(ecg);
				}

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
				contextualTabGroup.Header = CultureManager.GetLocalizedString("Feature");
				contextualTabGroup.Background = Brushes.DarkGoldenrod;

				string header = contextualTabGroup.Header.ToString();
				if (header.Length > 0)
				{
					contextualTabGroup.Visibility = System.Windows.Visibility.Visible;
				}

				return contextualTabGroup;
			}
		}

		public System.Windows.Controls.Ribbon.RibbonGroup ActivePluginRibbonGroup
		{
			get
			{
				if (ActiveFeature is IRibbonFeature)
				{
					var content = (ActiveFeature as IRibbonFeature).PreDialogRibbonTabGroup;
					content.DataContext = (ActiveFeature as IRibbonFeature).PreDialogRibbonViewModel;
					return content;
				}
				return null;
			}
		}
	}
}
