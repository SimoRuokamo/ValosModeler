
using Epx.BIM.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using ValosModeler.Views.Model3DView.Visuals;
using GLGraphicsLib;
using OpenTK.Graphics.OpenGL;
using System.Windows.Media.Media3D;
using ValosModeler;

namespace ValosModeler.Views.Model3DView
{
    /// <summary>
    /// Must be disposed.
    /// </summary>
    public class GLModelPartsContainer : VisualModelRoot
	{
		OcclusionChecker occlChecker = new OcclusionChecker();
		public static int MeshCounter = 0;
		public static int PointsCounter = 0;
		public HashSet<ModelBaseNode> ModelNodes { get; private set; } = new HashSet<ModelBaseNode>();
		private Dictionary<ModelBaseNode, GLModelPartVisual>  modelObjects = new Dictionary<ModelBaseNode, GLModelPartVisual>();
		//private Dictionary<ModelBaseNode, Node3DViewModel> _viewModels = new Dictionary<ModelBaseNode, Node3DViewModel>();
		public Box3 ModelBounds { get; private set; } = Box3.Empty;
		private Vector3D _localoffset = new Vector3D();
		protected List<GLVisual> _toDispose = new List<GLVisual>();
		public GLModelPartsContainer(IRenderingEngine renderViewport) : base(renderViewport)
		{
			GLModelPartVisual.UseMeshPool = true;
			////////////////////////////
			//PointsConverters.PointsTest();
			//////////////////////////////
			SetReferencePlane(new Vector3(0, 0, -0.01f), Vector3.UnitZ);
			Node3DViewModel.MediatorStatic.Register(this);
		}
		public void CreateModel(bool resetRefPlane = true)
		{
			_localoffset = new Vector3D();
			MeshCounter = 0;
			PointsCounter = 0;
			_toDispose.AddRange(_children);
			_children.Clear();
			modelObjects.Clear();
			//estimate offset
			var nodebounds = Rect3D.Empty;
			foreach (var node in ModelNodes)
			{
				if (node.Geometry3D != null && node is Model3DNode mnode)
				{
					var origin = mnode.Origin;
					nodebounds.Union(origin.WinPoint());
				}
			}
			if (!nodebounds.IsEmpty && ((Vector3D)nodebounds.Location).Length > 1.0e6 && resetRefPlane)//code to process huge offsets in imported models
				_localoffset = -(Vector3D)nodebounds.Location;
			var bounds = Box3.Empty;
			var gridSetAlready = false;
			foreach (var node in ModelNodes)
			{
				//var exist = _children.OfType<IModelPartVisual>().Any(p => p.AttachedNode == node);
				var exist = modelObjects.ContainsKey(node);
				if (node.Geometry3D != null && !exist)
				{
					var glVisual = new GLModelPartVisual(node, _localoffset.BimPoint());
					var box = glVisual.OutGeom.Bounds;
					if (!box.IsEmpty)
					{
						AddVisualChild(glVisual);
						modelObjects[node] = glVisual;
						bounds.Unite(ref box);
					}
				}
				if (node is Epx.BIM.GridMesh.GridMesh gr)
				{
					var id = gr.UniqueID;
					if (gridSetAlready)
						gr.IsCurrentGrid = false;
				}
				if (node is Epx.BIM.GridMesh.GridMesh grid && grid.IsCurrentGrid)
				{
					var id = grid.UniqueID;
					var gbox = SetReferencePlane(grid);
					resetRefPlane = false;
					bounds.Unite(ref gbox);
					gridSetAlready = true;
				}
				if (bounds.Max.X > 60000)
				{

				}
			}
			if (resetRefPlane || ReferencePlane == null)
			{
				var minZ = bounds.Min.Z;
				var size = Math.Max(bounds.Max.X - bounds.Min.X, bounds.Max.Y - bounds.Min.Y);
				SetReferencePlane(new Vector3(bounds.Center.X, bounds.Center.Y, minZ), Vector3.UnitZ, (float)size);
			}
			else if (ReferencePlane != null)
			{
				if (!_children.Contains(ReferencePlane))
					_children.Add(ReferencePlane);
			}
			//set bounds for camera placement
			if (ModelNodes.Count > 0 && Transform.IsIdentity) //set transform once
			{
				var center = bounds.Center;
				Transform = new GLTransform(Matrix4.CreateTranslation(-center));
				bounds.Translate(-center);
			}
			ModelBounds = bounds;

			_isModelGeometryDirty = true;
			EnqueueRepaintRequest();
		}

		public Box3 SetReferencePlane(Epx.BIM.GridMesh.GridMesh gridNode)
		{
			Box3 bounds = Box3.Empty;
			if (_refPlane != null)
				RemoveVisualChild(_refPlane);
			_refPlane = new GLGridVisual();
			(_refPlane as GLGridVisual).SetDataFromGridNode(gridNode);
			AddVisualChild(_refPlane);

			var lines = gridNode.Grid3DLinesInFirstDirection;
			lines.AddRange(gridNode.Grid3DLinesInSecondDirection);
			foreach (var line in lines)
			{
				bounds.Inflate(line.StartPoint.ToGLPoint());
				bounds.Inflate(line.EndPoint.ToGLPoint());
			}
			return bounds;
		}
		public void SetVisibleNodes(IEnumerable<ModelBaseNode> nodes)
		{
			lock (RenderLock)
			{
				System.Diagnostics.Stopwatch counter = new System.Diagnostics.Stopwatch();
				counter.Start();
				bool emptyDB = ModelNodes.Count == 0 || (ModelNodes.Count == 1 && ModelNodes.FirstOrDefault() is Epx.BIM.GridMesh.GridMesh);
				var newNodes = nodes.Except(ModelNodes).ToList();
				var removedNodes = ModelNodes.Except(nodes).ToList();
				if (removedNodes.Count() > 0)
				{
					foreach (var node in removedNodes)
						RemoveNodeGeometry(node);
					_isModelGeometryDirty = removedNodes.Count > 10;
				}
				bool requireRecreate = newNodes.Count() > 1000;// && newNodes.Count() > ModelNodes.Count * 2;
				if (emptyDB || requireRecreate)
				{
					foreach (var node in nodes)
						ModelNodes.Add(node);
					CreateModel(emptyDB); //reset ref plane if empty
					if (newNodes.Count > 1)
						ResetCamera(ModelBounds);
				}
				else if (newNodes.Count() > 0)
				{
					foreach (var node in newNodes)
						AddNodeGeometry(node);
					if(_currentDragMode == DragMode.Commited)
						_currentDragMode = DragMode.None;
					if(_currentDragMode == DragMode.None)
						_isModelGeometryDirty = true;
				}
				counter.Stop();
				var consumed = counter.ElapsedMilliseconds;
			}
			RenderViewport.RenderRequest(RepaintReason.ModelChanged);
		}
		#region Mediator events

		private enum DragMode
		{
			None,
			Started,
			Commited,
			Cancelled
		}
		private DragMode _currentDragMode = DragMode.None;

		[Infrastructure.MediatorMessageSink(Infrastructure.Events.DesignCommand.StartDragging)]
		public void OnStartDragObject(Infrastructure.Events.DesignCommandPayload param)
		{
			_currentDragMode = DragMode.Started;
		}
		[Infrastructure.MediatorMessageSink(Infrastructure.Events.DesignCommand.CommitDragging)]
		public void OnCommitDragObject(Infrastructure.Events.DesignCommandPayload param)
		{
			_currentDragMode = DragMode.Commited;
		}
		[Infrastructure.MediatorMessageSink(Infrastructure.Events.DesignCommand.CancelDragging)]
		public void OnCancelDragObject(Infrastructure.Events.DesignCommandPayload param)
		{
			_currentDragMode = DragMode.Cancelled;
		}


		[Infrastructure.MediatorMessageSink(Infrastructure.Events.Update3D.MessageID)]
		public void OnUpdate3D(Infrastructure.Events.GeneralNodePayload param)
		{
			lock (RenderLock)
			{
				EnqueueRepaintRequest();
			}
		}

		[Infrastructure.MediatorMessageSink(Infrastructure.Events.Update3D.MessageIDGeometryOnly)]
		public void OnUpdateGeometry(Infrastructure.Events.GeneralNodePayload param)
		{
			lock (RenderLock)
			{
				//must be call on actual update, not add new geoms
				var nodes = param.DataNodes.OfType<ModelBaseNode>();
				foreach (var node in nodes)
				{
					//var exist = _children.OfType<IModelPartVisual>().Any(p => p.AttachedNode == node) || _children.OfType<IGridMeshVisual>().Any(p => p.AttachedGrid == node);
					var exist = modelObjects.ContainsKey(node);
					if (exist)
					{
						RemoveNodeGeometry(node);
						AddNodeGeometry(node);
					}
				}
			}
		}

		[Infrastructure.MediatorMessageSink(Infrastructure.Events.ModelChanged.NodeRemoved)]
		public void OnNodeRemoved(Infrastructure.Events.GeneralNodePayload param)
		{

		}

		[Infrastructure.MediatorMessageSink(Infrastructure.Events.NodeSelected.MessageID)]
		public void OnObjectSelected(Infrastructure.Events.GeneralNodePayload param)
		{
			SetNodeSelected(param.DataNode);
		}
		#endregion

		private void ResetCamera(Box3 modelbox)
		{
			if (!modelbox.IsEmpty)
			{
				var sizeVec = modelbox.Size;
				var size = sizeVec.Length;
				var camera = RenderViewport.Camera;
				camera.DefaultState.Target = new Vector3(modelbox.Center.X, modelbox.Center.Y, modelbox.Min.Z);
				var elevation = Math.Max(sizeVec.Z, size / 2);
				camera.DefaultState.Position = camera.DefaultState.Target + new Vector3(-size, -size, elevation);
				camera.NearPlaneDistance = 0;
				camera.FarPlaneDistance = size * 20;
				camera.DefaultState.Width = size;
				camera.ResetToDefault();
				EnqueueRepaintRequest();
			}

		}
		public void RemoveNodeGeometry(ModelBaseNode node)
		{
			lock (RenderLock)
			{
				if (node != null)
				{
					RemoveNodeGeometryInternal(node);
					var children = node.GetDescendantNodes<ModelBaseNode>();
					children.ForEach(c => RemoveNodeGeometryInternal(c));
					ModelNodes.Remove(node);
					EnqueueRepaintRequest();
				}
			}
		}

		private void RemoveNodeGeometryInternal(ModelBaseNode node)
		{
			//var visual = _children.OfType<GLModelPartVisual>().FirstOrDefault(c => c.ModelNode == node);
			if(modelObjects.TryGetValue(node, out var visual))
			//if (visual != null)
			{
				RemoveVisualChild(visual);
				modelObjects.Remove(node);
				_toDispose.Add(visual);
			}
		}

		public void AddNodeGeometry(ModelBaseNode node)
		{
			lock (RenderLock)
			{
				if (!ModelNodes.Contains(node))
				{
					if (node is IParentBooleanGeometry)
					{
						//var visual = _children.OfType<GLModelPartVisual>().FirstOrDefault(c => c.ModelNode == node.Parent);
						if(modelObjects.TryGetValue(node, out var visual))
						{
							visual?.RegenerateAllGeoms();
							foreach(var child in node.Parent.Children)
							{
								if(child is ModelBaseNode mc)
								{
									//visual = _children.OfType<GLModelPartVisual>().FirstOrDefault(c => c.ModelNode == child);
									if(modelObjects.TryGetValue(mc, out visual))
										visual?.RegenerateAllGeoms();
								}
							}
						}
					}
					//else
					{
						AddNodeGeometryInternal(node);
						var children = node.GetDescendantNodes<ModelBaseNode>();
						children.ForEach(c => AddNodeGeometryInternal(c));
					}
					EnqueueRepaintRequest();
					ModelNodes.Add(node);
				}
			}
		}
		bool repaintRequested = false;
		private void EnqueueRepaintRequest()
		{
			RenderViewport.RenderRequest(RepaintReason.ModelChanged);
			if (!repaintRequested && Application.Current != null && Application.Current.Dispatcher != null)
			{
				//repaintRequested = true;
				//Application.Current.Dispatcher.BeginInvoke(new Action(() =>
				//{
				//	RenderViewport.RenderRequest(RepaintReason.ModelChanged);
				//	repaintRequested = false;
				//}));
			}
		}

		private void AddNodeGeometryInternal(ModelBaseNode node)
		{
			//var exist = _children.OfType<IModelPartVisual>().Any(p => p.AttachedNode == node);
			var exist = modelObjects.ContainsKey(node);
			if (node.Geometry3D != null && !exist)
			{
				var glVisual = new GLModelPartVisual(node, _localoffset.BimPoint());
				glVisual.IsTemporary = _currentDragMode == DragMode.Started;
				var box = glVisual.OutGeom.Bounds;
				if (!box.IsEmpty)
				{
					AddVisualChild(glVisual);
					modelObjects[node] = glVisual;
				}
			}
		}

		private void SetNodeSelected(Epx.BIM.BaseDataNode node)
		{
			lock (RenderLock)
			{
				if (RenderViewport is IModelEditorContainer viewport)
				{
					var parts = _children.OfType<IModelPartVisual>();
					IModelPartVisual partVisual = null;
					foreach (var part in parts)
					{
						part.IsSelected = false;
						part.IsHighlighted = false;
						if (part.AttachedNode == node)
						{
							partVisual = part;
						}
					}
					//need to call this in UI thread
					System.Windows.Threading.Dispatcher.CurrentDispatcher.BeginInvoke(new Action(() =>
					{
						if (partVisual != null)
						{
							partVisual.IsHighlighted = false;
							if (viewport.SelectedNodes.Contains(node))
								partVisual.IsSelected = true;
							else
								partVisual.IsSelected = false;
						}
						//var select2 = parts.Where(_ => _.IsSelected).ToList();
						EnqueueRepaintRequest();
					}));
					EnqueueRepaintRequest();
				}
			}
		}
		public override void Render(RenderEventArgs e)
		{
			var wasEnabled = IsHitTestEnabled;
			IsHitTestEnabled = false;
			lock (RenderLock)
			{
				if (_isModelGeometryDirty || IsDisposed)
				{
					_toDispose.ForEach(_ => _.Dispose());
					_toDispose.Clear();
					RegenerateScene(e);
					_isModelGeometryDirty = false;
					IsDisposed = false;
				}
				RenderModels(e);
				//_refPlane.Render(e);
				var allparts = _children.Where(_ => _ is GLModelPartVisual mp && mp.IsVisible).Cast<GLModelPartVisual>();
				//var selected= _children.OfType<IModelPartVisual>();
				//var selectedPart = selected.FirstOrDefault(_ => _.IsSelected);
				//allparts = allparts.Where(_ => _.IsVisible);
				var opaqueParts = allparts.Where(_ => !_.IsTransparent);

				var pos = e.CurrentCamera.State.Position;
				var target = e.CurrentCamera.State.Target;
				var dir = (pos - target).Normalized() * VisualModelRoot.SCENE_SIZE; // outer dir
				var eye = target + dir;
				var eye2d = new Vector2(eye.X, eye.Y);
				var transpParts = allparts.Where(_ => _.IsTransparent);
				var sortedTransp = transpParts.OrderByDescending(p => MaxDistance(eye2d, p.OutGeom.MidPoints));
				//firstly render opaque parts
				//var numOpaq = opaqueParts.Count();
				foreach (var part in opaqueParts)
					part.Render(e);
				//render transparent parts from distant to nearest
				//var numTransp = sortedTransp.Count();
				foreach (var part in sortedTransp)
					part.Render(e);
			}
			IsHitTestEnabled = wasEnabled;
		}

		private float MaxDistance(Vector2 eye2d, IEnumerable<Vector2> points)
		{
			if (points.Count() > 0)
				return points.Max(p => (eye2d - p).LengthFast);
			return VisualModelRoot.SCENE_SIZE * 10;

		}

		protected override void Dispose(bool manual) //dispose GL resources only
		{
			if (!IsDisposed && manual)
			{
				base.Dispose(manual);
				_models.ForEach(_ => _.Dispose());
				_children.ForEach(_ => _.Dispose());
				IsDisposed = true;
			}
		}
		/// <summary>
		/// Must be call on document closed 
		/// </summary>
		public void DisposeFinally()
		{
			Node3DViewModel.MediatorStatic.Unregister(this);
		}
	}

}
