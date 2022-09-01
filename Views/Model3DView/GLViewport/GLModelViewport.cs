using Enterprixe.WPF.Tools.Viewport.WireBase;
using Epx.BIM.Models;
using System.Collections.Generic;
using GLGraphicsLib;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ValosModeler.Views.Model3DView;
using Enterprixe.WPF.Tools.Localization;
using OpenTK.Mathematics;
namespace GLWPFViewPort
{
	/// <summary>
	/// Class GLModelViewport.
	/// </summary>
	public partial class GLModelViewport : RenderingViewport, IWireHost, IModelEditorContainer
	{
		public static readonly DependencyProperty FPS =DependencyProperty.Register( nameof(FPS), typeof(string), typeof(GLModelViewport));
		protected override void RenderScene(RenderEventArgs e)
		{
			try
			{
				var before = Environment.TickCount;
				ModelRoot?.RenderScene(e);
				var after = Environment.TickCount;
				var span = after - before;
				if(span > 0)
				{
					Dispatcher.Invoke(() => { SetValue(FPS, "FPS= " +(1000 / Math.Max(1, span)).ToString("F1")); });
				}
			}
			catch(Exception ex)
			{
				var mess = ex.Message;
				System.Diagnostics.Debugger.Break();
			}
		}

		internal VisualModelRoot ModelRoot { get; private set; }

		public IEnumerable<ModelBaseNode> ModelParts
		{
			get { return (IEnumerable<ModelBaseNode>)GetValue(PartsProperty); }
			set { SetValue(PartsProperty, value); }
		}
		public static readonly DependencyProperty PartsProperty =
			DependencyProperty.Register( nameof(ModelParts), typeof(IEnumerable<ModelBaseNode>), typeof(GLModelViewport),
				new PropertyMetadata((d,e)=>
				{
					if((d as GLModelViewport).ModelRoot is GLModelPartsContainer mcontainer)
						mcontainer.SetVisibleNodes(e.NewValue as IEnumerable<ModelBaseNode>);
				}));
		//current grid
		public Epx.BIM.GridMesh.GridMesh CurrentGrid
		{
			get { return (Epx.BIM.GridMesh.GridMesh)GetValue(CurrentGridProperty); }
			set { SetValue(CurrentGridProperty, value); }
		}
		public static readonly DependencyProperty CurrentGridProperty =
			DependencyProperty.Register( nameof(CurrentGrid), typeof(Epx.BIM.GridMesh.GridMesh), typeof(GLModelViewport),
				new PropertyMetadata((d, e) =>
				{
					if ((d as GLModelViewport).ModelRoot is GLModelPartsContainer mcontainer)
						mcontainer.SetReferencePlane(e.NewValue as Epx.BIM.GridMesh.GridMesh);
				}));

		//editor
		public ModelEditor Editor => (ModelEditor)GetValue(EditorProperty);
		public static readonly DependencyProperty EditorProperty =DependencyProperty.Register( nameof(Editor), typeof(ModelEditor), typeof(GLModelViewport));

		public IEnumerable<Epx.BIM.BaseDataNode> SelectedNodes => ViewModel.ContextualNodes;
		/// <summary>
		/// Initializes a new instance of the <see cref="ModelViewport"/> class.
		/// </summary>
		public GLModelViewport()
		{
			/////////
			TestCode();
			/////////
			if(System.ComponentModel.DesignerProperties.GetIsInDesignMode(this))
				return;
			DataContextChanged += ModelViewport_DataContextChanged;
			//setup camera to scene
			var size = VisualModelRoot.SCENE_SIZE;
			Camera.DefaultState.Position = new Vector3(-19.7f, -31.4f, 18.8f);
			Camera.NearPlaneDistance = 0;
			Camera.FarPlaneDistance = size * 2;
			Camera.DefaultState.Width = size;
			Camera.ResetToDefault();
		}

		private void ViewModel_ModelClosing(object sender, EventArgs e)
		{
			(ModelRoot as GLModelPartsContainer)?.DisposeFinally();
		}

		protected override void OnViewport_GlCleanup()
		{
			base.OnViewport_GlCleanup();
			ModelRoot?.Dispose();
		}

		//IWireHost implementation
		public Enterprixe.WPF.Tools.Viewport.WireBase.IWireHost Viewport => this;
		public event ViewChangedEventHandler ViewChanged;
		public void OnViewChanged()
		{
			ViewChanged?.Invoke(this);
		}
		//
		private void ModelViewport_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			ModelRoot = new GLModelPartsContainer(this);
			(ModelRoot as GLModelPartsContainer).SetVisibleNodes(ViewModel.DisplayedParts);
			SetValue(EditorProperty, new ModelEditor(this) { ViewModel = ViewModel });
			ViewModel.ModelClosing += ViewModel_ModelClosing;
		}
		

		protected ModelViewViewModel ViewModel => DataContext as ModelViewViewModel;

		#region Mouse

		protected override void OnMouseWheel(MouseWheelEventArgs e)
		{
			base.OnMouseWheel(e);
			ViewModel.UpdateDimensionsOverlay();
		}

		protected override void OnMouseDown(MouseButtonEventArgs e)
		{
			base.OnMouseDown(e);
		}
		protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
		{
			Editor.OnMouseLeftButtonDown(e);
			//var pos = e.GetPosition(this);
			//var screenPoint = new OpenTK.Vector2((float)pos.X, (float)pos.Y);
			//ModelRoot.SelectObjectToMove(screenPoint);
			var pos = e.GetPosition(this);
			var screenPoint = new Vector2((float)pos.X, (float)pos.Y);
			Camera.HitPivotPoint = ModelRoot.ObjectHitPoint(screenPoint);

			base.OnMouseLeftButtonDown(e);
		}
		protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
		{
			Camera.HitPivotPoint = null;
			Editor.OnMouseLeftButtonUp(e);
			base.OnMouseLeftButtonUp(e);
		}
		protected override void OnMouseRightButtonUp(MouseButtonEventArgs e)
		{
			//CreateContextMenu();
			Editor.OnMouseRightButtonUp(e);
			base.OnMouseRightButtonUp(e);
		}
		protected override void OnMouseMove(MouseEventArgs e)
		{
			var pos = e.GetPosition(this);
			var screenPoint = new OpenTK.Mathematics.Vector2((float)pos.X, (float)pos.Y);
			//ModelRoot.HighLightHitObject(screenPoint);
			ModelRoot?.SnappedRefPlanePoint(screenPoint);
			Editor.OnMouseMove(e);
			base.OnMouseMove(e);
		}

		protected ResourceDictionary _viewportResources = null;
		protected void CreateContextMenu()
		{
			if(_viewportResources == null)
			{
				_viewportResources = new ResourceDictionary();
				string assemblyName = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;
				ResourceDictionary contextMenuIconsResourceDictionary = new ResourceDictionary();
				contextMenuIconsResourceDictionary.Source = new Uri("pack://application:,,,/" + assemblyName + ";component/Themes/Icons.xaml");//, UriKind.Absolute);
				_viewportResources.MergedDictionaries.Add(contextMenuIconsResourceDictionary);
			}
			ContextMenu = new ContextMenu();

			var item = new MenuItem();
			item.Header = CultureManager.GetLocalizedString("Reset View");
			item.Icon = new Image { Source = _viewportResources["Camera3DIcon"] as System.Windows.Media.DrawingImage, Width = 16, Height = 16 };
			item.Click += (s, e) =>
			 {
				 Camera.ResetToDefault();
				 RenderRequest(RepaintReason.CameraChanged); //need immediate update
			};
			ContextMenu.Items.Add(item);
			ContextMenu.Items.Add(new Separator());
			//
			item = new MenuItem();
			item.Header = CultureManager.GetLocalizedString("Remove Object");
			item.Icon = new Image { Source = _viewportResources["RightClick_Delete"] as System.Windows.Media.DrawingImage, Width = 16, Height = 16 };
			item.IsEnabled = (ViewModel.ContextualNode as Epx.BIM.Models.ModelBaseNode)?.Geometry3D != null;
			item.Click += (s, e) =>
			{
				ViewModel.RemoveContextualNodes();
			};
			ContextMenu.Items.Add(item);
			//
			item = new MenuItem();
			item.Header = CultureManager.GetLocalizedString("Regenerate Object");
			item.Icon = new Image { Source = _viewportResources["ResetIcon"] as System.Windows.Media.DrawingImage, Width = 16, Height = 16 };
			item.IsEnabled = (ViewModel.ContextualNode as Epx.BIM.Models.ModelBaseNode)?.Geometry3D != null;
			item.Click += (s, e) =>
			{
				//Editor.RegenerateContextualNode();
			};
			ContextMenu.Items.Add(item);


			ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.MousePoint;
			ContextMenu.IsOpen = true;
		}

		#endregion //Mouse

		#region Key Events

		protected override void OnKeyDown(KeyEventArgs e)
		{
			Editor.OnKeyDown(e);
			base.OnKeyDown(e);
		}

		protected override void OnKeyUp(KeyEventArgs e)
		{
			Editor.OnKeyUp(e);
			base.OnKeyDown(e);
		}

		#endregion //key events


		void TestCode()
		{
			/*
			int counter = 0;
			while (++counter < 100)
			{
				var faceRegex = new System.Text.RegularExpressions.Regex(@"^f\s+(-?\d+)\/(-?\d+)\/(-?\d+)\s+(-?\d+)\/(-?\d+)\/(-?\d+)\s+(-?\d+)\/(-?\d+)\/(-?\d+)(?:\s+(-?\d+)\/(-?\d+)\/(-?\d+))?");

				var line1 = "f 38/1/138 37/1/139 19/1/140 18/1/137";
				var line2 = "f 19/0/141 13/0/142 12/0/143";
				var line = line2;

				if (faceRegex.IsMatch(line))
				{
					var match = faceRegex.Match(line);
					for (int i = 1; i <= 10; i += 3)
					{
						var position = int.Parse(match.Groups[i].Value);
						var normal = int.Parse(match.Groups[i + 2].Value);
						var uvs = int.Parse(match.Groups[i + 1].Value);
					}
				}
			}
			*/
		}

	}
}
