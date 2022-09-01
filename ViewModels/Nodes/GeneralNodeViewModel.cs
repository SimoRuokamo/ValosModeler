using AvalonDock.Layout;
using Enterprixe.WPF.Tools.Viewport;
using Epx.BIM;
using Epx.BIM.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls.Ribbon;
using System.Windows.Input;
using ValosModeler.Infrastructure;
using ValosModeler.Infrastructure.Events;
using ValosModeler.Views.Model3DView;

namespace ValosModeler.ViewModels.Nodes
{
	/// <summary>
	/// Base view model for all Data node view models and generic viewmodel for generic DataNodes.
	/// </summary>
	public class GeneralNodeViewModel : ViewModelBase
	{
		private List<PluginInfo> _supportedFeatures;
		private List<PluginInfo> _externalPlugins;

		public GeneralNodeViewModel(BaseDataNode node, bool isSelected)
			: base(node)
		{
			RegisterToMediator();
			IsSelected = isSelected;

			_supportedFeatures = FeatureManager.Features.Where(p => p.AcceptedMasterType.IsAssignableFrom(DataNode.GetType())).ToList();
			List<PluginInfo> removedPlugins = new List<PluginInfo>();
			//foreach (var plugin in _supportedFeatures)
			//{
			//	if (plugin.FullClassName.Contains("ValosModeler.Features")) removedPlugins.Add(plugin);
			//}
			_externalPlugins = _supportedFeatures.Except(removedPlugins).ToList();
		}

		/// <summary>
		/// For selection of the contextual tab when it is shown.
		/// </summary>
		public bool IsSelected { get; set; }

		/// <summary>
		/// Gets or sets the name of the first editable node in the view model.
		/// </summary>
		/// <value>The name.</value>
		public string Name
		{
			get
			{
				var first = _dataNodes.First();
				bool allSameValues = _dataNodes.All(n => n.IsEditable == first.IsEditable);
				if (allSameValues)
					return first.Name;
				else
					return null;
			}
			set
			{
				bool allow = !string.IsNullOrWhiteSpace(value);
				if (_dataNodes.Count == 1) allow = allow && _dataNodes.First().Name != value;
				if (allow)
				{
					SetNodePropertyAction(_dataNodes, "Name", value, (o, p) =>
					{
						Infrastructure.Events.NodePropertyChanged.Publish(o, nameof(o.Name), this);
					});
				}
			}
		}

		#region Mediator

		/// <summary>
		/// Called when [view model node property changed].
		/// </summary>
		/// <param name="payload">The payload.</param>
		public override void OnViewModelNodePropertyChanged(NodePropertyChangedPayload payload)
		{
			if (_dataNodes.Intersect(payload.DataNodes).Count() > 0)
			{
				foreach (string str in payload.ChangedPropertyNames)
				{
					// map data node names to viewmodel property names (not needed if both are the same)
				}
				base.OnViewModelNodePropertyChanged(payload);
			}
		}

		#endregion

		#region Features/Plugins

		public System.Windows.Visibility PluginsMenuVisibility
		{
			get { return _externalPlugins.Count > 0 ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed; }
		}

		public bool PluginsMenuEnabled
		{
			get { return _externalPlugins.Count > 0 ? true : false; }
		}

		public List<RibbonMenuItem> PluginsMenuItems
		{
			get
			{
				List<RibbonMenuItem> retval = new List<RibbonMenuItem>();
				foreach (var plugin in _externalPlugins.OrderBy(p => p.StartAttributes == null || !p.StartAttributes.Any() ? 100 : 
				  -p.StartAttributes.Count))
				{
					RibbonMenuItem item = new RibbonMenuItem();
					item.Header = plugin.NameForMenu;
					item.Command = RunPluginCommand;
					item.CommandParameter = item;
					item.ToolTip = plugin.MenuToolTip;
					item.Tag = plugin;
					item.ImageSource = plugin.IconImageSource as System.Windows.Media.ImageSource;
					if (plugin.StartAttributes != null && plugin.StartAttributes.Any() && !string.IsNullOrWhiteSpace(plugin.StartAttributes[0]))
					{
						string[] splitted = plugin.StartAttributes[0].Split('.');
						RibbonMenuItem parent = retval.FirstOrDefault(m => m.Header.ToString() == splitted[0]);
						if (parent == null)
						{
							parent = new RibbonMenuItem();
							parent.Header = splitted[0];
							retval.Add(parent);
						}
						for (int ij = 1; ij < splitted.Length; ++ij)
						{
							RibbonMenuItem nextParent = null;
							foreach (System.Windows.Controls.ItemsControl menuItem in parent.Items)
							{
								RibbonMenuItem mi = menuItem as RibbonMenuItem;
								if (mi != null)
								{
									if (mi.Header.ToString() == splitted[ij])
									{
										nextParent = mi;
									}
								}
							}
							if (nextParent == null)
							{
								nextParent = new RibbonMenuItem();
								nextParent.Header = splitted[ij];
								parent.Items.Add(nextParent);
								parent = nextParent;
							} else
							{
								parent = nextParent;
							}
						}
						parent.Items.Add(item);
					}
					else
					{
						retval.Add(item);
					}
				}
				return retval;
			}
		}

		#region RunPluginCommand
		RelayCommand _runPluginCommand;
		public ICommand RunPluginCommand
		{
			get
			{
				if (_runPluginCommand == null)
					_runPluginCommand = new RelayCommand(execute => this.ExecuteRunPlugin(execute), canexecute => this.CanExecuteRunPlugin(canexecute));
				return _runPluginCommand;
			}
		}

		protected virtual bool CanExecuteRunPlugin(object parameter)
		{
			RibbonMenuItem item = parameter as RibbonMenuItem;
			PluginInfo plugin = null;
			if (item != null)
			{
				plugin = item.Tag as PluginInfo;
			}
			else if (parameter is string)
			{
				string stringParam = parameter as string;
				plugin = _supportedFeatures.FirstOrDefault(p => p.PluginType.ToString().Contains(stringParam));
			}
			
			if (plugin != null)
			{
				bool isViewModeAllowed = false;
				View3DMode? viewmode = null;

				if (ViewModelBase.LastActiveDockableContent.ContainsKey(typeof(ModelView)))
				{
					ModelViewViewModel viewVM = ViewModelBase.LastActiveDockableContent[typeof(ModelView)] as ModelViewViewModel;
					viewmode = viewVM.ViewportViewMode;
				}
				else
				{

					var view = DockingManager.Layout.Descendents().OfType<LayoutContent>().Where(lc => lc.Content is ModelView).Select(lc => lc.Content).FirstOrDefault() as ModelView;
					ModelViewViewModel viewVM = view != null ? view.DataContext as ModelViewViewModel : null;
					if (viewVM != null)
					{
						viewmode = viewVM.ViewportViewMode;
					}
				}

				if( ViewModelBase.LastActiveModelView is ModelViewViewModel viewm)
				{
					viewmode = viewm.ViewportViewMode;
				}

				if (viewmode.HasValue)
				{
					if (viewmode == View3DMode.Mode3D && plugin.AllowedViewModes.Contains(PluginTool.PluginViewModes.View3D)) isViewModeAllowed = true;
					else if (viewmode == View3DMode.Top && plugin.AllowedViewModes.Contains(PluginTool.PluginViewModes.Z)) isViewModeAllowed = true;
					else if (viewmode == View3DMode.Left && plugin.AllowedViewModes.Contains(PluginTool.PluginViewModes.X)) isViewModeAllowed = true;
					else if (viewmode == View3DMode.Right && plugin.AllowedViewModes.Contains(PluginTool.PluginViewModes.Y)) isViewModeAllowed = true;
				}

				var pluginTool = FeatureManager.GetPluginTool(plugin.PluginType,plugin.StartAttributes);
				Project prj = DataNode.GetParent<Project>();
				pluginTool.OnSetCreateMode(DataNode, null);
				BaseDataNode targetNode = prj == null ? null : prj.GetTargetFolder();
				bool isValidTarget = (prj == null ? false : pluginTool.IsValidTarget(targetNode));

				if (isValidTarget && DataNode.DatabaseRoot.ConnectedToServer)
				{
					if (targetNode != null) isValidTarget &= targetNode.IsEditable;
				}

				return isViewModeAllowed && isValidTarget;
			}
			else
			{
				return false;
			}
		}

		private void ExecuteRunPlugin(object parameter)
		{
			RibbonMenuItem item = parameter as RibbonMenuItem;
			PluginInfo plugin = null;
			if (item != null)
			{
				plugin = item.Tag as PluginInfo;
			}
			else if (parameter is string)
			{
				string stringParam = parameter as string;
				plugin = _supportedFeatures.FirstOrDefault(p => p.PluginType.Name.ToString().StartsWith(stringParam));
			}
			

			if (plugin != null)
			{
				plugin.MasterNode = DataNode;
				Infrastructure.Events.DesignCommand.Run(plugin, this);
			}
		}
		#endregion //RunPluginCommand

		#region EditPluginCommand
		RelayCommand _editPluginCommand;
		public ICommand EditPluginCommand
		{
			get
			{
				if (_editPluginCommand == null)
					_editPluginCommand = new RelayCommand(execute => this.ExecuteEditPlugin(execute), canexecute => this.CanExecuteEditPlugin(canexecute));
				return _editPluginCommand;
			}
		}

		private bool CanExecuteEditPlugin(object parameter)
		{
			if (DataNode is IPluginNode && !string.IsNullOrEmpty((DataNode as IPluginNode).FullClassName))
			{
				var pluginInfo = FeatureManager.Features.FirstOrDefault(p => p.PluginType.ToString().Contains((DataNode as IPluginNode).FullClassName));
				
				return DataNode is IPluginNode && pluginInfo != null && pluginInfo.SupportsEditMode;
			}
			return false;
		}

		private void ExecuteEditPlugin(object parameter)
		{
			PluginInfo plugin = null;

			if (DataNode is IPluginNode && !string.IsNullOrEmpty((DataNode as IPluginNode).FullClassName))
			{
				plugin = FeatureManager.Features.FirstOrDefault(p => p.PluginType.ToString().Contains((DataNode as IPluginNode).FullClassName));
				
			}

			if (plugin != null)
			{
				plugin.MasterNode = DataNode;
				Infrastructure.Events.DesignCommand.Edit(plugin, this);
			}
		}
		#endregion //EditPluginCommand
		#endregion



	}
}
