using Epx.BIM;
using System;
using System.Collections.Generic;
using System.Linq;
using GLGraphicsLib;
using GLGraphicsLib.ModelObjects;
using Epx.BIM.GridMesh;
using System.Drawing;
using OpenTK;
using OpenTK.Graphics.OpenGL;
#if NETCoreOnly
	using Epx.BIM.BaseTools;
#else
using System.Windows.Media.Media3D;
#endif

namespace ValosModeler.Views.Model3DView.Visuals
{
	public class GLGridVisual : ReferencePlane, IGridMeshVisual
	{
		GridMesh _gridNode;
		public GridMesh AttachedGrid => _gridNode;
		public override bool IsVisible => _gridNode.IsShownIn3D;
		public void SetDataFromGridNode(GridMesh gridNode)
		{
			DrawLabels = false;
			DrawCursor = false;
			DrawGLSnaps = false;
			DrawXYZAtOrigin = false;
			PlaneOpacity = 0;
			DrawAlteredZeroLines = true;
			_gridNode = gridNode;
			Origin = (_gridNode.Origin + _gridNode.ZLevels.FirstOrDefault() * _gridNode.ZAxis).ToGLPoint();
			XDirection = ((Point3D)_gridNode.XAxis).ToGLPoint();
			YDirection = ((Point3D)_gridNode.YAxis).ToGLPoint();
			XLineSpacing = (float) gridNode.SpacingFirstDirection;
			YLineSpacing = (float)gridNode.SpacingSecondDirection;
			_isModelGeometryDirty = true;
		}

		protected override void DrawGridLines()
		{
			_gridNode.UpdateGridLines();
			var lines = _gridNode.Grid3DLinesInFirstDirection;
			lines.AddRange(_gridNode.Grid3DLinesInSecondDirection);
			foreach(var line in lines)
			{
				var p1 = new Vector2((float)line.StartPoint.X, (float)line.StartPoint.Y);
				var p2 = new Vector2((float)line.EndPoint.X, (float)line.EndPoint.Y);
				DrawLine(p1, p2);
			}
		}

		protected override void CreateRectPlane(IGraphicsContext context)
		{
			if (PlaneOpacity > 0)
			{
				var xmin = _gridNode.Grid2DLinesInFirstDirection.First().Distance;// distances in X dir
				var xmax = _gridNode.Grid2DLinesInFirstDirection.Last().Distance;// distances in X dir
				if (_gridNode.GenerateFirstDirectionGridLinesTwoWay)
					xmin = -xmax;
				var ymin = _gridNode.Grid2DLinesInSecondDirection.First().Distance;// distances in X dir
				var ymax = _gridNode.Grid2DLinesInSecondDirection.Last().Distance;// distances in X dir
				if (_gridNode.GenerateSecondDirectionGridLinesTwoWay)
					ymin = -ymax;
				var cx = (xmin + xmax) / 2;
				var cy = (ymin + ymax) / 2;
				var center = Origin + XDirection * (float)cx + YDirection * (float)cy;
				CreateRectPlane(center, Normal, (float)(xmax - xmin), (float)(ymax - ymin), context);
			}
		}
		//ISnappable
		public SnapPoint3D GetSnapPoint(Point3D referencePoint, double tolerance, IEnumerable<Epx.BIM.GridMesh.SnapType> enabledSnaps)
		{
			var gridPoint = AttachedGrid.GetSnapPoint3D(referencePoint, tolerance, enabledSnaps);
			gridPoint.Node = AttachedGrid;
			return gridPoint;
		}
	}

}
