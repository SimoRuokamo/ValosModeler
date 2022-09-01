using Enterprixe.WPF.Tools.Localization;
using Epx.BIM;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ValosModeler.Infrastructure;

namespace ValosModeler.Views
{
	/// <summary>
	/// 
	/// </summary>
	/// <seealso cref="ViewModelBase" />
	public class ModelExplorerViewModelBase : ViewModelBase
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ProjectExplorerViewModelBase"/> class.
		/// </summary>
		/// <param name="dataModel">The data model.</param>
		public ModelExplorerViewModelBase(DataModel dataModel) : base(dataModel)
		{

			//Register all decorated methods to the Mediator
			//RegisterToMediator(); // leave this to deriving class
			dataModel.IsShownIn3D = true;
			dataModel.IsExpanded = true;
			_rootItem = new ModelExplorerItemViewModel(dataModel, this);

			//Register all decorated methods to the Mediator
			RegisterToMediator();
			IsCloseable = false;
		}

		/// <summary>
		/// Projects the tree item view model clicked.
		/// </summary>
		/// <param name="newItem">The new item.</param>
		public virtual void ProjectTreeItemViewModelClicked(ModelExplorerItemViewModel newItem)
		{
			if (newItem is ModelExplorerItemViewModel)
			{
				ModelExplorerItemViewModel newItemVM = newItem as ModelExplorerItemViewModel;
				bool wasCtrlDown = false;
				//Unless the ctrl button is pressed, clear any existing selections.
				if (!(Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)))
				{
					SelectedItems.Clear();
					(_rootItem as ModelExplorerItemViewModel).SetSelectedItem(newItemVM.DataNode, false);
					SelectedItems.Add(newItemVM);
				}
				else
				{
					wasCtrlDown = true;
					newItemVM.IsSelected = !newItemVM.IsSelected;
					if (newItemVM.IsSelected)
					{
						SelectedItems.Add(newItemVM);
					}
					else
					{
						SelectedItems.Remove(newItemVM);
					}
				}

				if (newItemVM.IsSelected && !wasCtrlDown)
				{
					//Mediator.NotifyColleagues<List<BaseDataNode>>(MediatorMessages.SetContextualElement, SelectedItems.Select(i => i.DataNode).ToList());
					//Mediator.NotifyColleagues<BaseDataNode>(MediatorMessages.SetContextualTab, newItem.DataNode);
				}
				else if (wasCtrlDown)
				{
					//Mediator.NotifyColleagues<List<BaseDataNode>>(MediatorMessages.SetContextualElement, SelectedItems.Select(i => i.DataNode).ToList());
					//Mediator.NotifyColleagues<BaseDataNode>(MediatorMessages.SetContextualTab, null);
				}

				OnPropertyChanged("SelectedItem");
				OnPropertyChanged("SelectedNode");
			}
		}

		/// <summary>
		/// Key down event for a tree item.
		/// </summary>
		/// <param name="key">The key.</param>
		/// <param name="item">The item.</param>
		public virtual void ProjectTreeItemViewModelKeyDown(Key key, ModelExplorerItemViewModel item)
		{
			if (key == Key.Delete)
			{
				if (ModelInstanceManager.Instance.RemoveNodeCommand.CanExecute(new List<BaseDataNode>() { item.DataNode }))
				{
					ModelInstanceManager.Instance.RemoveNodeCommand.Execute(new List<BaseDataNode>() { item.DataNode });
				}
			}
#if DEBUG
			if(key == Key.G || key == Key.R)
				item.RegenerateGeometry();
#endif
		}

		public virtual void UpdateTree(Infrastructure.Events.GeneralNodePayload param)
		{
		}

		#region Commands
		// Use naming convention: [CommandName]Command, Execute[CommandName], CanExecute[CommandName]

		#region RefreshTreeCommand
		/// <summary>
		/// The _refresh tree command
		/// </summary>
		RelayCommand _refreshTreeCommand;
		/// <summary>
		/// Gets the refresh tree command.
		/// </summary>
		/// <value>The refresh tree command.</value>
		public ICommand RefreshTreeCommand // for debug
		{
			get
			{
				if (_refreshTreeCommand == null)
					_refreshTreeCommand = new RelayCommand(execute => this.ExecuteRefreshTree(execute), canexecute => this.CanExecuteRefreshTree(canexecute));
				return _refreshTreeCommand;
			}
		}

		/// <summary>
		/// Determines whether this instance [can execute refresh tree] the specified parameter.
		/// </summary>
		/// <param name="parameter">The parameter.</param>
		/// <returns><c>true</c> if this instance [can execute refresh tree] the specified parameter; otherwise, <c>false</c>.</returns>
		private bool CanExecuteRefreshTree(object parameter)
		{
			return true;
		}

		/// <summary>
		/// Executes the refresh tree.
		/// </summary>
		/// <param name="parameter">The parameter.</param>
		private void ExecuteRefreshTree(object parameter)
		{
			UpdateTree(null);
		}
		#endregion //RefreshTreeCommand

		#region GarbageCollectCommand
		/// <summary>
		/// The _garbage collect command
		/// </summary>
		RelayCommand _garbageCollectCommand;
		/// <summary>
		/// Gets the garbage collect command.
		/// </summary>
		/// <value>The garbage collect command.</value>
		public ICommand GarbageCollectCommand
		{
			get
			{
				if (_garbageCollectCommand == null)
					_garbageCollectCommand = new RelayCommand(execute => this.ExecuteGarbageCollect(execute), canexecute => this.CanExecuteGarbageCollect(canexecute));
				return _garbageCollectCommand;
			}
		}

		/// <summary>
		/// Determines whether this instance [can execute garbage collect] the specified parameter.
		/// </summary>
		/// <param name="parameter">The parameter.</param>
		/// <returns><c>true</c> if this instance [can execute garbage collect] the specified parameter; otherwise, <c>false</c>.</returns>
		private bool CanExecuteGarbageCollect(object parameter)
		{
			return true;
		}

		/// <summary>
		/// Executes the garbage collect.
		/// </summary>
		/// <param name="parameter">The parameter.</param>
		private void ExecuteGarbageCollect(object parameter)
		{
			System.Runtime.GCSettings.LargeObjectHeapCompactionMode = System.Runtime.GCLargeObjectHeapCompactionMode.CompactOnce;
			GC.Collect();
		}
		#endregion //GarbageCollectCommand

		#endregion

		#region Helpers



		#endregion

		#region ContextMenu

		private ContextMenu _contextMenu;
		/// <summary>
		/// Gets the tree context menu.
		/// </summary>
		/// <value>The tree context menu.</value>
		public ContextMenu TreeContextMenu
		{
			get
			{

				if (_contextMenu == null)
				{
					_contextMenu = new ContextMenu();

					// customizations here

					foreach (var item in BaseContextMenuItems)
					{
						_contextMenu.Items.Add(item);
					}
				}
				return _contextMenu.Items.Count > 0 ? _contextMenu : null;
			}
		}

		/// <summary>
		/// Gets the base context menu items.
		/// </summary>
		/// <value>The base context menu items.</value>
		private List<Control> BaseContextMenuItems
		{
			get
			{
				List<Control> items = new List<Control>();

				return items;
			}
		}

		#endregion //ContextMenu

		/// <summary>
		/// Gets the current data model from the <see cref="ModelInstanceManager"/> if it is in use or the <see cref="ViewModelBase.DataNode"/>.
		/// </summary>
		/// <value>The current model.</value>
		protected DataModel CurrentModel
		{
			get
			{
				return DataNode as DataModel;
			}
		}

		/// <summary>
		/// The _root item
		/// </summary>
		protected ModelExplorerItemViewModel _rootItem;
		/// <summary>
		/// Gets the root item.
		/// </summary>
		/// <value>The root item.</value>
		public ModelExplorerItemViewModel RootItem
		{
			get { return _rootItem; }
		}

		/// <summary>
		/// Gets the root.
		/// </summary>
		/// <value>The root.</value>
		public ReadOnlyCollection<ModelExplorerItemViewModel> Root
		{
			get
			{
				return new ReadOnlyCollection<ModelExplorerItemViewModel>(new List<ModelExplorerItemViewModel>() { _rootItem });
			}
		}

		/// <summary>
		/// The _selected items
		/// </summary>
		private List<ModelExplorerItemViewModel> _selectedItems = new List<ModelExplorerItemViewModel>();
		/// <summary>
		/// Gets the selected items.
		/// </summary>
		/// <value>The selected items.</value>
		public List<ModelExplorerItemViewModel> SelectedItems
		{
			get { return _selectedItems; }
		}

		/// <summary>
		/// Gets the selected item.
		/// </summary>
		/// <value>The selected item.</value>
		public ModelExplorerItemViewModel SelectedItem
		{
			get { return _selectedItems.Count > 0 ? _selectedItems[0] : null; }
		}

		/// <summary>
		/// Gets the selected nodes.
		/// </summary>
		/// <value>The selected nodes.</value>
		public List<Epx.BIM.BaseDataNode> SelectedNodes
		{
			get { return _selectedItems.Select(i => i.DataNode).ToList(); }
		}

		/// <summary>
		/// Gets the selected node.
		/// </summary>
		/// <value>The selected node.</value>
		public Epx.BIM.BaseDataNode SelectedNode
		{
			get { return _selectedItems.Select(i => i.DataNode).ToList().FirstOrDefault(); }
		}

		/// <summary>
		/// Gets the selected nodes filtered.
		/// </summary>
		/// <value>The selected nodes filtered.</value>
		public List<Epx.BIM.BaseDataNode> SelectedNodesFiltered
		{
			get { return FilterSelectedNodes(_selectedItems.Select(i => i.DataNode).ToList()); }
		}

		/// <summary>
		/// Gets a value indicating whether [support node visibility option].
		/// </summary>
		/// <value><c>true</c> if [support node visibility option]; otherwise, <c>false</c>.</value>
		public virtual bool SupportNodeVisibilityOption
		{
			get { return true; }
		}

		/// <summary>
		/// Gets or sets a value indicating whether this instance is root node visible.
		/// </summary>
		/// <value><c>true</c> if this instance is root node visible; otherwise, <c>false</c>.</value>
		public bool IsRootNodeVisible { get; set; }

		/// <summary>
		/// Removes child nodes from list if parent is also selected.
		/// </summary>
		/// <param name="selectedNodes">The selected nodes.</param>
		/// <returns>List&lt;BaseDataNode&gt;.</returns>
		protected List<BaseDataNode> FilterSelectedNodes(List<BaseDataNode> selectedNodes)
		{
			List<BaseDataNode> filteredNodes = new List<BaseDataNode>(selectedNodes);
			List<BaseDataNode> removedNodes = new List<BaseDataNode>();

			foreach (BaseDataNode node in selectedNodes)
			{
				if (removedNodes.Contains(node)) continue;

				foreach (var n in selectedNodes)
				{
					// if 'node' is a descendant of another node 'n' then remove it
					if (n.IsDescendant(node))
					{
						filteredNodes.Remove(node);
						removedNodes.Add(node);
					}
				}
			}

			return filteredNodes;
		}

		/// <summary>
		/// Gets the is server connected.
		/// </summary>
		/// <value>The is server connected.</value>
		public string IsServerConnected
		{
			get
			{
				string returnString = CultureManager.GetLocalizedString("Disconnected");
				if ((CurrentModel as DataModel).ConnectedToServer)
				{
					if ((CurrentModel as DataModel).DataBaseMode == DataModel.DataMode.ConnectedToServerOffLine)
					{
						returnString = CultureManager.GetLocalizedString("Connected. Not logged in");
					}
					else
					{
						returnString = CultureManager.GetLocalizedString("Connected");
					}
				}
				return returnString;
			}
		}

		/// <summary>
		/// Gets the is server connected visibility.
		/// </summary>
		/// <value>The is server connected visibility.</value>
		public Visibility IsServerConnectedVisibility
		{
			get { return (CurrentModel as DataModel).ConnectedToServer ? Visibility.Visible : Visibility.Collapsed; }
		}
	}
}
