using Epx.BIM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ValosModeler.Infrastructure.Events
{
	public class NodeSelected
	{
		/// <summary>
		/// 
		/// </summary>
		public const string MessageID = "NodeSelected";

		/// <summary>
		/// Clear selection (null node sent).
		/// </summary>
		/// <param name="sender"></param>
		public static void Clear(object sender)
		{
			ViewModelBase.MediatorStatic.NotifyColleagues<GeneralNodePayload>(MessageID, new GeneralNodePayload(new List<BaseDataNode>(0), sender));
		}
		public static void Publish(BaseDataNode node, object sender)
		{
			ViewModelBase.MediatorStatic.NotifyColleagues<GeneralNodePayload>(MessageID, new GeneralNodePayload(node, sender));
		}
		public static void Publish(List<BaseDataNode> nodes, object sender)
		{
			ViewModelBase.MediatorStatic.NotifyColleagues<GeneralNodePayload>(MessageID, new GeneralNodePayload(nodes, sender));
		}
	}
}
