using Epx.BIM.Models;
using Enterprixe.ValosUITools.Elements3D;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Enterprixe.WPF.Tools.Elements3D;

namespace ValosModeler.Views.Model3DView.Visuals
{
	/// <summary>
	/// Class ModelPart3DVisual.
	/// </summary>
	/// <seealso cref="Enterprixe.ValosUITools.Elements3D.ModelVisual3DBase" />
	public class ModelObjectVisual : ModelVisual3DBase
	{
		/// <summary>
		/// Creates the composite model.
		/// </summary>
		/// <param name="productionMode">The production mode.</param>
		protected override void CreateCompositeModel(ModelProductionMode productionMode = ModelProductionMode.All)
		{
			this.Children.Clear();
			if (!(Node as ModelBaseNode).HasGeometry) return;
			List<Epx.BIM.Models.Geometry.ModelGeometry3D> geoms = Epx.BooleanSDK.ModelTools.GetFinalGeometries((Node as ModelBaseNode).Geometry3D);
			if (geoms == null || geoms.Count < 1) return;
//			Epx.BIM.Models.Geometry.ModelGeometry3D mg3 = (Node as ModelBaseNode).Geometry3D;
//			if (mg3 == null) return;
			List<Point3D> outlinePointsAll = new List<Point3D>();
			Model3DGroup modelGroup = new Model3DGroup();
			Model3DCollection modelCol = new Model3DCollection();
			modelGroup.Children = modelCol;
			foreach (Epx.BIM.Models.Geometry.ModelGeometry3D mg3 in geoms)
			{
				foreach (Epx.BIM.Models.Geometry.SolidMesh sm in mg3.GetSolidMeshes())
				{
					sm.GetMeshPointsAndIndices(out var pCol, out var indices);
					MeshGeometry3D mesh = new MeshGeometry3D();
					mesh.Positions = new Point3DCollection(pCol);
					mesh.TriangleIndices = new Int32Collection(indices);
					GeometryModel3D model = new GeometryModel3D();
					model.Geometry = mesh;
					SolidColorBrush nailBrush = new SolidColorBrush(sm.Color);
					nailBrush.Opacity = sm.Opacity;
					nailBrush.Freeze();
					if (Viewport != null && Viewport.DataContext is Views.Model3DView.ModelViewViewModel
						&& ((Viewport.DataContext as ModelViewViewModel).ContextualNodes.Contains(Node) /*|| (Viewport.DataContext as ModelViewViewModel).ContextualNodes.Any(n => n.IsDescendant(Node))*/))
						model.Material = new DiffuseMaterial(new SolidColorBrush(HighlightableVisual3D.ChangeColorBrightness(sm.Color, 0.5)));
					else
					{
						var scheduleBrush = GetScheduleBrush();
						if (scheduleBrush != null)
							model.Material = new DiffuseMaterial(scheduleBrush);
						else
							model.Material = new DiffuseMaterial(nailBrush);
					}

					Transform3DGroup transform = new Transform3DGroup();
					//               transform.Children.Add(new MatrixTransform3D((Node as ModelBaseNode).LocalToGlobal));
					transform.Children.Add(new MatrixTransform3D(sm.LocalToGlobal));
					model.Transform = transform;
					modelCol.Add(model);
					//				this.Content = model;
					IList<Point3D> outlinePoints = sm.GetVisibleLinePairs();
					Epx.BIM.GeometryTools.GeometryMath.TransformPoints(outlinePoints, transform.Value);
					outlinePointsAll.AddRange(outlinePoints); // minimize WireBase instances
				}
			}

			var mg3d = geoms.FirstOrDefault();
			ModelPartVisual3D visual = new ModelPartVisual3D(modelGroup, Node, 
				new DiffuseMaterial(new SolidColorBrush(HighlightableVisual3D.ChangeColorBrightness(mg3d != null ? mg3d.Color : Colors.Lime, 0.5))));
			this.Children.Add(visual);
		}

		protected SolidColorBrush GetScheduleBrush()
		{
			DateTime worldClock = MainWindowViewModel.WorldClock.ToUniversalTime();

			var schedule = Node.GetChildNode<Epx.BIM.Scheduling.ScheduleData>();
			if (schedule == null) schedule = Node.Parent.GetChildNode<Epx.BIM.Scheduling.ScheduleData>();
			if (schedule != null)
			{
				var schedulinState = schedule.GetSchedulingState(worldClock);
				if (schedulinState == Epx.BIM.Scheduling.SchedulingStateEnum.Establishment)
				{
					var brush = new SolidColorBrush(Color.FromRgb(237,28,36));
					brush.Freeze();
					return brush;
				}
				else if (schedulinState == Epx.BIM.Scheduling.SchedulingStateEnum.Design)
				{
					var brush = new SolidColorBrush(Color.FromRgb(255, 143, 48));
					brush.Freeze();
					return brush;
				}
				else if (schedulinState == Epx.BIM.Scheduling.SchedulingStateEnum.Manufacturing)
				{
					var brush = new SolidColorBrush(Color.FromRgb(255, 234, 0));
					brush.Freeze();
					return brush;
				}
				else if (schedulinState == Epx.BIM.Scheduling.SchedulingStateEnum.Transport)
				{
					var brush = new SolidColorBrush(Color.FromRgb(189, 248, 66));
					brush.Freeze();
					return brush;
				}
				else if (schedulinState == Epx.BIM.Scheduling.SchedulingStateEnum.Erection)
				{
					var brush = new SolidColorBrush(Color.FromRgb(86, 199, 5));
					brush.Freeze();
					return brush;
				}
				else return null;
			}

//			established RGB  237    28       36
//designed RGB  255    143     48
//manufactured RGB  255    234     0
//transported RGB  189    248     66
//erected RGB  86      199     5

			return null;
		}
	}
}
