using Epx.BIM.GridMesh;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;

namespace ValosModeler.Views.Model3DView.Visuals
{
	public class SnapMark : Shape
	{
		public static readonly DependencyProperty SnapTypeProperty = DependencyProperty.Register(
		"SnapType", typeof(SnapType), typeof(SnapMark), new FrameworkPropertyMetadata(SnapType.GridIntersection, FrameworkPropertyMetadataOptions.AffectsRender));
		public SnapType SnapType
		{
			get { return (SnapType)GetValue(SnapTypeProperty); }
			set { SetValue(SnapTypeProperty, value); }
		}
		public static readonly DependencyProperty OriginProperty = DependencyProperty.Register(
		"Origin", typeof(Point), typeof(SnapMark), new FrameworkPropertyMetadata(new Point(0, 0), FrameworkPropertyMetadataOptions.AffectsRender));
		public Point Origin
		{
			get { return (Point)GetValue(OriginProperty); }
			set { SetValue(OriginProperty, value); }
		}

		Geometry _geometry;
		Geometry _geomCross;
		Geometry _geomGeometryNode;
		Geometry _geomGeometryEdge;
		Geometry _geomGeometryAny;
		Geometry _geomGeometryEdgeEnds;
		Geometry _geomGeometryEdgeMiddle;
		Geometry _geomGridMiddle;
		Geometry _geomGridLine;
		Geometry _geomGridIntersection;
		Geometry _geomPerpendicular;

		public SnapMark()
		{
			// cache geometries for DefiningGeometry
			_geomCross = Geometry.Parse("M -8 -8 L 8 8 M -8 8 L 8 -8");
			_geomGeometryNode = Geometry.Parse("M -8 -8 L 8 8 M -8 8 L 8 -8 M -8 -8 L 8 -8 8 8 -8 8 -8 -8");
			_geomGeometryEdge = Geometry.Parse("M -8 -8 L 8 8 L -8 8 L 8 -8 L -8 -8 Z");
			_geomGeometryAny = Geometry.Parse("M 8 -8 L -8 -8 L8 8 L-8 8 ");
			_geomGeometryEdgeEnds = Geometry.Parse("M -8 -8 L 8 -8 8 8 -8 8 -8 -8");
			_geomGeometryEdgeMiddle = Geometry.Parse("M 0 -8 L 8 8 -8 8 0 -8 ");
			_geomGridMiddle = Geometry.Parse("M 0 -8 L 8 8 -8 8 0 -8 ");
			_geomGridLine = Geometry.Parse("M -8 -8 L 8 8 L -8 8 L 8 -8 L -8 -8 Z");
			_geomGridIntersection = Geometry.Parse("M -8 -8 L 8 8 M -8 8 L 8 -8 M -8 -8 L 8 -8 8 8 -8 8 -8 -8");
			_geomPerpendicular = Geometry.Parse("M 0 -20 L 0 0 L 20 0 ");
			_geometry = _geomCross;

			Visibility = System.Windows.Visibility.Collapsed;
			StrokeThickness = 2;
			Stroke = Brushes.Yellow;
			IsHitTestVisible = false;
		}

		protected override Geometry DefiningGeometry
		{
			get
			{
				// anything temporary created with new here will get left on the heap
				switch (SnapType)
				{
					case SnapType.GeometryNode:
						_geometry = _geomGeometryNode;
						break;
					case SnapType.GeometryEdge:
						_geometry = _geomGeometryEdge;
						break;
					case SnapType.GeometryAny:
						_geometry = _geomGeometryAny;
						break;
					case SnapType.GeometryEdgeEnds:
					case SnapType.ReferenceLineEnd:
						_geometry = _geomGeometryEdgeEnds;
						break;
					case SnapType.GeometryEdgeMiddle:
						_geometry = _geomGeometryEdgeMiddle;
						break;
					case SnapType.GridMiddle:
						_geometry = _geomGridMiddle;
						break;
					case SnapType.GridLine:
						_geometry = _geomGridLine;
						break;
					case SnapType.GridIntersection:
						_geometry = _geomGridIntersection;
						break;
					case SnapType.Perpendicular:
						_geometry = _geomPerpendicular;
						break;
					case SnapType.ReferenceLine:
						_geometry = _geomCross;
						break;

					default:
						_geometry = _geomCross;
						break;
				}
				return _geometry;
			}
		}

		private void UpdateSnapMarkProperties()
		{
			Visibility = (SnapType == SnapType.None) ? Visibility = Visibility.Collapsed : Visibility.Visible;
			Stroke = Brushes.Yellow;
			StrokeThickness = 2;
			switch (SnapType)
			{
				case SnapType.GeometryNode:
					Stroke = Brushes.Red;
					StrokeThickness = 2;
					break;
				case SnapType.GeometryEdge:
					Stroke = Brushes.Green;
					StrokeThickness = 1;
					break;
				case SnapType.GeometryAny:
					Stroke = Brushes.Green;
					StrokeThickness = 1;
					break;
				case SnapType.GeometryEdgeEnds:
					Stroke = Brushes.Green;
					StrokeThickness = 2;
					break;
				case SnapType.GeometryEdgeMiddle:
					Stroke = Brushes.Green;
					StrokeThickness = 2;
					break;
				case SnapType.GridMiddle:
					break;
				case SnapType.GridLine:
					break;
				case SnapType.GridIntersection:
					break;
				case SnapType.Perpendicular:
					Stroke = Brushes.Navy;
					StrokeThickness = 2;
					break;
				case SnapType.ReferenceLine:
				case SnapType.ReferenceLineEnd:
					Stroke = Brushes.Cyan;
					StrokeThickness = 2;
					break;
				default:
					break;
			}
		}

		/// <summary>
		/// RenderTransform the mark to the origin.
		/// </summary>
		/// <param name="addedTransform">A transform to add after the translation.</param>
		public void RegenerateMark(Transform addedTransform = null)
		{
			if (addedTransform == null)
			{
				RenderTransform = new TranslateTransform(Origin.X, Origin.Y);
			}
			else
			{
				TransformGroup trfGroup = new TransformGroup();
				trfGroup.Children.Add(new TranslateTransform(Origin.X, Origin.Y));
				trfGroup.Children.Add(addedTransform);
				RenderTransform = trfGroup;
			}
		}

		protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
		{
			if (e.Property == SnapMark.SnapTypeProperty)
			{
				UpdateSnapMarkProperties();
				RegenerateMark();
			}
			else if (e.Property == SnapMark.OriginProperty)
			{
				UpdateSnapMarkProperties();
				RegenerateMark();
			}
			base.OnPropertyChanged(e);
		}

	}
}
