using Epx.BIM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using ValosService;

namespace ValosModeler.Infrastructure
{
	/// <summary>
	/// Class AppModelInstanceManager.
	/// </summary>
	/// <seealso cref="ModelInstanceManager" />
	public class AppModelInstanceManager : ModelInstanceManager
	{
		/// <summary>
		/// Implement this in the derived class which sets the singleton instance to an instance of the derived type.
		/// </summary>
		public static void Initialize()
		{
			if (_instance == null)
			{
				_instance = new AppModelInstanceManager(); // create instance of the derived class
			}
		}

		/// <summary>
		/// Temporary, will be removed/cleared with major release. Check old projects for compatibility with current data model structure to prevent program crashes
		/// with missing nodes etc..
		/// </summary>
		protected override void CheckBackwardCompatability()
		{
			base.CheckBackwardCompatability();
		}

		public override List<ModelEvent> GetEvents(DataModel dataModel)
		{
			return base.GetEvents(dataModel);
		}

		/// <summary>
		/// Previews the remove node.
		/// </summary>
		/// <param name="nodes">The nodes.</param>
		/// <param name="otherNodesToRemove">The other nodes to remove.</param>
		protected override void PreviewRemoveNode(List<BaseDataNode> nodes, ref List<BaseDataNode> otherNodesToRemove)
		{
			base.PreviewRemoveNode(nodes, ref otherNodesToRemove);
		}

		/// <summary>
		/// Ask if the node needs to be replaced etc.
		/// </summary>
		/// <param name="isCut">else is copy</param>
		/// <param name="node">The node.</param>
		/// <param name="replaceNode">If true the child of the same type will be replaced.</param>
		/// <returns>True if can paste.</returns>
		protected override bool PreviewPasteNode(bool isCut, BaseDataNode node, out bool replaceNode)
		{
			return base.PreviewPasteNode(isCut, node, out replaceNode);
		}

		/// <summary>
		/// Called when [tree loaded].
		/// </summary>
		/// <param name="node">The node.</param>
		/// <param name="error">The error.</param>
		protected override void OnTreeLoaded(BaseDataNode node, ServerServiceError error)
		{
			base.OnTreeLoaded(node, error);
		}

		/// <summary>
		/// Called when [tree un loaded].
		/// </summary>
		/// <param name="node">The node.</param>
		/// <param name="error">The error.</param>
		protected override void OnTreeUnLoaded(BaseDataNode node, ServerServiceError error)
		{
			base.OnTreeUnLoaded(node, error);
		}

		/// <summary>
		/// Called when [check in executed].
		/// </summary>
		/// <param name="node">The node.</param>
		/// <param name="error">The error.</param>
		protected override void OnCheckInExecuted(BaseDataNode node, ServerServiceError error)
		{
			base.OnCheckInExecuted(node, error);
		}

		/// <summary>
		/// Called when [check out executed].
		/// </summary>
		/// <param name="node">The node.</param>
		/// <param name="error">The error.</param>
		protected override void OnCheckOutExecuted(BaseDataNode node, ServerServiceError error)
		{
			base.OnCheckOutExecuted(node, error);
		}

		#region Commands

		//#region AddNodeCommand
		///// <summary>
		///// The _add node command
		///// </summary>
		//RelayCommand _addNodeCommand;
		///// <summary>
		///// Gets the add node command.
		///// </summary>
		///// <value>The add node command.</value>
		//public ICommand AddNodeCommand
		//{
		//	get
		//	{
		//		if (_addNodeCommand == null)
		//			_addNodeCommand = new RelayCommand(execute => this.ExecuteAddNode(execute), canexecute => this.CanExecuteAddNode(canexecute));
		//		return _addNodeCommand;
		//	}
		//}

		///// <summary>
		///// Determines whether this instance [can execute add node] the specified parameter.
		///// </summary>
		///// <param name="parameter">The parameter.</param>
		///// <returns><c>true</c> if this instance [can execute add node] the specified parameter; otherwise, <c>false</c>.</returns>
		//private bool CanExecuteAddNode(object parameter)
		//{
		//	Type type = Type.GetType(parameter as string);
		//	if (type != null)
		//	{
		//		BaseDataNode child = Activator.CreateInstance(type) as BaseDataNode;
		//		return ModelInstanceManager.Instance.CanExecuteAddNode(ViewModelBase.MainViewModel.ViewModelProjectExplorer.SelectedNode, child);
		//	}
		//	else
		//		return false;
		//}

		///// <summary>
		///// Executes the add node.
		///// </summary>
		///// <param name="parameter">The parameter.</param>
		//private void ExecuteAddNode(object parameter)
		//{
		//	Type type = Type.GetType(parameter as string);
		//	if (type != null)
		//	{
		//		BaseDataNode child = Activator.CreateInstance(type) as BaseDataNode;
		//		ModelInstanceManager.Instance.ExecuteAddNode(ViewModelBase.MainViewModel.ViewModelProjectExplorer.SelectedNode, child);
		//	}
		//}
		//#endregion //AddNodeCommand

		#endregion

	}
}
