using Epx.BIM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using ValosModeler.Infrastructure;
using ValosModeler.Infrastructure.Events;

namespace ValosModeler.Views
{
	public class ModelExplorerViewModel : ModelExplorerViewModelBase
	{
		public ModelExplorerViewModel(DataModel node) : base(node)
		{
			WindowTitle = "Model Explorer";
		}

		public override void ProjectTreeItemViewModelClicked(ModelExplorerItemViewModel newItem)
		{
			if (newItem is ModelExplorerItemViewModel newItemVM)
			{
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
					Infrastructure.Events.NodeSelected.Publish(SelectedItems.Select(i => i.DataNode).ToList(), this);
					Infrastructure.Events.ShowRibbonContextualTabs.Publish(newItemVM.DataNode);
				}
				else if (wasCtrlDown)
				{
					Infrastructure.Events.NodeSelected.Publish(SelectedItems.Select(i => i.DataNode).ToList(), this);
					Infrastructure.Events.ShowRibbonContextualTabs.Publish(null);
				}

				OnPropertyChanged("SelectedItem");
				OnPropertyChanged("SelectedNode");
			}
		}

		[MediatorMessageSink(Infrastructure.Events.ModelChanged.MessageID)]
		public override void UpdateTree(Infrastructure.Events.GeneralNodePayload param)
		{
			Infrastructure.Events.GeneralNodePayload payload = param as Infrastructure.Events.GeneralNodePayload;

			ModelExplorerItemViewModel selectedItem = (_rootItem as ModelExplorerItemViewModel).GetSelectedItem();
			BaseDataNode selectedNode = selectedItem != null ? selectedItem.DataNode : null;

			_rootItem = new ModelExplorerItemViewModel(CurrentModel, this);

			if (selectedNode != null) (_rootItem as ModelExplorerItemViewModel).SetSelectedItem(selectedNode, true);

			// The above can be done in sync to avoid thread concurrency issues when looping through child collections.
			// Refresh the UI in background though.
			Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background,
				new Action(() => OnPropertyChanged("Root")));
		}

		[MediatorMessageSink(Infrastructure.Events.ModelChanged.NodeAdded)]
		public void UpdateTreeAdded(Infrastructure.Events.GeneralNodePayload payload)
		{
			UpdateTree(payload);
		}
		[MediatorMessageSink(Infrastructure.Events.ModelChanged.NodeRemoved)]
		public void UpdateTreeRemoved(Infrastructure.Events.GeneralNodePayload payload)
		{
			UpdateTree(payload);
		}

		[MediatorMessageSink(Infrastructure.Events.NodePropertyChanged.MessageID)]
		public void UpdateTreeItemProperty(Infrastructure.Events.NodePropertyChangedPayload payload)
		{
			// TODO per actual changed property, for now refresh whole tree
			UpdateTree(null); 
		}

		[MediatorMessageSink(Infrastructure.Events.NodeSelected.MessageID)]
		public void SetProjectTreeSelectedItem(Infrastructure.Events.GeneralNodePayload payload)
		{
			if (payload.Sender != this)
			{
				SelectedItems.Clear();
				(_rootItem as ModelExplorerItemViewModel).SetSelectedItems(payload.DataNodes.ToList(), false);
				SelectedItems.AddRange((_rootItem as ModelExplorerItemViewModel).GetSelectedItems());
			}
		}
	}
}
