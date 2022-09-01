using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ValosModeler.Views
{
	/// <summary>
	/// Interaction logic for ModelExplorer.xaml
	/// </summary>
	public partial class ModelExplorer : UserControl
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ProjectTreeControl"/> class.
		/// </summary>
		public ModelExplorer()
		{
			InitializeComponent();
			DataContextChanged += ProjectTreeControl_DataContextChanged;
		}

		/// <summary>
		/// Handles the DataContextChanged event of the ProjectTreeControl control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
		void ProjectTreeControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			if (e.NewValue != null && e.NewValue is ModelExplorerViewModel)
			{
			}
		}

		/// <summary>
		/// Handles the MouseLeftButtonUp event of the TreeViewItem control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="MouseButtonEventArgs"/> instance containing the event data.</param>
		private void TreeViewItem_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			// If code in *.xaml.cs file is a must, use commands to separate view and model. (or call a method on the ViewModel, but no data logic)
			ModelExplorerItemViewModel item = (sender as StackPanel).DataContext as ModelExplorerItemViewModel;
			if (item != null)
				item.RootViewModel.ProjectTreeItemViewModelClicked(item);
		}

		/// <summary>
		/// Handles the MouseRightButtonDown event of the TreeViewItem control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="MouseEventArgs"/> instance containing the event data.</param>
		private void TreeViewItem_MouseRightButtonDown(object sender, MouseEventArgs e)
		{
			TreeViewItem item = sender as TreeViewItem;
			if (item != null && item.DataContext is ModelExplorerItemViewModel && !(item.DataContext as ModelExplorerItemViewModel).IsSelected)
			{
				item.Focus();
				(item.DataContext as ModelExplorerItemViewModel).RootViewModel.ProjectTreeItemViewModelClicked((item.DataContext as ModelExplorerItemViewModel));
			}
			e.Handled = true;
		}

		/// <summary>
		/// Handles the ContextMenuOpening event of the TreeViewItem control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="ContextMenuEventArgs"/> instance containing the event data.</param>
		private void TreeViewItem_ContextMenuOpening(object sender, ContextMenuEventArgs e)
		{
			TreeViewItem senderItem = sender as TreeViewItem;

			// Child items inherit parent's context menu. Disable showing of context menu for child items with no overridden context menu.
			if (senderItem.ContextMenu == null) e.Handled = true;
		}

		/// <summary>
		/// Handles the KeyDown event of the TreeViewItem control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="KeyEventArgs"/> instance containing the event data.</param>
		private void TreeViewItem_KeyDown(object sender, KeyEventArgs e)
		{
			TreeViewItem item = sender as TreeViewItem;
			// IsSelected must be checked! Event happens for item chain.
			if(item.DataContext is ModelExplorerItemViewModel itemViewModel)
			{
				if(item.IsFocused && itemViewModel.IsSelected)
					itemViewModel.RootViewModel.ProjectTreeItemViewModelKeyDown(e.Key, itemViewModel);
			}

			//if (item != null && item.IsFocused && item.DataContext is ModelExplorerItemViewModel && (item.DataContext as ModelExplorerItemViewModel).IsSelected)
			//{
			//	if (this.DataContext is ModelExplorerItemViewModel && item != null && item.DataContext is ModelExplorerItemViewModel)
			//	{
			//		(this.DataContext as ModelExplorerItemViewModel).RootViewModel.ProjectTreeItemViewModelKeyDown(e.Key, item.DataContext as ModelExplorerItemViewModel);
			//	}
			//}
		}

		bool isNavigating = false;
		private void TreeView_SelectedItemChanged(object sender, RoutedEventArgs e)
		{
			TreeViewItem item = sender as TreeViewItem;
			if (item != null && item.IsSelected && !isNavigating)
			{
				isNavigating = true;
				System.Windows.Threading.Dispatcher.CurrentDispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background,
				(Action)delegate
				{
					item.BringIntoView();
					isNavigating = false;
				});
				e.Handled = true;
			}
		}
		private bool suppressRequestBringIntoView;
		private void TreeViewItem_RequestBringIntoView(object sender, RequestBringIntoViewEventArgs e)
		{
			// Ignore re-entrant calls
			if(suppressRequestBringIntoView)
				return;
			// Cancel the current scroll attempt
			e.Handled = true;
			// Call BringIntoView using a rectangle that extends into "negative space" to the left of our
			// actual control. This allows the vertical scrolling behaviour to operate without adversely
			// affecting the current horizontal scroll position.
			suppressRequestBringIntoView = true;
			TreeViewItem tvi = sender as TreeViewItem;
			if(tvi != null)
			{
				Rect newTargetRect = new Rect(-1000, 0, tvi.ActualWidth + 1000, tvi.ActualHeight);
				tvi.BringIntoView(newTargetRect);
			}
			suppressRequestBringIntoView = false;
		}
	}
}
