using Epx.BIM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ValosModeler.Infrastructure.Events
{
	public class Update3D
	{
		public const string MessageID = "Update3D";
		public const string MessageIDGeometryOnly = "Update3DGeometry";
		public const string MessageIDOtherOnly = "Update3DOther";

		/// <summary>
		/// Updates the 3d asynchronous.
		/// </summary>
		/// <param name="node">The node.</param>
		public static void PublishAsync(Epx.BIM.BaseDataNode node)
		{
			ViewModelBase.MediatorStatic.NotifyColleaguesAsync<GeneralNodePayload>(MessageID, new GeneralNodePayload(Update3DMappings.GetUpdate3DNode(node), null));
		}
		/// <summary>
		/// Updates the 3d asynchronous.
		/// </summary>
		/// <param name="nodes">The nodes.</param>
		public static void PublishAsync(IEnumerable<Epx.BIM.BaseDataNode> nodes)
		{
			if (nodes != null && nodes.Any())
				ViewModelBase.MediatorStatic.NotifyColleaguesAsync<GeneralNodePayload>(MessageID, new GeneralNodePayload(nodes.Select(n => Update3DMappings.GetUpdate3DNode(n)).Distinct(), null));
		}
		/// <summary>
		/// Updates the 3d.
		/// </summary>
		/// <param name="node">The node.</param>
		public static void Publish(Epx.BIM.BaseDataNode node)
		{
			ViewModelBase.MediatorStatic.NotifyColleagues<GeneralNodePayload>(MessageID, new GeneralNodePayload(Update3DMappings.GetUpdate3DNode(node), null));
		}
		/// <summary>
		/// Update3s the d.
		/// </summary>
		/// <param name="nodes">The nodes.</param>
		public static void Publish(IEnumerable<Epx.BIM.BaseDataNode> nodes)
		{
			if (nodes != null && nodes.Any())
				ViewModelBase.MediatorStatic.NotifyColleagues<GeneralNodePayload>(MessageID, new GeneralNodePayload(nodes.Select(n => Update3DMappings.GetUpdate3DNode(n)).Distinct(), null));
		}

		/// <summary>
		/// Update3s the d geometry only asynchronous.
		/// </summary>
		/// <param name="node">The node.</param>
		public static void PublishGeometryOnlyAsync(Epx.BIM.BaseDataNode node)
		{
			ViewModelBase.MediatorStatic.NotifyColleaguesAsync<GeneralNodePayload>(MessageIDGeometryOnly, new GeneralNodePayload(Update3DMappings.GetUpdate3DNode(node), null));
		}
		/// <summary>
		/// Update3s the d geometry only asynchronous.
		/// </summary>
		/// <param name="nodes">The nodes.</param>
		public static void PublishGeometryOnlyAsync(IEnumerable<Epx.BIM.BaseDataNode> nodes)
		{
			if (nodes != null && nodes.Any())
				ViewModelBase.MediatorStatic.NotifyColleaguesAsync<GeneralNodePayload>(MessageIDGeometryOnly, new GeneralNodePayload(nodes.Select(n => Update3DMappings.GetUpdate3DNode(n)).Distinct(), null));
		}
		/// <summary>
		/// Update3s the d geometry only.
		/// </summary>
		/// <param name="node">The node.</param>
		public static void PublishGeometryOnly(Epx.BIM.BaseDataNode node)
		{
			ViewModelBase.MediatorStatic.NotifyColleagues<GeneralNodePayload>(MessageIDGeometryOnly, new GeneralNodePayload(Update3DMappings.GetUpdate3DNode(node), null));
		}
		/// <summary>
		/// Update3s the d geometry only.
		/// </summary>
		/// <param name="nodes">The nodes.</param>
		public static void PublishGeometryOnly(IEnumerable<Epx.BIM.BaseDataNode> nodes)
		{
			if (nodes != null && nodes.Any())
				ViewModelBase.MediatorStatic.NotifyColleagues<GeneralNodePayload>(MessageIDGeometryOnly, new GeneralNodePayload(nodes.Select(n => Update3DMappings.GetUpdate3DNode(n)).Distinct(), null));
		}

		/// <summary>
		/// Update3s the d other only asynchronous.
		/// </summary>
		/// <param name="node">The node.</param>
		public static void PublishOtherOnlyAsync(Epx.BIM.BaseDataNode node)
		{
			ViewModelBase.MediatorStatic.NotifyColleaguesAsync<GeneralNodePayload>(MessageIDOtherOnly, new GeneralNodePayload(Update3DMappings.GetUpdate3DNode(node), null));
		}
		/// <summary>
		/// Update3s the d other only asynchronous.
		/// </summary>
		/// <param name="nodes">The nodes.</param>
		public static void PublishOtherOnlyAsync(IEnumerable<Epx.BIM.BaseDataNode> nodes)
		{
			if (nodes != null && nodes.Any())
				ViewModelBase.MediatorStatic.NotifyColleaguesAsync<GeneralNodePayload>(MessageIDOtherOnly, new GeneralNodePayload(nodes.Select(n => Update3DMappings.GetUpdate3DNode(n)).Distinct(), null));
		}
		/// <summary>
		/// Update3s the d other only.
		/// </summary>
		/// <param name="node">The node.</param>
		public static void PublishOtherOnly(Epx.BIM.BaseDataNode node)
		{
			ViewModelBase.MediatorStatic.NotifyColleagues<GeneralNodePayload>(MessageIDOtherOnly, new GeneralNodePayload(Update3DMappings.GetUpdate3DNode(node), null));
		}
		/// <summary>
		/// Update 3D, other only.
		/// </summary>
		/// <param name="nodes">The nodes.</param>
		public static void PublishOtherOnly(IEnumerable<Epx.BIM.BaseDataNode> nodes)
		{
			if (nodes != null && nodes.Any())
				ViewModelBase.MediatorStatic.NotifyColleagues<GeneralNodePayload>(MessageIDOtherOnly, new GeneralNodePayload(nodes.Select(n => Update3DMappings.GetUpdate3DNode(n)).Distinct(), null));
		}
	}
}
