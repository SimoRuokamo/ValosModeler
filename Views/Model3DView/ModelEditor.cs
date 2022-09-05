using Enterprixe.ValosUITools.Elements3D;
using Enterprixe.WPF.Tools.Localization;
using Enterprixe.WPF.Tools.Viewport;
using Enterprixe.WPF.Tools.Viewport.WireBase;
using Epx.BIM;
using Epx.BIM.GeometryTools;
using Epx.BIM.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Media3D;
using ValosModeler.Views.Model3DView.Visuals;
using GLGraphicsLib;
using Enterprixe.WPF.Tools.Elements3D;
using ValosModeler;

namespace ValosModeler.Views.Model3DView
{
    public interface IModelEditorContainer
	{
		ModelEditor Editor { get; }
		IEnumerable<BaseDataNode> SelectedNodes { get; }
	}
	internal struct HitResult
	{
		internal ISnappableVisual VisualHit;
		internal Point3D PointHit;
	}

	/// <summary>
	/// Class ModelEditor
	/// user input abstraction for any kind of 3D viewports
	/// </summary>
	public class ModelEditor : IPointInputToolController, IViewController
	{

		ModelViewViewModel _ViewModel = null;
		internal ModelViewViewModel ViewModel
		{
			get => _ViewModel;
			set
			{
				_ViewModel = value;
				_ViewModel.FeatureEngine = new FeatureEngineViewModel(this, _ViewModel);
			}
		}

		#region PointInputTool, IPointInputToolController

		public PointInputTool PointInputTool { get; set; }

		private bool _IsPickPointOperation = false;
		public bool IsPickPointOperation
		{
			get { return _IsPickPointOperation; }
			set
			{
				_IsPickPointOperation = value;
				_snapsEnabled = value;
				if (_snapsEnabled)
					_viewport.Cursor = Cursors.Cross;
				else
					_viewport.Cursor = Cursors.Arrow;
				if (ViewModel != null && !_snapsEnabled)
				{
					ViewModel.UpdateSnapMark(null);
				}
			}
		}

		private bool _setRelativeInputPoint = false;
		private bool _isRelativePointSet = true;
		public bool SetRelativeInputPoint
		{
			get { return _setRelativeInputPoint; }
			set
			{
				if (_setRelativeInputPoint != value)
				{
					_setRelativeInputPoint = value;
					if (_setRelativeInputPoint) _isRelativePointSet = false;
					if (_setRelativeInputPoint && ViewModel != null)
					{
						PointInputTool.EndInput();
						ViewModel.SetCommandStatus(-2);
					}
					else if (ViewModel != null)
					{
						ViewModel.SetCommandStatus(-1);
					}
				}
				//ViewModel.SetRelativeInputPoint = value;
			}
		}

		public Enterprixe.WPF.Tools.Viewport.WireBase.IWireHost Viewport => _viewport as IWireHost;
		/// <summary>
		/// The grid marked as the current one to use from the currently displayed grids. (From the ViewModel.)
		/// </summary>
		/// <value>The current grid.</value>
		public Epx.BIM.GridMesh.GridMesh CurrentGrid => _ViewModel?.CurrentGrid;

		public Point GetScreenPoint(Point3D point)
		{
			if (_viewport is GLWPFViewPort.RenderingViewport)
			{
				var p = _modelRoot.Point3DtoPoint2D(point.ToGLPoint());
				return new Point(p.X, p.Y);
			}
			else if (_viewport is Viewport3D)
			{
				return Enterprixe.WPF.Tools.Viewport.ViewportInfo.Point3DtoPoint2D(_viewport as Viewport3D, point);
			}
			else
			{
				throw new NotImplementedException("Unknown base class");
			}
		}

		#endregion
		private List<HighlightableVisual3D> _selectedVisuals = new List<HighlightableVisual3D>(); // visual because of highlight and tempmaterial
		/// <summary>
		/// Adds the current _designCommandVisual to the list of selected visuals.
		/// </summary>
		public void AddSelectedVisual()
		{
			if (MouseOverVisual is HighlightableVisual3D) _selectedVisuals.Add(MouseOverVisual as HighlightableVisual3D);
		}


		ModelViewViewModel.DesignCommands _ActiveCommand = ModelViewViewModel.DesignCommands.None;
		public ModelViewViewModel.DesignCommands ActiveCommand
		{
			get => _ActiveCommand;
			set 
			{
				if (_ActiveCommand != value)
				{
					ClearPreviousCommand(_ActiveCommand);
					SetupNewCommand(value);
					_ActiveCommand = value;
				}
			}
		}

		FrameworkElement _viewport = null;
		VisualModelRoot _modelRoot = null;
		public ModelEditor(FrameworkElement viewport)
		{
			_viewport = viewport;
			if (_viewport is GLWPFViewPort.GLModelViewport)
				_modelRoot = (_viewport as GLWPFViewPort.GLModelViewport).ModelRoot;
			//string assemblyName = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;
			//ResourceDictionary contextMenuIconsResourceDictionary = new ResourceDictionary();
			//contextMenuIconsResourceDictionary.Source = new Uri("pack://application:,,,/" + assemblyName + ";component/Themes/Icons.xaml");//, UriKind.Absolute);
			//_viewportResources.MergedDictionaries.Add(contextMenuIconsResourceDictionary);
		}
		

		#region Mouse

		internal void OnMouseWheel(MouseWheelEventArgs e)
		{
			Point p = e.GetPosition(_viewport);
			//GetHitTestResult(p);
			GetSnapPoint(p);
			ViewModel.UpdateDimensionsOverlay();
		}

		private Point _mouseDownPoint;
		private Epx.BIM.GridMesh.SnapPoint3D _mouseDownGlobalPoint;
		private Epx.BIM.GridMesh.SnapPoint3D _mouseDownGlobalPointPrevious;
		private Point _mouseDownPosition;
		bool _LeftButtonCaptured = false;
		internal void OnMouseLeftButtonDown(MouseButtonEventArgs e)
		{
			_LeftButtonCaptured = true;
			_mouseDownPosition = e.GetPosition(_viewport);
			if (SetRelativeInputPoint)
			{
				PointInputTool.StartInput(GetSnapPoint(_mouseDownPosition).Point.WinPoint());
				_isRelativePointSet = true;
				return;
			}
			else if (IsEditingObject)
			{
				EditingObjectEnded();
				return;
			}
			_mouseDownPoint = _mouseDownPosition; // _mouseDownPoint should only be used here in MouseDown
			_viewport.Focus();

			// An edit visual was hit, store start conditions
			if (MouseOverVisual != null && MouseOverVisual is IModelPartVisual)
			{
				_modifiersAtEditMouseDown = Keyboard.Modifiers;
				_keysAtEditMouseDown = new List<Key>(_keysDown);

			}
			else // do always, if viewport rotate previous mousedown point is restored in mouse move
			{
				// GetSnapPoint() will not always return a hit to the part if the point given is from a previous _snapped_ point.
				// The given point will only hit exactly the WireLine but not the actual part.

				_mouseDownGlobalPointPrevious = _mouseDownGlobalPoint;
				_mouseDownGlobalPoint = GetSnapPoint(_mouseDownPoint);
			}
		}

		private Point _mouseUpPosition;
		private Point _mouseUpPoint;
		private bool _viewportRotateOperation;
		private List<Point3D> _pickedPoints = new List<Point3D>();
		internal void OnMouseLeftButtonUp(MouseButtonEventArgs e)
		{
			_mouseUpPosition = e.GetPosition(_viewport);

			//if (_panOperation)
			//{
			//	return;
			//}
			if (SetRelativeInputPoint && _isRelativePointSet)
			{
				SetRelativeInputPoint = false;
			}
			else if (!IsEditingObject && ViewModel != null && !_viewportRotateOperation  && !SetRelativeInputPoint)
			{
				_mouseUpPoint = _mouseUpPosition;
				if (_snapPoint.IsSnapped) _mouseUpPoint = _snapPoint.ScreenPoint.WinPoint();
				switch (ViewModel.ActiveCommand)
				{
					case ModelViewViewModel.DesignCommands.RunPlugin:
					case ModelViewViewModel.DesignCommands.EditPlugin:
						ViewModel.FeatureEngine.PluginMouseUp(_snapPoint.Point.WinPoint(), MouseOverNode);
						break;
					// No command in progress, perform regular mouse click
					default:
						//GetHitTestResult(_mouseUpPosition);
						if (HitDetected)
						{
							if (MouseOverNode != null && MouseOverVisual != null && !_isPickEditingObject)
							{
								if (Keyboard.Modifiers == ModifierKeys.Control)
								{
									if (MouseOverNode is Model2DNode &&
										!ViewModel.ContextualNodes.Where(n => n is Model3DNode).Any())
									{
										ViewModel.AddRemoveContextualNode(MouseOverNode);
									}
									else if (MouseOverNode is Model3DNode &&
										!ViewModel.ContextualNodes.Where(n => n is Model2DNode).Any())
									{
										ViewModel.AddRemoveContextualNode(MouseOverNode);
									}
								}
								else
								{
									ViewModel.SetContextualNode(MouseOverNode, true, true, true, true);
								}
							}
							else if (MouseOverVisual != null || _isPickEditingObject)
							{
								ToggleMousePickEdit();
							}
						}
						else if (!_viewportRotateOperation)
						{
							if (_isPickEditingObject)
							{
								ToggleMousePickEdit();
							}
							else
							{
								SetHoverText(null);
								ClearSelections();
							}
						}
						if (!_isPickEditingObject)
							_mouseDownGlobalPoint = null;
						break;
				}
			}
			// viewport rotation not possible during drag edit
			else if (IsEditingObject) // An edit operation has ended, ie. move start point
			{
				EditingObjectEnded();
			}
			else if (_isPickEditingObject)
			{
				ToggleMousePickEdit();
			}
			_viewportRotateOperation = false;
			_LeftButtonCaptured = false;
		}
		internal void OnMouseMove(MouseEventArgs e)
		{
			if (/*_ContextMenuPan ||*/ e.MiddleButton == MouseButtonState.Pressed)
			{ 
				ViewModel.UpdateDimensionsOverlay();
				ViewModel.UpdateSnapMark(null);
				return;
			}
			if (_snapsEnabled)
				_viewport.Cursor = Cursors.Cross;
			Point viewportPosition = e.GetPosition(_viewport);
			if ((viewportPosition - _mouseDownPosition).Length < 0.01 || (viewportPosition - _mouseUpPosition).Length < 0.01)
			{
				return; // Mouse.Capture raises MouseMove event even if mouse didn't move.
			}
			else if (SetRelativeInputPoint)
			{
				//GetHitTestResult(viewportPosition);
				GetSnapPoint(viewportPosition);
				return;
			}
			// Start an edit operation on an edit visual, ie. move start point
			if (_LeftButtonCaptured && !IsEditingObject && MouseOverVisual != null && MouseOverVisual is ModelEditVisual3D)
			{
				_isDragEditingObject = true;
				ViewModel.IsViewportCommandInProgress = true;
				//ViewModel.ViewportCommandNode = (MouseOverVisual as ModelEditVisual3D).AttachedNode;
				//ViewModel.ViewportCommandVisual = MouseOverVisual;
				_disableNextAltKey = true;
				IsPickPointOperation = true;
				if (_mouseDownGlobalPoint != null)
					PointInputTool.StartInput(_mouseDownGlobalPoint.Point.WinPoint());
				Mouse.OverrideCursor = Cursors.Cross;
			}
			else if (_LeftButtonCaptured && !_isDragEditingObject) // viewport is being rotated
			{
				// restore mouse down point to previous correct value. the mouse down event before _LeftButtonCaptured should be ignored (viewport rotate)
				_mouseDownGlobalPoint = _mouseDownGlobalPointPrevious;

				ViewModel.UpdateDimensionsOverlay();
				if (ViewModel.ViewportViewMode == View3DMode.Mode3D)
					_viewportRotateOperation = true;
				else
					_viewportRotateOperation = false;
			}
			// Focus keyboard here if not focused so key presses work
			//else 
			if (ViewModel != null && (ViewModel.DesignCommandInProgress || IsEditingObject) && Keyboard.FocusedElement != this)
			{
				// prevent stealing focus when editing text
				if (!(Keyboard.FocusedElement is TextBox))
				{
					_viewport.Focus();
				}
			}

			if (_restorePointInputStart)
			{
				if (_mouseDownGlobalPoint != null && !PointInputTool.IsMoveConstrained)
					PointInputTool.StartInput(_mouseDownGlobalPoint.Point.WinPoint());
				_restorePointInputStart = false;
			}

			if (ViewModel != null)
			{
				// An edit operation is happening on an edit visual. Prevents viewport rotation.
				if (IsEditingObject)
				{
					if (MouseOverVisual != null)
					{
						//GetHitTestResult(viewportPosition);
						GetSnapPoint(viewportPosition);
						if (PointInputTool != null && PointInputTool.IsActive && _mouseDownGlobalPoint != null)
							PointInputTool.MoveInput(_snapPoint.Point.WinPoint());
					}
					ViewModel.UpdateDimensionsOverlay();
				}
				else if (!_viewportRotateOperation) // No edit operation. Perform model highlighting and Active command operations.
				{
					var highlighted = MouseOverVisual;
					RestoreMouseOverHighlights();
					ProcessHitPoints(viewportPosition);
					//GetHighlightHitTestResult(viewportPosition);
					GetSnapPoint(viewportPosition);

					if (HitDetected && !ViewModel.FeatureEngine.IsTemporaryNode(MouseOverNode))
					{
						Guid selectedElementID = Guid.Empty;
						if (ViewModel.ContextualNode != null) selectedElementID = ViewModel.ContextualNode.UniqueID;

						if (MouseOverVisual != null)
						{
							// Highlight the mouse over model if it is not the selected model
							if (MouseOverNode.UniqueID != selectedElementID)
							{
								MouseOverVisual.IsHighlighted = true;
							}
							SetHoverText(MouseOverVisual);
							ShowMouseOverToolTip(MouseOverNode);
						}
						else
						{
							ShowMouseOverToolTip(null);
						}
					}
					else
					{
						ShowMouseOverToolTip(null);
						SetHoverText(null);
					}
					if(highlighted != MouseOverVisual && highlighted != null)
						highlighted.IsHighlighted = false;
					if (PointInputTool != null && PointInputTool.IsActive)
						PointInputTool.MoveInput(_snapPoint.Point.WinPoint());

					#region ActiveCommands

					// Handle design commands
					switch (ViewModel.ActiveCommand)
					{
						case ModelViewViewModel.DesignCommands.RunPlugin:
						case ModelViewViewModel.DesignCommands.EditPlugin:
							ViewModel.FeatureEngine.PluginMouseMove(viewportPosition, _snapPoint, _mouseDownGlobalPoint, MouseOverVisual);
							break;

						default:
							break;
					}

					#endregion ActiveCommands
				}
			}
		}
		
		/// <summary>
		/// Restore materials and highlights before hit test.
		/// </summary>
		private void RestoreMouseOverHighlights(bool clearStatusText = true)
		{
			// Restore materials and highlights before hit test.
			if (MouseOverVisual != null)
			{
				if (MouseOverVisual != null)
				{
					//MouseOverVisual.IsHighlighted = false;
				}
				if (clearStatusText) SetHoverText(null);
				if (!IsEditingObject)
				{
					MouseOverVisual = null;
					MouseOverNode = null;
				}
			}
		}

	

		private IModelPartVisual _mousePickEditVisual = null;

		protected void ToggleMousePickEdit()
		{
			if (!_isPickEditingObject)
			{
				_mousePickEditVisual = MouseOverVisual;
				_isPickEditingObject = true;

				_disableNextAltKey = true;
				IsPickPointOperation = true;
				Mouse.OverrideCursor = Cursors.Cross;
				if (_mouseDownGlobalPoint != null)
					PointInputTool.StartInput(_mouseDownGlobalPoint.Point.WinPoint());

				// TODO ViewModel.SetCurrentDisplayedGrids(

				if (_mousePickEditVisual.AttachedNode is Epx.BIM.Models.ModelBaseNode)
				{
					ViewModel.AddOutlineOverride(_mousePickEditVisual.AttachedNode as Epx.BIM.Models.ModelBaseNode);
				}
			}
			else
			{
				//_editVisualMoveInProgress = false;
				_mousePickEditVisual = null;
				_isPickEditingObject = false;

				Mouse.OverrideCursor = null;
				PointInputTool.EndInput();
			}
		}

		/// <summary>
		/// Set mouseover text for highlightable visuals.
		/// </summary>
		/// <param name="mouseovervisual"></param>
		private void SetHoverText(IModelPartVisual mouseovervisual)
		{
			if (mouseovervisual == null)
			{
//				ViewModel.SetHoverText(string.Empty);
#if DEBUG
//				ViewModel.SetHoverText($" Rendered {_modelRoot.InnerMeshPool.NumObjects} objects");
#endif
			}
			else 
			{
				BaseDataNode attachedNode = mouseovervisual.AttachedNode;
				string hoverText = mouseovervisual.HoverText;
				if (!string.IsNullOrEmpty(hoverText))
				{
					ViewModel.SetHoverText(CultureManager.GetLocalizedString(hoverText));
				}
				else
				{
					string nodePath = attachedNode.GetPathUpToNode<Project>();
					if (ViewModel.WindowTitleNodePath != null) nodePath = nodePath.Replace(ViewModel.WindowTitleNodePath + "\\", "");

					if (ViewModel.ContextualNode == null)
						ViewModel.SetHoverText(CultureManager.GetLocalizedString("Click to select") + @" \" + nodePath + "\"");
					else if (ViewModel.ContextualNode.UniqueID != attachedNode.UniqueID)
						ViewModel.SetHoverText(CultureManager.GetLocalizedString("Click to select") + @" \" + nodePath + "\"");
				}
			}
		}

		#endregion //Mouse

		#region Key Events

		public ModifierKeys ModifiersAtEditMouseDown
		{
			get { return _modifiersAtEditMouseDown; }
		}

		private ModifierKeys _modifiersAtEditMouseDown = ModifierKeys.None;
		private List<Key> _keysAtEditMouseDown = new List<Key>();
		private List<Key> _keysDown = new List<Key>();

		private void ModelViewport_PreviewKeyDown(object sender, KeyEventArgs e)
		{
			if (!_keysDown.Contains(e.Key)) _keysDown.Add(e.Key);
		}
		private void ModelViewport_PreviewKeyUp(object sender, KeyEventArgs e)
		{
			_keysDown.RemoveAll(k => k == e.Key);
		}

		internal void OnKeyDown(KeyEventArgs e)
		{
			if (e.Key == Key.I)
			{
				// disabled for now because of new snap
				//int nextindex = ViewModel.MouseMoveIncrements.IndexOf(ViewModel.MouseMoveIncrement) + 1;
				//if (nextindex >= ViewModel.MouseMoveIncrements.Count) nextindex = 0;
				//ViewModel.MouseMoveIncrement = ViewModel.MouseMoveIncrements[nextindex];
				//ViewModel.CommandStatusDetailText = ViewModel.MouseMoveIncrement.ToString();
			}
			//else if (e.Key == Key.F2 || e.Key == Key.D)
			//{
			//	ViewModel.ToggleDesignView();
			//}
			else if (e.Key == Key.R)
			{
				// R reserved for point input tool set relative point
			}
			else if (IsEditingObject || ViewModel.DesignCommandInProgress)
			{
				if (e.Key == Key.System)
				{
					e.Handled = true; // Prevent Alt system key action when editing
				}
				else if (e.Key == Key.Escape)
				{
					if (SetRelativeInputPoint)
					{
						// do nothing, cancel not allowed
					}
					else if (IsEditingObject) EditingObjectEnded(true);
					else CancelCommand(ViewModel.ActiveCommand);
				}
				else if (ViewModel.ActiveCommand == ModelViewViewModel.DesignCommands.RunPlugin || ViewModel.ActiveCommand == ModelViewViewModel.DesignCommands.EditPlugin)
				{
					if (!PointInputTool.IsActive && !SetRelativeInputPoint && e.Key != Key.LeftCtrl && e.Key != Key.RightCtrl && e.Key != Key.LeftAlt && e.Key != Key.RightAlt && e.Key != Key.LeftShift && e.Key != Key.RightShift)
						ViewModel.FeatureEngine.PluginKeyDown(e.Key, new Point3D(), null);
				}
			}
			else
			{
				if (e.Key == Key.System)
				{
					if (e.SystemKey == Key.LeftAlt)// && _disableNextAltKey)
					{
						e.Handled = true; // Prevent Alt system key action
					}
				}
				else if (e.Key == Key.Delete)
				{
					ViewModel.RemoveContextualNodes();
				}
				else if(e.Key == Key.G)
				{
#if OPENGL && DEBUG
					RegenerateContextualNode();
#endif
				}
				else if (e.Key == Key.Escape)
				{
					//SelectedElementID = Guid.Empty;
					// "Unselect" selected member
					ViewModel.SetContextualNode(null);
					SetHoverText(null);
				}
				else if (e.Key == Key.LeftShift || e.Key == Key.RightShift)
				{
					ShowMouseOverToolTip(MouseOverNode ?? ViewModel.ContextualNode);
				}
			}

			if (e.Key == Key.D0 || e.Key == Key.NumPad0)
			{
				ViewModel.ToggleSnapsOnOff();
			}
			else if (e.Key == Key.D1 || e.Key == Key.NumPad1)
			{
				ViewModel.SnapGridIntersection = !ViewModel.SnapGridIntersection;
			}
			else if (e.Key == Key.D2 || e.Key == Key.NumPad2)
			{
				ViewModel.SnapGridMiddle = !ViewModel.SnapGridMiddle;
			}
			else if (e.Key == Key.D3 || e.Key == Key.NumPad3)
			{
				ViewModel.SnapGridLine = !ViewModel.SnapGridLine;
			}
			else if (e.Key == Key.D4 || e.Key == Key.NumPad4)
			{
				ViewModel.SnapGeometryEdgeEnds = !ViewModel.SnapGeometryEdgeEnds;
			}
			else if (e.Key == Key.D5 || e.Key == Key.NumPad5)
			{
				ViewModel.SnapGeometryEdgeMiddle = !ViewModel.SnapGeometryEdgeMiddle;
			}
			else if (e.Key == Key.D6 || e.Key == Key.NumPad6)
			{
				ViewModel.SnapGeometryEdge = !ViewModel.SnapGeometryEdge;
			}
		}

		private bool _disableNextAltKey; // Disable next Alt system key press
		private bool _controlModifierOverriden = false;
		private bool _restorePointInputStart = false;
		internal void OnKeyUp(KeyEventArgs e)
		{
			if (IsEditingObject /*|| ViewModel.DesignCommandInProgress*/)
			{
				if (e.Key == Key.System)
				{
					e.Handled = true; // Prevent Alt system key action
				}
				else if (e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl)
				{
					if (_controlModifierOverriden)
					{
						_controlModifierOverriden = false;
						_restorePointInputStart = true;
					}
				}
			}
			else
			{
				if (e.Key == Key.System)
				{
					if (e.SystemKey == Key.LeftAlt && _disableNextAltKey)
					{
						e.Handled = true; // Prevent Alt system key action
						_disableNextAltKey = false;
					}
				}
			}

			if (e.Key == Key.LeftShift || e.Key == Key.RightShift)
			{
				HideMouseOverToolTip();
			}
		}

		#endregion //Key events

		#region Edit operation

		private bool _isPickEditingObject; // An edit operation is an operation performed on an edit visual ie. dragging or moving
		private bool _isDragEditingObject;
		/// <summary>
		/// An edit operation is an operation performed on an edit visual ie. dragging or moving
		/// </summary>
		public bool IsEditingObject
		{
			get { return _isPickEditingObject || _isDragEditingObject; }
		}

		protected void EditingObjectEnded(bool isCancel = false)
		{
			if (_mousePickEditVisual != null && _mousePickEditVisual.AttachedNode is Epx.BIM.Models.ModelBaseNode)
			{
				// remove before actions to avoid having to send message to redraw.
				ViewModel.RemoveOutlineOverride(_mousePickEditVisual.AttachedNode as Epx.BIM.Models.ModelBaseNode);
			}

			//if (/*some SetNodePropertyAction edit case*/)
			//{
			//	ViewModel.EndSetNodePropertyAction(isCancel);
			//}

			if (isCancel && _isPickEditingObject)
			{
				// in normal case toggle is done in mouseup to prevent unnecessary mouse click events
				ToggleMousePickEdit();
			}

			ViewModel.UpdateDimensionsOverlay();

			// these same are in DisposeViewportStates()
			_isPickEditingObject = false;
			_isDragEditingObject = false;
			IsPickPointOperation = false;
			ViewModel.UpdateSnapMark(null);
			PointInputTool.EndInput();
			_mouseDownGlobalPoint = null;
			ViewModel.CurrentDisplayedGrids = null;
			ViewModel.IsViewportCommandInProgress = false;
			//ViewModel.ViewportCommandNode = null;
			//ViewModel.ViewportCommandVisual = null;
			//ViewModel.Mediator.NotifyColleaguesAsync<ModelViewViewModel.DesignCommands>(MediatorMessages.DesignCommandInProgressChanged, ModelViewViewModel.DesignCommands.None);

			Mouse.Capture(null);
			Keyboard.Focus(null);
			Mouse.OverrideCursor = null;
		}


		#endregion //Edit

		#region Commands / Selections
		/// <summary>
		/// Clear all model highlights and selections in the viewport.
		/// </summary>
		public void ClearSelections(bool clearContextualNode = true)
		{
			ViewModel.ResetContextualNodes(clearContextualNode);

			MouseOverVisual = null;
			MouseOverNode = null;
			//ViewModel.ViewportMouseOverVisual = MouseOverVisual;
		}

		/// <summary>
		/// Setup the new command when the command is started.
		/// </summary>
		/// <param name="newCommand"></param>
		private void SetupNewCommand(ModelViewViewModel.DesignCommands newCommand)
		{
			// GetSnapPoint() will not always return a hit to the part if the point given is from a previous _snapped_ point.
			// The given point will only hit exactly the WireLine but not the actual part.
			// Use visible or invisible _helperPoints and query the input point from their origins as a workaround.

			if (newCommand != ModelViewViewModel.DesignCommands.None)
			{
				//ClearSelections(false); //cannot clear ContextualNode generally bc many commands rely on it, commands should use ActiveCommandNode
				RestoreMouseOverHighlights(false);
				Mouse.OverrideCursor = Cursors.Cross;
			}

			_mouseDownGlobalPoint = null;

			switch (newCommand)
			{
				case ModelViewViewModel.DesignCommands.RunPlugin:
				case ModelViewViewModel.DesignCommands.EditPlugin:
					IsPickPointOperation = true;
					break;

				default:
					_snapsEnabled = false;
					break;
			}
		}

		/// <summary>
		/// Clears the temporary values of the previous command when the design command is changed.
		/// </summary>
		/// <param name="currentcommand"></param>
		private void ClearPreviousCommand(ModelViewViewModel.DesignCommands previouscommand)
		{
			if (previouscommand != ModelViewViewModel.DesignCommands.None)
			{
				PointInputTool.EndInput();
				ViewModel.CurrentDisplayedGrids = null;
				ViewModel.CurrentDXF = null;
				ViewModel.UpdateSnapMark(null);
				IsPickPointOperation = false;
				_mouseDownGlobalPoint = null;
				Mouse.OverrideCursor = null;
			}

			switch (previouscommand)
			{
				case ModelViewViewModel.DesignCommands.RunPlugin:
				case ModelViewViewModel.DesignCommands.EditPlugin:
					foreach (HighlightableVisual3D visual in _selectedVisuals)
					{
						visual.RestoreMaterial();
					}
					_selectedVisuals.Clear();
					ViewModel.EndDesignCommand(true);
					break;

				default:
					foreach (HighlightableVisual3D visual in _selectedVisuals)
					{
						visual.RestoreMaterial();
					}
					_selectedVisuals.Clear();
					break;
			}
		}

		/// <summary>
		/// Step by step cancel of the active command.
		/// </summary>
		private void CancelCommand(ModelViewViewModel.DesignCommands command)
		{
			if (SetRelativeInputPoint) return;

			Mouse.Capture(null);
			PointInputTool.EndInput();

			if (ViewModel != null)
			{
				ViewModel.SetCommandStatus(0);

				switch (command)
				{
					case ModelViewViewModel.DesignCommands.RunPlugin:
					case ModelViewViewModel.DesignCommands.EditPlugin:
						foreach (HighlightableVisual3D visual in _selectedVisuals)
						{
							visual.RestoreMaterial();
						}
						_selectedVisuals.Clear();
						ViewModel.EndDesignCommand(true);
						break;

					default:
						foreach (HighlightableVisual3D visual in _selectedVisuals)
						{
							visual.RestoreMaterial();
						}
						_selectedVisuals.Clear();
						ViewModel.ActiveCommand = ModelViewViewModel.DesignCommands.None;
						break;
				}

				if (ViewModel.ActiveCommand == ModelViewViewModel.DesignCommands.None)
				{
					Mouse.OverrideCursor = null;
				}
			}
		}
		#endregion //Commands

		#region Hit Test

		protected ModelBaseNode MouseOverNode;
		protected IModelPartVisual MouseOverVisual;

		protected bool HitDetected { get; set; }
		//protected Point3D HitPoint { get; set; }
		
		protected void ProcessHitPoints(Point screenPoint)
		{
			if (!IsEditingObject)
			{
				MouseOverNode = null;
				MouseOverVisual = null;
			}
			HitDetected = false;
			if (_modelRoot != null)
			{
				var scrPoint = new Vector2((float)screenPoint.X, (float)screenPoint.Y);
				if(ViewModel.ActiveCommand == ModelViewViewModel.DesignCommands.None)
				{
					var hitObject = _modelRoot.HighLightHitObject(scrPoint);
					MouseOverVisual = hitObject as IModelPartVisual;
					if(MouseOverVisual != null)
					{
						HitDetected = true;
						MouseOverNode = MouseOverVisual.AttachedNode as ModelBaseNode;
						if(ViewModel.FeatureEngine.IsTemporaryNode(MouseOverNode))
							MouseOverVisual.IsHighlighted = false;
					}
				}
				else
				{
					
					Ray ray3d = _modelRoot.Camera.GetPickingRay( scrPoint);
					var hitParam = new HitTestParams() { ViewRay = ray3d };
					_modelRoot.HitTest(hitParam);
					if (hitParam.Entries.Count > 0)
					{
						var hitEntries = hitParam.Entries.OrderBy(e => e.DistToRayOrigin);
						foreach(var entry in hitEntries)
						{
							if(entry.VisualObject is IModelPartVisual mouseOver)
							{
								var overNode = mouseOver.AttachedNode as ModelBaseNode;
								if(!ViewModel.FeatureEngine.IsTemporaryNode(overNode))
								{
									HitDetected = true;
									MouseOverVisual = mouseOver;
									MouseOverNode = overNode;
									break;
								}
							}
						}
					}
				}
			}
		}
		#endregion

		#region Snap

		/// <summary>
		/// 
		/// </summary>
		public Epx.BIM.GridMesh.SnapPoint3D SnapPoint
		{
			get { return _snapPoint; }
		}
		
		/// <summary>
		/// On/off switch for snaps.
		/// </summary>
		private bool _snapsEnabled = false;
		private Epx.BIM.GridMesh.SnapPoint3D _snapPoint = new Epx.BIM.GridMesh.SnapPoint3D();
		/// <summary>
		/// Use _snapsEnabled to enable/disable snaps.
		/// Use private field _snapPoint for always up-to-date point.
		/// Updates snap mark overlay.
		/// </summary>
		/// <param name="screenPoint"></param>
		/// <returns></returns>
		private Epx.BIM.GridMesh.SnapPoint3D GetSnapPoint(Point screenPoint, bool getRawPoint = false)
		{
			// GetSnapPoint() will not always return a hit to the part if the point given is from a previous _snapped_ point.
			// The given point will only hit exactly the WireLine but not the actual part.

			Epx.BIM.GridMesh.SnapPoint3D snapPoint = new Epx.BIM.GridMesh.SnapPoint3D();
			var perpendicularGrids = ViewModel.GetPerpendicularGrids();
			var perpendicularDXFs = ViewModel.GetPerpendicularDXFs();
			var currentUCS = ViewModel.CurrentGrid;// != null ? ViewModel.CurrentGrid : new Epx.BIM.GridMesh.GridUCS();
			if (_snapsEnabled && !getRawPoint)
			{
				Ray ray3d = _modelRoot.Camera.GetPickingRay(new Vector2((float)screenPoint.X, (float)screenPoint.Y));
				var hitParam = new HitTestParams() { ViewRay = ray3d };
				_modelRoot.HitTest(hitParam);
				bool geometrySnapsEnabled = ViewModel.EnabledSnaps.Intersect(Epx.BIM.GridMesh.SnapPoint3D.GeometrySnapTypes).Any();
				bool gridSnapsEnabled = ViewModel.EnabledSnaps.Intersect(Epx.BIM.GridMesh.SnapPoint3D.GridSnapTypes).Any();
				List<Epx.BIM.GridMesh.SnapPoint3D> foundSnaps = new List<Epx.BIM.GridMesh.SnapPoint3D>();
				foreach (var r in hitParam.Entries)
				{
					if (geometrySnapsEnabled && r.VisualObject is ISnappableVisual snapV)
					{
						var hPoint = ray3d.Origin + ray3d.Direction * r.DistToRayOrigin;
						var tp = _modelRoot.Transform.Inverted.TransformPoint(hPoint);
						var modelPoint = snapV.GetSnapPoint(tp.ToEPXPoint(), SnapTolerance, ViewModel.EnabledSnaps);

						if (!ViewModel.FeatureEngine.IsTemporaryNode(modelPoint.Node))
						{
							modelPoint.ScreenPoint = GetScreenPoint(modelPoint.Point.WinPoint()).BimPoint();
							foundSnaps.Add(modelPoint);
						}
					}
				}
				double minDistance = double.MaxValue;
				foreach (var snap in foundSnaps)
				{
					// closest snap point
					if (snap.SnapDistance < minDistance)
					{
						snapPoint = snap;
						minDistance = snap.SnapDistance;
					}
				}
				// give more weight to geometry non edge snaps (when searching from adjacent members)
				minDistance = double.MaxValue;
				var geometryNonEdgeSnaps = foundSnaps.Where(s => s.SnapType == Epx.BIM.GridMesh.SnapType.GeometryEdgeEnds || s.SnapType == Epx.BIM.GridMesh.SnapType.GeometryEdgeMiddle || s.SnapType == Epx.BIM.GridMesh.SnapType.GeometryNode);
				if (snapPoint.SnapType == Epx.BIM.GridMesh.SnapType.GeometryEdge && geometryNonEdgeSnaps.Any())
				{
					foreach (var snap in geometryNonEdgeSnaps)
					{
						double dist = Math.Abs(snap.SnapDistance - snapPoint.SnapDistance); // TODO why the difference between snap distances?
						if (dist < SnapTolerance && dist < minDistance)
						{
							snapPoint = snap;
							minDistance = dist;
						}
					}
				}

				// grid snap plane hidden
				if (!snapPoint.IsSnapped && currentUCS != null && currentUCS.IsShownIn3D && !perpendicularGrids.Contains(currentUCS))
				{
					Point3D? plane3dPoint = null;
					var ucsToGlobal = Matrix3D.Identity;
					plane3dPoint = GetPlane3DCoordinate(screenPoint, ucsToGlobal.Transform(currentUCS.Origin.WinPoint()), ucsToGlobal.Transform(currentUCS.XAxis.WinPoint()), ucsToGlobal.Transform(currentUCS.YAxis.WinPoint()));
					if (plane3dPoint.HasValue)
					{
						Epx.BIM.GridMesh.SnapPoint3D gridPoint = currentUCS.GetSnapPoint3D(currentUCS.GlobalToLocal.Transform(plane3dPoint.Value.BimPoint()), SnapTolerance, ViewModel.EnabledSnaps);

						if (ViewModel.EnabledSnaps.Contains(gridPoint.SnapType))
						{
							gridPoint.Point = currentUCS.LocalToGlobal.Transform(gridPoint.Point);
							gridPoint.ScreenPoint = GetScreenPoint(  gridPoint.Point.WinPoint()).BimPoint();
							gridPoint.Node = currentUCS;
							snapPoint = gridPoint;
						}
					}
				}

				ViewModel.UpdateSnapMark(snapPoint);
			}

			if (!snapPoint.IsSnapped )//&& hitResultListFiltered.Count > 0)
			{
				// no snaps, check grid hit  
				//var firstHit = hitResultListFiltered.FirstOrDefault(hr => (hr is RayMeshGeometry3DHitTestResult && hr.VisualHit is GridMeshVisual3D));
				//if (firstHit is RayMeshGeometry3DHitTestResult)
				//{
				//	//
				//	snapPoint.Point = (firstHit as RayMeshGeometry3DHitTestResult).PointHit;
				//	if (firstHit.VisualHit is GridMeshVisual3D)
				//	{
				//		snapPoint.Point = (firstHit.VisualHit as GridMeshVisual3D).AttachedGrid.LocalToGlobal.Transform(snapPoint.Point);
				//	}
				//	snapPoint.ScreenPoint = point;
				//	snapPoint.Node = GetAttachedNode(firstHit.VisualHit);
				//}
			}

			if (snapPoint.Point == Epx.BIM.GridMesh.SnapPoint3D.EmptyPoint)
			{
				//grid not visible or grid snap plane hidden, nothing hit
				double zLevel = currentUCS != null ? currentUCS.Origin.Z : 0;
				Vector3D camDir = ViewModel.ViewportController.CurrentCamera.LookDirection;
				camDir = ViewModel.ViewportController.CurrentCamera.Transform.Transform(camDir);
				camDir.Negate();

				Point planepoint = new Point();
				if (currentUCS != null && !perpendicularGrids.Contains(currentUCS))
				{
					Point3D? plane3dPoint = null;
					plane3dPoint = GetPlane3DCoordinate(screenPoint, currentUCS.Origin.WinPoint(), currentUCS.XAxis.WinPoint(), currentUCS.YAxis.WinPoint());
					if (plane3dPoint.HasValue) snapPoint.Point = plane3dPoint.Value.BimPoint();
				}
				else if (ViewModel.ViewportViewMode == View3DMode.Mode3D)
				{
					bool isPerp = false;
					double angle = Math.Abs(Vector3D.AngleBetween(camDir, new Vector3D(0, 0, 1)));
					if (angle > 85 && angle < 95) isPerp = true;

					if (isPerp)
					{
						angle = Math.Abs(Vector3D.AngleBetween(camDir, new Vector3D(1, 0, 0)));
						if (angle > 85 && angle < 95)
						{
							planepoint = GeometryMath.ToPoint(GetXZCoordinate(screenPoint, 0).BimPoint()).WinPoint();
						}
						else
						{
							planepoint = GeometryMath.ToPoint(GetYZCoordinate(screenPoint, 0).BimPoint()).WinPoint();
						}
					}
					else
					{
						planepoint = GeometryMath.ToPoint(GetXYCoordinate(screenPoint, 0).BimPoint()).WinPoint();
					}
				}
				else if (ViewModel.ViewportViewMode == View3DMode.Top)
				{
					planepoint = GeometryMath.ToPoint(GetXYCoordinate(screenPoint, 0).BimPoint()).WinPoint();
				}
				else if (ViewModel.ViewportViewMode == View3DMode.Right)
				{
					planepoint = GeometryMath.ToPoint(GetXZCoordinate(screenPoint, 0).BimPoint()).WinPoint();
				}
				else if (ViewModel.ViewportViewMode == View3DMode.Left)
				{
					planepoint = GeometryMath.ToPoint(GetYZCoordinate(screenPoint, 0).BimPoint()).WinPoint();
				}
				snapPoint.Point = new Point3D(planepoint.X, planepoint.Y, 0).BimPoint();
				snapPoint.ScreenPoint = screenPoint.BimPoint();
			}

#if DEBUG
			if (double.IsNaN(snapPoint.Point.X))
			{
				System.Diagnostics.Debug.Fail("Snap point is NaN");
			}
#endif

			_snapPoint =  snapPoint;
			if (!getRawPoint && PointInputTool != null) // && _mouseDownGlobalPoint != null)
			{
				if (currentUCS == null) currentUCS = new Epx.BIM.GridMesh.GridUCS();
				// keep objects which are not input in the grid plane leveled
				Epx.BIM.GridMesh.GridMesh overrideGrid = ViewModel.CurrentGrid != null ? null : currentUCS;
				Epx.BIM.GridMesh.GridMesh currentUCStrfd = new Epx.BIM.GridMesh.GridUCS();
				var cucsXAxis = currentUCS.XAxis;
				var cucsYAxis = currentUCS.YAxis;
				var cucsOrigin = currentUCS.Origin;
				{
					currentUCStrfd.XAxis = cucsXAxis;
					currentUCStrfd.YAxis = cucsYAxis;
					currentUCStrfd.Origin = cucsOrigin;
				}

				bool removeMoveConstraints = false;
				bool orthoOn = false;
				if (Keyboard.Modifiers == ModifierKeys.Control)
				{
					orthoOn = true;
				}

				// keep objects which are not input in the grid plane leveled
				Point3D? planePoint = null;
				planePoint = GetPlane3DCoordinate(screenPoint, PointInputTool.IsActive ? PointInputTool.Start
												: currentUCStrfd.Origin.WinPoint(), currentUCStrfd.XAxis.WinPoint(), currentUCStrfd.YAxis.WinPoint());

				// align with axes or nearest direction
				if (orthoOn && PointInputTool.IsActive && !PointInputTool.IsMoveConstrained && planePoint.HasValue && currentUCS != null)
				{
					Point3D planeLevel = planePoint.Value;// !_snapPoint.IsSnapped ? planePoint.Value : _snapPoint.Point;
														  //var mouseMoveModelPoint = GetPlane3DCoordinate(point, planeLevel, currentUCStrfd.XAxis, currentUCStrfd.YAxis);

					if (planePoint.HasValue)
					{
						Vector3D mouseMoveDirection = planePoint.Value - PointInputTool.Start;// _mouseDownGlobalPoint.Point;
						if (mouseMoveDirection.Length > 0)
						{
							List<Vector3D> directionVectors = new List<Vector3D>();
							Vector3D xaxis = new Vector3D(currentUCStrfd.XAxis.X, currentUCStrfd.XAxis.Y, currentUCStrfd.XAxis.Z);
							Vector3D yaxis = new Vector3D(currentUCStrfd.YAxis.X, currentUCStrfd.YAxis.Y, currentUCStrfd.YAxis.Z);
							directionVectors.Add(xaxis);
							directionVectors.Add(yaxis);
							xaxis = new Vector3D(currentUCStrfd.XAxis.X, currentUCStrfd.XAxis.Y, currentUCStrfd.XAxis.Z);
							yaxis = new Vector3D(currentUCStrfd.YAxis.X, currentUCStrfd.YAxis.Y, currentUCStrfd.YAxis.Z);
							xaxis.Negate();
							yaxis.Negate();
							directionVectors.Add(xaxis);
							directionVectors.Add(yaxis);

							double closestAngle = 360;
							Vector3D closestDir = new Vector3D();
							foreach (Vector3D direction in directionVectors)
							{
								double angle = Math.Abs(Vector3D.AngleBetween(direction, mouseMoveDirection));
								if (angle < closestAngle)
								{
									closestAngle = angle;
									closestDir = direction;
								}
							}
							closestAngle = Vector3D.AngleBetween(closestDir, currentUCStrfd.XAxis.WinPoint());
							var mouseMoveDirLocal = currentUCStrfd.GlobalToLocal.Transform(mouseMoveDirection.BimPoint());
							if (mouseMoveDirLocal.X < 0 && mouseMoveDirLocal.Y < 0)
								closestAngle *= -1;

							closestDir.Normalize();
							PointInputTool.AddMoveConstraint(PointInputConstraints.Angle);
							PointInputTool.AddMoveConstraint(PointInputConstraints.ZCoord);
							PointInputTool.SetMoveAngleAndLength(closestAngle, 0);
							if (PointInputTool.IsActive)
								PointInputTool.SetMoveVector(new Vector3D(PointInputTool.MoveToPoint.X, PointInputTool.MoveToPoint.Y, 0), false);
							else
								PointInputTool.SetMoveVector(new Vector3D(mouseMoveDirection.X, mouseMoveDirection.Y, 0), false);
							removeMoveConstraints = true;
						}
					}
				}

				if (!_snapPoint.IsSnapped && planePoint.HasValue)
				{
					_snapPoint.Point = planePoint.Value.BimPoint();
				}
				//_snapPoint.PointWithoutMoveConstraints = _snapPoint.Point;
				_snapPoint = PointInputTool.ApplyMoveConstraints(_snapPoint, overrideGrid);
				_snapPoint.ScreenPoint = GetScreenPoint( _snapPoint.Point.WinPoint()).BimPoint();

				if (removeMoveConstraints)
				{
					PointInputTool.RemoveMoveConstraint(PointInputConstraints.Angle);
					PointInputTool.RemoveMoveConstraint(PointInputConstraints.ZCoord);
				}
			}

			_snapPoint.Point = GeometryMath.RoundPoint(_snapPoint.Point);
			return _snapPoint;
		}

		protected Point3D GetXYCoordinate(Point coordinate, double zValue)
		{
			var range = GetPickingRange(coordinate);
			return range.PointFromZ(zValue);// now only in Z plane			
		}

		/// <summary>
		/// Get point from XZ plane (Y == 0).
		/// </summary>
		/// <param name="coordinate"></param>
		/// <returns></returns>
		protected Point3D GetXZCoordinate(Point coordinate, double yValue)
		{
			var range = GetPickingRange(coordinate); 
			return range.PointFromY(yValue);// now only in Y plane
		}

		/// <summary>
		/// Get point from YZ plane (X == 0).
		/// </summary>
		/// <param name="coordinate"></param>
		/// <returns></returns>
		protected Point3D GetYZCoordinate(Point coordinate, double xValue)
		{
			var range = GetPickingRange(coordinate);
			return range.PointFromX(xValue);
		}

		private LineRange GetPickingRange(Point screenPoint)
		{
			var ray3d = _modelRoot.Camera.GetPickingRay(new Vector2((float)screenPoint.X, (float)screenPoint.Y));
			var point2 = ray3d.Origin + ray3d.Direction * ray3d.Length;
			var range = new LineRange(ray3d.Origin.ToEPXPoint(), point2.ToEPXPoint());
			return range;
		}

		protected Point3D? GetPlane3DCoordinate(Point screenPoint, Point3D planePoint, Vector3D planeXAxis, Vector3D planeYAxis)
		{
			var ray =  _modelRoot.Point2DtoPoint3D(new Vector2((float)screenPoint.X, (float)screenPoint.Y));
			var range = new LineRange( ray.Item1.ToEPXPoint(), ray.Item2.ToEPXPoint());
			Vector3D normal = Vector3D.CrossProduct(planeXAxis, planeYAxis);
			var point3d = range.PointFromPlane(planePoint, normal);

			//var plane = new GLGraphicsLib.Tools.Plane3D(((Point3D)normal).ToGLPoint(), planePoint.ToGLPoint());
			//var projPoint = plane.LineIntersection(ray3d.Origin, ray3d.Origin + ray3d.Direction * 1000);
			//if (projPoint.HasValue)
			//{
			//}

			return point3d;
		}

		private BaseDataNode GetAttachedNode(object visual)
		{
			BaseDataNode attachedNode = null;
			if (visual is IModelPartVisual)
			{
				attachedNode = (visual as IModelPartVisual).AttachedNode;
			}
			else if (visual is IGridMeshVisual)
			{
				attachedNode = (visual as IGridMeshVisual).AttachedGrid;
			}
			return attachedNode;
		}
		
		/// <summary>
		/// Adaptive based on Camera width.
		/// </summary>
		private double SnapTolerance
		{
			get
			{
				var tol = 175;
				if (_modelRoot != null)
				{
					var camera = _modelRoot.Camera;
					if (camera.State.Width < 10000)
						tol = 100;
					if (camera.State.Width < 4000)
						tol = 60;
					if (camera.State.Width < 2000)
						tol = 30;
				}
				return tol;
			}
		}

#endregion



#region ToolTip

		/// <summary>
		/// Shows the mouse over information.
		/// </summary>
		protected void ShowMouseOverToolTip(BaseDataNode node)
		{
			HideMouseOverToolTip(); // hide previous

			if ((_viewport.Parent as FrameworkElement).ToolTip is ToolTip) ((_viewport.Parent as FrameworkElement).ToolTip as ToolTip).IsOpen = true;
		}
		/// <summary>
		/// Hides the mouse over information.
		/// </summary>
		/// <returns><c>true</c> if a tooltip was open and it was closed, <c>false</c> otherwise.</returns>
		protected bool HideMouseOverToolTip()
		{
			bool wasClosed = false;
			if ((_viewport.Parent as FrameworkElement).ToolTip is ToolTip)
			{
				((_viewport.Parent as FrameworkElement).ToolTip as ToolTip).IsOpen = false;
				wasClosed = true;
			}
			(_viewport.Parent as FrameworkElement).ToolTip = null;
			return wasClosed;
		}

		#endregion

		internal void OnMouseRightButtonUp(MouseButtonEventArgs e)
		{
			CreateContextMenu();
		}
		protected ResourceDictionary _viewportResources = null;
		internal void CreateContextMenu()
		{
			if (_viewportResources == null)
			{
				_viewportResources = new ResourceDictionary();
				string assemblyName = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;
				_viewportResources.Source = new Uri("pack://application:,,,/" + assemblyName + ";component/Themes/Icons.xaml");//, UriKind.Absolute);
			}

			var newMenu = new System.Windows.Controls.ContextMenu();
			if (ViewModel.ActiveCommand != ModelViewViewModel.DesignCommands.None)
			{
				var menuItem = new MenuItem();
				menuItem.Header = CultureManager.GetLocalizedString("Back");
				menuItem.Click += (s, e) =>
				{
					ViewModel.EndDesignCommand(true);// CancelCommand(viewmodel.ActiveCommand);
				};
				//menuItem.Icon = new System.Windows.Controls.Image { Source = _viewportResources["BackIcon"] as DrawingImage, Width = 16, Height = 16 };
				menuItem.IsEnabled = true;
				newMenu.Items.Add(menuItem);

				if (ViewModel.ActiveCommand == ModelViewViewModel.DesignCommands.RunPlugin || ViewModel.ActiveCommand == ModelViewViewModel.DesignCommands.EditPlugin)
				{
					menuItem = new MenuItem();
					menuItem.Header = CultureManager.GetLocalizedString("Enter");
					menuItem.Command = ViewModel.FeatureEngine.EnterCommand;
					//menuItem.Icon = new System.Windows.Controls.Image { Source = _viewportResources["EnterIcon"] as DrawingImage, Width = 16, Height = 16 };
					menuItem.ToolTip = CultureManager.GetLocalizedString("Sends an Enter keypress to the plugin.");
					newMenu.Items.Add(menuItem);
				}

				menuItem = new MenuItem();
				menuItem.Header = CultureManager.GetLocalizedString("End Command");
				menuItem.Command = ViewModel.EndCommandCommand;
				//menuItem.Icon = new System.Windows.Controls.Image { Source = _viewportResources["ExitIcon"] as DrawingImage, Width = 16, Height = 16 };
				newMenu.Items.Add(menuItem);

				newMenu.Items.Add(new Separator());
			}
			if (_viewport is Base3DViewport b3dvp)
			{
				foreach (var vitem in b3dvp.GetBase3DViewPortItems())
				{
					vitem.Header = CultureManager.GetLocalizedString((string)vitem.Header);
					newMenu.Items.Add(vitem);
				}
			}

			// 
			newMenu.Items.Add(new Separator());
			//
			var item = new MenuItem();
			item.Header = CultureManager.GetLocalizedString("Remove Object");
			item.Icon = new Image { Source = _viewportResources["RightClick_Delete"] as System.Windows.Media.DrawingImage, Width = 16, Height = 16 };
			item.IsEnabled = (ViewModel.ContextualNode as Epx.BIM.Models.ModelBaseNode)?.Geometry3D != null;
			item.Click += (s, e) =>
			{
				ViewModel.RemoveContextualNodes();
			};
			newMenu.Items.Add(item);
#if DEBUG
			var node = ViewModel.ContextualNodes.FirstOrDefault();
			if (node is Valos.Ifc.ValosIfcSpatialNode ifcNode)
			{
				item = new MenuItem();
				item.Header = CultureManager.GetLocalizedString("Regenerate Object");
				item.Icon = new Image { Source = _viewportResources["ResetIcon"] as System.Windows.Media.DrawingImage, Width = 16, Height = 16 };
				item.IsEnabled = (ViewModel.ContextualNode as Epx.BIM.Models.ModelBaseNode)?.Geometry3D != null;
				item.Click += (s, e) =>
				{
					RegenerateContextualNode();
				};
				newMenu.Items.Add(item);
			}
#endif
			newMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.MousePoint;
			newMenu.IsOpen = true;
			_viewport.ContextMenu = newMenu;
		}

		private void RegenerateContextualNode()
		{
			var node = ViewModel.ContextualNodes.FirstOrDefault();
			if (node is Valos.Ifc.ValosIfcSpatialNode ifcNode)
			{
				ifcNode.RegenerateGeometry();
				Infrastructure.Events.Update3D.PublishGeometryOnly(ifcNode);
			}
		}
		//IViewController
		public View3DMode ViewMode { get; set; } //TODO
		public void ResetView()
		{
			if(_viewport is GLWPFViewPort.GLModelViewport glView)
			{
				glView.Camera.ResetToDefault();
				glView.RenderRequest(RepaintReason.CameraChanged); //need immediate update
			}
			else if(_viewport is ModelViewport mView)
			{
				mView.ViewController.ResetView();
			}
		}

	}
}
