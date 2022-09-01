using Enterprixe.WPF.Tools.Elements3D;
using Epx.BIM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Epx.BIM.Models;
namespace ValosModeler.Views.Model3DView.Visuals
{
	public class ModelPartVisual3D : HighlightableVisual3D, IModelPartVisual
	{
		public ModelPartVisual3D()
		{
		}

		public ModelPartVisual3D(Model3D model, BaseDataNode id, Material highlightMaterial)
			: base(model, highlightMaterial)
		{
			AttachedNode = id;
		}

		public BaseDataNode AttachedNode { get; protected set; }

		public virtual string HoverText { get; }
		public bool IsSelected { get; set; } //do nothing here

		//ISnappable
		public Epx.BIM.GridMesh.SnapPoint3D GetSnapPoint(Point3D referencePoint, double tolerance, IEnumerable<Epx.BIM.GridMesh.SnapType> enabledSnaps)
		{
			bool useOverride = (AttachedNode as ModelBaseNode).Outline3DOverride != null;
			var modelPoint = (AttachedNode as ModelBaseNode).GetSnapPoint3D(referencePoint, tolerance, enabledSnaps, useOverride, true);
			modelPoint.Node = AttachedNode;
			return modelPoint;
		}
	}
}
