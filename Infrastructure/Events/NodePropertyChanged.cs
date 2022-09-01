using Epx.BIM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ValosModeler.Infrastructure.Events
{
	public class NodePropertyChangedPayload
	{
		List<BaseDataNode> _nodes;
		List<string> _changedPropertyNames;

		public NodePropertyChangedPayload(List<BaseDataNode> nodes, string property, object sender)
		{
			_nodes = nodes;
			_changedPropertyNames = new List<string>(1) { property };
			Sender = sender;
		}

		public NodePropertyChangedPayload(BaseDataNode node, string property, object sender)
		{
			_nodes = new List<BaseDataNode>() { node };
			_changedPropertyNames = new List<string>(1) { property };
			Sender = sender;
		}

		public NodePropertyChangedPayload(string property, object sender)
		{
			_nodes = null;
			_changedPropertyNames = new List<string>(1) { property };
			Sender = sender;
		}
		public NodePropertyChangedPayload(List<BaseDataNode> nodes, List<string> properties, object sender)
		{
			_nodes = nodes;
			_changedPropertyNames = properties;
			Sender = sender;
		}

		public NodePropertyChangedPayload(BaseDataNode node, List<string> properties, object sender)
		{
			_nodes = new List<BaseDataNode>() { node };
			_changedPropertyNames = properties;
			Sender = sender;
		}

		public NodePropertyChangedPayload(List<string> properties, object sender)
		{
			_nodes = null;
			_changedPropertyNames = properties;
			Sender = sender;
		}

		/// <summary>
		/// reference to project data node
		/// </summary>
		public List<BaseDataNode> DataNodes
		{
			get
			{
				return _nodes;
			}
			set
			{
				_nodes = value;
			}
		}

		/// <summary>
		/// Contains changed property names separated with '|'
		/// </summary>
		public List<string> ChangedPropertyNames
		{
			get
			{
				return _changedPropertyNames;
			}
			set
			{
				_changedPropertyNames = value;
			}
		}

		/// <summary>
		/// Sender signature.
		/// </summary>
		public object Sender { get; private set; }
	}

	/// <summary>
	/// Notifies that a node's property was changed.
	/// </summary>
	public class NodePropertyChanged
	{
		public const string MessageID = "NodePropertyChanged";
		/// <summary>
		/// 
		/// </summary>
		public static void Publish(BaseDataNode node, string changedPropertyName, object sender)
		{
			ViewModelBase.MediatorStatic.NotifyColleagues<NodePropertyChangedPayload>(MessageID, new NodePropertyChangedPayload(node, changedPropertyName, sender));
		}
		public static void Publish(BaseDataNode node, List<string> changedPropertyNames, object sender)
		{
			ViewModelBase.MediatorStatic.NotifyColleagues<NodePropertyChangedPayload>(MessageID, new NodePropertyChangedPayload(node, changedPropertyNames, sender));
		}
		/// <summary>
		/// 
		/// </summary>
		public static void Publish(List<BaseDataNode> nodes, string changedPropertyName, object sender)
		{
			ViewModelBase.MediatorStatic.NotifyColleagues<NodePropertyChangedPayload>(MessageID, new NodePropertyChangedPayload(nodes, changedPropertyName, sender));
		}
		public static void Publish(List<BaseDataNode> nodes, List<string> changedPropertyNames, object sender)
		{
			ViewModelBase.MediatorStatic.NotifyColleagues<NodePropertyChangedPayload>(MessageID, new NodePropertyChangedPayload(nodes, changedPropertyNames, sender));
		}
	}
}
