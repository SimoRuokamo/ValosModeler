using Enterprixe.WPF.Tools.Viewport;
using Enterprixe.WPF.Tools.Viewport.WireBase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;
using ValosModeler;

namespace ValosModeler.Views.Model3DView
{
    public enum PointInputConstraints
	{
		Nothing,
		XCoord,
		YCoord,
		ZCoord,
		Angle,
		Length,
	}

	/// <summary>
	/// The triangular "rubberband" tool for entering points in the model.
	/// </summary>
	public class PointInputTool //: INotifyPropertyChanged
	{
		private Point3D _start, _end;
		private IPointInputToolController _controller;
		private PointInputToolControl _control;

		public PointInputTool(Line line, IPointInputToolController controller, PointInputToolControl control)
		{
			PrecisionAngle = 2;
			PrecisionLength = 3;
			Line = line;
			LineX = new Line();
			LineY = new Line();
			LineZ = new Line();
			LineAngle = new Line();
			LineX.Stroke = System.Windows.Media.Brushes.Red;
			LineY.Stroke = System.Windows.Media.Brushes.Green;
			LineZ.Stroke = System.Windows.Media.Brushes.Blue;
			LineAngle.Stroke = System.Windows.Media.Brushes.DimGray;
			Text = new TextBlock();
			Text.IsHitTestVisible = false;
			TextX = new TextBlock();
			TextX.IsHitTestVisible = false;
			TextY = new TextBlock();
			TextY.IsHitTestVisible = false;
			TextZ = new TextBlock();
			TextZ.IsHitTestVisible = false;
			TextAngle = new TextBlock();
			TextAngle.IsHitTestVisible = false;
			TextX.Foreground = System.Windows.Media.Brushes.Red;
			TextY.Foreground = System.Windows.Media.Brushes.Green;
			TextZ.Foreground = System.Windows.Media.Brushes.Blue;
			TextAngle.Foreground = System.Windows.Media.Brushes.Black;
			TextBoxH = new TextBox();
			TextBoxX = new TextBox();
			TextBoxY = new TextBox();
			TextBoxZ = new TextBox();
			TextBoxAngle = new TextBox();
			TextBoxX.Foreground = System.Windows.Media.Brushes.Red;
			TextBoxY.Foreground = System.Windows.Media.Brushes.Green;
			TextBoxZ.Foreground = System.Windows.Media.Brushes.Blue;
			TextBoxAngle.Foreground = System.Windows.Media.Brushes.Black;
			TextBoxH.Width = 63; // match to precision
			TextBoxX.Width = 63;
			TextBoxY.Width = 63;
			TextBoxZ.Width = 63;
			TextBoxAngle.Width = 43;

			_control = control;
			_controller = controller;
			var editor = _controller.Viewport;
			if (editor is IWireHost)
				(editor as IWireHost).ViewChanged += (s)=> { RecalculatePositions(); };
			SetShapesVisibility(Visibility.Collapsed);

			foreach (FrameworkElement shape in Shapes)
			{
				if (!(shape is TextBox))
				{
					shape.IsHitTestVisible = false;
				}
			}
		}

		public void RecalculatePositions()
		{
			//if( !Aux3D.GeometryHelper.IsValidPoint(Start) ||  !Aux3D.GeometryHelper.IsValidPoint(End))
			//    return;
			//else
			if (/*!StartRubber ||*/ CurrentGrid == null) //TODO StartRubber, does this funciton need to run silently?
			{
				return;
			}
			else if (Start == End)
			{
				SetShapesVisibility(Visibility.Collapsed);
				return;
			}
			else
			{
				SetShapesVisibility(_isActive ? Visibility.Visible : Visibility.Collapsed);
			}

			SynchronizeLockedTextBoxes();

			// assume Z-plane
			var localStart = CurrentGrid.GlobalToLocal.Transform(Start.BimPoint());
			var localEnd = CurrentGrid.GlobalToLocal.Transform(End.BimPoint());
			var localMove = localEnd - localStart;
			Point3D corner3D = new Point3D(localEnd.X, localStart.Y, localStart.Z);
			Point3D corner3DZ = new Point3D(localEnd.X, localEnd.Y, localStart.Z);

			var start = GetScreenPoint(Start);
			var end = GetScreenPoint(End);
			var corner = GetScreenPoint(CurrentGrid.LocalToGlobal.Transform(corner3D.BimPoint()).WinPoint());
			var cornerZ = GetScreenPoint(CurrentGrid.LocalToGlobal.Transform(corner3DZ.BimPoint()).WinPoint());
			if (Double.IsInfinity(start.X) || Double.IsNaN(start.X) || Double.IsInfinity(start.Y) || Double.IsNaN(start.Y)
				|| Double.IsInfinity(end.X) || Double.IsNaN(end.X) || Double.IsInfinity(end.Y) || Double.IsNaN(end.Y))
				return;
			#region Length
			// *** H ***
			Line.X1 = start.X; Line.Y1 = start.Y;
			Line.X2 = end.X; Line.Y2 = end.Y;

			bool hConstrained = MoveTool.MoveConstraints.Contains(PointInputConstraints.Length);
			string textH = hConstrained ? MoveTool.Length.ToString(StringFormatLength) : localMove.Length.ToString(StringFormatLength);
			Text.Text = textH;
			Text.FontWeight = hConstrained ? FontWeights.Bold : FontWeights.Regular;
			Canvas.SetLeft(Text, Line.X1 + (Line.X2 - Line.X1) / 2);
			Canvas.SetTop(Text, Line.Y1 + (Line.Y2 - Line.Y1) / 2);

			TextBoxH.Text = textH;
			TextBoxH.FontWeight = hConstrained ? FontWeights.Bold : FontWeights.Regular;
			TextBoxH.IsReadOnly = hConstrained;
			Canvas.SetLeft(TextBoxH, Line.X1 + (Line.X2 - Line.X1) / 2);
			Canvas.SetTop(TextBoxH, Line.Y1 + (Line.Y2 - Line.Y1) / 2);
			#endregion

			#region X
			// *** X ***
			LineX.X1 = start.X; LineX.Y1 = start.Y;
			LineX.X2 = corner.X; LineX.Y2 = corner.Y;

			bool xConstrained = MoveTool.MoveConstraints.Contains(PointInputConstraints.XCoord);
			string textX = xConstrained ? MoveTool.MoveToPoint.X.ToString(StringFormatLength) : localMove.X.ToString(StringFormatLength);
			TextX.Text = textX;
			TextX.FontWeight = xConstrained ? FontWeights.Bold : FontWeights.Regular;
			if (Math.Round(MoveTool.MoveToPoint.X, 3) == 0.0)
			{
				TextX.Visibility = Visibility.Collapsed;
				LineX.Visibility = Visibility.Collapsed;
				TextBoxX.Visibility = Visibility.Collapsed;
			}
			else if (TextX.Text.Replace("-", "") == Text.Text)
			{
				if (xConstrained)
				{
					Text.Visibility = Visibility.Collapsed;
					Line.Visibility = Visibility.Collapsed;
					TextBoxH.Visibility = Visibility.Collapsed;
				}
				else if (!TextBoxX.IsKeyboardFocused) // prevent focused textbox from being Collapsed and locking model view
				{
					TextX.Visibility = Visibility.Collapsed;
					LineX.Visibility = Visibility.Collapsed;
					TextBoxX.Visibility = Visibility.Collapsed;
				}
			}
			Canvas.SetLeft(TextX, LineX.X1 + (LineX.X2 - LineX.X1) / 2);
			Canvas.SetTop(TextX, LineX.Y1 + (LineX.Y2 - LineX.Y1) / 2);

			TextBoxX.Text = textX;
			TextBoxX.FontWeight = xConstrained ? FontWeights.Bold : FontWeights.Regular;
			TextBoxX.IsReadOnly = xConstrained;
			Canvas.SetLeft(TextBoxX, LineX.X1 + (LineX.X2 - LineX.X1) / 2);
			Canvas.SetTop(TextBoxX, LineX.Y1 + (LineX.Y2 - LineX.Y1) / 2);
			#endregion

			double angle = 0;
			Point anglePoint = new Point();

			// Z is visible
			if (Math.Abs(localEnd.Z - localStart.Z) > 0.001)
			{
				LineZ.Visibility = Line.Visibility;
				TextZ.Visibility = Line.Visibility;
				//TextAngle.Visibility = Visibility.Collapsed;
				LineAngle.Visibility = Line.Visibility;

				#region Y
				// *** Y ***
				LineY.X1 = corner.X; LineY.Y1 = corner.Y;
				LineY.X2 = cornerZ.X; LineY.Y2 = cornerZ.Y;

				bool yConstrained = MoveTool.MoveConstraints.Contains(PointInputConstraints.YCoord);
				string textY = yConstrained ? MoveTool.MoveToPoint.Y.ToString(StringFormatLength) : localMove.Y.ToString(StringFormatLength);
				TextY.Text = textY;
				TextY.FontWeight = yConstrained ? FontWeights.Bold : FontWeights.Regular;
				if (Math.Round(MoveTool.MoveToPoint.Y, 3) == 0.0)
				{
					TextY.Visibility = Visibility.Collapsed;
					LineY.Visibility = Visibility.Collapsed;
					TextBoxY.Visibility = Visibility.Collapsed;
				}
				else if (TextY.Text.Replace("-", "") == Text.Text)
				{
					if (yConstrained)
					{
						Text.Visibility = Visibility.Collapsed;
						Line.Visibility = Visibility.Collapsed;
						TextBoxH.Visibility = Visibility.Collapsed;
					}
					//else if(!TextBoxY.IsKeyboardFocused)
					//{
					//    TextY.Visibility = Visibility.Collapsed;
					//    LineY.Visibility = Visibility.Collapsed;
					//    TextBoxY.Visibility = Visibility.Collapsed;
					//}
				}
				Canvas.SetLeft(TextY, LineY.X1 + (LineY.X2 - LineY.X1) / 2);
				Canvas.SetTop(TextY, LineY.Y1 + (LineY.Y2 - LineY.Y1) / 2);

				TextBoxY.Text = textY;
				TextBoxY.FontWeight = yConstrained ? FontWeights.Bold : FontWeights.Regular;
				TextBoxY.IsReadOnly = yConstrained;
				Canvas.SetLeft(TextBoxY, LineY.X1 + (LineY.X2 - LineY.X1) / 2);
				Canvas.SetTop(TextBoxY, LineY.Y1 + (LineY.Y2 - LineY.Y1) / 2);
				#endregion

				#region Z
				// *** Z ***
				LineZ.X1 = cornerZ.X; LineZ.Y1 = cornerZ.Y;
				LineZ.X2 = end.X; LineZ.Y2 = end.Y;

				bool zConstrained = MoveTool.MoveConstraints.Contains(PointInputConstraints.ZCoord);
				string textZ = zConstrained ? MoveTool.MoveToPoint.Z.ToString(StringFormatLength) : localMove.Z.ToString(StringFormatLength);
				TextZ.Text = textZ;
				TextZ.FontWeight = zConstrained ? FontWeights.Bold : FontWeights.Regular;
				if (TextZ.Text.Replace("-", "") == Text.Text || Math.Round(MoveTool.MoveToPoint.Z, 3) == 0.0)
				{
					TextZ.Visibility = Visibility.Collapsed;
					LineZ.Visibility = Visibility.Collapsed;
				}
				Canvas.SetLeft(TextZ, LineZ.X1 + (LineZ.X2 - LineZ.X1) / 2);
				Canvas.SetTop(TextZ, LineZ.Y1 + (LineZ.Y2 - LineZ.Y1) / 2);

				TextBoxZ.Text = textZ;
				TextBoxZ.FontWeight = zConstrained ? FontWeights.Bold : FontWeights.Regular;
				TextBoxZ.IsReadOnly = zConstrained;
				if (TextBoxZ.Text == Text.Text || TextBoxZ.Text == "0") TextBoxZ.Visibility = Visibility.Collapsed;
				Canvas.SetLeft(TextBoxZ, LineZ.X1 + (LineZ.X2 - LineZ.X1) / 2);
				Canvas.SetTop(TextBoxZ, LineZ.Y1 + (LineZ.Y2 - LineZ.Y1) / 2);
				#endregion

				// calculate angle in XY plane 
				angle = Vector3D.AngleBetween(corner3D - Start, corner3DZ - Start);
				if (double.IsNaN(angle) && MoveTool.MoveConstraints.Contains(PointInputConstraints.Angle)) angle = MoveTool.Angle;
				else if (MoveTool.MoveConstraints.Contains(PointInputConstraints.Angle)) angle = MoveTool.Angle;
				else if (double.IsNaN(angle)) angle = 90;
				MoveTool.SetMoveAngleAndLength(angle, hConstrained ? MoveTool.Length : localMove.Length);
				Point3D corner3Dmidpoint = new Point3D((corner3D.X + corner3DZ.X) / 2, (corner3D.Y + corner3DZ.Y) / 2, Start.Z);
				Vector3D anglepointdir = corner3Dmidpoint - Start;
				anglepointdir.Normalize();
				Point3D anglePoint3D = Start + anglepointdir * (corner3Dmidpoint - Start).Length * 0.2;
				anglePoint = GetScreenPoint(anglePoint3D);
			}
			else // Z not visible
			{
				LineZ.Visibility = Visibility.Collapsed;
				TextZ.Visibility = Visibility.Collapsed;
				//TextAngle.Visibility = Line.Visibility;
				LineAngle.Visibility = Visibility.Collapsed;

				#region Y
				// *** Y ***
				LineY.X1 = corner.X; LineY.Y1 = corner.Y;
				LineY.X2 = end.X; LineY.Y2 = end.Y;

				bool yConstrained = MoveTool.MoveConstraints.Contains(PointInputConstraints.YCoord);
				string textY = yConstrained ? MoveTool.MoveToPoint.Y.ToString(StringFormatLength) : localMove.Y.ToString(StringFormatLength);
				TextY.Text = textY;
				TextY.FontWeight = yConstrained ? FontWeights.Bold : FontWeights.Regular;
				if (Math.Round(MoveTool.MoveToPoint.Y, 3) == 0.0)
				{
					TextY.Visibility = Visibility.Collapsed;
					LineY.Visibility = Visibility.Collapsed;
					TextBoxY.Visibility = Visibility.Collapsed;
				}
				else if (TextY.Text.Replace("-", "") == Text.Text)
				{
					if (yConstrained)
					{
						Text.Visibility = Visibility.Collapsed;
						Line.Visibility = Visibility.Collapsed;
						TextBoxH.Visibility = Visibility.Collapsed;
					}
					else if (!TextBoxY.IsKeyboardFocused)
					{
						TextY.Visibility = Visibility.Collapsed;
						LineY.Visibility = Visibility.Collapsed;
						TextBoxY.Visibility = Visibility.Collapsed;
					}
				}
				Canvas.SetLeft(TextY, LineY.X1 + (LineY.X2 - LineY.X1) / 2);
				Canvas.SetTop(TextY, LineY.Y1 + (LineY.Y2 - LineY.Y1) / 2);

				TextBoxY.Text = textY;
				TextBoxY.FontWeight = yConstrained ? FontWeights.Bold : FontWeights.Regular;
				TextBoxY.IsReadOnly = yConstrained;
				Canvas.SetLeft(TextBoxY, LineY.X1 + (LineY.X2 - LineY.X1) / 2);
				Canvas.SetTop(TextBoxY, LineY.Y1 + (LineY.Y2 - LineY.Y1) / 2);
				#endregion

				// update Z silently
				TextZ.Text = (End.Z - corner3DZ.Z).ToString(StringFormatLength);
				TextBoxZ.Text = (End.Z - corner3DZ.Z).ToString(StringFormatLength);

				// calculate angle in XY plane 
				angle = Vector3D.AngleBetween(corner3D - localStart.WinPoint(), localMove.WinPoint());
				if (double.IsNaN(angle) && MoveTool.MoveConstraints.Contains(PointInputConstraints.Angle)) angle = MoveTool.Angle;
				else if (MoveTool.MoveConstraints.Contains(PointInputConstraints.Angle)) angle = MoveTool.Angle;
				else if (double.IsNaN(angle)) angle = 90;
				MoveTool.SetMoveAngleAndLength(angle, hConstrained ? MoveTool.Length : localMove.Length);
				Point3D corner3Dmidpoint = new Point3D((corner3D.X + localEnd.X) / 2, (corner3D.Y + localEnd.Y) / 2, localStart.Z);
				var anglepointdir = corner3Dmidpoint - localStart.WinPoint();
				anglepointdir.Normalize();
				Point3D anglePoint3D = localStart.WinPoint() + anglepointdir * (corner3Dmidpoint - localStart.WinPoint()).Length * 0.2;
				anglePoint = GetScreenPoint(CurrentGrid.LocalToGlobal.Transform(anglePoint3D.BimPoint()).WinPoint());
			}

			#region Angle
			// *** A ***
			LineAngle.X1 = start.X; LineAngle.Y1 = start.Y;
			LineAngle.X2 = cornerZ.X; LineAngle.Y2 = cornerZ.Y;

			bool aConstrained = MoveTool.MoveConstraints.Contains(PointInputConstraints.Angle);
			TextAngle.Text = aConstrained ? MoveTool.Angle.ToString(StringFormatAngle) : angle.ToString(StringFormatAngle) + "\u00B0";
			TextAngle.FontWeight = aConstrained ? FontWeights.Bold : FontWeights.Regular;
			if (angle < 0.0001 || angle == 90) LineAngle.Visibility = Visibility.Collapsed;
			Canvas.SetLeft(TextAngle, anglePoint.X);
			Canvas.SetTop(TextAngle, anglePoint.Y);

			TextBoxAngle.Text = aConstrained ? MoveTool.Angle.ToString(StringFormatAngle) : angle.ToString(StringFormatAngle);
			TextBoxAngle.FontWeight = aConstrained ? FontWeights.Bold : FontWeights.Regular;
			TextBoxAngle.IsReadOnly = aConstrained;
			Canvas.SetLeft(TextBoxAngle, anglePoint.X);
			Canvas.SetTop(TextBoxAngle, anglePoint.Y);
			#endregion
		}

		public Point3D Start
		{
			get { return _start; }
			private set
			{
				_start = value;
				RecalculatePositions();
			}
		}
		public Point3D End
		{
			get { return _end; }
			private set
			{
				_end = value;
				RecalculatePositions();
			}
		}
		private bool _isActive = false;
		public bool IsActive
		{
			get
			{
				return _isActive;
			}
			set
			{
				if (value && CurrentGrid == null) return;

				_isActive = value;
				if (!value)
				{
					SetShapesVisibility(Visibility.Collapsed);
					ResetExactMove();
				}
				else
				{
					SetShapesVisibility(Visibility.Visible);
				}
			}
		}

		public int PrecisionLength { get; set; }
		public int PrecisionAngle { get; set; }

		private string StringFormatLength
		{
			get { return "N" + PrecisionLength.ToString("0"); }
		}
		private string StringFormatAngle
		{
			get { return "N" + PrecisionAngle.ToString("0"); }
		}

		public bool SetRelativeInputPoint { get; set; }

		#region Control

		public void StartInput(Point3D startPoint, Vector3D? originalDirection)
		{
			if (CurrentGrid == null) return;

			ResetExactInput();
			MoveToPoint = new Point3D();
			if (originalDirection.HasValue)
				OriginalDirection = originalDirection.Value;
			else
				OriginalDirection = null;
			IsActive = true;
			Start = startPoint;
			End = startPoint;
			IsPointInputLocked = false;
		}
		public void StartInput(Point3D startPoint)
		{
			StartInput(startPoint, null);
		}

		public void MoveInput(Vector3D vector)
		{
			SetMoveVector(vector);

			if (!IsExactInputFocusCaptured)
			{
				RedrawSnapPoints();
			}
		}

		public void MoveInput(Point3D toPoint)
		{
			MoveInput(toPoint - _start);
		}

		public void EndInput()
		{
			IsActive = false;
			OriginalDirection = null;
			ResetExactInput();
		}

		public void SetExactInputLock(bool isLocked)
		{
			if (isLocked)
			{
				IsExactInputFocusCaptured = true;
				_control.MouseEventCanvasHitTestOn = false;
			}
			else
			{
				IsExactInputFocusCaptured = false;
				_control.MouseEventCanvasHitTestOn = true;
			}
		}

		public void ResetExactInput()
		{
			ClearMoveConstraints();
			SetExactInputLock(false);
			_isOriginalDirLocked = false;
			_isPerpendicularDirLocked = false;
		}

		public void ExactInputNext()
		{
			if ((End - Start).Length < 0.1) return;
			LockVisibleTextBoxIfChanged();
			TextBox visibleBox = ShowNextTextBox();
			if (visibleBox != null) SetExactInputLock(true);
			else SetExactInputLock(false);

			RedrawSnapPoints();
		}

		public void ExactInputCommit()
		{
			PointInputConstraints moveConstraint = LockVisibleTextBox();
			if (moveConstraint != PointInputConstraints.Nothing)
			{
				SetExactInputLock(false);
			}
			else if (moveConstraint == PointInputConstraints.Nothing || LockedTextBoxes.Contains(VisibleTextBox)) // the box is already locked, do nothing (different case from above)
			{
				SetExactInputLock(false);
				VisibleTextBox = null;
			}

			RedrawSnapPoints();
		}

		public void ExactInputEsc()
		{
			if (UnlockVisibleTextBox() == null) SetExactInputLock(false);
			RedrawSnapPoints();
		}

		public void RedrawSnapPoints()
		{
			if (!IsPointInputLocked && IsActive)
			{
				this.End = _controller.SnapPoint.Point.WinPoint();
			}
		}

		/// <summary>
		/// Is not used, is it work in progress? Gets or sets a value indicating whether this instance is point input locked.
		/// </summary>
		/// <value><c>true</c> if this instance is point input locked; otherwise, <c>false</c>.</value>
		private bool IsPointInputLocked { get; set; }
		public bool IsExactInputFocusCaptured { get; set; }

		public bool DoKeyDown(ref KeyEventArgs e)
		{
			return _control.DoKeyDown(ref e);
		}

		#endregion

		#region UI component

		public Line Line { get; private set; }
		private Line LineX { get; set; }
		private Line LineY { get; set; }
		private Line LineZ { get; set; }
		private Line LineAngle { get; set; }

		private TextBlock Text { get; set; }
		private TextBlock TextX { get; set; }
		private TextBlock TextY { get; set; }
		private TextBlock TextZ { get; set; }
		private TextBlock TextAngle { get; set; }

		public TextBox TextBoxH { get; set; }
		public TextBox TextBoxX { get; set; }
		public TextBox TextBoxY { get; set; }
		public TextBox TextBoxZ { get; set; }
		public TextBox TextBoxAngle { get; set; }

		private List<FrameworkElement> _shapes = null;
		public IEnumerable<FrameworkElement> Shapes
		{
			get
			{
				if (_shapes == null)
				{
					_shapes = new List<FrameworkElement>()
					{
						Line, LineX, LineY, LineZ, LineAngle,
						Text, TextX, TextY, TextZ, TextAngle,
						TextBoxH, TextBoxX, TextBoxY, TextBoxZ, TextBoxAngle
					};
				}
				return _shapes;
			}
		}

		public void SetShapesVisibility(Visibility visibility)
		{
			foreach (FrameworkElement shape in Shapes)
			{
				if (shape is TextBox)
				{
					if (shape == VisibleTextBox && IsActive) shape.Visibility = Visibility.Visible;
					else shape.Visibility = Visibility.Collapsed;
				}
				else
				{
					shape.Visibility = visibility;
				}
			}
		}

		private TextBox _visibleTextBox = null;
		public TextBox VisibleTextBox
		{
			get { return _visibleTextBox; }
			set
			{
				if (_visibleTextBox != null) _visibleTextBox.PreviewKeyDown -= new KeyEventHandler(_visibleTextBox_PreviewKeyDown);
				_visibleTextBox = value;
				if (_visibleTextBox != null)
				{
					_visibleTextBox.PreviewKeyDown += new KeyEventHandler(_visibleTextBox_PreviewKeyDown);
					_visibleTextBox.SelectAll();
				}
			}
		}

		private void _visibleTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
		{
			TextBox visibleBox = sender as TextBox;

			if (visibleBox.Visibility == Visibility.Visible)
			{
				if (e.Key == Key.Back && LockedTextBoxes.Contains(visibleBox))
				{
					UnlockVisibleTextBox(false);
					RecalculatePositions();
				}
				else if (e.Key == Key.Delete)
				{
					UnlockVisibleTextBox(false);
					ShowNextTextBox();
					RecalculatePositions();
					if (VisibleTextBox != null)
					{
						Keyboard.Focus(VisibleTextBox);
						VisibleTextBox.SelectAll();
					}
					else
					{
						SetExactInputLock(false);
					}
					e.Handled = true;
				}
			}
		}

		private List<TextBox> _lockedTextBoxes = new List<TextBox>();
		public List<TextBox> LockedTextBoxes
		{
			get { return _lockedTextBoxes; }
			set
			{
				if (_lockedTextBoxes != value)
				{
					_lockedTextBoxes = value;
				}
			}
		}

		private int _lockedTextBoxIndex = 0;
		public TextBox ShowNextTextBox()
		{
			bool hVisible = Text.Visibility == Visibility.Visible;
			bool xVisible = TextX.Visibility == Visibility.Visible;
			bool yVisible = TextY.Visibility == Visibility.Visible;
			var localStart = CurrentGrid.GlobalToLocal.Transform(Start.BimPoint());
			var localEnd = CurrentGrid.GlobalToLocal.Transform(End.BimPoint());
			bool zVisible = Math.Abs(localEnd.Z - localStart.Z) > 0.001;

			List<TextBox> nonZLockedTBs = new List<TextBox>(LockedTextBoxes);
			nonZLockedTBs.Remove(TextBoxZ);

			if (nonZLockedTBs.Count == 2)
			{
				if (_lockedTextBoxIndex < 2)
				{
					VisibleTextBox = nonZLockedTBs[_lockedTextBoxIndex];
					_lockedTextBoxIndex++;
				}
				else if (LockedTextBoxes.Contains(TextBoxZ) && _lockedTextBoxIndex < 3)
				{
					VisibleTextBox = TextBoxZ;
					_lockedTextBoxIndex++;
				}
				else
				{
					_lockedTextBoxIndex = 0;
					VisibleTextBox = null;
				}
			}
			else
			{
				if (VisibleTextBox == TextBoxAngle)
				{
					if (hVisible) VisibleTextBox = TextBoxH;
					else VisibleTextBox = nonZLockedTBs[0];
				}
				else if (VisibleTextBox == TextBoxH)
				{
					if (xVisible) VisibleTextBox = TextBoxX;
					else if (!xVisible && yVisible) VisibleTextBox = TextBoxY;
					else if (!xVisible && !yVisible && zVisible) VisibleTextBox = TextBoxZ;
					else VisibleTextBox = null;
				}
				else if (VisibleTextBox == TextBoxX)
				{
					if (yVisible) VisibleTextBox = TextBoxY;
					else if (!yVisible && zVisible) VisibleTextBox = TextBoxZ;
					else VisibleTextBox = null;
				}
				else if (VisibleTextBox == TextBoxY)
				{
					if (zVisible) VisibleTextBox = TextBoxZ;
					else VisibleTextBox = null;
				}
				else if (VisibleTextBox == TextBoxZ)
				{
					VisibleTextBox = null;
				}
				else
				{
					VisibleTextBox = TextBoxAngle;
				}
			}

			return VisibleTextBox;
		}

		public PointInputConstraints LockVisibleTextBox(bool deselectAfterLock = true)
		{
			PointInputConstraints retval = PointInputConstraints.Nothing;

			if (VisibleTextBox != null && IsTextNumeric(VisibleTextBox.Text))
			{
				if (VisibleTextBox == TextBoxH)
				{
					if (MoveTool.CanAddConstraint(PointInputConstraints.Length))
					{
						LockedTextBoxes.Add(VisibleTextBox);
						retval = PointInputConstraints.Length;
					}
				}
				else if (VisibleTextBox == TextBoxX)
				{
					if (MoveTool.CanAddConstraint(PointInputConstraints.XCoord))
					{
						LockedTextBoxes.Add(VisibleTextBox);
						retval = PointInputConstraints.XCoord;
					}
				}
				else if (VisibleTextBox == TextBoxY)
				{
					if (MoveTool.CanAddConstraint(PointInputConstraints.YCoord))
					{
						LockedTextBoxes.Add(VisibleTextBox);
						retval = PointInputConstraints.YCoord;
					}
				}
				else if (VisibleTextBox == TextBoxZ)
				{
					if (MoveTool.CanAddConstraint(PointInputConstraints.ZCoord))
					{
						LockedTextBoxes.Add(VisibleTextBox);
						retval = PointInputConstraints.ZCoord;
					}
				}
				else if (VisibleTextBox == TextBoxAngle)
				{
					if (MoveTool.CanAddConstraint(PointInputConstraints.Angle))
					{
						LockedTextBoxes.Add(VisibleTextBox);
						retval = PointInputConstraints.Angle;
					}
				}
			}

			if (retval != PointInputConstraints.Nothing)
			{
				MoveTool.SetMoveAngleAndLength(GetTextBoxDouble(TextBoxAngle), GetTextBoxDouble(TextBoxH));
				MoveTool.SetMoveVector(new Vector3D(
					GetTextBoxDouble(TextBoxX),
					GetTextBoxDouble(TextBoxY),
					GetTextBoxDouble(TextBoxZ)), false);
				MoveTool.AddMoveConstraint(retval, retval == PointInputConstraints.Angle);

				List<TextBox> nonZLockedTBs = new List<TextBox>(LockedTextBoxes);
				nonZLockedTBs.Remove(TextBoxZ);
				if (deselectAfterLock || nonZLockedTBs.Count == 2) VisibleTextBox = null;
			}
			return retval;
		}

		public bool LockVisibleTextBoxIfChanged()
		{
			bool retval = false;

			if (VisibleTextBox == TextBoxH)
			{
				if (VisibleTextBox.Text != Text.Text) retval = true;
			}
			else if (VisibleTextBox == TextBoxX)
			{
				if (VisibleTextBox.Text != TextX.Text) retval = true;
			}
			else if (VisibleTextBox == TextBoxY)
			{
				if (VisibleTextBox.Text != TextY.Text) retval = true;
			}
			else if (VisibleTextBox == TextBoxZ)
			{
				if (VisibleTextBox.Text != TextZ.Text) retval = true;
			}
			else if (VisibleTextBox == TextBoxAngle)
			{
				if (VisibleTextBox.Text != TextAngle.Text.Replace("\u00B0", "")) retval = true;
			}

			if (retval)
			{
				LockVisibleTextBox(false);
				_lockedTextBoxIndex++;
			}

			return retval;
		}

		private void ResetExactMove()
		{
			LockedTextBoxes.Clear();
			_lockedTextBoxIndex = 0;
			VisibleTextBox = null;
		}

		public TextBox UnlockVisibleTextBox(bool deselectVisibleTextBoxAfterUnlock = true)
		{
			if (VisibleTextBox == TextBoxH)
			{
				MoveTool.RemoveMoveConstraint(PointInputConstraints.Length);
			}
			else if (VisibleTextBox == TextBoxX)
			{
				MoveTool.RemoveMoveConstraint(PointInputConstraints.XCoord);
			}
			else if (VisibleTextBox == TextBoxY)
			{
				MoveTool.RemoveMoveConstraint(PointInputConstraints.YCoord);
			}
			else if (VisibleTextBox == TextBoxZ)
			{
				MoveTool.RemoveMoveConstraint(PointInputConstraints.ZCoord);
			}
			else if (VisibleTextBox == TextBoxAngle)
			{
				MoveTool.RemoveMoveConstraint(PointInputConstraints.Angle);
			}

			LockedTextBoxes.Remove(VisibleTextBox);
			if (_lockedTextBoxIndex > 0) _lockedTextBoxIndex--;
			if (deselectVisibleTextBoxAfterUnlock) VisibleTextBox = null;
			return VisibleTextBox;
		}

		private bool _isOriginalDirLocked = false;
		public bool TryLockAngleToOriginalDir()
		{
			bool retval = false;
			if (VisibleTextBox == TextBoxAngle && MoveTool.OriginalDirection.HasValue)
			{
				double angle = Vector3D.AngleBetween(new Vector3D(1, 0, 0), new Vector3D(MoveTool.OriginalDirection.Value.X, MoveTool.OriginalDirection.Value.Y, 0));
				if (angle > 90) angle = 180 - angle;
				else if (angle < -90) angle = 180 + angle;
				TextBoxAngle.Text = angle.ToString();

				if (LockVisibleTextBox() == PointInputConstraints.Angle)
				{
					retval = true;
				}
				else if (MoveTool.MoveConstraints.Contains(PointInputConstraints.Angle))
				{
					MoveTool.SetMoveAngleAndLength(angle, GetTextBoxDouble(TextBoxH));
					VisibleTextBox = null;
					retval = true;
				}
			}

			_isOriginalDirLocked = retval;
			return retval;
		}

		private bool _isPerpendicularDirLocked = false;
		public bool TryLockAngleToPerpendicularDir()
		{
			bool retval = false;
			if (VisibleTextBox == TextBoxAngle && MoveTool.PerpendicularDirection.HasValue)
			{
				double angle = Vector3D.AngleBetween(new Vector3D(1, 0, 0), new Vector3D(MoveTool.PerpendicularDirection.Value.X, MoveTool.PerpendicularDirection.Value.Y, 0));
				if (angle > 90) angle = 180 - angle;
				else if (angle < -90) angle = 180 + angle;
				TextBoxAngle.Text = angle.ToString();

				if (LockVisibleTextBox() == PointInputConstraints.Angle)
				{
					retval = true;
				}
				else if (MoveTool.MoveConstraints.Contains(PointInputConstraints.Angle))
				{
					MoveTool.SetMoveAngleAndLength(angle, GetTextBoxDouble(TextBoxH));
					VisibleTextBox = null;
					retval = true;
				}
			}

			_isPerpendicularDirLocked = retval;
			return retval;
		}

		/// <summary>
		/// If constraint set from ribbontab need to add it to LockedTextBoxes.
		/// </summary>
		private void SynchronizeLockedTextBoxes()
		{
			_lockedTextBoxes.Clear();

			foreach (PointInputConstraints fp in MoveTool.MoveConstraints)
			{
				if (fp == PointInputConstraints.Angle) _lockedTextBoxes.Add(TextBoxAngle);
				else if (fp == PointInputConstraints.Length) _lockedTextBoxes.Add(TextBoxH);
				else if (fp == PointInputConstraints.XCoord) _lockedTextBoxes.Add(TextBoxX);
				else if (fp == PointInputConstraints.YCoord) _lockedTextBoxes.Add(TextBoxY);
				else if (fp == PointInputConstraints.ZCoord) _lockedTextBoxes.Add(TextBoxZ);
			}
		}

		#endregion //UI component

		private PointInputTool MoveTool
		{
			get { return this; }
		}

		public Epx.BIM.GridMesh.GridMesh CurrentGrid
		{
			get
			{
				return _controller.CurrentGrid != null ? _controller.CurrentGrid : new Epx.BIM.GridMesh.GridUCS();
			}
		}

		#region Helpers

		private bool IsTextNumeric(string str)
		{
			bool ret = true;
			double value;
			if (!double.TryParse(str, out value))
			{
				ret = false;
			}

			return ret;
		}

		public double GetTextBoxDouble(TextBox text)
		{
			double value;
			if (!double.TryParse(text.Text, out value))
			{
				value = 0;
			}
			return value;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="point">In model CS.</param>
		/// <returns></returns>
		private Point GetScreenPoint(Point3D point)
		{
			//if (_editor != null)
			//    return Enterprixe.WPF.Tools.Drawings3D.ViewportInfo.Point3DtoPoint2D(_editor, point);
			return _controller.GetScreenPoint(point);
		}

		public Point3D RoundPoint3D(Point3D point)
		{
			point.X = Math.Round(point.X, PrecisionLength);
			point.Y = Math.Round(point.Y, PrecisionLength);
			point.Z = Math.Round(point.Z, PrecisionLength);
			return point;
		}
		public Vector3D RoundVector3D(Vector3D point)
		{
			point.X = Math.Round(point.X, PrecisionLength);
			point.Y = Math.Round(point.Y, PrecisionLength);
			point.Z = Math.Round(point.Z, PrecisionLength);
			return point;
		}
		#endregion

		#region MoveTool (constraints)

		private Vector3D? _originalDirection = null;
		public Vector3D? OriginalDirection
		{
			get
			{
				if (_originalDirection.HasValue)
					return _originalDirection;
				else
					return (Vector3D)MoveToPoint; // return current direction
			}
			set
			{
				if (_originalDirection != value)
				{
					_originalDirection = value;
					//RaisePropertyChanged(() => this.OriginalDirection);
				}
			}
		}
		/// <summary>
		/// Perpendicular to original direction.
		/// </summary>
		public Vector3D? PerpendicularDirection
		{
			get
			{
				if (OriginalDirection.HasValue && CurrentGrid != null)
				{
					var oDir = OriginalDirection.Value;
					oDir.Normalize();
					var perpDir = Vector3D.CrossProduct(oDir, CurrentGrid.ZAxis.WinPoint());
					perpDir.Normalize();
					return perpDir;
				}
				else
					return null;
			}
		}

		public Point3D MoveToPoint { get; set; }

		private double _moveH = 0;
		public double Length
		{
			get { return _moveH; }
		}

		private Vector3D? _angleLockedDir = null;
		private double _angle = 0;
		public double Angle
		{
			get { return _angle; }
		}
		/// <summary>
		/// Update the move vector without fixing coordinates.
		/// </summary>
		/// <param name="moveVector"></param>
		public void SetMoveVector(Vector3D moveVector, bool needTransform = true)
		{
			if (needTransform && CurrentGrid != null)
				moveVector = CurrentGrid.GlobalToLocal.Transform(moveVector.BimPoint()).WinPoint();
			MoveToPoint = RoundPoint3D((Point3D)moveVector);
		}
		/// <summary>
		/// Update angle and length without fixing them.
		/// </summary>
		/// <param name="angle"></param>
		/// <param name="length"></param>
		public void SetMoveAngleAndLength(double angle, double length)
		{
			_angle = Math.Abs(Math.Round(angle, PrecisionAngle));
			_moveH = Math.Abs(Math.Round(length, PrecisionLength));
		}


		/// <summary>
		/// Gets a value indicating whether any of the tool components have had their value locked.
		/// </summary>
		/// <value><c>true</c> if this instance is move constrained; otherwise, <c>false</c>.</value>
		public bool IsMoveConstrained
		{
			get { return _moveConstraints.Count > 0; }
		}

		public bool AddMoveConstraint(PointInputConstraints constraint, bool lockAngleDir = false)
		{
			bool retval = false;

			if (CanAddConstraint(constraint))
			{
				_moveConstraints.Add(constraint);
				retval = true;

				if (lockAngleDir && constraint == PointInputConstraints.Angle)
				{
					_angleLockedDir = (Vector3D)MoveToPoint;
				}
				else if (constraint == PointInputConstraints.Angle)
				{
					_angleLockedDir = null;
				}
			}

			return retval;
		}

		public void RemoveMoveConstraint(PointInputConstraints constraint)
		{
			_moveConstraints.RemoveAll(fp => fp == constraint);
			if (constraint == PointInputConstraints.Angle)
				_angleLockedDir = null;
		}

		public bool CanAddConstraint(PointInputConstraints constraint)
		{
			bool retval = false;

			if (_moveConstraints.Contains(PointInputConstraints.ZCoord) &&
				!_moveConstraints.Contains(constraint) && _moveConstraints.Count < 3) retval = true;
			else if (!_moveConstraints.Contains(constraint) && _moveConstraints.Count < 2) retval = true;
			else if (constraint == PointInputConstraints.ZCoord && !_moveConstraints.Contains(constraint)) retval = true;

			if (constraint == PointInputConstraints.YCoord && _moveConstraints.Contains(PointInputConstraints.Angle))
			{
				retval = _angle >= 1.0;
			}
			else if (constraint == PointInputConstraints.XCoord && _moveConstraints.Contains(PointInputConstraints.Angle))
			{
				retval = _angle <= 89.0 || _angle >= 91.0;
			}

			return retval;
		}

		public void ClearMoveConstraints()
		{
			_moveConstraints.Clear();
			_angleLockedDir = null;
		}

		private List<PointInputConstraints> _moveConstraints = new List<PointInputConstraints>();
		public List<PointInputConstraints> MoveConstraints
		{
			get { return _moveConstraints; }
		}

		public Epx.BIM.GridMesh.SnapPoint3D ApplyMoveConstraints(Epx.BIM.GridMesh.SnapPoint3D modelPoint, Epx.BIM.GridMesh.GridMesh overrideGrid = null)
		{
			if (_moveConstraints.Count > 0)
			{
				var currentGrid = overrideGrid != null ? overrideGrid : CurrentGrid;

				Point3D mouseDownModelPoint = Start;
				Vector3D exactMoveVector = new Vector3D();
				Vector3D mouseMoveModelVector = modelPoint.Point.WinPoint() - mouseDownModelPoint;

				if (mouseMoveModelVector.Length < 0.0001)
				{
					// commented out to support snapping to self, original location
					//return modelPoint;
				}

				mouseMoveModelVector = currentGrid.GlobalToLocal.Transform(mouseMoveModelVector.BimPoint()).WinPoint();
				if (!_moveConstraints.Contains(PointInputConstraints.Angle))
				{
					double angle = _angle;
					Point3D corner3D = new Point3D(modelPoint.Point.X, mouseDownModelPoint.Y, mouseDownModelPoint.Z);
					Point3D corner3DZ = new Point3D(modelPoint.Point.X, modelPoint.Point.Y, mouseDownModelPoint.Z);
					_angle = Vector3D.AngleBetween(corner3D - mouseDownModelPoint, corner3DZ - mouseDownModelPoint);
					if (double.IsNaN(_angle)) _angle = angle;
				}

				if (_moveConstraints.Contains(PointInputConstraints.Angle))
				{
					double angle = _angle;
					double x = 100;
					Vector3D angleDir = new Vector3D(x, Math.Tan(angle * Math.PI / 180) * x, 0);
					angleDir.Normalize();

					if (_isOriginalDirLocked || _isPerpendicularDirLocked)
					{
						if (mouseMoveModelVector.X > 0 && mouseMoveModelVector.Y < 0)
						{
							angleDir.Y *= -1;
						}
						else if (mouseMoveModelVector.X < 0 && mouseMoveModelVector.Y > 0)
						{
							angleDir.X *= -1;
						}
						else if (mouseMoveModelVector.X < 0 && mouseMoveModelVector.Y < 0)
						{
							angleDir.X *= -1;
							angleDir.Y *= -1;
						}
					}
					else if (_angleLockedDir.HasValue && !_isOriginalDirLocked && !_isPerpendicularDirLocked)
					{
						if (_angleLockedDir.Value.X > 0 && _angleLockedDir.Value.Y < 0)
						{
							angleDir.Y *= -1;
						}
						else if (_angleLockedDir.Value.X < 0 && _angleLockedDir.Value.Y > 0)
						{
							angleDir.X *= -1;
						}
					}
					//else
					//{
					//	if (mouseMoveModelVector.X > 0 && mouseMoveModelVector.Y < 0)
					//	{
					//		angleDir.Y *= -1;
					//	}
					//	else if (mouseMoveModelVector.X < 0 && mouseMoveModelVector.Y > 0)
					//	{
					//		angleDir.X *= -1;
					//	}
					//	else if (mouseMoveModelVector.X < 0 && mouseMoveModelVector.Y < 0)
					//	{
					//		angleDir.X *= -1;
					//		angleDir.Y *= -1;
					//	}
					//}

					//if (mouseMoveModelVector.Z < 0.0001 && mouseMoveModelVector.Z > -0.0001)
					//{
					//	double len = Vector3D.DotProduct(mouseMoveModelVector, angleDir);
					//	angleDir = angleDir * len;
					//}
					//else
					//{
					//	double len = Vector3D.DotProduct(new Vector3D(mouseMoveModelVector.X, mouseMoveModelVector.Y, 0), angleDir);
					//	angleDir = angleDir * len;// new Vector3D(mouseMoveModelVector.X, mouseMoveModelVector.Y, 0).Length;
					//	angleDir.Z = mouseMoveModelVector.Z;
					//}

					double len = Vector3D.DotProduct(new Vector3D(mouseMoveModelVector.X, mouseMoveModelVector.Y, 0), angleDir);
					angleDir = angleDir * len;
					angleDir.Z = mouseMoveModelVector.Z;

					exactMoveVector = angleDir;
				}

				if (_moveConstraints.Contains(PointInputConstraints.ZCoord))
				{
					if (exactMoveVector == new Vector3D()) exactMoveVector = mouseMoveModelVector;
					exactMoveVector.Z = MoveToPoint.Z;
				}

				if (_moveConstraints.Contains(PointInputConstraints.Length))
				{
					if (exactMoveVector != new Vector3D())
					{
						double length = exactMoveVector.Length;
						double scale = _moveH / length;
						exactMoveVector.Normalize();
						exactMoveVector = exactMoveVector * length * scale;
					}
					else
					{
						Vector3D hMove = new Vector3D(mouseMoveModelVector.X, mouseMoveModelVector.Y, mouseMoveModelVector.Z);
						if (hMove != new Vector3D()) hMove.Normalize();
						exactMoveVector = hMove * _moveH;
					}
				}

				if (_moveConstraints.Contains(PointInputConstraints.XCoord))
				{
					if (exactMoveVector == new Vector3D()) exactMoveVector = mouseMoveModelVector;
					if (_moveConstraints.Contains(PointInputConstraints.Angle))
					{
						exactMoveVector.X = MoveToPoint.X;
						int sign = Math.Sign(exactMoveVector.Y);
						exactMoveVector.Y = Math.Tan(_angle * Math.PI / 180) * MoveToPoint.X;
						if (Math.Sign(exactMoveVector.Y) != sign) exactMoveVector.Y *= -1;
					}
					else if (_moveConstraints.Contains(PointInputConstraints.Length))
					{
						double val = _moveH * _moveH - MoveToPoint.X * MoveToPoint.X;
						if (val <= 0.0)
						{
							RemoveMoveConstraint(PointInputConstraints.XCoord);
						}
						else
						{
							exactMoveVector.X = MoveToPoint.X;
							exactMoveVector.Y = Math.Sqrt(val);
							exactMoveVector.Z = mouseMoveModelVector.Z;
						}
					}
					else
					{
						exactMoveVector.X = MoveToPoint.X;
					}
				}

				if (_moveConstraints.Contains(PointInputConstraints.YCoord))
				{
					if (exactMoveVector == new Vector3D()) exactMoveVector = mouseMoveModelVector;
					if (_moveConstraints.Contains(PointInputConstraints.Angle))
					{
						int sign = Math.Sign(exactMoveVector.X);
						exactMoveVector.X = MoveToPoint.Y / Math.Tan(_angle * Math.PI / 180);
						if (Math.Sign(exactMoveVector.X) != sign) exactMoveVector.X *= -1;
						exactMoveVector.Y = MoveToPoint.Y;
					}
					else if (_moveConstraints.Contains(PointInputConstraints.Length))
					{
						double val = _moveH * _moveH - MoveToPoint.Y * MoveToPoint.Y;
						if (val <= 0.0)
						{
							RemoveMoveConstraint(PointInputConstraints.YCoord);
						}
						else
						{
							exactMoveVector.X = Math.Sqrt(val);
							exactMoveVector.Y = MoveToPoint.Y;
							exactMoveVector.Z = mouseMoveModelVector.Z;
						}
					}
					else
					{
						exactMoveVector.Y = MoveToPoint.Y;
					}
				}

				if (exactMoveVector != new Vector3D())
				{
					exactMoveVector = currentGrid.LocalToGlobal.Transform(exactMoveVector.BimPoint()).WinPoint();
				}
				modelPoint.Point = (mouseDownModelPoint + exactMoveVector).BimPoint();
			}

			return modelPoint;
		}

		#endregion
	
	}
}
