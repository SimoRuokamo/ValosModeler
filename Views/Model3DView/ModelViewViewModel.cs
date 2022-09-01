using Enterprixe.WPF.Tools.Localization;
using Enterprixe.WPF.Tools.Viewport;
using Epx.BIM;
using Epx.BIM.GridMesh;
using Epx.BIM.GridMesh.DXF;
using Epx.BIM.Models;
using Epx.BIM.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using ValosModeler.Infrastructure;
using ValosModeler.Views.Model3DView.Visuals;
using ValosModeler;

namespace ValosModeler.Views.Model3DView
{
    public class ModelViewViewModel : ViewModelBase
	{
		/// <summary>
		/// The _data model
		/// </summary>
		private DataModel _dataModel;
		/// <summary>
		/// The _filter node
		/// </summary>
		private BaseDataNode _filterNode; // starting node for window contents

		/// <summary>
		/// Initializes a new instance of the <see cref="ModelWindowViewModel"/> class.
		/// </summary>
		/// <param name="dataModel">The data model.</param>
		public ModelViewViewModel(DataModel dataModel) : base(dataModel)
		{
			//Register all decorated methods to the Mediator
			RegisterToMediator();
			_dataModel = dataModel;
			_filterNode = dataModel;

			ViewController viewController = new ViewController();
			viewController.Camera3D = Camera3D;
			viewController.CameraLeft = CameraX;
			viewController.CameraRight = CameraY;
			viewController.CameraTop = CameraZ;
			ViewportController = viewController;

			InitializeOverlay();

			if (WindowTitleNodePath != null)
				WindowTitle = _WindowTitle + " - " + WindowTitleNodePath;

			System.Windows.Threading.Dispatcher.CurrentDispatcher.BeginInvoke(
				(Action)delegate
				{
					var target = DesignFile.GetTargetFolder();
					if (target != null)
						SetContextualNode(target);
				});
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ModelWindowViewModel"/> class.
		/// </summary>
		/// <param name="dataModel">The data model.</param>
		/// <param name="filterNode">The filter node.</param>
		public ModelViewViewModel(DataModel dataModel, BaseDataNode filterNode)
			: this(dataModel)
		{
			_filterNode = filterNode != null ? filterNode : dataModel;
			if (WindowTitleNodePath != null)
				WindowTitle = _WindowTitle + " - " + WindowTitleNodePath;
		}

		/// <summary>
		/// Gets the design file.
		/// </summary>
		/// <value>The design file.</value>
		public DataModel DesignFile
		{
			get { return _dataModel; }
		}

		/// <summary>
		/// Gets the filter node.
		/// </summary>
		/// <value>The filter node.</value>
		public BaseDataNode FilterNode
		{
			get { return _filterNode; }
		}

		/// <summary>
		/// The _ window title
		/// </summary>
		private string _WindowTitle = CultureManager.GetLocalizedString("Model View");
		

		/// <summary>
		/// Gets the window title node path.
		/// </summary>
		/// <value>The window title node path.</value>
		public string WindowTitleNodePath
		{
			get { return _filterNode is DataModel ? null : _filterNode.GetPathUpToNode<Project>(); }
		}

		#region Viewport

		/// <summary>
		/// Gets the camera3 d.
		/// </summary>
		/// <value>The camera3 d.</value>
		public OrthographicCamera Camera3D
		{
			get
			{
				OrthographicCamera camera = new OrthographicCamera();
				camera.Position = new Point3D(-10000, -10000, 7000);
				Vector3D lookDir = new Vector3D(2, 2, -1); // LookDirection and UpDirection must be orthogonal
				lookDir.Normalize();
				camera.LookDirection = lookDir;
				camera.UpDirection = new Vector3D(0.5, 0.5, 2);
				camera.Width = 15000;
				camera.Position = camera.Position - lookDir * 300000; // Set orthocam Position so it is around it's Width distance away from the target (here 300000 is the MaxModelSize of the viewport).
				return camera;
			}
		}

		/// <summary>
		/// The _viewport grid cams x
		/// </summary>
		private Dictionary<Guid, OrthographicCamera> _viewportGridCamsX = new Dictionary<Guid, OrthographicCamera>();
		/// <summary>
		/// Gets the camera x.
		/// </summary>
		/// <value>The camera x.</value>
		public OrthographicCamera CameraX
		{
			get
			{
				var currentGrid = CurrentGridFromData;
				if (currentGrid != null)
				{
					OrthographicCamera existingCamera;
					if (!_viewportGridCamsX.TryGetValue(currentGrid.UniqueID, out existingCamera))
					{
						OrthographicCamera camera = new OrthographicCamera();
						var axis = currentGrid.XAxis;
						axis.Normalize();
#if NETCoreOnly
						camera.Position = (currentGrid.Origin - axis * 10000).WinPoint();
						camera.LookDirection = axis.WinPoint();
						camera.UpDirection = currentGrid.ZAxis.WinPoint();
#else
						camera.Position = currentGrid.Origin - axis * 300000;
						camera.LookDirection = axis * 1;
						var zaxis = currentGrid.ZAxis;
						zaxis.Normalize();
						camera.UpDirection = zaxis;

#endif

						camera.Width = 15000;
						_viewportGridCamsX[currentGrid.UniqueID] = camera;
						return camera;
					}
					else
					{
						return existingCamera;
					}
				}
				else
				{
					return CameraXDefault;
				}
			}
		}
		/// <summary>
		/// Gets the camera x default.
		/// </summary>
		/// <value>The camera x default.</value>
		public OrthographicCamera CameraXDefault
		{
			get
			{
				{
					OrthographicCamera camera = new OrthographicCamera();
					camera.Position = new Point3D(-300000, 0, 0);
					camera.LookDirection = new Vector3D(1, 0, 0);
					camera.UpDirection = new Vector3D(0, 0, 1);
					camera.Width = 15000;
					return camera;
				}
			}
		}

		/// <summary>
		/// The _viewport grid cams y
		/// </summary>
		private Dictionary<Guid, OrthographicCamera> _viewportGridCamsY = new Dictionary<Guid, OrthographicCamera>();
		/// <summary>
		/// Gets the camera y.
		/// </summary>
		/// <value>The camera y.</value>
		public OrthographicCamera CameraY
		{
			get
			{
				var currentGrid = CurrentGridFromData;
				if (currentGrid != null)
				{
					OrthographicCamera existingCamera;
					if (!_viewportGridCamsY.TryGetValue(currentGrid.UniqueID, out existingCamera))
					{
						OrthographicCamera camera = new OrthographicCamera();
						var axis = currentGrid.YAxis;
						axis.Normalize();
#if NETCoreOnly
						camera.Position = (currentGrid.Origin - axis * 10000).WinPoint();
						camera.LookDirection = axis.WinPoint();
						camera.UpDirection = currentGrid.ZAxis.WinPoint();
#else
						camera.Position = currentGrid.Origin - axis * 300000;
						camera.LookDirection = axis * 1;
						var zaxis = currentGrid.ZAxis;
						zaxis.Normalize();
						camera.UpDirection = zaxis;

#endif

						camera.Width = 15000;
						_viewportGridCamsY[currentGrid.UniqueID] = camera;
						return camera;
					}
					else
					{
						return existingCamera;
					}
				}
				else
				{
					return CameraYDefault;
				}
			}
		}
		/// <summary>
		/// Gets the camera y default.
		/// </summary>
		/// <value>The camera y default.</value>
		public OrthographicCamera CameraYDefault
		{
			get
			{
				{
					OrthographicCamera camera = new OrthographicCamera();
					camera.Position = new Point3D(0, -300000, 0);
					camera.LookDirection = new Vector3D(0, 1, 0);
					camera.UpDirection = new Vector3D(0, 0, 1);
					camera.Width = 15000;
					//camera.FarPlaneDistance = 1000000;
					return camera;
				}
			}
		}

		/// <summary>
		/// The _viewport grid cams z
		/// </summary>
		private Dictionary<Guid, OrthographicCamera> _viewportGridCamsZ = new Dictionary<Guid, OrthographicCamera>();
		/// <summary>
		/// Gets the camera z.
		/// </summary>
		/// <value>The camera z.</value>
		public OrthographicCamera CameraZ
		{
			get
			{
				var currentGrid = CurrentGridFromData;
				if (currentGrid != null)
				{
					OrthographicCamera existingCamera;
					if (!_viewportGridCamsZ.TryGetValue(currentGrid.UniqueID, out existingCamera))
					{
						OrthographicCamera camera = new OrthographicCamera();
						var zAxis = currentGrid.ZAxis;
						zAxis.Normalize();

#if NETCoreOnly
						camera.Position = (currentGrid.Origin - zAxis * 10000).WinPoint();
						camera.LookDirection = -zAxis.WinPoint();
						camera.UpDirection = currentGrid.YAxis.WinPoint();
#else
						camera.Position = currentGrid.Origin + zAxis * 300000;
						camera.LookDirection = zAxis * -1;
						var yaxis = currentGrid.YAxis;
						yaxis.Normalize();
						camera.UpDirection = yaxis;

#endif

						camera.Width = 15000;
						_viewportGridCamsZ[currentGrid.UniqueID] = camera;
						return camera;
					}
					else
					{
						return existingCamera;
					}
				}
				else
				{
					return CameraZDefault;
				}
			}
		}
		/// <summary>
		/// Gets the camera z default.
		/// </summary>
		/// <value>The camera z default.</value>
		public OrthographicCamera CameraZDefault
		{
			get
			{
				{
					OrthographicCamera camera = new OrthographicCamera();
					camera.Position = new Point3D(0, 0, 300000);
					camera.LookDirection = new Vector3D(0, 0, -1);
					camera.UpDirection = new Vector3D(0, 1, 0);
					camera.Width = 15000;
					return camera;
				}
			}
		}

		/// <summary>
		/// Gets or sets the viewport controller.
		/// </summary>
		/// <value>The viewport controller.</value>
		public ViewController ViewportController { get; set; }

		/// <summary>
		/// Gets or sets the viewport view mode.
		/// </summary>
		/// <value>The viewport view mode.</value>
		public View3DMode ViewportViewMode
		{
			get { return ViewportController.ViewMode; }
			set
			{
				if (value == View3DMode.Top)
					ViewportController.CameraTop = CameraZ; // use getter so that an up-to-date camera is retrieved (grid may have changed)
				else if (value == View3DMode.Left)
					ViewportController.CameraLeft = CameraX;
				else if (value == View3DMode.Right)
					ViewportController.CameraRight = CameraY;

				ViewportController.ViewMode = value;
			}
		}

		/// <summary>
		/// Sets the viewport view cam3 d.
		/// </summary>
		/// <param name="viewCam">The view cam.</param>
		public void SetViewportViewCam3D(View3DMode viewCam)
		{
			if (ViewportViewMode == View3DMode.Mode3D)
			{
				if (viewCam == View3DMode.Top)
					ViewportController.SetNewCurrentCamera(CameraZDefault); // get default cams
				else if (viewCam == View3DMode.Left)
				{
					ViewportController.SetNewCurrentCamera(CameraXDefault);
				}
				else if (viewCam == View3DMode.Right)
					ViewportController.SetNewCurrentCamera(CameraYDefault);
			}
		}

		/// <summary>
		/// Reset grid specific cameras when the grid changes.
		/// </summary>
		/// <param name="grid">The grid.</param>
		private void ResetViewportCameras(Model3DNode grid)
		{
			bool removed = false;
			removed = removed || _viewportGridCamsX.Remove(grid.UniqueID);
			removed = removed || _viewportGridCamsY.Remove(grid.UniqueID);
			removed = removed || _viewportGridCamsZ.Remove(grid.UniqueID);
			if (removed) ViewportViewMode = ViewportViewMode;
		}

		/// <summary>
		/// Set from the viewport. For info about viewport dimension.
		/// </summary>
		/// <value>The actual width of the viewport.</value>
		//public double ViewportActualWidth { get; set; }
		/// <summary>
		/// Set from the viewport. For info about viewport dimension.
		/// </summary>
		/// <value>The actual height of the viewport.</value>
		//public double ViewportActualHeight { get; set; }

		/// <summary>
		/// Indicates if the relative input point is being set.
		/// </summary>
		//public bool SetRelativeInputPoint { get; set; }

#endregion // Viewport

#region Displayed parts

		/// <summary>
		/// The _displayed parts
		/// </summary>
		private List<ModelBaseNode> _displayedParts = new List<ModelBaseNode>();
		/// <summary>
		/// Gets the displayed parts.
		/// </summary>
		/// <value>The displayed parts.</value>
		public List<ModelBaseNode> DisplayedParts
		{
			get
			{
				_displayedParts = new List<ModelBaseNode>();

				_displayedParts.AddRange(_filterNode.GetDescendantNodes<Model3DNode>().Where(
					p => !(p is Epx.BIM.Models.Geometry.ModelGeometry3D)).ToList());
				_displayedParts.AddRange(_filterNode.GetDescendantNodes<Model2DNode>());
				if (_displayedParts.Any(n => n is Epx.BIM.Models.ModelFolderNode))
				{

				}
				return _displayedParts;
			}
			private set
			{
				if (_displayedParts != value)
				{
					_displayedParts = value;
					OnPropertyChanged("DisplayedParts");
				}
			}
		}

		/// <summary>
		/// The _ helper objects
		/// </summary>
		private List<object> _HelperObjects = new List<object>();
		/// <summary>
		/// Helper objects to draw in 3d model.
		/// </summary>
		/// <value>The helper objects.</value>
		public List<object> HelperObjects
		{
			get
			{
				return _HelperObjects;
			}
			set
			{
				if (_HelperObjects != value)
				{
					_HelperObjects = value;
					OnPropertyChanged("HelperObjects");
				}
			}
		}

		private List<Epx.BIM.ReferencePlanes.ReferencePlanes> _displayedPlanes;
		/// <summary>
		/// Gets the displayed reference planes.
		/// </summary>
		/// <value>The displayed reference planes.</value>
		public List<Epx.BIM.ReferencePlanes.ReferencePlanes> DisplayedReferencePlanes
		{
			get
			{
				List<Epx.BIM.ReferencePlanes.ReferencePlanes> retVal = new List<Epx.BIM.ReferencePlanes.ReferencePlanes>();

				retVal = retVal.Where(x => x.IsSupremePlane).ToList();
				_displayedPlanes = retVal;
				return retVal;
			}
			private set
			{
				if (_displayedPlanes != value)
				{
					_displayedPlanes = value;
					OnPropertyChanged("DisplayedReferencePlanes");
				}
			}
		}

		/// <summary>
		/// The _current DXF
		/// </summary>
		private DXFMesh _currentDXF = null;
		/// <summary>
		/// Gets or sets the current DXF.
		/// </summary>
		/// <value>The current DXF.</value>
		public DXFMesh CurrentDXF
		{
			get { return _currentDXF; }// == null ? _displayedDXFs.DefaultIfEmpty(null).First() : _currentDXF; }
			set
			{
				if (_currentDXF != value)
				{
					_currentDXF = value;
					OnPropertyChanged("CurrentDXF");
					OnPropertyChanged("DisplayedDXFs");
				}
			}
		}

		/// <summary>
		/// The _displayed dx fs
		/// </summary>
		private List<ArbitraryMesh> _displayedDXFs = new List<ArbitraryMesh>();
		/// <summary>
		/// Gets the displayed dx fs.
		/// </summary>
		/// <value>The displayed dx fs.</value>
		public List<Epx.BIM.GridMesh.ArbitraryMesh> DisplayedDXFs
		{
			get
			{
				_displayedDXFs = new List<ArbitraryMesh>();

				if (_currentDXF != null)
				{
					_displayedDXFs = new List<ArbitraryMesh>() { _currentDXF };
				}

				return _displayedDXFs;
			}
			private set
			{
				if (_displayedDXFs != value)
				{
					_displayedDXFs = value;
					OnPropertyChanged("DisplayedDXFs");
				}
			}
		}
		/// <summary>
		/// Returns grids perpendicular to the camera from the displayed grids.
		/// </summary>
		/// <returns>List&lt;DXFMesh&gt;.</returns>
		public List<DXFMesh> GetPerpendicularDXFs()
		{
			Vector3D camDir = ViewportController.CurrentCamera.LookDirection;
			camDir.Negate();
			List<DXFMesh> removelist = new List<DXFMesh>();
			foreach (var grid in _displayedDXFs.Where(g => !g.Is3D))
			{
				if (grid is Epx.BIM.GridMesh.DXF.DXFMesh)
				{
					var trfZaxis = grid.LocalToGlobal.Transform(grid.ZAxis);
					double angle = Vector3D.AngleBetween(camDir, trfZaxis.WinPoint());
					if (angle > 85 && angle < 95) // don't show perpendicular grid
												  //_displayedDXFs.Remove(grid);
						removelist.Add(grid as Epx.BIM.GridMesh.DXF.DXFMesh);
				}
			}
			return removelist;
		}

		/// <summary>
		/// The _displayed grids
		/// </summary>
		private List<ModelBaseNode> _displayedGrids = new List<ModelBaseNode>();
		/// <summary>
		/// Gets the displayed grids.
		/// </summary>
		/// <value>The displayed grids.</value>
		public List<ModelBaseNode> DisplayedGrids
		{
			get
			{
				_displayedGrids = new List<ModelBaseNode>();

				if (_CurrentDisplayedGrids != null)
				{
					_displayedGrids = new List<ModelBaseNode>(_CurrentDisplayedGrids);
				}
				_displayedGrids.AddRange(_filterNode.GetDescendantNodes<Model3DNode>(n => !(n is Valos.Ifc.IfcModel), true).OfType<GridMesh>());

				return _displayedGrids;
			}
			private set
			{
				if (_displayedGrids != value)
				{
					_displayedGrids = value;
					OnPropertyChanged("DisplayedGrids");
				}
			}
		}
		/// <summary>
		/// Returns grids perpendicular to the camera from the displayed grids.
		/// </summary>
		/// <returns>List&lt;GridMesh&gt;.</returns>
		public List<GridMesh> GetPerpendicularGrids()
		{
			Vector3D camDir = ViewportController.CurrentCamera.LookDirection;
			camDir.Negate();
			List<GridMesh> removeList = new List<GridMesh>();
			foreach (var grid in _displayedGrids.Cast<GridMesh>())
			{
				var trfZAxis = grid.LocalToGlobal.Transform(grid.ZAxis);
				double angle = Vector3D.AngleBetween(camDir, trfZAxis.WinPoint());
				if (angle > 85 && angle < 95) // don't show perpendicular grid
					removeList.Add(grid);
			}
			return removeList;
		}

		/// <summary>
		/// Use if not setting but instead adding.
		/// </summary>
		public void UpdateDisplayedGridsAndDXFs()
		{
			OnPropertyChanged("DisplayedGrids");
			OnPropertyChanged("DisplayedDXFs");
			OnPropertyChanged("CurrentDisplayedGrids");
		}

		/// <summary>
		/// The _ current displayed grids
		/// </summary>
		private List<GridMesh> _CurrentDisplayedGrids = null;
		/// <summary>
		/// Temp grids for displaying during a command for example. (See CurrentGrid)
		/// </summary>
		/// <value>The current displayed grids.</value>
		public List<GridMesh> CurrentDisplayedGrids
		{
			get { return _CurrentDisplayedGrids == null ? DisplayedGrids.Cast<GridMesh>().ToList() : _CurrentDisplayedGrids; }
			set
			{

				_CurrentDisplayedGrids = value;
				OnPropertyChanged("DisplayedGrids");
				OnPropertyChanged("CurrentDisplayedGrids");

			}
		}

		/// <summary>
		/// The grid marked as the current one to use from the currently displayed grids.
		/// </summary>
		/// <value>The current grid.</value>
		public GridMesh _currentGrid = null;
		public GridMesh CurrentGrid
		{
			get
			{
				GridMesh retVal = _currentGrid;
				if (retVal == null)
				{
					var grids = CurrentDisplayedGrids;
					if (grids != null)
					{
						var currentGrid = grids.FirstOrDefault(g => g.IsCurrentGrid);
						retVal = currentGrid;
					}
				}
				//if (retVal == null && DesignCommandInProgress)
				//	retVal = DefaultCommandGrid;
				return retVal;
			}
			set => _currentGrid = value;
		}
		/// <summary>
		/// The grid marked as the current one to use from thea actual data grids.
		/// </summary>
		/// <value>The current grid from data.</value>
		protected GridMesh CurrentGridFromData
		{
			get
			{
				var grids = new List<GridMesh>();

				if (grids.Count > 0)
				{
					var currentGrid = grids.FirstOrDefault(g => g.IsCurrentGrid);
					return currentGrid;
				}
				else
					return null;
			}
		}

		/// <summary>
		/// Default grid for commands if one is not found.
		/// </summary>
		/// <value>The default command grid.</value>
		public GridMesh DefaultCommandGrid
		{
			get
			{
				var defaultUCS = new Epx.BIM.GridMesh.GridUCS();
				// give parent reference so that grid will find correct LocalToGlobal transform
				var target = DesignFile.GetTargetFolder();
				if (target != null)
					defaultUCS.Parent = target;
				return defaultUCS;
			}
		}

		#endregion Model/Current parts

		#region ContextualNode
		public bool RegenerateSelectedNode { get; set; } = true;
		/// <summary>
		/// Sets the selected/contextual 3D element and contextual tab. Updates 3D.
		/// </summary>
		/// <param name="node">The node.</param>
		/// <param name="updateProjectTree">Don't update project tree from project tree. (when coming from OnSetContextualElement())</param>
		/// <param name="setContextualTab">if set to <c>true</c> [set contextual tab].</param>
		/// <param name="setContextualElement">if set to <c>true</c> [set contextual element].</param>
		/// <param name="selectContextualTab">if set to <c>true</c> [select contextual tab].</param>
		public void SetContextualNode(BaseDataNode node, bool updateProjectTree = true, bool setContextualTab = true, bool setContextualElement = true, bool selectContextualTab = true)
		{
			BaseDataNode previousContextualNode = ContextualNode;
#if DEBUG
			if (node == null && true == (ContextualNode as ModelBaseNode)?.IsTarget)
				return;
#endif

			List<BaseDataNode> oldContextuals = new List<BaseDataNode>();
			if (ContextualNodes.Count > 1)
			{
				oldContextuals = ContextualNodes.Select(n => Infrastructure.Events.Update3DMappings.GetUpdate3DNode(n)).ToList();
				oldContextuals = oldContextuals.Distinct().ToList();
			}

			// set new contextual node
			ContextualNode = node;
			UpdateSelected(oldContextuals);
			if (previousContextualNode != null)
			{
				UpdateSelected(previousContextualNode);
			}

			// Careful, sent to self.
			//if (setContextualElement) Mediator.NotifyColleaguesAsync<List<BaseDataNode>>(MediatorMessages.SetContextualElement, new List<BaseDataNode>() { node });
			//if (updateProjectTree) Mediator.NotifyColleagues<List<BaseDataNode>>(MediatorMessages.SetProjectTreeSelectedItem, new List<BaseDataNode>() { node });
			if (setContextualElement || updateProjectTree) 
				Infrastructure.Events.NodeSelected.Publish(node, this);

			// redraw new contextual node, not async
			if (ContextualNode != null)
			{
				System.Windows.Threading.Dispatcher.CurrentDispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background,
				(Action)delegate
				{
					UpdateSelected(ContextualNode);
				});
			}
			else // ContextualNode == null
			{
			}

			// Careful, this is sent to self. Don't set async, contextual viewmodel not fast enough to register handler for DesignCommandInProgress event.
			if (setContextualTab && selectContextualTab) Infrastructure.Events.ShowRibbonContextualTabs.Publish(node);// Mediator.NotifyColleagues<BaseDataNode>(MediatorMessages.SetContextualTabSelected, node);
			else if (setContextualTab) Infrastructure.Events.ShowRibbonContextualTabs.Publish(node);

		}

		/// <summary>
		/// Removed if already in list.
		/// </summary>
		/// <param name="node">The node.</param>
		/// <param name="updateProjectTree">if set to <c>true</c> [update project tree].</param>
		/// <param name="updateTabs">if set to <c>true</c> [update tabs].</param>
		/// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
		public bool AddRemoveContextualNode(BaseDataNode node, bool updateProjectTree = true, bool updateTabs = true)
		{
			bool nodeAdded = true;
			if (_ContextualNodes != null)
			{
				if (!_ContextualNodes.Contains(node))
				{
					_ContextualNodes.Add(node);
					// Careful, this is sent to self. Don't set async, contextual viewmodel not fast enough to register handler for DesignCommandInProgress event.
					if (updateTabs) Infrastructure.Events.ShowRibbonContextualTabs.Publish(null);
				}
				else
				{
					_ContextualNodes.Remove(node);
					nodeAdded = false;
				}
			}
			else
			{
				_ContextualNodes = new List<BaseDataNode>() { node };
			}

			OnPropertyChanged("ContextualNodes");
			if (updateProjectTree)
				Infrastructure.Events.NodeSelected.Publish(_ContextualNodes, this);

			// redraw new contextual node
			if (node != null)
			{
				UpdateSelected(node);
			}

			return nodeAdded;
		}

		/// <summary>
		/// Sets the contextual nodes.
		/// </summary>
		/// <param name="selectedNodes">The selected nodes.</param>
		/// <param name="updateProjectTree">if set to <c>true</c> [update project tree].</param>
		/// <param name="updateTabs">if set to <c>true</c> [update tabs].</param>
		public void SetContextualNodes(List<BaseDataNode> selectedNodes, bool updateProjectTree = true, bool updateTabs = true)
		{
			var notSelectedAnymore = _ContextualNodes.Except(selectedNodes).ToList();
			_ContextualNodes = new List<BaseDataNode>();
			UpdateSelected(notSelectedAnymore);
			selectedNodes.ForEach(n => AddRemoveContextualNode(n, updateProjectTree, updateTabs));
		}

		/// <summary>
		/// Resets the contextual nodes.
		/// </summary>
		/// <param name="clearContextualTab">if set to <c>true</c> [clear contextual tab].</param>
		public void ResetContextualNodes(bool clearContextualTab)
		{
			List<BaseDataNode> guidsToDraw = new List<BaseDataNode>();
			if (ContextualNodes.Count > 1)
			{
				guidsToDraw = ContextualNodes.Select(n => Infrastructure.Events.Update3DMappings.GetUpdate3DNode(n)).ToList();
				guidsToDraw = guidsToDraw.Distinct().ToList();
			}

			Infrastructure.Events.NodeSelected.Publish(new List<BaseDataNode>(0), this);
			SetContextualNode(null, false, clearContextualTab, false);
			UpdateSelected(guidsToDraw);
		}

		//updating 3D
		private void UpdateSelected(BaseDataNode node) { if (RegenerateSelectedNode) Infrastructure.Events.Update3D.PublishAsync(node); }
		private void UpdateSelected(IEnumerable<BaseDataNode> nodes) { if (RegenerateSelectedNode) Infrastructure.Events.Update3D.PublishAsync(nodes); }


		//expose removed and added nodes to listeners
		//public IEnumerable<BaseDataNode> JustRemoved { get; private set; }
		//public IEnumerable<BaseDataNode> JustAdded { get; private set; }
		public void RemoveContextualNodes()
		{
			AppModelInstanceManager.Instance.RemoveNodeCommand.Execute(ContextualNodes);
#if DEBUG && OPENGL
			AppModelInstanceManager.Instance.SaveRequired = false;
#endif
		}

		/// <summary>
		/// Use ActiveCommandNode instead.
		/// </summary>
		/// <value>The contextual node.</value>
		public BaseDataNode ContextualNode
		{
			get
			{
				return _ContextualNodes != null && _ContextualNodes.Count > 0 ? _ContextualNodes.First() : null;
			}
			set
			{
				if (value != null)
					ContextualNodes = new List<BaseDataNode>() { value };
				else
					ContextualNodes = new List<BaseDataNode>();
				OnPropertyChanged("ContextualNode");
			}
		}
		/// <summary>
		/// The _ contextual nodes
		/// </summary>
		private List<BaseDataNode> _ContextualNodes = new List<BaseDataNode>();
		/// <summary>
		/// Use ActiveCommandNodes instead. Should not be null.
		/// </summary>
		/// <value>The contextual nodes.</value>
		public List<BaseDataNode> ContextualNodes
		{
			get
			{
				return _ContextualNodes;
			}
			set
			{
				if (_ContextualNodes != value)
				{
					_ContextualNodes = value;
					OnPropertyChanged("ContextualNodes");
				}
			}
		}

#endregion //ContextualNode

#region Design Commands

		public enum DesignCommands
		{
			None,
			RunPlugin,
			EditPlugin
		};
		private DesignCommands _ActiveCommand = DesignCommands.None;
		private bool _isActiveCommandChanging = false;
		/// <summary>
		/// Set from viewport.
		/// </summary>
		public FeatureEngineViewModel FeatureEngine { get; set; }

#if !NETCoreOnly
		public void StartDesignCommand(PluginInfo plugin, bool isEdit = false)
		{
			if (ActiveCommand != DesignCommands.None) return;
			ActiveCommand = isEdit ? DesignCommands.EditPlugin : DesignCommands.RunPlugin;
			FeatureEngine.BeginPlugin(plugin);
		}

		public void EndDesignCommand(bool isCancel = false)
		{
			FeatureEngine.EndRunPlugin(isCancel); // this still reads _ActiveCommand
			ActiveCommand = DesignCommands.None;
		}
			/// <summary>
		/// Called when [end command].
		/// </summary>
		/// <param name="param">The parameter.</param>
		[MediatorMessageSink(Infrastructure.Events.DesignCommand.EndID)]
		public void OnEndCommand(Infrastructure.Events.DesignCommandPayload param)
		{
			// only run the command in the viewmodel which was last active
			if (IsLastActiveModelWindow)
			{
				EndDesignCommand(false);
			}
		}

		[MediatorMessageSink(Infrastructure.Events.DesignCommand.CancelID)]
		public void OnCancelCommand(Infrastructure.Events.DesignCommandPayload param)
		{
			// only run the command in the viewmodel which was last active
			if (IsLastActiveModelWindow)
			{
				EndDesignCommand(true);
			}
		}

		/// <summary>
		/// Called when [run command].
		/// </summary>
		/// <param name="param">The parameter.</param>
		[MediatorMessageSink(Infrastructure.Events.DesignCommand.RunID)]
		internal void OnRunCommand(Infrastructure.Events.DesignCommandPayload param)//(DesignCommandPayload param)
		{
			// only run the command in the viewmodel which was last active
			if (IsLastActiveModelWindow)
			{
				StartDesignCommand(param.Plugin, false);
			}
		}

		[MediatorMessageSink(Infrastructure.Events.DesignCommand.EditID)]
		internal void OnEditCommand(Infrastructure.Events.DesignCommandPayload param)//(DesignCommandPayload param)
		{
			// only run the command in the viewmodel which was last active
			if (IsLastActiveModelWindow)
			{
				StartDesignCommand(param.Plugin, true);
			}
		}
#else
		public void StartDesignCommand(PluginInfo plugin, bool isEdit = false)
		{
		}

		public void EndDesignCommand(bool isCancel = false)
		{
		}

#endif
		/// <summary>
		/// Starts and ends design commands.
		/// </summary>
		public DesignCommands ActiveCommand
		{
			get
			{
				return _ActiveCommand;
			}
			set
			{
				if (_ActiveCommand != value && !_isActiveCommandChanging)
				{
					_isActiveCommandChanging = true;

					if (value != DesignCommands.None)
					{
						DesignCommandInProgress = true;
						//SetCommandStatus(1);
					}
					else
					{
						DesignCommandInProgress = false;
						SetCommandStatus(0);
					}

					_ActiveCommand = value;
					_isActiveCommandChanging = false;
					OnPropertyChanged();
					OnPropertyChanged(nameof(IsAnyCommandInProgress));
				}
				Infrastructure.Events.DesignCommand.Changed(IsAnyCommandInProgress, this);
			}
		}

		private bool _DesignCommandInProgress = false;
		/// <summary>
		/// Is command in progress.
		/// </summary>
		public bool DesignCommandInProgress
		{
			get
			{
				return _DesignCommandInProgress;
			}
			protected set
			{
				if (_DesignCommandInProgress != value)
				{
					_DesignCommandInProgress = value;
					OnPropertyChanged();
				}
			}
		}
		/// <summary>
		/// Set from the viewport. For model edit commands not included inthe DesignCommands. Should be replaced by GenericTrussEdit.
		/// </summary>
		private bool _IsViewportCommandInProgress = false;
		public bool IsViewportCommandInProgress
		{
			get { return _IsViewportCommandInProgress; }
			set
			{
				if (_IsViewportCommandInProgress != value)
				{
					_IsViewportCommandInProgress = value;
					OnPropertyChanged();
					Infrastructure.Events.DesignCommand.Changed(IsAnyCommandInProgress, this);
				}
			}
		}
		/// <summary>
		/// Is a DesignCommand or a Viewport command in progress.
		/// </summary>
		public bool IsAnyCommandInProgress => DesignCommandInProgress || IsViewportCommandInProgress;

		private string _tempStatusText = string.Empty;
		/// <summary>
		/// Sets a temporary text for 3d window command info/statusbar.
		/// </summary>
		/// <param name="text">Unlocalized text.</param>
		public void SetHoverText(string text)
		{
			// don't use SetCommandStatusText() here
			if (!string.IsNullOrEmpty(text))
			{
				_tempStatusText = CommandStatusText;
				CommandStatusText = text;
			}
			else
			{
				CommandStatusText = _tempStatusText;
				_tempStatusText = string.Empty;
			}
		}

		private int _commandStatus = 0;
		public int CommandStatus
		{
			get { return _commandStatus; }
		}
		/// <summary>
		/// Sets the text for 3d window command info/statusbar. Sends AreCommandsEnabledChanged message.
		/// </summary>
		/// <param name="status"></param>
		/// <param name="statusTextOverride">not localized</param>
		public void SetCommandStatus(int status, string statusTextOverride = null)
		{
			string newText = string.Empty;
			if (status >= 0)
			{
				_commandStatus = status;
			}

			// status == -1 resets to previous _commandStatus
			if (status == -2)
			{
				newText = "Select a reference point";
			}
			else if (_commandStatus > 0)
			{
				//if (ActiveCommand == DesignCommands.RunPlugin)
				{
					// plugin localization is handled inside plugin
					// uses statusTextOverride
				}
			}
			else
			{
				newText = string.Empty;
			}

			if (!string.IsNullOrEmpty(statusTextOverride))
			{
				SetCommandStatusText(statusTextOverride);
			}
			else
			{
				SetCommandStatusText(string.IsNullOrEmpty(newText) ? newText : CultureManager.GetLocalizedString(newText));
			}

		}

		private string _CommandStatusText = "";
		/// <summary>
		/// Use SetCommandStatusText() to set. Only SetHoverText uses setter.
		/// </summary>
		public string CommandStatusText
		{
			get
			{
				return !string.IsNullOrWhiteSpace(_CommandStatusText) ? _CommandStatusText : "";
			}
			set
			{
				if (_CommandStatusText != value)
				{
					_CommandStatusText = value;
					OnPropertyChanged("CommandStatusText");
				}
			}
		}
		/// <summary>
		/// Use this to set CommandStatusText. 
		/// </summary>
		/// <param name="text"></param>
		public void SetCommandStatusText(string text)
		{
			CommandStatusText = text;
			_tempStatusText = CommandStatusText; // does not work correctly or does it?
		}

#endregion //Design Commands

#region Mediator

		/// <summary>
		/// Called when [node removed].
		/// </summary>
		/// <param name="param">The parameter.</param>
		[MediatorMessageSink(Infrastructure.Events.ModelChanged.NodeRemoved)]
		public void OnNodeRemoved(Infrastructure.Events.GeneralNodePayload param)
		{
			//JustRemoved = param.DataNodes;
			OnPropertyChanged("DisplayedParts");
			OnPropertyChanged("DisplayedGrids");
			OnPropertyChanged("DisplayedDXFs");
			OnPropertyChanged("DisplayedReferencePlanes");
			//JustRemoved = null;
		}

		/// <summary>
		/// Called when [node added].
		/// </summary>
		/// <param name="param">The parameter.</param>
		[MediatorMessageSink(Infrastructure.Events.ModelChanged.NodeAdded)]
		public void OnNodeAdded(Infrastructure.Events.GeneralNodePayload param)
		{
			//JustAdded = param.DataNodes;
			OnPropertyChanged("DisplayedParts");
			OnPropertyChanged("DisplayedGrids");
			OnPropertyChanged("DisplayedDXFs");
			OnPropertyChanged("DisplayedReferencePlanes");
			//JustAdded = null;
		}

		[MediatorMessageSink(Infrastructure.Events.ModelChanged.MessageID)]
		public void OnModelChanged(Infrastructure.Events.GeneralNodePayload param)
		{
			OnPropertyChanged("DataNode"); //root changed - whole redraw
			OnPropertyChanged("DisplayedParts"); // call for WPF viewport only
			//OnPropertyChanged("DisplayedGrids");
			//OnPropertyChanged("DisplayedDXFs");
			//OnPropertyChanged("DisplayedReferencePlanes");
		}

		/// <summary>
		/// Called when [update3 d].
		/// </summary>
		/// <param name="param">The parameter.</param>
		[MediatorMessageSink(Infrastructure.Events.Update3D.MessageID)]
		public void OnUpdate3D(Infrastructure.Events.GeneralNodePayload param)
		{
			if (param == null || param.DataNode == null)
			{
				OnPropertyChanged("DisplayedParts");
				OnPropertyChanged("DisplayedGrids");
				OnPropertyChanged("DisplayedDXFs");
				OnPropertyChanged("DisplayedReferencePlanes");
				OnPropertyChanged("HelperObjects");
			}
		}

		/// <summary>
		/// Called when [update3 d helpers].
		/// </summary>
		/// <param name="param">The parameter.</param>
		//[MediatorMessageSink(MediatorMessages.Update3DHelpers)]
		public void OnUpdate3DHelpers(string param)
		{
			OnPropertyChanged("HelperObjects");
		}

		/// <summary>
		/// Called when [set contextual element].
		/// </summary>
		/// <param name="parameters">The parameters.</param>
		//[MediatorMessageSink(MediatorMessages.SetContextualElement)]
		public void OnSetContextualElement(List<BaseDataNode> parameters)
		{
			BaseDataNode param = null;
			if (parameters != null && parameters.Count == 1)
				param = parameters.First();

			// Works together with ProjectTreeViewItem.ExecuteMouseLeftButtonUp()

			if (ContextualNode == param && _ContextualNodes.Count == 1) return; // message was sent to self, need to implement sender signature really

			if (parameters.Count == 1)
			{
				SetContextualNode(param, false, false, false);
			}
			else
			{
				SetContextualNodes(parameters, false);
			}
		}

		/// <summary>
		/// Called when [hide contextual node].
		/// </summary>
		/// <param name="node">The node.</param>
		//[MediatorMessageSink(MediatorMessages.HideContextualNode)]
		public void OnHideContextualNode(BaseDataNode node)
		{
			if (ContextualNode == node)
			{
				ContextualNode = null;
			}
		}

	
		/// <summary>
		/// Called when [set contextual tab].
		/// </summary>
		/// <param name="param">The parameter.</param>
		[MediatorMessageSink(Infrastructure.Events.NodeSelected.MessageID)]
		public void OnSetContextualTab(Infrastructure.Events.GeneralNodePayload param)
		{
			if (param.Sender == this) return;
			// careful, this is sent to self
			//if (ContextualNode != param.DataNode && param.DataNode is BaseDataNode && IsLastActiveModelWindow())
			//{
			//}
			OnSetContextualElement(param.DataNodes.ToList());
		}

		/// <summary>
		/// Determines whether [is last active model window].
		/// </summary>
		/// <returns><c>true</c> if [is last active model window]; otherwise, <c>false</c>.</returns>
		public bool IsLastActiveModelWindow
		{
			get
			{
				return ViewModelBase.LastActiveModelView == this;
				//if (!ViewModelBase.LastActiveDockableContent.ContainsKey(typeof(ModelView)))
				//{
				//	return true; // if no model window has been active and we come here assume program started case where model has not been touched yet
				//}
				//else if (ViewModelBase.LastActiveDockableContent.ContainsKey(typeof(ModelView)))
				//{
				//	if (ViewModelBase.LastActiveDockableContent[typeof(ModelView)] == this)
				//	{
				//		return true;
				//	}
				//}
				//return false;
			}
		}

		/// <summary>
		/// Called when [view model node property changed].
		/// </summary>
		/// <param name="payload">The payload.</param>
		public override void OnViewModelNodePropertyChanged(Infrastructure.Events.NodePropertyChangedPayload payload)
		{
			var grid = payload.DataNodes.Where(n => n is GridMesh && _filterNode.IsDescendant(n)).FirstOrDefault() as GridMesh;
			if (grid != null)
			{
				if(payload.ChangedPropertyNames.Contains("Origin"))
					ResetViewportCameras(grid);
				if (payload.ChangedPropertyNames.Contains("IsCurrentGrid") && grid.IsCurrentGrid)
				{
					CurrentGrid = grid;
					OnPropertyChanged("CurrentGrid");
				}
			}
			base.OnViewModelNodePropertyChanged(payload);
		}
#endregion

		#region Commands

		#region EndCommandCommand
		RelayCommand _endCommandCommand;
		public ICommand EndCommandCommand
		{
			get
			{
				if (_endCommandCommand == null)
					_endCommandCommand = new RelayCommand(execute => this.ExecuteEndCommand(execute), canexecute => this.CanExecuteEndCommand(canexecute));
				return _endCommandCommand;
			}
		}

		private bool CanExecuteEndCommand(object parameter)
		{
			//return ActiveCommand != DesignCommands.None;
			return true;
		}

		private void ExecuteEndCommand(object parameter)
		{
			EndDesignCommand(true);
			//if (!SetRelativeInputPoint)
			//	ActiveCommand = DesignCommands.None;
		}
#endregion //EndCommandCommand

#endregion //Commands

#region Snap

		private List<SnapType> _EnabledSnaps = new List<SnapType>()
		{
			SnapType.GridIntersection, SnapType.GridMiddle,
			SnapType.GeometryEdge, SnapType.GeometryEdgeEnds, SnapType.GeometryEdgeMiddle
		};

		public List<SnapType> EnabledSnaps
		{
			get { return _EnabledSnaps; }
			set
			{
				if (_EnabledSnaps != value)
				{
					_EnabledSnaps = value;
					OnPropertyChanged();
				}
			}
		}

		public bool SnapGridLine
		{
			get { return _EnabledSnaps.Contains(SnapType.GridLine); }
			set
			{
				if (SnapGridLine != value)
				{
					if (value) _EnabledSnaps.Add(SnapType.GridLine);
					else _EnabledSnaps.Remove(SnapType.GridLine);
					OnPropertyChanged();
					if (_savedEnabledSnaps != null) _savedEnabledSnaps = null;
				}
			}
		}
		public bool SnapGridIntersection
		{
			get { return _EnabledSnaps.Contains(SnapType.GridIntersection); }
			set
			{
				if (SnapGridIntersection != value)
				{
					if (value) _EnabledSnaps.Add(SnapType.GridIntersection);
					else _EnabledSnaps.Remove(SnapType.GridIntersection);
					OnPropertyChanged();
					if (_savedEnabledSnaps != null) _savedEnabledSnaps = null;
				}
			}
		}
		public bool SnapGridMiddle
		{
			get { return _EnabledSnaps.Contains(SnapType.GridMiddle); }
			set
			{
				if (SnapGridMiddle != value)
				{
					if (value) _EnabledSnaps.Add(SnapType.GridMiddle);
					else _EnabledSnaps.Remove(SnapType.GridMiddle);
					OnPropertyChanged();
					if (_savedEnabledSnaps != null) _savedEnabledSnaps = null;
				}
			}
		}

		public bool SnapGeometryEdgeEnds
		{
			get { return _EnabledSnaps.Contains(SnapType.GeometryEdgeEnds); }
			set
			{
				if (SnapGeometryEdgeEnds != value)
				{
					if (value) _EnabledSnaps.Add(SnapType.GeometryEdgeEnds);
					else _EnabledSnaps.Remove(SnapType.GeometryEdgeEnds);
					OnPropertyChanged();
					if (_savedEnabledSnaps != null) _savedEnabledSnaps = null;
				}
			}
		}
		public bool SnapGeometryEdgeMiddle
		{
			get { return _EnabledSnaps.Contains(SnapType.GeometryEdgeMiddle); }
			set
			{
				if (SnapGeometryEdgeMiddle != value)
				{
					if (value) _EnabledSnaps.Add(SnapType.GeometryEdgeMiddle);
					else _EnabledSnaps.Remove(SnapType.GeometryEdgeMiddle);
					OnPropertyChanged();
					if (_savedEnabledSnaps != null) _savedEnabledSnaps = null;
				}
			}
		}
		public bool SnapGeometryEdge
		{
			get { return _EnabledSnaps.Contains(SnapType.GeometryEdge); }
			set
			{
				if (SnapGeometryEdge != value)
				{
					if (value) _EnabledSnaps.Add(SnapType.GeometryEdge);
					else _EnabledSnaps.Remove(SnapType.GeometryEdge);
					OnPropertyChanged();
					if (_savedEnabledSnaps != null) _savedEnabledSnaps = null;
				}
			}
		}

		private List<ModelBaseNode> _outlineOverrideNodes = new List<ModelBaseNode>();
		/// <summary>
		/// Ghost outline of starting geometry.
		/// </summary>
		public List<ModelBaseNode> OutlineOverrideNodes
		{
			get { return _outlineOverrideNodes; }
		}
		public void AddOutlineOverride(ModelBaseNode node)
		{
			//if (node is MemberIsolatedFormulation || node is MemberPolylineFormulation)
			//{
			//	node = (node as BaseCut).FirstMember; // when editing linecut or polylinecut draw ghost member
			//}

			List<ModelBaseNode> outlinedNodes = new List<ModelBaseNode>();

			//if (node is PlanarStructure)
			//{
			//	outlinedNodes.AddRange((node as PlanarStructure).GetMembers());
			//	outlinedNodes.AddRange((node as PlanarStructure).GetSideMembers());
			//	//outlinedNodes.AddRange((node as PlanarStructure).NailPlatesNode.NailPlates);
			//}
			//else
			{
				outlinedNodes.Add(node);
			}

			foreach (var n in outlinedNodes)
			{
				_outlineOverrideNodes.Add(n);
				n.Outline3DOverride = n.GetOutline3D();
				if (n is Model3DNode)
					n.Outline3DOverrideGlobalToLocal = (n as Model3DNode).GlobalToLocal;
				//else if (n.HasParent<PlanarStructure>())
				//	n.Outline3DOverrideGlobalToLocal = new MatrixTransform3D(n.GetParent<PlanarStructure>().GlobalToLocal);
			}
		}
		/// <summary>
		/// Removes all outline overrides.
		/// </summary>
		/// <param name="node">The node. Not used.</param>
		public void RemoveOutlineOverride(ModelBaseNode node)
		{
			foreach (var n in _outlineOverrideNodes)
			{
				n.Outline3DOverride = null;
#if NETCoreOnly
				n.Outline3DOverrideGlobalToLocal = Epx.BIM.BaseTools.Matrix3D.Identity;
#else
				n.Outline3DOverrideGlobalToLocal = Matrix3D.Identity;
#endif
			}
			_outlineOverrideNodes.Clear();
		}

		private List<SnapType> _savedEnabledSnaps = null;
		//private bool _savedSnapCurrentTrussOnlyValue = false;

		public void ToggleSnapsOnOff()
		{
			if (_savedEnabledSnaps == null)
			{
				_savedEnabledSnaps = new List<SnapType>(_EnabledSnaps);
				_EnabledSnaps = new List<SnapType>();
				OnPropertyChanged("SnapGridIntersection");
				OnPropertyChanged("SnapGridMiddle");
				OnPropertyChanged("SnapGridLine");
				OnPropertyChanged("SnapGeometryEdgeEnds");
				OnPropertyChanged("SnapGeometryEdgeMiddle");
				OnPropertyChanged("SnapGeometryEdge");
				OnPropertyChanged("SnapCurrentTrussOnly");
			}
			else
			{
				_EnabledSnaps = new List<SnapType>(_savedEnabledSnaps);
				OnPropertyChanged("SnapGridIntersection");
				OnPropertyChanged("SnapGridMiddle");
				OnPropertyChanged("SnapGridLine");
				OnPropertyChanged("SnapGeometryEdgeEnds");
				OnPropertyChanged("SnapGeometryEdgeMiddle");
				OnPropertyChanged("SnapGeometryEdge");
				OnPropertyChanged("SnapCurrentTrussOnly");
				_savedEnabledSnaps = null;
			}
		}

		#endregion // Snap

		#region Overlay
		private System.Windows.Shapes.Rectangle _overlayRectangle = new System.Windows.Shapes.Rectangle();
		private void InitializeOverlay()
		{
			_OverlayShapes.Add(_OverlaySnapMark);
			AddRectangle(new Point(10, 10), new Point(20, 20));
			_overlayRectangle.IsVisibleChanged += _overlayRectangle_IsVisibleChanged;
			_overlayRectangle.Visibility = Visibility.Collapsed;
			_OverlayShapes.Add(_overlayRectangle);
			OnPropertyChanged("OverlayShapes");
		}

		private void _overlayRectangle_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			System.Diagnostics.Debugger.Break();
		}

		public void AddRectangle(Point originalScreenPoint, Point newScreenPoint)
		{
			System.Windows.Shapes.Rectangle rubberband = _overlayRectangle;
			rubberband.Stroke = Brushes.Navy;
			rubberband.StrokeThickness = 1;
			rubberband.StrokeDashArray = new DoubleCollection(new double[] { 2 });
			rubberband.Width = 20;
			rubberband.Height = 20;
			double left = Math.Min(originalScreenPoint.X, newScreenPoint.X);
			double top = Math.Min(originalScreenPoint.Y, newScreenPoint.Y);
			double width = Math.Abs(originalScreenPoint.X - newScreenPoint.X);
			double height = Math.Abs(originalScreenPoint.Y - newScreenPoint.Y);
			rubberband.Width = width;
			rubberband.Height = height;
			System.Windows.Controls.Canvas.SetLeft(rubberband, left);
			System.Windows.Controls.Canvas.SetTop(rubberband, top);
			rubberband.Visibility = Visibility.Visible;
			//OverlayRectangle = rubberband;
			OnPropertyChanged("OverlayShapes");
		}

		//public void ClearRectangle()
		//{
		//	_overlayRectangle.Visibility = Visibility.Collapsed;
		//	OnPropertyChanged("OverlayShapes");
		//	UpdateResultsOverlay();
		//}

		private SnapMark _OverlaySnapMark = new SnapMark();
		public SnapMark OverlaySnapMark
		{
			get { return _OverlaySnapMark; }
			set
			{
				if (_OverlaySnapMark != value)
				{
					_OverlaySnapMark = value;
					OnPropertyChanged("OverlaySnapMark");
				}
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="snapPoint">Null value hides snap mark.</param>
		public void UpdateSnapMark(SnapPoint3D snapPoint)
		{
			if (snapPoint != null)
			{
				if (snapPoint.SnapType != SnapType.None)
				{
					_OverlaySnapMark.Visibility = System.Windows.Visibility.Visible;
					_OverlaySnapMark.Origin = snapPoint.ScreenPoint.WinPoint();
				}
				_OverlaySnapMark.SnapType = snapPoint.SnapType;
			}
			else
			{
				_OverlaySnapMark.SnapType = SnapType.None;
				_OverlaySnapMark.Visibility = System.Windows.Visibility.Collapsed;
			}
			OnPropertyChanged("OverlayShapes");//TODO find correct way to update the mark directly
		}

		private List<FrameworkElement> _OverlayShapes = new List<FrameworkElement>();
		public List<FrameworkElement> OverlayShapes
		{
			get { return _OverlayShapes; }
			set
			{
				if (_OverlayShapes != value)
				{
					_OverlayShapes = value;
					OnPropertyChanged("OverlayShapes");
				}
			}
		}

		private bool _UpdateResultsOverlayTrigger = false;
		public bool UpdateResultsOverlayTrigger
		{
			get { return _UpdateResultsOverlayTrigger; }
			set
			{
				if (_UpdateResultsOverlayTrigger != value)
				{
					_UpdateResultsOverlayTrigger = value;
					OnPropertyChanged("UpdateResultsOverlayTrigger");
				}
			}
		}

		public void UpdateResultsOverlay()
		{
			OnPropertyChanged("UpdateResultsOverlayTrigger");
		}

		private bool _IsDimensionsOverlayVisible = true;
		public bool IsDimensionsOverlayVisible
		{
			get { return _IsDimensionsOverlayVisible; }
			set
			{
				if (_IsDimensionsOverlayVisible != value)
				{
					_IsDimensionsOverlayVisible = value;
					OnPropertyChanged("IsDimensionsOverlayVisible");
					UpdateDimensionsOverlay();
				}
			}
		}

		private bool _updateDimensionOverlayFocusTrigger = true;
		public bool UpdateDimensionOverlayFocusTrigger
		{
			get { return _updateDimensionOverlayFocusTrigger; }
			set { _updateDimensionOverlayFocusTrigger = value; OnPropertyChanged("UpdateDimensionOverlayFocusTrigger"); }
		}

		private bool _UpdateDimensionsOverlayTrigger = false;
		public bool UpdateDimensionsOverlayTrigger
		{
			get { return _UpdateDimensionsOverlayTrigger; }
			set
			{
				if (_UpdateDimensionsOverlayTrigger != value)
				{
					_UpdateDimensionsOverlayTrigger = value;
					OnPropertyChanged("UpdateDimensionsOverlayTrigger");
				}
			}
		}

		public void UpdateDimensionsOverlay()
		{
			OnPropertyChanged("UpdateDimensionsOverlayTrigger");
		}

		#endregion

		public event EventHandler ModelClosing;

		protected override void Dispose(bool disposing)
		{
			ModelClosing?.Invoke(this, EventArgs.Empty);
			
			// these only trigger dispose if the wpf model view (the container) has been used/initialized
			DisplayedParts = new List<ModelBaseNode>();// disposes Node3DViewModels in Container
			DisplayedDXFs = new List<ArbitraryMesh>();// disposes Node3DViewModels in Container
			DisplayedGrids = new List<ModelBaseNode>();// disposes Node3DViewModels in Container
			DisplayedReferencePlanes = new List<Epx.BIM.ReferencePlanes.ReferencePlanes>();// disposes Node3DViewModels in Container
			CurrentDisplayedGrids = new List<GridMesh>() { new GridUCS("dummy", true) };

			base.Dispose(disposing);
		}
	}
}
