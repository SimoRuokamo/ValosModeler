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
using System.Runtime.Serialization.Formatters;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using ValosModeler.Views.Model3DView.Visuals;
using Enterprixe.WPF.Tools.Elements3D;

namespace ValosModeler.Views.Model3DView
{
	/// <summary>
	/// Class ModelViewport.
	/// </summary>
	/// <seealso cref="Enterprixe.WPF.Tools.Viewport.Base3DViewport" />
	public class ModelViewport : Enterprixe.WPF.Tools.Viewport.Base3DViewport, IPointInputToolController
	{
		/// <summary>
		/// Initializes static members of the <see cref="ModelViewport"/> class.
		/// </summary>
		static ModelViewport()
		{
			FocusableProperty.OverrideMetadata(typeof(ModelViewport), new UIPropertyMetadata(true));
		}

		protected ModelViewViewModel ViewModel
		{
			get { return DataContext as ModelViewViewModel; }
		}

		private bool _snapsEnabled = false;
		private Epx.BIM.GridMesh.SnapPoint3D _snapPoint = new Epx.BIM.GridMesh.SnapPoint3D();
		private bool _IsPickPointOperation = false;
		private bool _setRelativeInputPoint = false;
		private bool _isRelativePointSet=false;

		#region PointInputTool, IPointInputToolController

		public PointInputTool PointInputTool { get; set; }

		

		public bool IsPickPointOperation
		{
			get { return _IsPickPointOperation; }
			set
			{
				_IsPickPointOperation = value;
				_snapsEnabled = value;
				if (_snapsEnabled)
					Cursor = Cursors.Cross;
				else
					Cursor = Cursors.Arrow;
				if (ViewModel != null && !_snapsEnabled)
				{
					ViewModel.UpdateSnapMark(null);
				}
			}
		}


		public bool SetRelativeInputPoint
		{
			get { return _setRelativeInputPoint; }
			set
			{
				if(_setRelativeInputPoint != value)
				{
					_setRelativeInputPoint = value;
					if(_setRelativeInputPoint)
						_isRelativePointSet = false;
					if(_setRelativeInputPoint && ViewModel != null)
					{
						PointInputTool.EndInput();
						ViewModel.SetCommandStatus(-2);
					}
					else if(ViewModel != null)
					{
						ViewModel.SetCommandStatus(-1);
					}
				}
				//ViewModel.SetRelativeInputPoint = value;
			}
		}

		public Enterprixe.WPF.Tools.Viewport.WireBase.IWireHost Viewport
		{
			get { return this; }
		}

		/// <summary>
		/// The grid marked as the current one to use from the currently displayed grids. (From the ViewModel.)
		/// </summary>
		/// <value>The current grid.</value>
		public Epx.BIM.GridMesh.GridMesh CurrentGrid
		{
			get
			{
				if(DataContext is ModelViewViewModel)
					return (DataContext as ModelViewViewModel).CurrentGrid;
				else
					return null;
			}
		}

		public Point GetScreenPoint(Point3D point)
		{
			return Enterprixe.WPF.Tools.Viewport.ViewportInfo.Point3DtoPoint2D(this, point);
		}
		/// <summary>
		/// 
		/// </summary>
		public Epx.BIM.GridMesh.SnapPoint3D SnapPoint
		{
			get { return _snapPoint; }
		}

		private List<HighlightableVisual3D> _selectedVisuals = new List<HighlightableVisual3D>(); // visual because of highlight and tempmaterial
		/// <summary>
		/// Adds the current _designCommandVisual to the list of selected visuals.
		/// </summary>
		public void AddSelectedVisual()
		{
			if(MouseOverVisual is HighlightableVisual3D) _selectedVisuals.Add(MouseOverVisual as HighlightableVisual3D);
		}
#if NETCoreOnly
		internal ModelVisual3D MouseOverVisual { get; set; }
#endif

		#endregion


#if !NETCoreOnly

		#region Dependency Properties

		public ModelViewViewModel.DesignCommands ActiveCommand
		{
			get { return (ModelViewViewModel.DesignCommands)GetValue(ActiveCommandProperty); }
			set { SetValue(ActiveCommandProperty, value); }
		}
		public static readonly DependencyProperty ActiveCommandProperty =
			DependencyProperty.Register("ActiveCommand", typeof(ModelViewViewModel.DesignCommands), typeof(ModelViewport), new UIPropertyMetadata(ModelViewViewModel.DesignCommands.None, ActiveCommandPropertyChanged));

		/// <summary>
		/// Commands start here. Do tasks required to set up the new command and end the previous command (if there was one).
		/// </summary>
		/// <param name="d"></param>
		/// <param name="e"></param>
		protected static void ActiveCommandPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			ModelViewport trussViewport = d as ModelViewport;
			ModelViewViewModel viewmodel = trussViewport.DataContext as ModelViewViewModel;

			//trussViewport.ClearPreviousCommand((ModelViewViewModel.DesignCommands)e.OldValue, (ModelViewViewModel.DesignCommands)e.NewValue);
			//trussViewport.SetupNewCommand((ModelViewViewModel.DesignCommands)e.NewValue);
		}

#endregion


		/// <summary>
		/// Initializes a new instance of the <see cref="ModelViewport"/> class.
		/// </summary>
		public ModelViewport()
		{
			PivotAroundMousePoint = true;
			ReverseRotateDirInViewportHalf = false;
			ReverseRotateDir = true;
			PreviewKeyDown += ModelViewport_PreviewKeyDown;
			PreviewKeyUp += ModelViewport_PreviewKeyUp;
			DataContextChanged += ModelViewport_DataContextChanged;
			SizeChanged += ModelViewport_SizeChanged;
		}

		private void ModelViewport_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			if (ViewModel is ModelViewViewModel)
			{
				//ViewModel.ViewportActualWidth = e.NewSize.Width;
				//ViewModel.ViewportActualHeight = e.NewSize.Height;
			}
		}

		private void ModelViewport_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			if (ViewModel is ModelViewViewModel)
			{
				//ViewModel.ViewportActualWidth = this.ActualWidth;
				//ViewModel.ViewportActualHeight = this.ActualHeight;
				ViewModel.FeatureEngine = new FeatureEngineViewModel(this, ViewModel);
			}
		}
	
#region Mouse

		protected override void OnMouseWheel(MouseWheelEventArgs e)
		{
			//if (Keyboard.Modifiers == ModifierKeys.Shift && this.IsEnabled && !IsEditingObject)
			//{
			//	// some command
			//}
			//else
			{
				//if (Keyboard.Modifiers == ModifierKeys.Control && ViewModel.DesignViewEnabled)
				//{
				//	// skip Ctrl modifier from base
				//	if (LockMouse)
				//		return;
				//	else
				//		ZoomAtPoint(-e.Delta);
				//}
				//else
				base.OnMouseWheel(e);

				Point p = e.GetPosition(this);
				GetHitTestResult(p);
				GetSnapPoint(p);

				ViewModel.UpdateDimensionsOverlay();
			}
		}

		protected override void OnMouseDown(MouseButtonEventArgs e)
		{
			base.OnMouseDown(e);
		}


		private Point _mouseDownPoint;
		private Epx.BIM.GridMesh.SnapPoint3D _mouseDownGlobalPoint;
		private Epx.BIM.GridMesh.SnapPoint3D _mouseDownGlobalPointPrevious;
		private Point _mouseDownPosition;
		//private Point3D _originalPoint3D;

		protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
		{
			ModelViewViewModel ViewModel = DataContext as ModelViewViewModel;
			_mouseDownPosition = e.GetPosition(this);

			if (_ContextMenuPan)
			{
				base.OnMouseLeftButtonDown(e);
				return;
			}
			else if (SetRelativeInputPoint)
			{
				PointInputTool.StartInput(GetSnapPoint(_mouseDownPosition).Point);
				_isRelativePointSet = true;
				base.OnMouseLeftButtonDown(e);
				return;
			}
			// viewport rotate during _isEditingObject does not work because of the edit is ended here in the MouseDown
			else if (IsEditingObject)
			{
				EditingObjectEnded();
				base.OnMouseLeftButtonDown(e);
				return;
			}

			//if (_snapPoint != null && _snapPoint.Point != Epx.BIM.GridMesh.SnapPoint3D.EmptyPoint)
			//{
			//	// can cause missed hits below (Snappoint found by mousemove in corner. Hittest is so exact it will find WireLines hits but not hit the actual member visual.)
			//	// Is it needed to use already found snap point's screen point? Better to always use actual mouse position, it is the same as for the already found snap point.
			//	_mouseDownPoint = _snapPoint.ScreenPoint; 
			//}
			//else
			{
				_mouseDownPoint = _mouseDownPosition; // _mouseDownPoint should only be used here in MouseDown
			}
			this.Focus();

			// An edit visual was hit, store start conditions
			if (MouseOverVisual != null && MouseOverVisual is ModelEditVisual3D)
			{
				//Keyboard.Focus(this);
				_modifiersAtEditMouseDown = Keyboard.Modifiers;
				_keysAtEditMouseDown = new List<Key>(_keysDown);

				// TODO get the actual point being edited (hit to handle is not it). Save these in the visual, not here.
				//if (MouseOverVisual is TrussFrameEditVisual3D || MouseOverVisual is BuildingPartEditVisual3D)
				//{
				//	if (MouseOverVisual is TrussFrameEditVisual3D)
				//	{
				//		_originalPoint3D = (MouseOverVisual as TrussFrameEditVisual3D).Origin;
				//		_original3DAxis = ((MouseOverVisual as TrussFrameEditVisual3D).AttachedNode as PlanarStructure).XAxis;
				//		_original3DOrigin = ((MouseOverVisual as TrussFrameEditVisual3D).AttachedNode as PlanarStructure).AlignedStartPoint;
				//	}
				//	_mouseDownPoint = ViewportInfo.Point3DtoPoint2D(this, _originalPoint3D);
				//	var mouseDownPoint = new Epx.BIM.GridMesh.SnapPoint3D(_originalPoint3D);
				//	mouseDownPoint.ScreenPoint = ViewportInfo.Point3DtoPoint2D(this, mouseDownPoint.Point);
				//	_mouseDownGlobalPoint = mouseDownPoint;
				//}
			}
			else // do always, if viewport rotate previous mousedown point is restored in mouse move
			{
				// GetSnapPoint() will not always return a hit to the part if the point given is from a previous _snapped_ point.
				// The given point will only hit exactly the WireLine but not the actual part.

				_mouseDownGlobalPointPrevious = _mouseDownGlobalPoint;
				_mouseDownGlobalPoint = GetSnapPoint(_mouseDownPoint);
			}

			base.OnMouseLeftButtonDown(e);
		}

		private Point _mouseUpPosition;
		private Point _mouseUpPoint;
		private bool _viewportRotateOperation;
		private List<Point3D> _pickedPoints = new List<Point3D>();
		protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
		{
			_mouseUpPosition = e.GetPosition(this);
			if (_panOperation)
			{
				base.OnMouseLeftButtonUp(e);
				return;
			}

			if (SetRelativeInputPoint && _isRelativePointSet)
			{
				SetRelativeInputPoint = false;
			}
			else if (!IsEditingObject && ViewModel != null && !_viewportRotateOperation && !_panOperation && !SetRelativeInputPoint)
			{
				_mouseUpPoint = _mouseUpPosition;
				if (_snapPoint.IsSnapped) _mouseUpPoint = _snapPoint.ScreenPoint;

				switch (ViewModel.ActiveCommand)
				{
#region ActiveCommands

					case ModelViewViewModel.DesignCommands.RunPlugin:
					case ModelViewViewModel.DesignCommands.EditPlugin:
						ViewModel.FeatureEngine.PluginMouseUp(_snapPoint.Point, MouseOverNode);
						break;

#endregion //ActiveCommands

					// No command in progress, perform regular mouse click
					default:
						//GetHitTestResult(_mouseUpPosition);
						if (HitDetected)
						{
							Keyboard.Focus(this);
							if (MouseOverNode != null && !(MouseOverVisual is ModelEditVisual3D) && !_isPickEditingObject)
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
							else if (MouseOverVisual is ModelEditVisual3D || _isPickEditingObject)
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
								// "Unselect" selected member
								SetHoverText(null);
								//if (ViewModel.CurrentTruss != null)
								//{
								//	// if a current truss exists, leave its tab visible
								//	ClearSelections(false);
								//	ViewModel.Mediator.NotifyColleagues<BaseDataNode>(MediatorMessages.SetContextualTab, ViewModel.CurrentTruss);
								//}
								//else
								{
									ClearSelections();
								}
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
			base.OnMouseLeftButtonUp(e);
		}

		protected override void OnMouseMove(MouseEventArgs e)
		{
			if (e.MiddleButton == MouseButtonState.Pressed) _panOperation = true;

			if (_ContextMenuPan || _panOperation)
			{
				base.OnMouseMove(e);
				ViewModel.UpdateDimensionsOverlay();
				ViewModel.UpdateSnapMark(null);
				return;
			}

			Point viewportPosition = e.GetPosition(this);
			_lastMovePosition = viewportPosition;
			_lastZoomAtPoint = null;
			if (_snapsEnabled)
				Cursor = Cursors.Cross;
			if ((viewportPosition - _mouseDownPosition).Length < 0.01 || (viewportPosition - _mouseUpPosition).Length < 0.01)
			{
				return; // Mouse.Capture raises MouseMove event even if mouse didn't move.
			}
			else if (SetRelativeInputPoint)
			{
				GetHitTestResult(viewportPosition);
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
					PointInputTool.StartInput(_mouseDownGlobalPoint.Point);
				Mouse.OverrideCursor = Cursors.Cross;
				//if (MouseOverVisual is TrussEditVisual3D && (MouseOverVisual as TrussEditVisual3D).IsMemberEditVisual) ViewModel.SetCurrentDisplayedGrids((MouseOverVisual as TrussEditVisual3D).AttachedNode.GetParent<Member>());
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
					this.Focus();
				}
			}

			if (_restorePointInputStart)
			{
				if (_mouseDownGlobalPoint != null && !PointInputTool.IsMoveConstrained)
					PointInputTool.StartInput(_mouseDownGlobalPoint.Point);
				_restorePointInputStart = false;
			}

			if (ViewModel != null)
			{
				// An edit operation is happening on an edit visual. Prevents viewport rotation.
				if (IsEditingObject)
				{
					if (MouseOverVisual is ModelEditVisual3D)
					{
						GetHitTestResult(viewportPosition);
						GetSnapPoint(viewportPosition);
						viewportPosition = _snapPoint.ScreenPoint;

						if (PointInputTool != null && PointInputTool.IsActive && _mouseDownGlobalPoint != null)
							PointInputTool.MoveInput(_snapPoint.Point);

						if (MouseOverVisual is ModelEditVisual3D)
						{
							EditModelPartMouseMove(viewportPosition);
						}
					}
					ViewModel.UpdateDimensionsOverlay();
				}
				else if (!_viewportRotateOperation) // No edit operation. Perform model highlighting and Active command operations.
				{
					RestoreMouseOverHighlights();
					GetHighlightHitTestResult(viewportPosition);
					GetSnapPoint(viewportPosition);

					if (HitDetected)
					{
						Guid selectedElementID = Guid.Empty;
						if (ViewModel.ContextualNode != null) selectedElementID = ViewModel.ContextualNode.UniqueID;

						HighlightableVisual3D mouseovervisual = MouseOverVisual as HighlightableVisual3D;
						if (mouseovervisual != null)
						{
							// Highlight the mouse over model if it is not the selected model
							if (MouseOverNode.UniqueID != selectedElementID || mouseovervisual is ModelEditVisual3D)
							{
								//if (mouseovervisual is TrussPartVisual3D)
								//{
								//	// highlight the "assembly" instead of the individual part
								//	var trussPartVisual = mouseovervisual as TrussPartVisual3D;
								//	if (trussPartVisual.AttachedNode is Model2DNode)
								//	{
								//		var modelPart2D = trussPartVisual.AttachedNode as Model2DNode;
								//		if (modelPart2D.GetParent<PlanarStructure>() != ViewModel.CurrentTruss)
								//		{
								//			var visuals = GetHighlightableVisual3Ds(modelPart2D);// GetTrussVisual3Ds<TrussPartVisual3D>(v => modelPart2D.GetParent<PlanarStructure>().IsDescendant(v.AttachedNode) && (v.AttachedNode is Member || v.AttachedNode is MetalWeb));
								//			if (visuals != null) _highLightedVisuals.AddRange(visuals);
								//			_highLightedVisuals.ForEach(v => v.IsHighlighted = true);
								//		}
								//	}
								//}
								//if (_highLightedVisuals.Count == 0) 
								if (!ViewModel.FeatureEngine.IsTemporaryNode(MouseOverNode))
									mouseovervisual.IsHighlighted = true;
							}
							SetHoverText(mouseovervisual);

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
					}

					if (PointInputTool != null && PointInputTool.IsActive)
						PointInputTool.MoveInput(_snapPoint.Point);

#region ActiveCommands

					// Handle design commands
					switch (ViewModel.ActiveCommand)
					{
						case ModelViewViewModel.DesignCommands.RunPlugin:
						case ModelViewViewModel.DesignCommands.EditPlugin:
							ViewModel.FeatureEngine.PluginMouseMove(viewportPosition, _snapPoint, _mouseDownGlobalPoint, MouseOverVisual as ModelPartVisual3D);
							break;

						default:
							break;
					}

#endregion ActiveCommands
				}
			}

			if (!IsEditingObject && ViewModel != null)
			{
				if (_LeftButtonCaptured && _viewportRotateOperation)
					base.OnMouseMove(e);
			}
		}

		private void EditModelPartMouseMove(Point viewportPosition)
		{
		}

		private List<HighlightableVisual3D> _highLightedVisuals = new List<HighlightableVisual3D>();
		/// <summary>
		/// Restore materials and highlights before hit test.
		/// </summary>
		private void RestoreMouseOverHighlights(bool clearStatusText = true)
		{
			// Restore materials and highlights before hit test.
			if (MouseOverVisual != null)
			{
				if (MouseOverVisual is HighlightableVisual3D)
				{
					(MouseOverVisual as HighlightableVisual3D).IsHighlighted = false;
				}
				if (clearStatusText) SetHoverText(null);

				if (!IsEditingObject)
				{
					MouseOverVisual = null;
					MouseOverNode = null;
					//ViewModel.ViewportMouseOverVisual = MouseOverVisual;
				}
			}

			_highLightedVisuals.ForEach(v => v.IsHighlighted = false);
			_highLightedVisuals.Clear();
		}


		private ModelEditVisual3D _mousePickEditVisual = null;

		protected void ToggleMousePickEdit()
		{
			if (!_isPickEditingObject)
			{
				_mousePickEditVisual = MouseOverVisual as ModelEditVisual3D;
				_isPickEditingObject = true;

				_disableNextAltKey = true;
				IsPickPointOperation = true;
				Mouse.OverrideCursor = Cursors.Cross;
				if (_mouseDownGlobalPoint != null)
					PointInputTool.StartInput(_mouseDownGlobalPoint.Point);

				// TODO ViewModel.SetCurrentDisplayedGrids(
				//if (MouseOverVisual is TrussEditVisual3D && (MouseOverVisual as TrussEditVisual3D).IsMemberEditVisual) ViewModel.SetCurrentDisplayedGrids((MouseOverVisual as TrussEditVisual3D).AttachedNode.GetParent<Member>());

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
		private void SetHoverText(HighlightableVisual3D mouseovervisual)
		{
			if (mouseovervisual == null)
			{
				ViewModel.SetHoverText(string.Empty);
			}
			//else if (mouseovervisual is MemberEditVisual3D)
			//{
			//	if ((MouseOverVisual as MemberEditVisual3D).IsStartPoint)
			//	{
			//		ViewModel.SetHoverText(CultureManager.GetLocalizedStringByKey("_move_member_start"));
			//	}
			//	else if ((MouseOverVisual as MemberEditVisual3D).IsEndPoint)
			//	{
			//		ViewModel.SetHoverText(CultureManager.GetLocalizedStringByKey("_move_member_end"));
			//	}
			//	else if ((MouseOverVisual as MemberEditVisual3D).IsMiddlePoint)
			//	{
			//		if ((MouseOverVisual as MemberEditVisual3D).AttachedNode is SideMember)
			//			ViewModel.SetHoverText(CultureManager.GetLocalizedStringByKey("_move_member_mid"));
			//		else
			//			ViewModel.SetHoverText(CultureManager.GetLocalizedStringByKey("_move_member_mid"));
			//	}
			//}
			else if (mouseovervisual is ModelPartVisual3D)
			{
				BaseDataNode attachedNode = null;
				if (mouseovervisual is ModelPartVisual3D) attachedNode = (mouseovervisual as ModelPartVisual3D).AttachedNode;

				string hoverText = (mouseovervisual as ModelPartVisual3D).HoverText;
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
			else if (mouseovervisual is ModelEditVisual3D)
			{
				BaseDataNode attachedNode = null;
				if (mouseovervisual is ModelEditVisual3D) attachedNode = (mouseovervisual as ModelEditVisual3D).AttachedNode;

				string hoverText = (mouseovervisual as ModelEditVisual3D).HoverText;
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

		protected override void OnKeyDown(KeyEventArgs e)
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
					else ViewModel.EndDesignCommand(true);// CancelCommand(ViewModel.ActiveCommand);
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
					//if (ViewModel.MoveCopyEngine.IsRemoveAllowed)
					//{
					//	ViewModel.RemoveNodesAction();//
					//}
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

			base.OnKeyDown(e);
		}

		private void KeyDownEnter(bool isClosed = false)
		{

		}

		private bool _disableNextAltKey; // Disable next Alt system key press
		private bool _controlModifierOverriden = false;
		private bool _restorePointInputStart = false;
		protected override void OnKeyUp(KeyEventArgs e)
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

			base.OnKeyUp(e);
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
			_modifiersAtEditMouseDown = ModifierKeys.None;
			_keysAtEditMouseDown.Clear();

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
		//private void SetupNewCommand(ModelViewViewModel.DesignCommands newCommand)
		//{
		//	// GetSnapPoint() will not always return a hit to the part if the point given is from a previous _snapped_ point.
		//	// The given point will only hit exactly the WireLine but not the actual part.
		//	// Use visible or invisible _helperPoints and query the input point from their origins as a workaround.

		//	if (newCommand != ModelViewViewModel.DesignCommands.None)
		//	{
		//		//ClearSelections(false); //cannot clear ContextualNode generally bc many commands rely on it, commands should use ActiveCommandNode
		//		RestoreMouseOverHighlights(false);
		//		Mouse.OverrideCursor = Cursors.Cross;
		//	}

		//	_mouseDownGlobalPoint = null;

		//	switch (newCommand)
		//	{
		//		case ModelViewViewModel.DesignCommands.RunPlugin:
		//		case ModelViewViewModel.DesignCommands.EditPlugin:
		//			IsPickPointOperation = true;
		//			//ViewModel.SetCurrentDisplayedGrids(ViewModel.CurrentTruss);
		//			break;

		//		default:
		//			_snapsEnabled = false;
		//			break;
		//	}
		//	this.Focus();
		//}

		///// <summary>
		///// Clears the temporary values of the previous command when the design command is changed.
		///// </summary>
		///// <param name="currentcommand"></param>
		//private void ClearPreviousCommand(ModelViewViewModel.DesignCommands previouscommand, ModelViewViewModel.DesignCommands newcommand)
		//{
		//	if (previouscommand != ModelViewViewModel.DesignCommands.None)
		//	{
		//		PointInputTool.EndInput();
		//		ViewModel.CurrentDisplayedGrids = null;
		//		ViewModel.CurrentDXF = null;
		//		ViewModel.UpdateSnapMark(null);
		//		IsPickPointOperation = false;
		//		_mouseDownGlobalPoint = null;
		//		Mouse.OverrideCursor = null;
		//	}

		//	switch (previouscommand)
		//	{
		//		case ModelViewViewModel.DesignCommands.RunPlugin:
		//		case ModelViewViewModel.DesignCommands.EditPlugin:
		//			foreach (HighlightableVisual3D visual in _selectedVisuals)
		//			{
		//				visual.RestoreMaterial();
		//			}
		//			_selectedVisuals.Clear();
		//			//ViewModel.EndDesignCommand(true);
		//			break;

		//		default:
		//			foreach (HighlightableVisual3D visual in _selectedVisuals)
		//			{
		//				visual.RestoreMaterial();
		//			}
		//			_selectedVisuals.Clear();
		//			break;
		//	}
		//}

		///// <summary>
		///// Step by step cancel of the active command.
		///// </summary>
		//private void CancelCommand(ModelViewViewModel.DesignCommands command)
		//{
		//	if (SetRelativeInputPoint) return;

		//	Mouse.Capture(null);
		//	PointInputTool.EndInput();

		//	if (ViewModel != null)
		//	{
		//		ViewModel.SetCommandStatus(0);

		//		switch (command)
		//		{
		//			case ModelViewViewModel.DesignCommands.RunPlugin:
		//			case ModelViewViewModel.DesignCommands.EditPlugin:
		//				foreach (HighlightableVisual3D visual in _selectedVisuals)
		//				{
		//					visual.RestoreMaterial();
		//				}
		//				_selectedVisuals.Clear();
		//				ViewModel.EndDesignCommand(true);
		//				break;

		//			default:
		//				foreach (HighlightableVisual3D visual in _selectedVisuals)
		//				{
		//					visual.RestoreMaterial();
		//				}
		//				_selectedVisuals.Clear();
		//				ViewModel.ActiveCommand = ModelViewViewModel.DesignCommands.None;
		//				break;
		//		}

		//		if (ViewModel.ActiveCommand == ModelViewViewModel.DesignCommands.None)
		//		{
		//			Mouse.OverrideCursor = null;
		//		}
		//	}
		//}

#endregion //Commands

#region ContextMenu

		protected override void CreateContextMenu()
		{
			ModelViewViewModel viewmodel = DataContext as ModelViewViewModel;
			var newMenu = new System.Windows.Controls.ContextMenu();

			if (viewmodel.ActiveCommand != ModelViewViewModel.DesignCommands.None)
			{
				var menuItem = new MenuItem();
				menuItem.Header = CultureManager.GetLocalizedString("Back");
				menuItem.Click += (s, e) =>
				{
					viewmodel.EndDesignCommand(true);// CancelCommand(viewmodel.ActiveCommand);
				};
				//menuItem.Icon = new System.Windows.Controls.Image { Source = _viewportResources["BackIcon"] as DrawingImage, Width = 16, Height = 16 };
				menuItem.IsEnabled = true;
				newMenu.Items.Add(menuItem);

				if (viewmodel.ActiveCommand == ModelViewViewModel.DesignCommands.RunPlugin || viewmodel.ActiveCommand == ModelViewViewModel.DesignCommands.EditPlugin)
				{
					menuItem = new MenuItem();
					menuItem.Header = CultureManager.GetLocalizedString("Enter");
					menuItem.Command = viewmodel.FeatureEngine.EnterCommand;
					//menuItem.Icon = new System.Windows.Controls.Image { Source = _viewportResources["EnterIcon"] as DrawingImage, Width = 16, Height = 16 };
					menuItem.ToolTip = CultureManager.GetLocalizedString("Sends an Enter keypress to the plugin.");
					newMenu.Items.Add(menuItem);
				}

				menuItem = new MenuItem();
				menuItem.Header = CultureManager.GetLocalizedString("End Command");
				menuItem.Command = viewmodel.EndCommandCommand;
				//menuItem.Icon = new System.Windows.Controls.Image { Source = _viewportResources["ExitIcon"] as DrawingImage, Width = 16, Height = 16 };
				newMenu.Items.Add(menuItem);

				newMenu.Items.Add(new Separator());
			}

			foreach (var item in GetBase3DViewPortItems())
			{
				newMenu.Items.Add(item);
			}

			ContextMenu = newMenu;
			ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.MousePoint;
			ContextMenu.IsOpen = true;
		}

#endregion //ContextMenu

#region Hit Test

		public BaseDataNode MouseOverNode
		{
			get { return (BaseDataNode)GetValue(MouseOverNodeProperty); }
			set { SetValue(MouseOverNodeProperty, value); }
		}
		public static readonly DependencyProperty MouseOverNodeProperty =
			DependencyProperty.Register("MouseOverNode", typeof(BaseDataNode), typeof(ModelViewport), new UIPropertyMetadata(null));

		public ModelVisual3D MouseOverVisual
		{
			get { return (ModelVisual3D)GetValue(MouseOverVisualProperty); }
			set { SetValue(MouseOverVisualProperty, value); }
		}
		public static readonly DependencyProperty MouseOverVisualProperty =
			DependencyProperty.Register("MouseOverVisual", typeof(ModelVisual3D), typeof(ModelViewport), new UIPropertyMetadata(null));

		protected bool HitDetected { get; set; }
		protected Point3D HitPoint { get; set; }
		protected List<HitTestResult> _allHitTestResults = new List<HitTestResult>();

		protected void GetHighlightHitTestResult(Point point)
		{
			if (!IsEditingObject)
			{
				MouseOverNode = null;
				MouseOverVisual = null;
			}
			_allHitTestResults.Clear();
			HitDetected = false;

			base.GetHitTestResult(point);

			foreach (var htr in _allHitTestResults)
			{
				var res = HitTestProcessing(htr);
				if (res == HitTestResultBehavior.Stop) break;
			}
		}

		protected override void GetHitTestResult(Point point)
		{
			_allHitTestResults.Clear();
			base.GetHitTestResult(point);
		}

		protected override HitTestFilterBehavior HitTestFilterCallback(DependencyObject o)
		{
			if (o is WireBase)
				return HitTestFilterBehavior.ContinueSkipSelfAndChildren;
			else
				return HitTestFilterBehavior.Continue;
		}

		protected override HitTestResultBehavior HitTestResultCallback(HitTestResult result)
		{
			if (_isPivotPointHitTest)
			{
				return base.HitTestResultCallback(result);
			}

			HitTestResult htResult = result as HitTestResult;
			if (htResult != null)
			{
				_allHitTestResults.Add(htResult);
			}
			return HitTestResultBehavior.Continue;
		}

		protected HitTestResultBehavior HitTestProcessing(HitTestResult result)
		{
			HitTestResult htResult = result as HitTestResult;
			if (htResult != null)
			{
				RayMeshGeometry3DHitTestResult rayhit = htResult as RayMeshGeometry3DHitTestResult;
				if (rayhit != null)
				{
					HitDetected = true;
					HitPoint = rayhit.PointHit;
					ModelVisual3D visual3D = htResult.VisualHit as ModelVisual3D;

					if (IsEditingObject && visual3D is ModelEditVisual3D)
					{
						HitDetected = false;
						return HitTestResultBehavior.Continue;
					}
					else if (ViewModel.ActiveCommand != ModelViewViewModel.DesignCommands.None && visual3D is ModelEditVisual3D)
					{
						HitDetected = false;
						return HitTestResultBehavior.Continue;
					}
					else if (visual3D is ModelEditVisual3D)
					{
						MouseOverVisual = visual3D;
						BaseDataNode hitnode = (visual3D as ModelEditVisual3D).AttachedNode;
						if (MouseOverNode != hitnode) MouseOverNode = hitnode;

					}
					else if (visual3D is ModelPartVisual3D)
					{
						MouseOverVisual = visual3D;
						BaseDataNode hitnode = (visual3D as ModelPartVisual3D).AttachedNode;
						if (MouseOverNode != hitnode) MouseOverNode = hitnode;

					}
					else if (visual3D is GridMeshVisual3D)
					{
						if (_isPivotPointHitTest && (visual3D as GridMeshVisual3D).AttachedGrid.IsCurrentGrid)
						{
							// set the hitpoint for pivot point testing in base viewport
							HitPoint = (visual3D as GridMeshVisual3D).AttachedGrid.LocalToGlobal.Transform(rayhit.PointHit);
						}
						else
						{
							HitDetected = false;
						}
						return HitTestResultBehavior.Continue; // continue even if _isPivotPointHitTest				
					}
					else if (visual3D is WireBase || htResult.VisualHit is Visual3D)
					{
						HitDetected = false;
						return HitTestResultBehavior.Continue;
					}
					else
					{
						MouseOverNode = null;
					}
				}
				else
				{
					return HitTestResultBehavior.Continue;
				}
			}

			return HitTestResultBehavior.Stop;
		}

		/// <summary>
		/// Perform hit testing using a circular area around the point.
		/// </summary>
		/// <param name="point"></param>
		/// <param name="radius"></param>
		private void GetAreaHitTestResult(Point point, double radius)
		{
			GetHitTestResult(point);
			if (!HitDetected)
			{
				IList<Point> circlePoints = GeometryMath.GetRoundPolygon(point, radius, 8);

				foreach (Point p in circlePoints)
				{
					GetHitTestResult(p);
					if (HitDetected)
					{
						break;
					}
				}
			}
		}

		private List<HitTestResult> GetAreaHitTestResults(Point point, double radius)
		{
			List<HitTestResult> retval = new List<HitTestResult>();
			IList<Point> circlePoints = GeometryMath.GetRoundPolygon(point, radius, 8);

			foreach (Point p in circlePoints)
			{
				retval.AddRange(GetAllHitTestResults(p));
			}
			retval.AddRange(GetAllHitTestResults(point));

			return retval;
		}

		private List<HitTestResult> GetAllHitTestResults(Point point)
		{
			List<HitTestResult> hitResultList = new List<HitTestResult>();
			PointHitTestParameters pointparams = new PointHitTestParameters(point);
			VisualTreeHelper.HitTest(this, MeshHitTestFilter, (r) =>
			{
				hitResultList.Add(r);
				return HitTestResultBehavior.Continue;
			},
			pointparams);
			return hitResultList;
		}

		private HitTestFilterBehavior MeshHitTestFilter(DependencyObject o)
		{
			//Test for the object value you want to filter.
			if (o is ModelEditVisual3D /*|| o is HelperVisual3D*/)
			{
				return HitTestFilterBehavior.ContinueSkipSelfAndChildren;
			}
			//else if (o is Truss3D)
			//{
			//	PlanarStructure truss = (o as Truss3D).TrussNode as PlanarStructure;
			//	if (truss != null)
			//	{
			//		if (ViewModel.SnapCurrentTrussOnly && !truss.IsCurrent)
			//			return HitTestFilterBehavior.ContinueSkipSelfAndChildren;
			//	}
			//	return HitTestFilterBehavior.Continue;
			//}
			else
			{
				// Visual object is part of hit test results enumeration.
				return HitTestFilterBehavior.Continue;
			}
		}
#endregion

#region Snap
		

		/// <summary>
		/// On/off switch for snaps.
		/// </summary>
		/// <summary>
		/// Use _snapsEnabled to enable/disable snaps.
		/// Use private field _snapPoint for always up-to-date point.
		/// Updates snap mark overlay.
		/// </summary>
		/// <param name="point"></param>
		/// <returns></returns>
		private Epx.BIM.GridMesh.SnapPoint3D GetSnapPoint(Point point, bool getRawPoint = false)
		{
			// GetSnapPoint() will not always return a hit to the part if the point given is from a previous _snapped_ point.
			// The given point will only hit exactly the WireLine but not the actual part.

			Epx.BIM.GridMesh.SnapPoint3D snapPoint = new Epx.BIM.GridMesh.SnapPoint3D();
			//List<HitTestResult> hitResultList = _allHitTestResults;// GetAllHitTestResults(point);
			//List<HitTestResult> hitResultListFiltered = FilterSnapHitResults(hitResultList);
			var hitResults = GetSnapHitResults();
			var perpendicularGrids = ViewModel.GetPerpendicularGrids();
			var perpendicularDXFs = ViewModel.GetPerpendicularDXFs();
			var currentUCS = ViewModel.CurrentGrid;// != null ? ViewModel.CurrentGrid : new Epx.BIM.GridMesh.GridUCS();

			if (_snapsEnabled && !getRawPoint)
			{
				bool geometrySnapsEnabled = ViewModel.EnabledSnaps.Intersect(Epx.BIM.GridMesh.SnapPoint3D.GeometrySnapTypes).Any();
				bool gridSnapsEnabled = ViewModel.EnabledSnaps.Intersect(Epx.BIM.GridMesh.SnapPoint3D.GridSnapTypes).Any();
				List<Epx.BIM.GridMesh.SnapPoint3D> foundSnaps = new List<Epx.BIM.GridMesh.SnapPoint3D>();

				foreach (var r in hitResults)
				{
					if (geometrySnapsEnabled && r.VisualHit is ISnappableVisual snapV )
					{
						var modelPoint = snapV.GetSnapPoint(r.PointHit, SnapTolerance, ViewModel.EnabledSnaps);
						if (!ViewModel.FeatureEngine.IsTemporaryNode(modelPoint.Node))
						{
							modelPoint.ScreenPoint = ViewportInfo.Point3DtoPoint2D(this, modelPoint.Point);
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
					//if (currentUCS.HasParent<PlanarStructure>())
					//{
					//	ucsToGlobal = currentUCS.LocalToGlobal;
					//}
					plane3dPoint = GetPlane3DCoordinate(point, ucsToGlobal.Transform(currentUCS.Origin), ucsToGlobal.Transform(currentUCS.XAxis), ucsToGlobal.Transform(currentUCS.YAxis));
					if (plane3dPoint.HasValue)
					{
						Epx.BIM.GridMesh.SnapPoint3D gridPoint = currentUCS.GetSnapPoint3D(currentUCS.GlobalToLocal.Transform(plane3dPoint.Value), SnapTolerance, ViewModel.EnabledSnaps);

						if (ViewModel.EnabledSnaps.Contains(gridPoint.SnapType))
						{
							gridPoint.Point = currentUCS.LocalToGlobal.Transform(gridPoint.Point);
							gridPoint.ScreenPoint = ViewportInfo.Point3DtoPoint2D(this, gridPoint.Point);
							gridPoint.Node = currentUCS;
							snapPoint = gridPoint;
						}
					}
				}

				ViewModel.UpdateSnapMark(snapPoint);
			}

			if (!snapPoint.IsSnapped && hitResults.Count > 0)
			{
				// no snaps, check grid hit  
				var firstHit = hitResults.FirstOrDefault(hr => hr.VisualHit is GridMeshVisual3D);
				if (firstHit.VisualHit is GridMeshVisual3D)
				{
					snapPoint.Point = firstHit.PointHit;
					var gridNode = (firstHit.VisualHit as GridMeshVisual3D).AttachedGrid;
					snapPoint.Point = gridNode.LocalToGlobal.Transform(snapPoint.Point);
					snapPoint.Node = gridNode;
				}
				snapPoint.ScreenPoint = point;
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
					plane3dPoint = GetPlane3DCoordinate(point, currentUCS.Origin, currentUCS.XAxis, currentUCS.YAxis);
					if (plane3dPoint.HasValue) snapPoint.Point = plane3dPoint.Value;
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
							planepoint = GeometryMath.ToPoint(GetXZCoordinate(point, 0));
							snapPoint.Point = new Point3D(planepoint.X, planepoint.Y, 0);
						}
						else
						{
							planepoint = GeometryMath.ToPoint(GetYZCoordinate(point, 0));
							snapPoint.Point = new Point3D(planepoint.X, planepoint.Y, 0);
						}
					}
					else
					{
						planepoint = GeometryMath.ToPoint(GetXYCoordinate(point, 0));
						snapPoint.Point = new Point3D(planepoint.X, planepoint.Y, 0);
					}
				}
				else if (ViewModel.ViewportViewMode == View3DMode.Top)
				{
					planepoint = GeometryMath.ToPoint(GetXYCoordinate(point, 0));
					snapPoint.Point = new Point3D(planepoint.X, planepoint.Y, 0);
				}
				else if (ViewModel.ViewportViewMode == View3DMode.Right)
				{
					planepoint = GeometryMath.ToPoint(GetXZCoordinate(point, 0));
					snapPoint.Point = new Point3D(planepoint.X, planepoint.Y, 0);
				}
				else if (ViewModel.ViewportViewMode == View3DMode.Left)
				{
					planepoint = GeometryMath.ToPoint(GetYZCoordinate(point, 0));
					snapPoint.Point = new Point3D(planepoint.X, planepoint.Y, 0);
				}

				snapPoint.ScreenPoint = point;
			}

#if DEBUG
			if (double.IsNaN(snapPoint.Point.X))
			{
				System.Diagnostics.Debug.Fail("Snap point is NaN");
			}
#endif
			_snapPoint = snapPoint;
			if (!getRawPoint && PointInputTool != null) // && _mouseDownGlobalPoint != null)
			{
				if (currentUCS == null) currentUCS = new Epx.BIM.GridMesh.GridUCS();
				// keep objects which are not input in the grid plane leveled
				Epx.BIM.GridMesh.GridMesh overrideGrid = ViewModel.CurrentGrid != null ? null : currentUCS;
				Epx.BIM.GridMesh.GridMesh currentUCStrfd = new Epx.BIM.GridMesh.GridUCS();
				Vector3D cucsXAxis = currentUCS.XAxis;
				Vector3D cucsYAxis = currentUCS.YAxis;
				Point3D cucsOrigin = currentUCS.Origin;
				{
					currentUCStrfd.XAxis = cucsXAxis;
					currentUCStrfd.YAxis = cucsYAxis;
					currentUCStrfd.Origin = cucsOrigin;
				}

				bool removeMoveConstraints = false;
				bool orthoOn = Keyboard.Modifiers == ModifierKeys.Control;
				// keep objects which are not input in the grid plane leveled
				Point3D? planePoint = null;
				planePoint = GetPlane3DCoordinate(point, PointInputTool.IsActive ? PointInputTool.Start : currentUCStrfd.Origin, currentUCStrfd.XAxis, currentUCStrfd.YAxis);

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
							closestAngle = Vector3D.AngleBetween(closestDir, currentUCStrfd.XAxis);
							var mouseMoveDirLocal = currentUCStrfd.GlobalToLocal.Transform(mouseMoveDirection);
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
					_snapPoint.Point = planePoint.Value;
				}
				//_snapPoint.PointWithoutMoveConstraints = _snapPoint.Point;
				_snapPoint = PointInputTool.ApplyMoveConstraints(_snapPoint, overrideGrid);
				_snapPoint.ScreenPoint = ViewportInfo.Point3DtoPoint2D(this, _snapPoint.Point);

				if (removeMoveConstraints)
				{
					PointInputTool.RemoveMoveConstraint(PointInputConstraints.Angle);
					PointInputTool.RemoveMoveConstraint(PointInputConstraints.ZCoord);
				}
			}

			_snapPoint.Point = GeometryMath.RoundPoint(_snapPoint.Point);
			return _snapPoint;
		}

		internal List<HitResult> GetSnapHitResults()
		{
			var hitTests = FilterSnapHitResults(_allHitTestResults);
			List<HitResult> hitResults = new List<HitResult>();
			foreach(var hitTest in hitTests )
			{
				if (hitTest is RayMeshGeometry3DHitTestResult rayhit)
					hitResults.Add(new HitResult { PointHit = rayhit.PointHit, VisualHit = rayhit.VisualHit as ISnappableVisual });
			}
			return hitResults;
		}

		private List<HitTestResult> FilterSnapHitResults(List<HitTestResult> hitResultList)
		{
			List<HitTestResult> retval = new List<HitTestResult>();

			hitResultList.RemoveAll(hit => ViewModel.FeatureEngine.IsTemporaryNode(GetAttachedNode(hit.VisualHit)));

			foreach (HitTestResult r in hitResultList)
			{
				if (r is PointHitTestResult) continue;

				RayMeshGeometry3DHitTestResult rayhit = r as RayMeshGeometry3DHitTestResult;
				BaseDataNode attachedNode = GetAttachedNode(r.VisualHit);
				bool skipHit = false;

				//if (ViewModel.FeatureEngine.IsTemporaryNode(attachedNode))
				//{
				//	continue;
				//}

				if (r.VisualHit is HighlightableVisual3D)
				{
					if ((r.VisualHit as HighlightableVisual3D).IsGhostVisual)
					{
						retval.Add(r);
						continue;
					}

					if (attachedNode is ModelBaseNode && hitResultList.IndexOf(r) > 0) //skip indirect hit
					{
						skipHit = true;
					}

					if (attachedNode != null && IsEditingObject)
					{
						if (attachedNode == MouseOverNode)
							skipHit = true;
					}

					if (skipHit)
					{
						//do nothing
					}
					else
					{
						retval.Add(r);
					}
				}
				else if (r.VisualHit is GridMeshVisual3D || r.VisualHit is GridWireLinesVisual3D /*|| r.VisualHit is DXFMeshVisual3D || r.VisualHit is DXFWireLinesVisual3D*/)
				{
					if (r.VisualHit is GridMeshVisual3D || r.VisualHit is GridWireLinesVisual3D)
					{

					}

					//if (IsEditingObject && r.VisualHit is DXFMeshVisual3D mxfVisual)
					//{
					//	if (attachedNode == MouseOverNode.GetParent<Epx.BIM.GridMesh.ArbitraryMesh>()) skipHit = true;
					//}

					if (skipHit)
					{
						//do nothing
					}
					else
					{
						retval.Add(r);
					}
				}
			}
			return retval;
		}

		private Epx.BIM.BaseDataNode GetAttachedNode(DependencyObject visual)
		{
			BaseDataNode attachedNode = null;
			if (visual is ModelObjectVisual)
			{
				attachedNode = (visual as ModelObjectVisual).Node;
			}
			else if (visual is ModelPartVisual3D)
			{
				attachedNode = (visual as ModelPartVisual3D).AttachedNode;
			}
			else if (visual is GridMeshVisual3D)
			{
				attachedNode = (visual as GridMeshVisual3D).AttachedGrid;
			}
			//else if (visual is DXFMeshVisual3D)
			//{
			//	attachedNode = (visual as DXFMeshVisual3D).AttachedGrid;
			//}
			else if (visual is GridWireLinesVisual3D)
			{
				attachedNode = (visual as GridWireLinesVisual3D).AttachedGrid;
			}
			//else if (visual is DXFWireLinesVisual3D)
			//{
			//	attachedNode = (visual as DXFWireLinesVisual3D).AttachedGrid;
			//}
			//else if (visual is ReferencePlanesVisual3D)
			//{
			//	attachedNode = (visual as ReferencePlanesVisual3D).AttachedNode;
			//}
			return attachedNode;
		}

		/// <summary>
		/// For non current truss hits this does not return the true member or plate.
		/// </summary>
		/// <param name="hitResultList"></param>
		/// <param name="index"></param>
		/// <returns></returns>
		private List<Epx.BIM.BaseDataNode> GetPreviousHitNodes(List<HitTestResult> hitResultList, int index)
		{
			List<BaseDataNode> nodes = new List<BaseDataNode>();
			for (int i = index - 1; i >= 0; i--)
			{
				var attachedNode = GetAttachedNode(hitResultList[i].VisualHit);
				nodes.Add(attachedNode);
			}
			return nodes;
		}

		/// <summary>
		/// Adaptive based on Camera width.
		/// </summary>
		private double SnapTolerance
		{
			get
			{
				var tol = 175;
				var camera = Camera as OrthographicCamera;
				if (camera != null)
				{
					if (camera.Width < 10000)
						tol = 100;
					if (camera.Width < 4000)
						tol = 60;
					if (camera.Width < 2000)
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

			if (!(MouseOverVisual is ModelEditVisual3D))
			{
				//if (node is NailPlate && (ViewModel.DesignCommandInProgress || ViewModel.CurrentTruss.IsDescendant(node)))
				//{
				//	(Parent as FrameworkElement).ToolTip = new ToolTip() { Content = (node as NailPlate).PlateSize };
				//}
			}

			if ((Parent as FrameworkElement).ToolTip is ToolTip) ((Parent as FrameworkElement).ToolTip as ToolTip).IsOpen = true;
		}

		/// <summary>
		/// Hides the mouse over information.
		/// </summary>
		/// <returns><c>true</c> if a tooltip was open and it was closed, <c>false</c> otherwise.</returns>
		protected bool HideMouseOverToolTip()
		{
			bool wasClosed = false;
			if ((Parent as FrameworkElement).ToolTip is ToolTip)
			{
				((Parent as FrameworkElement).ToolTip as ToolTip).IsOpen = false;
				wasClosed = true;
			}
			(Parent as FrameworkElement).ToolTip = null;
			return wasClosed;
		}

#endregion
#endif
	}
}
