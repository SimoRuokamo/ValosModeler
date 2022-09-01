using Epx.BIM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ValosModeler.Infrastructure.Events
{
	public class ModelChanged
	{
		/// <summary>
		/// General signal to update whole model tree.
		/// </summary>
		public const string MessageID = "RootChanged";
		public const string NodeAdded = "NodeAdded";
		public const string NodeRemoved = "NodeRemoved";

		/// <summary>
		/// General signal to update whole model tree.
		/// </summary>
		public static void Publish(object sender)
		{
			ViewModelBase.MediatorStatic.NotifyColleagues<GeneralNodePayload>(MessageID, new GeneralNodePayload(new List<BaseDataNode>(0), sender));
		}
		public static void PublishNodeAdded(BaseDataNode node, object sender)
		{
			ViewModelBase.MediatorStatic.NotifyColleagues<GeneralNodePayload>(NodeAdded, new GeneralNodePayload(node, sender));
		}
		public static void PublishNodeAdded(List<BaseDataNode> nodes, object sender)
		{
			ViewModelBase.MediatorStatic.NotifyColleagues<GeneralNodePayload>(NodeAdded, new GeneralNodePayload(nodes, sender));
		}
		public static void PublishNodeRemoved(BaseDataNode node, object sender)
		{
			ViewModelBase.MediatorStatic.NotifyColleagues<GeneralNodePayload>(NodeRemoved, new GeneralNodePayload(node, sender));
		}
		public static void PublishNodeRemoved(List<BaseDataNode> nodes, object sender)
		{
			ViewModelBase.MediatorStatic.NotifyColleagues<GeneralNodePayload>(NodeRemoved, new GeneralNodePayload(nodes, sender));
		}
	}
}
