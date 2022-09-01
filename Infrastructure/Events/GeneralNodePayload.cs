using Epx.BIM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ValosModeler.Infrastructure.Events
{
	/// <summary>
	/// Class GeneralNodePayload.
	/// </summary>
	public class GeneralNodePayload
	{
		/// <summary>
		/// The _nodes
		/// </summary>
		IEnumerable<BaseDataNode> _nodes;

		/// <summary>
		/// Initializes a new instance of the <see cref="GeneralNodePayload"/> class.
		/// </summary>
		/// <param name="node">The node.</param>
		public GeneralNodePayload(BaseDataNode node, object sender)
		{
			_nodes = new List<BaseDataNode>() { node };
			Sender = sender;
		}
		/// <summary>
		/// Initializes a new instance of the <see cref="GeneralNodePayload"/> class.
		/// </summary>
		/// <param name="nodes">The nodes.</param>
		public GeneralNodePayload(IEnumerable<BaseDataNode> nodes, object sender)
		{
			_nodes = nodes;
			Sender = sender;
		}

		/// <summary>
		/// reference to the first data node
		/// </summary>
		/// <value>The data node.</value>
		public BaseDataNode DataNode
		{
			get
			{
				return _nodes.FirstOrDefault();
			}
		}
		/// <summary>
		/// reference to the data nodes
		/// </summary>
		/// <value>The data nodes.</value>
		public IEnumerable<BaseDataNode> DataNodes
		{
			get { return _nodes; }
			set { _nodes = value; }
		}

		/// <summary>
		/// Sender signature.
		/// </summary>
		public object Sender { get; private set; }
	}
}
