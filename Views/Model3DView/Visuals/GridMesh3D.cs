using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media.Media3D;
using System.Windows.Media;
using Epx.BIM.GridMesh;
using Enterprixe.WPF.Tools.Viewport.WireBase;
using Enterprixe.ValosUITools.Elements3D;

/// <summary>
/// The Elements3D namespace.
/// </summary>
namespace ValosModeler.Views.Model3DView.Visuals
{
	/// <summary>
	/// Class GridMesh3D.
	/// </summary>
	/// <seealso cref="Enterprixe.ValosUITools.Elements3D.ModelVisual3DBase" />
	public class GridMesh3D : ModelVisual3DBase, IGridMeshVisual
	{
		/// <summary>
		/// The _parent viewport
		/// </summary>
		protected Viewport3DVisual _parentViewport;
		/// <summary>
		/// The _panel face
		/// </summary>
		//private GridMeshVisual3D _panelFace;

		/// <summary>
		/// Initializes a new instance of the <see cref="GridMesh3D"/> class.
		/// </summary>
		public GridMesh3D()
		{
			//IsHitTestVisible = true;
		}

		GridMeshVisual3D _panelFace;
		/// <summary>
		/// Creates the composite model.
		/// </summary>
		/// <param name="productionMode">The production mode.</param>
		protected override void CreateCompositeModel(ModelProductionMode productionMode)
		{
			//return; // disable, draws too many
			_panelFace = null;
			this.Children.Clear();
			GridMesh gridNode = Node as GridMesh;
			if (gridNode == null) return;
			this.Transform = new MatrixTransform3D(gridNode.LocalToGlobal);

			double width = 100;
			double height = 100;
			double min;
			gridNode.MinMaxFirst(out min, out width);
			gridNode.MinMaxSecond(out min, out height);

			if (width > 1 && height > 1 /*&& !IsDragging*/)
			{
				Material transparentMat = new DiffuseMaterial(Brushes.Transparent);
				transparentMat.Freeze();
				_panelFace = new GridMeshVisual3D(gridNode);
				GeometryModel3D model = new GeometryModel3D(
					RectangleMesh(new Size(width * 10, height * 10), new Point3D(0, 0, 0)),
					transparentMat);
				model.BackMaterial = transparentMat; // to allow snapping "from behind"
				model.Freeze();
				_panelFace.Content = model;

			}

			if (_parentViewport == null)
				//_parentViewport = Element3D.GetParentViewport(this);

				if (width > 1 && height > 1)
				{
					bool addCSModel = false;
					CreateGrid(addCSModel);
				}

			if (_panelFace != null && !gridNode.IsSnapPlaneHidden)// _panelFace can block visibility
				this.Children.Add(_panelFace); // must be added in the end of the tree
		}

		/// <summary>
		/// Creates the grid.
		/// </summary>
		/// <param name="addCSModel">if set to <c>true</c> [add cs model].</param>
		protected void CreateGrid(bool addCSModel = true)
		{
			List<Point3D> pointPairs = new List<Point3D>();
			GridMesh gridNode = Node as GridMesh;

			gridNode.UpdateGridLines();
			foreach (var line in gridNode.Grid3DLines)
			{
				pointPairs.Add(line.StartPoint);
				pointPairs.Add(line.EndPoint);
			}

			if (pointPairs.Count > 0)
			{
				WireLines lines = new GridWireLinesVisual3D(gridNode);
				lines.Lines = new Point3DCollection(pointPairs);
				lines.Color =  Colors.Gray;
				this.Children.Add(lines);
				if (gridNode.IsSnapPlaneHidden && gridNode.IsSnapHelpOn)
				{
					// make it easier to snap to 3d lines
					lines = new GridWireLinesVisual3D(gridNode);
					lines.Lines = new Point3DCollection(pointPairs);
					lines.Color = Colors.Transparent;
					lines.Thickness = 5;
					lines.UseSingleMaterial = true;
					this.Children.Add(lines);
				}
			}
		}

		/// <summary>
		/// Rectangles the mesh.
		/// </summary>
		/// <param name="size">The size.</param>
		/// <param name="origin">The origin.</param>
		/// <returns>MeshGeometry3D.</returns>
		public static MeshGeometry3D RectangleMesh(Size size, Point3D origin)
		{
			MeshGeometry3D mesh = new MeshGeometry3D
			{
				Positions = new Point3DCollection
			{
			   origin + new Vector3D(-size.Width/2, size.Height/2,0),
			   origin + new Vector3D(-size.Width/2,-size.Height/2,0),
			   origin + new Vector3D( size.Width/2,-size.Height/2,0),
			   origin + new Vector3D( size.Width/2, size.Height/2,0),
			   
			},
				TextureCoordinates = new PointCollection
			{
			   new Point(0,0),
			   new Point(0,1),
			   new Point(1,1),
			   new Point(1,0),
			},
				TriangleIndices = new Int32Collection { 0, 1, 2, 0, 2, 3 }
			};
			return mesh;
		}
		public GridMesh AttachedGrid { get; set; }
		//ISnappable
		public SnapPoint3D GetSnapPoint(Point3D plane3Dpoint, double tolerance, IEnumerable<SnapType> enabledSnaps)
		{
			var gridPoint = AttachedGrid.GetSnapPoint3D(plane3Dpoint, tolerance, enabledSnaps);
			gridPoint.Node = AttachedGrid;
			return gridPoint;
		}

	}

	/// <summary>
	/// Class GridMeshVisual3D.
	/// </summary>
	/// <seealso cref="System.Windows.Media.Media3D.ModelVisual3D" />
	public class GridMeshVisual3D : ModelVisual3D, IGridMeshVisual
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="GridMeshVisual3D"/> class.
		/// </summary>
		/// <param name="attachedGrid">The attached grid.</param>
		public GridMeshVisual3D(GridMesh attachedGrid)
		{
			AttachedGrid = attachedGrid;
		}

		/// <summary>
		/// Gets or sets the attached grid.
		/// </summary>
		/// <value>The attached grid.</value>
		public GridMesh AttachedGrid { get; set; }

		//ISnappable
		public SnapPoint3D GetSnapPoint(Point3D referencePoint, double tolerance, IEnumerable<Epx.BIM.GridMesh.SnapType> enabledSnaps)
		{
			var gridPoint = AttachedGrid.GetSnapPoint3D(referencePoint, tolerance, enabledSnaps);
			gridPoint.Node = AttachedGrid;
			return gridPoint;
		}

	}

	/// <summary>
	/// Class GridWireLinesVisual3D.
	/// </summary>
	/// <seealso cref="Enterprixe.WPF.Tools.Viewport.WireBase.WireLines" />
	public class GridWireLinesVisual3D : WireLines, IGridMeshVisual
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="GridWireLinesVisual3D"/> class.
		/// </summary>
		/// <param name="attachedGrid">The attached grid.</param>
		public GridWireLinesVisual3D(GridMesh attachedGrid)
		{
			//UseSingleMaterial = true;
			AttachedGrid = attachedGrid;
		}

		/// <summary>
		/// Gets or sets the attached grid.
		/// </summary>
		/// <value>The attached grid.</value>
		public GridMesh AttachedGrid { get; set; }

		//ISnappable
		public SnapPoint3D GetSnapPoint(Point3D referencePoint, double tolerance, IEnumerable<Epx.BIM.GridMesh.SnapType> enabledSnaps)
		{
			var ppoint = AttachedGrid.ProjectToGrid(referencePoint, true);
			var gridPoint = AttachedGrid.GetSnapPoint3D(ppoint, tolerance, enabledSnaps);
			gridPoint.Node = AttachedGrid;
			return gridPoint;
		}

	}
}
