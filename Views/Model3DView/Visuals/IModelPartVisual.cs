using Enterprixe.ValosUITools.Elements3D;
using Epx.BIM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace ValosModeler.Views.Model3DView.Visuals
{
	public interface  IModelPartVisual : ISnappableVisual
	{
		BaseDataNode AttachedNode { get; }
		string HoverText { get; }
		bool IsSelected { get; set; }
		bool IsHighlighted { get; set; }
	}

	public interface IGridMeshVisual : ISnappableVisual
	{
		Epx.BIM.GridMesh.GridMesh AttachedGrid { get; }
	}

	public interface ISnappableVisual
	{
		Epx.BIM.GridMesh.SnapPoint3D GetSnapPoint(Point3D referencePoint, double tolerance, IEnumerable<Epx.BIM.GridMesh.SnapType> enabledSnaps);
	}
}
