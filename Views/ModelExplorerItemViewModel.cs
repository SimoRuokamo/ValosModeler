using Enterprixe.WPF.Tools.Localization;
using Epx.BIM;
using Epx.BIM.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ValosModeler.Infrastructure;
using ValosModeler.Infrastructure.Events;

namespace ValosModeler.Views
{
	/// <summary>
	/// A ViewModel for one tree view item. If not using <see cref="ModelInstanceManager"/> override <see cref="BaseContextMenuItems"/> and <see cref="ItemContextMenu"/>.
	/// </summary>
	/// <seealso cref="ViewModelBase" />
	public class ModelExplorerItemViewModel : ViewModelBase
	{
		/// <summary>
		/// The _parent
		/// </summary>
		private ModelExplorerItemViewModel _parent;
		/// <summary>
		/// The _children
		/// </summary>
		private ReadOnlyCollection<ModelExplorerItemViewModel> _children;
		/// <summary>
		/// The _is selected
		/// </summary>
		private bool _isSelected;
		/// <summary>
		/// The _is expanded
		/// </summary>
		private bool _isExpanded;

		/// <summary>
		/// Initializes a new instance of the <see cref="ModelExplorerItemViewModel"/> class.
		/// </summary>
		/// <param name="node">The node.</param>
		/// <param name="rootViewModel">The root view model.</param>
		public ModelExplorerItemViewModel(BaseDataNode node, ModelExplorerViewModelBase rootViewModel)
			: this(node, rootViewModel, null)
		{
			// Avoid registering with mediator. UpdateProjectTree always creates new items.
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ModelExplorerItemViewModel"/> class.
		/// </summary>
		/// <param name="node">The node.</param>
		/// <param name="rootViewModel">The root view model.</param>
		/// <param name="parent">The parent.</param>
		private ModelExplorerItemViewModel(BaseDataNode node, ModelExplorerViewModelBase rootViewModel, ModelExplorerItemViewModel parent)
			: base(node)
		{
			RootViewModel = rootViewModel;
			_parent = parent;
			if (node != null)
			{
				_isExpanded = DataNode.IsExpanded;
				_IsShownIn3D = DataNode.IsShownIn3D || (_parent != null ? _parent.IsShownIn3D : false);

				if (_IsShownIn3D && !DataNode.IsShownIn3D)
					_IsShownIn3D = false;

				List<ModelExplorerItemViewModel> list = new List<ModelExplorerItemViewModel>();
				foreach (var child in DataNode.SortedChildren)
				{
					if (child.IsShownInTreeView)
					{
						ModelExplorerItemViewModel item = new ModelExplorerItemViewModel(child, RootViewModel, this);
						list.Add(item);
					}
				}
				_children = new ReadOnlyCollection<ModelExplorerItemViewModel>(list);
			}

		}

		/// <summary>
		/// The ViewModel of the tree view. Null for all items except for the root item.
		/// </summary>
		/// <value>The root view model.</value>
		public ModelExplorerViewModelBase RootViewModel { get; set; }

		/// <summary>
		/// Gets the parent.
		/// </summary>
		/// <value>The parent.</value>
		public ModelExplorerItemViewModel Parent
		{
			get { return _parent; }
		}

		/// <summary>
		/// Child ModelExplorerItemViewModel items.
		/// </summary>
		/// <value>The children.</value>
		public ReadOnlyCollection<ModelExplorerItemViewModel> Children
		{
			get { return _children != null ? _children : new ReadOnlyCollection<ModelExplorerItemViewModel>(new List<ModelExplorerItemViewModel>()); }
		}

		/// <summary>
		/// Gets/sets whether the TreeViewItem
		/// associated with this object is selected.
		/// </summary>
		/// <value><c>true</c> if this instance is selected; otherwise, <c>false</c>.</value>
		public bool IsSelected
		{
			get { return _isSelected; }
			set
			{
				if (value != _isSelected)
				{
					_isSelected = value;
					this.OnPropertyChanged("IsSelected");
				}
			}
		}
		/// <summary>
		/// Silent setter.
		/// </summary>
		/// <param name="value">if set to <c>true</c> [value].</param>
		private void SetIsSelected(bool value)
		{
			_isSelected = value;
			this.OnPropertyChanged("IsSelected");
			if(value)
			{

			}
		}
		/// <summary>
		/// Gets/sets whether the TreeViewItem
		/// associated with this object is expanded.
		/// </summary>
		/// <value><c>true</c> if this instance is expanded; otherwise, <c>false</c>.</value>
		public bool IsExpanded
		{
			get { return _isExpanded; }
			set
			{
				if (value != _isExpanded)
				{
					_isExpanded = value;
					if (DataNode != null)
						DataNode.IsExpanded = _isExpanded;
					this.OnPropertyChanged("IsExpanded");
				}

				// Expand all the way up to the root.
				System.Windows.Input.Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;
				if (_isExpanded && _parent != null && !_parent.IsExpanded)
				    _parent.IsExpanded = true;
				System.Windows.Threading.Dispatcher.CurrentDispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background,
				(Action)delegate
				{
					System.Windows.Input.Mouse.OverrideCursor = null;
				});
			}
		}

		/// <summary>
		/// Get/set whether this FolderNode is the Target Folder.
		/// </summary>
		/// <value><c>true</c> if this instance is target; otherwise, <c>false</c>.</value>
		public bool IsTarget
		{
			get
			{
				if (DataNode is Epx.BIM.Models.ModelBaseNode)
					return (DataNode as Epx.BIM.Models.ModelBaseNode).IsTarget;
				else
					return false;
			}
			set
			{
				if (DataNode is Epx.BIM.Models.ModelBaseNode && (DataNode as Epx.BIM.Models.ModelBaseNode).IsTarget != value)
				{
					Epx.BIM.Models.ModelBaseNode folder = DataNode as Epx.BIM.Models.ModelBaseNode;
					if (value)
					{
						folder.IsTarget = true;
						foreach (var item in GetDescendantTreeViewItemsWithData<Epx.BIM.Models.ModelBaseNode>(GetRootItem()))
						{
							if (item != this)
								item.IsTarget = false;
						}
					}
					else
					{
						folder.IsTarget = false;
					}
					this.OnPropertyChanged("IsTarget");
				}
				else if (DataNode is Epx.BIM.Models.ModelBaseNode && (DataNode as Epx.BIM.Models.ModelBaseNode).IsTarget == true)
				{
					(DataNode as Epx.BIM.Models.ModelBaseNode).IsTarget = false;
					this.OnPropertyChanged("IsTarget");
				}
			}
		}

		/// <summary>
		/// Gets a value indicating whether this instance is or includes visible object in3 d.
		/// </summary>
		/// <value><c>true</c> if this instance is or includes visible object in3 d; otherwise, <c>false</c>.</value>
		public bool IsOrIncludesVisibleObjectIn3D
		{
			get { return DataNode.IsOrIncludesVisibleObjectIn3D; }
		}

		/// <summary>
		/// Gets a value indicating whether this instance is model node.
		/// </summary>
		/// <value><c>true</c> if this instance is model node; otherwise, <c>false</c>.</value>
		public bool IsModelNode
		{
			get { return DataNode is Epx.BIM.Models.ModelBaseNode; }
		}

		/// <summary>
		/// Gets a value indicating whether this instance is reservation status visible.
		/// </summary>
		/// <value><c>true</c> if this instance is reservation status visible; otherwise, <c>false</c>.</value>
		public bool IsReservationStatusVisible
		{
			get
			{
				bool retVal = false;
				retVal = DataNode is DataModel ? (DataNode as DataModel).ConnectedToServer : DataNode.GetParent<DataModel>().ConnectedToServer;
				if (retVal)
				{
					retVal = !DataNode.IsUnderSmallestEditableShare();
				}
				return retVal;
			}
		}

		/// <summary>
		/// Gets the reservation status.
		/// </summary>
		/// <value>The reservation status.</value>
		public DataModel.ReservationStatus ReservationStatus
		{
			get { return DataNode.ReservationState; }
		}
		/// <summary>
		/// Flag for single node load state, subtree state ignored
		/// </summary>
		/// <value><c>true</c> if this instance is loaded; otherwise, <c>false</c>.</value>
		public override bool IsDataLoaded
		{
			get { return DataNode.IsDataLoaded; }
		}

		/// <summary>
		/// Gets a value indicating whether this instance is editable.
		/// </summary>
		/// <value><c>true</c> if this instance is editable; otherwise, <c>false</c>.</value>
		public bool IsEditable
		{
			get { return DataNode.IsEditable; }
		}

		/// <summary>
		/// UI name.
		/// </summary>
		/// <value>The name.</value>
		public string Name
		{
			get
			{
				string retVal;
				if (DataNode.IsGroupNode)
				{
					retVal = CultureManager.GetLocalizedString(DataNode.Name);
				}
				else
				{
					retVal = DataNode.Name;
				}
#if DEBUG
				retVal += (" " + DataNode.GetDescendantNodes<BaseDataNode>().Count.ToString() + "");
#endif
				return retVal;
			}
		}

		/// <summary>
		/// The _ is shown in3 d
		/// </summary>
		protected bool _IsShownIn3D = false;
		/// <summary>
		/// Gets or sets a value indicating whether this instance is shown in3 d.
		/// </summary>
		/// <value><c>true</c> if this instance is shown in3 d; otherwise, <c>false</c>.</value>
		public bool IsShownIn3D
		{
			get
			{
				return _IsShownIn3D;
			}
			set
			{
				if (_IsShownIn3D != value)
				{
					_IsShownIn3D = value;
					if (DataNode != null)
					{
						DataNode.IsShownIn3D = _IsShownIn3D;
					}
					foreach (ModelExplorerItemViewModel item in GetDescendantTreeViewItems())
					{
						item.SetShownIn3D(_IsShownIn3D);
					}
					if (!_IsShownIn3D && _parent != null && _parent.IsShownIn3D)
					{
						_parent.SetParentShownIn3D(_IsShownIn3D);
					}
					if (_IsShownIn3D && _parent != null)
					{
						_parent.SetParentShownIn3D(_IsShownIn3D);
					}
					OnIsShownIn3DChanged();
					OnPropertyChanged("IsShownIn3D");
				}
			}
		}

		/// <summary>
		/// Called when [is shown in3 d changed].
		/// </summary>
		protected virtual void OnIsShownIn3DChanged()
		{
			List<BaseDataNode> descendants = new List<BaseDataNode>();
			descendants.Add(DataNode);
			if (DataNode.IsOrIncludesVisibleObjectIn3D)
			{
				descendants.AddRange(DataNode.GetDescendantNodes<ModelBaseNode>());
			}
			Infrastructure.Events.Update3D.PublishAsync(descendants);
		}

		/// <summary>
		/// Handles parent ShownIn3D IsChecked state.
		/// NOTE! This is used instead of the IsShownIn3D property to avoid unnecessary redrawing of 3D.
		/// </summary>
		/// <param name="value">if set to <c>true</c> [value].</param>
		private void SetParentShownIn3D(bool value)
		{
			if (!value)
			{
				if (DataNode != null)
				{
					_IsShownIn3D = value;
					DataNode.IsShownIn3D = _IsShownIn3D;
					OnPropertyChanged("IsShownIn3D");
				}
				if (!_IsShownIn3D && _parent != null && _parent.IsShownIn3D)
				{
					_parent.SetParentShownIn3D(false);
				}
			}
			else
			{
				bool retval = true;
				foreach (var item in GetDescendantTreeViewItems())
				{
					if (!item.IsShownIn3D)
					{
						retval = false;
						break;
					}
				}
				_IsShownIn3D = retval;
				if (DataNode != null)
					DataNode.IsShownIn3D = _IsShownIn3D;
				OnPropertyChanged("IsShownIn3D");
				if (_IsShownIn3D && _parent != null)
				{
					_parent.SetParentShownIn3D(true);
				}
			}
		}

		/// <summary>
		/// "Silent" setter for IsShownIn3D. To avoid unnecessary redraw of 3D.
		/// </summary>
		/// <param name="value">if set to <c>true</c> [value].</param>
		private void SetShownIn3D(bool value)
		{
			_IsShownIn3D = value;
			if (DataNode != null)
				DataNode.IsShownIn3D = _IsShownIn3D;
			OnPropertyChanged("IsShownIn3D");
		}

		/// <summary>
		/// Gets a value indicating whether this instance is current grid.
		/// </summary>
		/// <value><c>true</c> if this instance is current grid; otherwise, <c>false</c>.</value>
		public bool IsCurrentGrid
		{
			get
			{
				if (DataNode is Epx.BIM.GridMesh.GridMesh)
				{
					return (DataNode as Epx.BIM.GridMesh.GridMesh).IsCurrentGrid;
				}
				else
				{
					return false;
				}
			}
		}

		/// <summary>
		/// The _item context menu
		/// </summary>
		protected ContextMenu _itemContextMenu;
		/// <summary>
		/// Gets the item context menu.
		/// </summary>
		/// <value>The item context menu.</value>
		public virtual ContextMenu ItemContextMenu
		{
			get
			{
				if (_itemContextMenu == null)
				{
					_itemContextMenu = new ContextMenu();
					_itemContextMenu.Opened += _itemContextMenu_Opened;
				}
				return _itemContextMenu;//.Items.Count > 0 ? _itemContextMenu : null;
			}
		}

		private void _itemContextMenu_Opened(object sender, RoutedEventArgs e)
		{
			_itemContextMenu.Items.Clear();
			ModelExplorerViewModel rootVM = RootViewModel as ModelExplorerViewModel;
			if (rootVM != null)
			{
				if (DataNode is DataModel || DataNode is FolderNode)
				{
					MenuItem item;
					item = new MenuItem();
					item.Header = CultureManager.GetLocalizedString("Add");
					item.Icon = new System.Windows.Controls.Image { Source = Application.Current.Resources["Icon_Add"] as System.Windows.Media.DrawingImage, Width = 16, Height = 16 };
					MenuItem subitem = new MenuItem();
					subitem.Header = CultureManager.GetLocalizedString("Project");
					subitem.Icon = new System.Windows.Controls.Image { Source = Application.Current.Resources["Icon_Add_Project"] as System.Windows.Media.DrawingImage, Width = 16, Height = 16 };
					subitem.Command = AppModelInstanceManager.Instance.AddProjectCommand;
					subitem.CommandParameter = this.DataNode;
					item.Items.Add(subitem);
					_itemContextMenu.Items.Add(item);
					_itemContextMenu.Items.Add(new Separator());
				}
				else if (DataNode is Project ||
					(DataNode is ModelBaseNode && (DataNode as ModelBaseNode).IsOriginalType && (DataNode as ModelBaseNode).CanBeTarget) ||
					(DataNode is ModelBaseNode && !(DataNode as ModelBaseNode).IsOriginalType) ||
					DataNode is FolderNode)
				{
					MenuItem item;

					if ((DataNode is ModelBaseNode && (DataNode as ModelBaseNode).IsOriginalType && (DataNode as ModelBaseNode).CanBeTarget) || (DataNode is ModelBaseNode && !(DataNode as ModelBaseNode).IsOriginalType))
					{
						item = new MenuItem();
						item.Header = CultureManager.GetLocalizedString("Set as Target Folder");
						item.Icon = new System.Windows.Controls.Image { Source = Application.Current.Resources["Icon_SetAsTargetFolder"] as System.Windows.Media.DrawingImage, Width = 16, Height = 16 };
						item.Command = AppModelInstanceManager.Instance.SetTargetFolderCommand;
						item.CommandParameter = this.DataNode;
						item.DataContext = this;
						_itemContextMenu.Items.Add(item);
					}
					item = new MenuItem();
					item.Header = CultureManager.GetLocalizedString("Add");
					item.Icon = new System.Windows.Controls.Image { Source = Application.Current.Resources["Icon_Add"] as System.Windows.Media.DrawingImage, Width = 16, Height = 16 };
					MenuItem subitem = new MenuItem();
					subitem.Header = CultureManager.GetLocalizedString("Folder");
					subitem.Icon = new System.Windows.Controls.Image { Source = Application.Current.Resources["Icon_Add_Folder"] as System.Windows.Media.DrawingImage, Width = 16, Height = 16 };
					subitem.Command = AppModelInstanceManager.Instance.AddFolderCommand;
					subitem.CommandParameter = this.DataNode;
					item.Items.Add(subitem);
					_itemContextMenu.Items.Add(item);
					_itemContextMenu.Items.Add(new Separator());
				}
				else if (DataNode is Epx.BIM.GridMesh.GridUCS)// && !(DataNode as BIM.ProjectData.GridMesh.GridUCS).IsCurrentGrid)
				{
					MenuItem item = new MenuItem();
					item.Header = CultureManager.GetLocalizedString("Set as Current Grid");
					item.Icon = new System.Windows.Controls.Image { Source = Application.Current.Resources["Icon_SetCurrentGrid"] as System.Windows.Media.DrawingImage, Width = 16, Height = 16 };
					item.CommandParameter = DataNode;
					item.Command = AppModelInstanceManager.Instance.SetCurrentGridCommand;
					item.DataContext = this.DataNode;
					_itemContextMenu.Items.Add(item);
					_itemContextMenu.Items.Add(new Separator());
				}

				if (!(DataNode is Epx.BIM.Settings.SettingsBaseNode))
				{
					foreach (var item in BaseContextMenuItems)
					{
						_itemContextMenu.Items.Add(item);
					}
				}
			}
		}

		/// <summary>
		/// Gets the base context menu items.
		/// </summary>
		/// <value>The base context menu items.</value>
		protected virtual List<Control> BaseContextMenuItems
		{
			get
			{
				List<Control> items = new List<Control>();
				ModelExplorerViewModel rootVM = RootViewModel as ModelExplorerViewModel;
				if (rootVM != null)
				{
					MenuItem item = new MenuItem();
					item.Header = CultureManager.GetLocalizedString("Copy");
					item.Icon = new System.Windows.Controls.Image { Source = Application.Current.Resources["Icon_Ribbon_COPY"] as System.Windows.Media.DrawingImage, Width = 16, Height = 16 };
					item.Command = AppModelInstanceManager.Instance.CopyNodeCommand;
					item.CommandParameter = rootVM.SelectedNodes;
					items.Add(item);
					item = new MenuItem();
					item.Header = CultureManager.GetLocalizedString("Cut");
					item.Icon = new System.Windows.Controls.Image { Source = Application.Current.Resources["Icon_Ribbon_CUT"] as System.Windows.Media.DrawingImage, Width = 16, Height = 16 };
					item.Command = AppModelInstanceManager.Instance.CutNodeCommand;
					item.CommandParameter = rootVM.SelectedNodes;
					item.Visibility = DataNode.IsGroupNode ? Visibility.Collapsed : Visibility.Visible;
					items.Add(item);
					item = new MenuItem();
					item.Header = CultureManager.GetLocalizedString("Paste");
					item.Icon = new System.Windows.Controls.Image { Source = Application.Current.Resources["Icon_Ribbon_PASTE"] as System.Windows.Media.DrawingImage, Width = 16, Height = 16 };
					item.Command = AppModelInstanceManager.Instance.PasteNodeCommand;
					item.CommandParameter = this.DataNode;
					items.Add(item);
					//item.Visibility = DataNode is Member || DataNode is NailPlate || DataNode is Support ? Visibility.Collapsed : Visibility.Visible;
					if (AppModelInstanceManager.Instance.RemoveNodeCommand.CanExecute(DataNode))
					{
						item = new MenuItem();
						item.Header = CultureManager.GetLocalizedString("Remove");
						item.Icon = new System.Windows.Controls.Image { Source = Application.Current.Resources["Right_Click_Remove"] as System.Windows.Media.DrawingImage, Width = 16, Height = 16 };
						item.CommandParameter = rootVM.SelectedNodes;
						item.Command = AppModelInstanceManager.Instance.RemoveNodeCommand;
						item.Visibility = DataNode.IsGroupNode ? Visibility.Collapsed : Visibility.Visible;
						items.Add(item);
						item.Click += Remove_Item_Click;//SLe - need to assign selected node here
					}
#if OPENGL && DEBUG
					if(DataNode is Valos.Ifc.ValosIfcSpatialNode)
					{
						items.Add(new Separator());
						item = new MenuItem();
						item.Header = CultureManager.GetLocalizedString("Regenerate Node Geometry");
						item.Visibility = DataNode.IsGroupNode ? Visibility.Collapsed : Visibility.Visible;
						items.Add(item);
						item.Click += Regenerate_Item_Click;
					}
#endif
					if(AppModelInstanceManager.Instance.LoadTreeCommand.CanExecute(DataNode))
					{
						items.Add(new Separator());

						item = new MenuItem();
						item.Header = CultureManager.GetLocalizedString("Load");
						item.Command = AppModelInstanceManager.Instance.LoadTreeCommand;
						item.CommandParameter = this.DataNode;
						items.Add(item);
						item = new MenuItem();
						item.Header = CultureManager.GetLocalizedString("Unload");
						item.CommandParameter = this.DataNode;
						item.Command = AppModelInstanceManager.Instance.UnloadTreeCommand;
						items.Add(item);
					}
					if(AppModelInstanceManager.Instance.CheckOutCommand.CanExecute(DataNode) || AppModelInstanceManager.Instance.CheckInCommand.CanExecute(DataNode))
					{
						items.Add(new Separator());
						item = new MenuItem();
						item.Header = CultureManager.GetLocalizedString("Check out");
						item.Command = AppModelInstanceManager.Instance.CheckOutCommand;
						item.CommandParameter = this.DataNode;
						items.Add(item);
						item = new MenuItem();
						item.Header = CultureManager.GetLocalizedString("Check in");
						item.Command = AppModelInstanceManager.Instance.CheckInCommand;
						item.CommandParameter = this.DataNode;
						items.Add(item);
					}
				}
				return items;
			}
		}

		private void Remove_Item_Click(object sender, RoutedEventArgs e)
		{
			var rootVM = RootViewModel as ModelExplorerViewModel;
			var removeItem = sender as MenuItem;
			removeItem.CommandParameter = rootVM.SelectedNodes;
		}

		private void Regenerate_Item_Click(object sender, RoutedEventArgs e)
		{
			RegenerateGeometry();
		
		}
		internal void RegenerateGeometry()
		{
			if(DataNode is Valos.Ifc.ValosIfcSpatialNode ifcNode)
			{
				ifcNode.RegenerateGeometry();
				Update3D.PublishGeometryOnly(ifcNode);
			}
		}

		/// <summary>
		/// Gets the item tool tip.
		/// </summary>
		/// <value>The item tool tip.</value>
		public virtual ToolTip ItemToolTip
		{
			get
			{
#if DEBUG
				ToolTip tt = new ToolTip();
				CultureManager.SetLocalizable(tt, false);
				tt.Content = DataNode.DebugValuesString;
				return tt;
#else
				return null;
#endif
			}
		}

		/// <summary>
		/// Gets the duration of the item tool tip show.
		/// </summary>
		/// <value>The duration of the item tool tip show.</value>
		public double ItemToolTipShowDuration
		{
			get
			{
#if DEBUG
				return 60000;
#else
				return 5000; // default value 5000
#endif
			}
		}

		/// <summary>
		/// Gets the icon image source.
		/// </summary>
		/// <value>The icon image source.</value>
		public ImageSource IconImageSource
		{
			get
			{
				string reskey = "Icon_Tree_" + DataNode.GetType().Name;
				DrawingImage iconSource = Application.Current.TryFindResource(reskey) as DrawingImage;
				if (iconSource != null)
				{
					return iconSource;
				}
				else // special cases or base class handling
				{
					if (DataNode is Epx.BIM.Plugins.PluginNode)
					{
						iconSource = Application.Current.TryFindResource("Icon_Tree_Parametric3DPluginNode") as DrawingImage;
					}
					else if (DataNode is Epx.BIM.Drawings.DrawingBaseBlock)
					{
						iconSource = Application.Current.TryFindResource("Icon_Tree_DrawingBaseBlock") as DrawingImage;
					}

					if (iconSource != null)
					{
						return iconSource;
					}
					else
						return null;
				}
			}
		}

		#region Helpers

		/// <summary>
		/// Get the root item of the tree view.
		/// </summary>
		/// <returns>ModelExplorerItemViewModel.</returns>
		public ModelExplorerItemViewModel GetRootItem()
		{
			ModelExplorerItemViewModel item = null;
			if (_parent != null)
			{
				item = _parent.GetRootItem();
			}
			else
				item = this;

			return item;
		}

		/// <summary>
		/// Get descendant tree view items which contain data of type T from tree. Including self.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="startingItem">Starting item.</param>
		/// <returns>List&lt;ModelExplorerItemViewModel&gt;.</returns>
		public static List<ModelExplorerItemViewModel> GetDescendantTreeViewItemsWithData<T>(ModelExplorerItemViewModel startingItem)
		{
			List<ModelExplorerItemViewModel> items = new List<ModelExplorerItemViewModel>();

			foreach (var child in startingItem._children)
			{
				if (child.DataNode is T)
					items.Add(child);

				if (child.Children.Count > 0)
					items.AddRange(GetDescendantTreeViewItemsWithData<T>(child));
			}
			return items;
		}

		/// <summary>
		/// Gets the descendant TreeView items.
		/// </summary>
		/// <returns>List&lt;ModelExplorerItemViewModel&gt;.</returns>
		public List<ModelExplorerItemViewModel> GetDescendantTreeViewItems()
		{
			List<ModelExplorerItemViewModel> items = new List<ModelExplorerItemViewModel>();

			foreach (ModelExplorerItemViewModel child in _children)
			{
				items.Add(child);

				if (child.Children.Count > 0)
					items.AddRange(child.GetDescendantTreeViewItems());
			}
			return items;
		}

		/// <summary>
		/// Gets the descendant TreeView items.
		/// </summary>
		/// <param name="breakOnFirst">if set to <c>true</c> [break on first].</param>
		/// <param name="matchPredicate">The match predicate.</param>
		/// <returns>List&lt;ModelExplorerItemViewModel&gt;.</returns>
		public List<ModelExplorerItemViewModel> GetDescendantTreeViewItems(bool breakOnFirst, Predicate<ModelExplorerItemViewModel> matchPredicate)
		{
			List<ModelExplorerItemViewModel> items = new List<ModelExplorerItemViewModel>();

			foreach (ModelExplorerItemViewModel child in _children)
			{
				if (matchPredicate != null)
				{
					if (matchPredicate(child))
					{
						items.Add(child);
						if (breakOnFirst) break;
					}
				}

				if (child.Children.Count > 0)
				{
					items.AddRange(child.GetDescendantTreeViewItems(breakOnFirst, matchPredicate));
					if (breakOnFirst && items.Count > 0) break;
				}
			}
			return items;
		}

		/// <summary>
		/// Get first parent with DataNode of type T
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns>ModelExplorerItemViewModel.</returns>
		public ModelExplorerItemViewModel GetParent<T>()
		{
			if (this.Parent != null && this.Parent.DataNode is T)
			{
				return this.Parent;
			}
			else if (this.Parent != null)
			{
				return this.Parent.GetParent<T>();
			}
			else
			{
				return null;
			}
		}
		/// <summary>
		/// Calls OnPropertyChanged for the items with the specified nodes.
		/// </summary>
		/// <param name="payload">The payload.</param>
		public void UpdateTreeItemProperty(NodePropertyChangedPayload payload)
		{
			if (payload.DataNodes == null || payload.DataNodes.Contains(this.DataNode))
			{
				foreach (string str in payload.ChangedPropertyNames)
				{
					OnPropertyChanged(str);
				}
			}

			foreach (ModelExplorerItemViewModel item in Children)
			{
				item.UpdateTreeItemProperty(payload);
			}
		}
		/// <summary>
		/// Set the selected item starting from this item to all descendant items.
		/// </summary>
		/// <param name="selectedNode">The selected node.</param>
		/// <param name="silent">if set to <c>true</c> [silent].</param>
		public void SetSelectedItem(BaseDataNode selectedNode, bool silent)
		{
			if (selectedNode.UniqueID == DataNode.UniqueID)
			{
				if (silent)
				{
					SetIsSelected(true); // to avoid message loop
				}
				else
				{
					IsSelected = true;
				}
			}
			else
			{
				SetIsSelected(false);
			}

			foreach (var item in Children)
			{
				item.SetSelectedItem(selectedNode, silent);
			}
		}
		/// <summary>
		/// Sets the selected items.
		/// </summary>
		/// <param name="selectedNodes">The selected nodes.</param>
		/// <param name="silent">if set to <c>true</c> [silent].</param>
		public void SetSelectedItems(List<BaseDataNode> selectedNodes, bool silent)
		{
			if (selectedNodes.Select(n => n != null ? n.UniqueID : System.Guid.Empty).Contains(DataNode.UniqueID))
			{
				if (silent)
				{
					SetIsSelected(true); // to avoid message loop
				}
				else
				{
					IsSelected = true;
					IsExpanded = true;
				}
			}
			else
			{
				SetIsSelected(false);
			}

			foreach (var item in Children)
			{
				item.SetSelectedItems(selectedNodes, silent);
			}
		}
		/// <summary>
		/// Get the selected item from this item to all descendant items.
		/// </summary>
		/// <returns>ModelExplorerItemViewModel.</returns>
		public ModelExplorerItemViewModel GetSelectedItem()
		{
			if (IsSelected)
			{
				return this;
			}
			else
			{
				foreach (var item in Children)
				{
					ModelExplorerItemViewModel selectedItem = item.GetSelectedItem();
					if (selectedItem != null)
						return selectedItem;
				}
			}
			return null;
		}

		/// <summary>
		/// Gets the selected items.
		/// </summary>
		/// <returns>List&lt;ModelExplorerItemViewModel&gt;.</returns>
		public List<ModelExplorerItemViewModel> GetSelectedItems()
		{
			return GetDescendantTreeViewItems(false, i => i.IsSelected);
		}
		#endregion

	}
}
