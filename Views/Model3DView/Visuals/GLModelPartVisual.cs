using Epx.BIM;
using System;
using System.Collections.Generic;
using System.Linq;
using GLGraphicsLib;
using GLGraphicsLib.ModelObjects;
using GLGraphicsLib.Tools;
using ObjectTK.Textures;
using System.Drawing;
#if NETCoreOnly
	using Epx.BIM.BaseTools;
#else
	using System.Windows.Media.Media3D;
#endif

namespace ValosModeler.Views.Model3DView.Visuals
{
	internal struct RenderMesh 
	{
		internal Mesh3D Mesh;
		internal System.Drawing.Color Color;
		internal float Opacity;
	}
	//[System.Diagnostics.DebuggerDisplay("{AttachedNode.Name}")]
	public class GLModelPartVisual : ModelObject, IModelPartVisual
	{
		public static bool UseMeshPool { get; set; } //must be enabled for large models
		public struct OuterGeom
		{
			public Box3 Bounds;
			public List<Vector2> MidPoints;
		}
		public OuterGeom OutGeom { get; protected set; }
		public Epx.BIM.Models.ModelBaseNode ModelNode { get; private set; }
		List<RenderMesh> _meshes3D = null;
		Vector3D _localOffset;

		public GLModelPartVisual(Epx.BIM.Models.ModelBaseNode node, Vector3D offset)
		{
			_localOffset = offset;
			ModelNode = node;
			//CalcOterGeom();
			CreateRenderMeshes();
			DebugName = ModelNode.Name;
		}
		public override bool IsVisible => ModelNode.IsShownIn3D;
		private void CreateRenderMeshes()
		{
			var box = Box3.Empty;
			_meshes3D = new List<RenderMesh>(10);
			HashSet<Vector2> midPoints = new HashSet<Vector2>();
			var geoms = new List<Epx.BIM.Models.Geometry.ModelGeometry3D>();
			if(ModelNode is Valos.Ifc.ValosIfcSpatialNode && ModelNode.Geometry3D != null) //no need to call GetFinalGeometries because all the Boolean operations done while importing
			{
				if(ModelNode.Geometry3D is Epx.BIM.Models.Geometry.ModelGeometry3DGroup group)
					geoms.AddRange(group.GetGeometries());
				else
					geoms.Add(ModelNode.Geometry3D);
			}
			else
			{
				geoms = Epx.BooleanSDK.ModelTools.GetFinalGeometries(ModelNode.Geometry3D);
			}

			if(ModelNode.Geometry3D is Valos.Ifc.ValosIfcEmptyGeometry ifcEmpty)
			{
				geoms = new List<Epx.BIM.Models.Geometry.ModelGeometry3D>();
				foreach(var err in ifcEmpty.ErrorResults)
				{
					if(err != null)	geoms.Add(err);
				}
			}
			foreach (Epx.BIM.Models.Geometry.ModelGeometry3D mg3 in geoms)
			{
				var mid = mg3.MaterialID;
				var mname = mg3.MaterialName;
				if(!string.IsNullOrEmpty(mname))
				{

				}
				bool isSecondOperand = false;
				if(mg3 is Valos.Ifc.ValosIfcExtrudedGeometry ifcExtr)
					isSecondOperand = ifcExtr.IsBooleanSecondOperand;
				var solmeshes = mg3.GetSolidMeshes( onlyPositiveVertexIndices:false, bSeparatePointsForTriangles:true);
				foreach (Epx.BIM.Models.Geometry.SolidMesh sm in solmeshes)
				{
					sm.GetMeshPointsAndIndices(out var pCol, out var indices);
					var trf = sm.LocalToGlobal;
					if(sm.Parent == null)
						trf = Matrix3D.Multiply(trf, ModelNode.LocalToGlobal);
					var trfpoints = new List<Point3D>(pCol.Count);
					foreach(var pt in pCol)
					{
						trfpoints.Add(trf.Transform(pt) + _localOffset);
					}
					var positions = GLHelpers.Points3DToGLPointArray(trfpoints);
					var uindices = GLHelpers.Ints32ToUintArray(indices);
					//var m3d = GLHelpers.MediaMatrixToGLMatrix(trf);
					//MeshTools.TransformPointsInPlace(positions, m3d);
					var mesh = new Mesh3D { Indices = uindices, Positions = positions };
#if NETCoreOnly
					var color = GLHelpers.ColorFromSDKColor(sm.Color);
#else
					var color = GLHelpers.ColorFromMediaColor(sm.Color);
#endif
					var opacity = (float)sm.Opacity;
					if (opacity == 0.7f) //default valos opacity
						opacity = 1;
					if(!isSecondOperand)
					{
						var bounds = mesh.ComputeAxisAlignedBoundingBox();
						midPoints.Add(new Vector2(bounds.Center.X, bounds.Center.Y));
						box.Unite(ref bounds);
					}
					RenderMesh rmesh = new RenderMesh { Mesh = mesh, Color = color, Opacity = opacity };
					_meshes3D.Add(rmesh);
					GLModelPartsContainer.MeshCounter++;
					GLModelPartsContainer.PointsCounter += positions.Length;
				}
			}
			OutGeom = new OuterGeom { Bounds = box, MidPoints = midPoints.ToList() };
		}
		public override void HitTest(HitTestParams hitParams)
		{
			if(IsVisible)
				base.HitTest(hitParams);
		}

		public BaseDataNode AttachedNode => ModelNode;
		public virtual string HoverText { get; }
		internal bool IsTransparent { get; private set; } = false;
		internal bool IsTemporary { get; set; } = false;

		public void RegenerateAllGeoms()
		{
			CreateRenderMeshes();
			_isModelGeometryDirty = true;
		}
		protected override void Regenerate(RenderEventArgs e)
		{
			_models.ForEach(_ => _.Dispose());
			_models.Clear();
			var texture = GetTexture();
			foreach (var rmesh in _meshes3D)
			{
				var renderMesh = new GLMeshObject(rmesh.Mesh, IsTemporary ? false : UseMeshPool);
				renderMesh.Texture = texture;
				Material = new ColoredMaterial { Color = rmesh.Color };
				renderMesh.Material = Material;
				renderMesh.Opacity = rmesh.Opacity;
				IsTransparent |= renderMesh.Opacity < 1;
				AppendModel(renderMesh);
			}
			base.Regenerate(e);
		}
		protected Texture2D GetTexture()
		{
			var imageBrush = ModelNode.GetChildNode<Epx.BIM.Models.CoatingImageBrush>();
			Texture2D texture = null;
			if(imageBrush != null)
			{
				using(var memstr = new System.IO.MemoryStream(imageBrush.ImageAsBytes))
				{
					System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(memstr);
					BitmapTexture.CreateCompatible(bmp, out texture);
					texture.LoadBitmap(bmp);
					texture.GenerateMipMaps();
				}
			}
			return texture;

		}
		public override void Render(RenderEventArgs e)
		{
			base.Render(e);

			//bool needRender = e.Reason != RepaintReason.MouseMoved || ColorChanged;
			//if (needRender)
			//{
			//	base.Render(e);
			//	ColorChanged = false;
			//}
		}

		//ISnappable
		public Epx.BIM.GridMesh.SnapPoint3D GetSnapPoint(Point3D referencePoint, double tolerance, IEnumerable<Epx.BIM.GridMesh.SnapType> enabledSnaps)
		{
			bool useOverride = (AttachedNode as Epx.BIM.Models.ModelBaseNode).Outline3DOverride != null;
			var modelPoint = (AttachedNode as Epx.BIM.Models.ModelBaseNode).GetSnapPoint3D(referencePoint, tolerance, enabledSnaps, useOverride, true);
			modelPoint.Node = AttachedNode;
			return modelPoint;
		}
	}
}
