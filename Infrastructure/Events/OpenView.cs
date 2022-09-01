using Epx.BIM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace ValosModeler.Infrastructure.Events
{
	public class OpenViewPayload
	{
		/// <summary>
		/// The data node. Evaluated in order: ControlView, ViewType+DataNode, Viewtype, DataNode.
		/// </summary>
		public BaseDataNode DataNode { get; private set; }
		/// <summary>
		/// Open this specific view instance. Evaluated in order: ControlView, ViewType+DataNode, Viewtype, DataNode.
		/// </summary>
		public UserControl ControlView { get; private set; }
		/// <summary>
		/// Open a view of this type. Evaluated in order: ControlView, ViewType+DataNode, Viewtype, DataNode.
		/// </summary>
		public Type ViewType { get; private set; }
		/// <summary>
		/// If false, will attempt to find existing view and activate it. If not found, will still create a new view.
		/// </summary>
		public bool CreateNewView { get; private set; }
		public bool OpenAsFloating { get; private set; }

		public OpenViewPayload(BaseDataNode node, bool createNewView, bool openAsFloating)
		{
			DataNode = node;
			CreateNewView = createNewView;
			OpenAsFloating = openAsFloating;
		}
		//public OpenViewPayload(IFormattedTextProvider text, bool createNewView, bool openAsFloating)
		//{
		//	TextProvider = text;
		//	CreateNewView = createNewView;
		//	OpenAsFloating = openAsFloating;
		//}
		public OpenViewPayload(UserControl control, bool createNewView, bool openAsFloating)
		{
			ControlView = control;
			CreateNewView = createNewView;
			OpenAsFloating = openAsFloating;
		}
		public OpenViewPayload(Type viewType, BaseDataNode node, bool createNewView, bool openAsFloating)
		{
			ViewType =  viewType;
			DataNode = node;
			CreateNewView = createNewView;
			OpenAsFloating = openAsFloating;
		}

	}

	/// <summary>
	/// Creates a new view.
	/// </summary>
	public class OpenView
	{
		public const string MessageID = "OpenView";
		/// <summary>
		/// Request to show a view for this node.
		/// </summary>
		/// <param name="node">The data node.</param>
		/// <param name="createNewView">If false, will attempt to find existing view and activate it. If not found, will still create a new view.</param>
		public static void Publish(BaseDataNode node, bool createNewView = true, bool openAsFloating = false)
		{
			ViewModelBase.MediatorStatic.NotifyColleagues<OpenViewPayload>(MessageID, new OpenViewPayload(node, createNewView, openAsFloating));
		}
		/// <summary>
		/// Request to show the control in a view.
		/// </summary>
		/// <param name="control"></param>
		/// <param name="createNewView"></param>
		/// <param name="openAsFloating"></param>
		public static void Publish(UserControl control, bool createNewView = true, bool openAsFloating = false)
		{
			ViewModelBase.MediatorStatic.NotifyColleagues<OpenViewPayload>(MessageID, new OpenViewPayload(control, createNewView, openAsFloating));
		}
		/// <summary>
		/// Request to open a view of this type.
		/// </summary>
		/// <param name="viewType"></param>
		/// <param name="node">The data node.</param>
		/// <param name="createNewView"></param>
		/// <param name="openAsFloating"></param>
		public static void Publish(Type viewType, BaseDataNode node = null, bool createNewView = true, bool openAsFloating = false)
		{
			ViewModelBase.MediatorStatic.NotifyColleagues<OpenViewPayload>(MessageID, new OpenViewPayload(viewType, node, createNewView, openAsFloating));
		}
	}
}
