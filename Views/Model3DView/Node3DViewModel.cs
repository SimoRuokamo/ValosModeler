using Epx.BIM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ValosModeler.Infrastructure;
using ValosModeler.Infrastructure.Events;

namespace ValosModeler.Views.Model3DView
{
	/// <summary>
	/// Class Node3DViewModel. A class which receives messages to force a redraw of objects in the View.
	/// </summary>
	/// <seealso cref="ViewModelBase" />
	public class Node3DViewModel : ViewModelBase
	{
		bool _registerUpdateAll;
		bool _registerGeometry;
		bool _registerOther;

		/// <summary>
		/// Initializes a new instance of the <see cref="Node3DViewModel"/> class.
		/// </summary>
		/// <param name="node">The node.</param>
		public Node3DViewModel(BaseDataNode node, bool registerUpdateAll = true, bool registerGeometry = false, bool registerOther = false)
			: base(node)
		{
			_registerUpdateAll = registerUpdateAll;
			_registerGeometry = registerGeometry;
			_registerOther = registerOther;
			//RegisterToMediator(false); // must unregister manually if not called in Dispose()
			if (registerUpdateAll) Mediator.RegisterHandler<GeneralNodePayload>(Update3D.MessageID, OnUpdate3D);
			if (registerGeometry) Mediator.RegisterHandler<GeneralNodePayload>(Update3D.MessageIDGeometryOnly, OnUpdate3DGeometryOnly);
			if (registerOther) Mediator.RegisterHandler<GeneralNodePayload>(Update3D.MessageIDOtherOnly, OnUpdate3DOtherOnly);
		}

		/// <summary>
		/// Gets or sets the node.
		/// </summary>
		/// <value>The node.</value>
		public BaseDataNode Node
		{
			get
			{
				return DataNode;
			}
			set
			{
				if (DataNode != value)
				{
					//DataNode = value;
					OnPropertyChanged("Node");
				}
			}
		}

		/// <summary>
		/// Gets or sets the node geometry only.
		/// </summary>
		/// <value>The node geometry only.</value>
		public BaseDataNode NodeGeometryOnly
		{
			get
			{
				return DataNode;
			}
			set
			{
				if (DataNode != value)
				{
					//DataNode = value;
					OnPropertyChanged("NodeGeometryOnly");
				}
			}
		}

		/// <summary>
		/// Gets or sets the node other only.
		/// </summary>
		/// <value>The node other only.</value>
		public BaseDataNode NodeOtherOnly
		{
			get
			{
				return DataNode;
			}
			set
			{
				if (DataNode != value)
				{
					//DataNode = value;
					OnPropertyChanged("NodeOtherOnly");
				}
			}
		}

		/// <summary>
		/// Gets or sets the node calculation results.
		/// </summary>
		/// <value>The node calculation results.</value>
		public BaseDataNode NodeCalculationResults
		{
			get
			{
				return DataNode;
			}
			set
			{
				if (DataNode != value)
				{
					//DataNode = value;
					OnPropertyChanged("NodeCalculationResults");
				}
			}
		}

		#region Mediator
		/// <summary>
		/// Called when [update 3D].
		/// </summary>
		/// <param name="param">The parameter.</param>
		//[MediatorMessageSink(Update3D.MessageID)]
		public void OnUpdate3D(GeneralNodePayload param)
		{
			//if (param is Guid)
			//{
			//	DoUpdate3D((Guid)param, null, "Node");
			//}
			//else 
			if (param is GeneralNodePayload payload)
			{
				foreach (BaseDataNode n in payload.DataNodes)
				{
					if (n != null && DoUpdate3D(n.UniqueID, n, "Node")) break;
					else if (n == null) { DoUpdate3D(Guid.Empty, null, "Node"); break; }
				}
			}
		}

		/// <summary>
		/// Does the update 3D.
		/// </summary>
		/// <param name="guid">The unique identifier.</param>
		/// <param name="node">The node.</param>
		/// <param name="propertyName">Name of the property.</param>
		/// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
		private bool DoUpdate3D(Guid guid, BaseDataNode node, string propertyName)
		{
			bool retVal = false;
			// if else separated for optimization, descendant search can be slow
			if (DataNode?.UniqueID == guid || guid == Guid.Empty)
			{
				OnPropertyChanged(propertyName);
				retVal = true;
			}
			return retVal;
		}

		/// <summary>
		/// Called when [update 3D geometry only].
		/// </summary>
		/// <param name="param">The parameter.</param>
		//[MediatorMessageSink(Update3D.MessageIDGeometryOnly)]
		public void OnUpdate3DGeometryOnly(object param)
		{
			if (param is Guid)
			{
				DoUpdate3D((Guid)param, null, "NodeGeometryOnly");
			}
			else if (param is GeneralNodePayload)
			{
				GeneralNodePayload payload = param as GeneralNodePayload;
				foreach (BaseDataNode n in payload.DataNodes)
				{
					if (DoUpdate3D(n.UniqueID, n, "NodeGeometryOnly")) break;
				}
			}
		}

		/// <summary>
		/// Called when [update 3D other only].
		/// </summary>
		/// <param name="param">The parameter.</param>
		//[MediatorMessageSink(Update3D.MessageIDOtherOnly)]
		public void OnUpdate3DOtherOnly(object param)
		{
			if (param is Guid)
			{
				DoUpdate3D((Guid)param, null, "NodeOtherOnly");
			}
			else if (param is GeneralNodePayload)
			{
				GeneralNodePayload payload = param as GeneralNodePayload;
				foreach (BaseDataNode n in payload.DataNodes)
				{
					if (DoUpdate3D(n.UniqueID, n, "NodeOtherOnly")) break;
				}
			}
		}

		/// <summary>
		/// Called when [update 3D current truss calculation results].
		/// </summary>
		/// <param name="param">The parameter.</param>
		//[MediatorMessageSink(MediatorMessages.Update3DCalculationResults)]
		public void OnUpdate3DCurrentCalculationResults(object param)
		{
			if (param is Guid)
			{
				DoUpdate3D((Guid)param, null, "NodeCalculationResults");
			}
			else if (param is GeneralNodePayload)
			{
				GeneralNodePayload payload = param as GeneralNodePayload;
				foreach (BaseDataNode n in payload.DataNodes)
				{
					if (DoUpdate3D(n.UniqueID, n, "NodeCalculationResults")) break;
				}
			}
		}
		#endregion

		protected override void Dispose(bool disposing)
		{
			if (_registerUpdateAll) Mediator.UnregisterHandler<GeneralNodePayload>(Update3D.MessageID, OnUpdate3D);
			if (_registerGeometry) Mediator.UnregisterHandler<GeneralNodePayload>(Update3D.MessageIDGeometryOnly, OnUpdate3DGeometryOnly);
			if (_registerOther) Mediator.UnregisterHandler<GeneralNodePayload>(Update3D.MessageIDOtherOnly, OnUpdate3DOtherOnly);
			base.Dispose(disposing);
		}
	}
}
