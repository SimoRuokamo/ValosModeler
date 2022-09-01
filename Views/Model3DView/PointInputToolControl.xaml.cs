using Enterprixe.WPF.Tools.Viewport;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ValosModeler.Views.Model3DView
{
	/// <summary>
	/// Two modes: 1) If Viewport is specified: Full Auto operation. 2) If Viewport is null, UI control positions must be updated
	/// manually when the view changes.
	/// </summary>
	public interface IPointInputToolController
	{
		Enterprixe.WPF.Tools.Viewport.WireBase.IWireHost Viewport { get; }
		PointInputTool PointInputTool { get; set; }
		bool IsPickPointOperation { get; set; }
		bool SetRelativeInputPoint { get; set; }
		Epx.BIM.GridMesh.SnapPoint3D SnapPoint { get; }
		/// <summary>
		/// The CS the tool will follow.
		/// </summary>
		Epx.BIM.GridMesh.GridMesh CurrentGrid { get; }
		/// <summary>
		/// Converts model point to screen point.
		/// </summary>
		/// <param name="point">In model CS.</param>
		/// <returns></returns>
		Point GetScreenPoint(Point3D point);
		void AddSelectedVisual();
	}

	/// <summary>
	/// Interaction logic for PointInputToolControl.xaml
	/// </summary>
	public partial class PointInputToolControl : UserControl
	{
		public IPointInputToolController Controller
		{
			get { return (IPointInputToolController)GetValue(ControllerProperty); }
			set { SetValue(ControllerProperty, value); }
		}
		public static readonly DependencyProperty ControllerProperty =
			DependencyProperty.Register("Controller", typeof(IPointInputToolController), typeof(PointInputToolControl), new UIPropertyMetadata(null, ControllerPropertyChanged));

		public bool MouseEventCanvasHitTestOn
		{
			get { return (bool)GetValue(MouseEventCanvasHitTestOnProperty); }
			set { SetValue(MouseEventCanvasHitTestOnProperty, value); }
		}
		public static readonly DependencyProperty MouseEventCanvasHitTestOnProperty =
			DependencyProperty.Register("MouseEventCanvasHitTestOn", typeof(bool), typeof(PointInputToolControl), new UIPropertyMetadata(true));

		private bool _isLoaded = false;
		protected PointInputTool _pointInputTool;

		public PointInputToolControl()
		{
			InitializeComponent();
			Focusable = true;
			Loaded += new RoutedEventHandler(PointInputToolControl_Loaded);
			DataContextChanged += new DependencyPropertyChangedEventHandler(PointInputToolControl_DataContextChanged);
		}

		void PointInputToolControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			if (!_isLoaded)
			{
				PointInputToolControl_Loaded(null, null);
			}
		}

		protected static void ControllerPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			if (!(d as PointInputToolControl)._isLoaded)
			{
				(d as PointInputToolControl).PointInputToolControl_Loaded(null, null);
			}
		}
		void PointInputToolControl_Loaded(object sender, RoutedEventArgs e)
		{
			if (!_isLoaded && Controller != null && DataContext != null)
			{
				_pointInputTool = new PointInputTool(new System.Windows.Shapes.Line(), Controller, this);
				_pointInputTool.Line.Stroke = Brushes.Black;
				_pointInputTool.Line.StrokeDashArray = new DoubleCollection() { 10, 5 };
				//ViewModel.OverlayShapes.AddRange(_pointInputTool.Shapes);
				foreach (var shape in _pointInputTool.Shapes)
				{
					mainCanvas.Children.Add(shape);
				}

				Controller.PointInputTool = _pointInputTool;

				_isLoaded = true;
				IsHitTestVisible = false;
			}
		}

		protected override void OnMouseMove(MouseEventArgs e)
		{
			//if (!_isExactInputFocusCaptured)
			//{
			//    RedrawSnapPoints();
			//}
			base.OnMouseMove(e);
		}
		protected override void OnKeyDown(KeyEventArgs e)
		{
			//DoKeyDown(ref e);
			base.OnKeyDown(e);
		}
		protected override void OnKeyUp(KeyEventArgs e)
		{
			base.OnKeyUp(e);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="e"></param>
		/// <returns>If a key press was handled.</returns>
		public bool DoKeyDown(ref KeyEventArgs e)
		{
			bool retval = false;
			if (_pointInputTool != null && Controller != null && Controller.IsPickPointOperation)
			{
				if (e.Key == Key.Tab && _pointInputTool.IsActive)
				{
					IsHitTestVisible = true;
					_pointInputTool.ExactInputNext();
					Focus();
					retval = true;
				}
				else if (e.Key == Key.Return && _pointInputTool.IsExactInputFocusCaptured)
				{
					_pointInputTool.ExactInputCommit();
					Focus();
					IsHitTestVisible = false;
					retval = true;
				}
				else if (e.Key == Key.Escape && _pointInputTool.IsExactInputFocusCaptured)
				{
					_pointInputTool.ExactInputEsc();
					Focus();
					IsHitTestVisible = false;
					retval = true;
				}
				else if (e.Key == Key.O && _pointInputTool.IsExactInputFocusCaptured)
				{
					if (_pointInputTool.TryLockAngleToOriginalDir())
					{
						_pointInputTool.SetExactInputLock(false);
						_pointInputTool.RedrawSnapPoints();
						e.Handled = true;
						//base.OnKeyDown(e); // return
						retval = true;
					}
				}
				else if (e.Key == Key.P && _pointInputTool.IsExactInputFocusCaptured)
				{
					if (_pointInputTool.TryLockAngleToPerpendicularDir())
					{
						_pointInputTool.SetExactInputLock(false);
						_pointInputTool.RedrawSnapPoints();
						e.Handled = true;
						//base.OnKeyDown(e); // return
						retval = true;
					}
				}
				else if (e.Key == Key.R)
				{
					if (_pointInputTool.CurrentGrid != null)
					{
						Controller.SetRelativeInputPoint = true;
						retval = true;
					}
				}
			}
			return retval;
		}

		public PointInputTool PointInputTool
		{
			get { return _pointInputTool; }
		}
	}
}
