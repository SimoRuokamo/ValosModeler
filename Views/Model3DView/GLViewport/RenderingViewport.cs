//globals
global using Matrix4 = OpenTK.Mathematics.Matrix4;
global using vfloat = System.Single;
global using Vector3 = OpenTK.Mathematics.Vector3;
global using Vector2 = OpenTK.Mathematics.Vector2;
global using Box2 = GLGraphicsLib.Tools.Box2;
global using Box3 = GLGraphicsLib.Tools.Box3;

//
using GLGraphicsLib;
using ObjectTK;
using ObjectTK.Shaders;
using OpenTK.Mathematics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Graphics;
using System;
using System.Drawing;
using System.Windows.Input;
using OpenTK.Wpf;


namespace GLWPFViewPort
{


    public class RenderingViewport : GLWpfControl, IRenderingEngine
    {
        internal double FPSStr { get; private set; }

		public RenderingViewport(double fPSStr)
		{
			FPSStr = fPSStr;
		}

		public Camera Camera { get; set; } = new OrthoCamera { PivotAroundMousePoint = true };
        IGraphicsContext Context = new GraphicsContext(); // unique context to create/dispose GL objects
        GLGraphicsLib.Shaders.GradientBckgShader BckgShader; // shader providing gradient background
        public RenderingViewport()
		{
            if(!System.ComponentModel.DesignerProperties.GetIsInDesignMode(this))
            {
                Ready += OpenGLInitialized;
                var Settings = new GLWpfControlSettings { MajorVersion = 2, MinorVersion = 1, RenderContinuously = false, UseDeviceDpi = false };
                Start(Settings);
                Render += OnGlRender;
                Unloaded += RenderingViewport_Unloaded;
                var size = VisualModelRoot.SCENE_SIZE;
                Camera.DefaultState.Position = new Vector3(-3.97f, -9.55f, 4.25f);
                Camera.NearPlaneDistance = 0;
                Camera.FarPlaneDistance = size * 2;
                Camera.DefaultState.Width = size;
                Camera.ResetToDefault();
                SizeChanged += (s, e) =>
                {
                    Camera.Viewport = new Box2(Vector2.Zero, new Vector2((float)ActualWidth, (float)ActualHeight));
                    RenderRequest(RepaintReason.ViewportSizeChanged);
                };

                Unloaded += (s, e) =>
                {
                    _scene?.Dispose();
                };
                Loaded += (s, e) =>
                {
                    ////setup camera to scene
                    //var size = VisualModelRoot.SCENE_SIZE;
                    //Camera.DefaultState.Position = new Vector3(-19.7f, -31.4f, 18.8f);
                    //Camera.NearPlaneDistance = 0;
                    //Camera.FarPlaneDistance = size * 2;
                    //Camera.DefaultState.Width = size;
                    //Camera.ResetToDefault();
                };
            }
        }

        protected VisualModelRoot CreateScene()
		{

            return VisualModelRoot.CreateTestModel(this);

        }
        VisualModelRoot _scene;
        bool _requireRegen = false;
        protected virtual void RenderScene(RenderEventArgs e)
        {
            try
            {
                var before = Environment.TickCount;
                if(_scene == null)
                {
                    _scene = CreateScene();
                    _scene.Camera = Camera;
                    _scene.RegenerateScene(e);
                }
#if DEBUG
                if(_requireRegen)
                    _scene.RegenerateScene(e);
                _requireRegen = false;
#endif
                _scene.RenderScene(e);

                var after = Environment.TickCount;
                var span = after - before;
                if(span > 0)
                    FPSStr = 1000d / Math.Max(1, span);

            }
            catch(Exception ex)
			{
                var mess = ex.Message;
                System.Diagnostics.Debugger.Break();
			}
        }
        private void RenderingViewport_Unloaded(object sender, System.Windows.RoutedEventArgs e)
        {
            OnViewport_GlCleanup();
        }

        protected virtual void OnViewport_GlCleanup()
		{
            ProgramFactory.DisposeAll(Context);
            _scene?.Dispose();
        }

        public void RenderRequest(RepaintReason reason = RepaintReason.Unknown)
        {
            lastReason = reason;
            Dispatcher.BeginInvoke(() => InvalidateVisual(), System.Windows.Threading.DispatcherPriority.Render);
        }

        
        protected void OpenGLInitialized()
        {
			//
			var versionMajor = GL.GetInteger(GetPName.MajorVersion);
            var versionMinor = GL.GetInteger(GetPName.MinorVersion);
            var shade = GL.GetString( StringName.ShadingLanguageVersion);
            BckgShader = new GLGraphicsLib.Shaders.GradientBckgShader(Context);
            //
            GL.Enable(EnableCap.DepthTest);
            GL.Enable(EnableCap.Blend);
            //GL.Enable(EnableCap.PolygonSmooth);//Enable that to view faces borders
            GL.Hint(HintTarget.PolygonSmoothHint, HintMode.Nicest);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            GL.ShadeModel(ShadingModel.Smooth);
            GL.LoadIdentity();
            GL.Enable(EnableCap.CullFace);
            GL.CullFace(CullFaceMode.Back);
            GL.LoadIdentity();
            // set nice clear color
            //GL.ClearColor(Color.FromArgb(100, Color.MidnightBlue));
            Camera.Viewport = new Box2(Vector2.Zero, new Vector2((float)ActualWidth, (float)ActualHeight));
            //
            // Check for necessary capabilities:
            var extensions = GL.GetString(StringName.Extensions);
            if (!GL.GetString(StringName.Extensions).Contains("GL_ARB_shading_language"))
            {
                throw new NotSupportedException(String.Format("This example requires OpenGL 2.0. Found {0}. Aborting.",
                    GL.GetString(StringName.Version).Substring(0, 3)));
            }
            //if (!extensions.Contains("GL_ARB_texture_compression") || !extensions.Contains("GL_EXT_texture_compression_s3tc"))
            //{
            //    throw new NotSupportedException("This example requires support for texture compression. Aborting.");
            //}
            //
            //update context data
            Context = new GraphicsContext(GetHashCode(), Framebuffer);
        }
        int counter = 0;
        RepaintReason lastReason = RepaintReason.CameraChanged;
        protected void OnGlRender(TimeSpan delta)
        {
            RenderEventArgs e = new RenderEventArgs((int)ActualWidth, (int)ActualHeight, Context);
            e.Reason = lastReason;
            //GL.ClearColor(Color.FromArgb(100, Color.Teal));
            //
            // set up viewport
            GL.Viewport(0, 0, (int)ActualWidth, (int)ActualHeight);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            //gradient background
            //var topColor = System.Drawing.ColorTranslator.FromHtml("#74A1EE");
            //var bottomColor = System.Drawing.ColorTranslator.FromHtml("#5D7293");
            var topColor = System.Drawing.Color.White;
            var bottomColor = System.Drawing.ColorTranslator.FromHtml("#91A9C4");

            BckgShader.DrawBackground(e, topColor, bottomColor);
            GL.Clear( ClearBufferMask.DepthBufferBit);
            e.FrameId = counter++;
            if(counter >= int.MaxValue)
                counter = 0;
            e.RenderCounter = 0;
            RenderScene(e);
        }

        #region mouse events
        //Mouse events are in UI thread - so no any calls to GL

        private Vector2? _mouseDownPos = null, _mouseCurrentPos = null;
        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            Focusable = true;
            Focus();
            var pos = e.GetPosition(this);
            _mouseDownPos = new Vector2((float)pos.X, (float)pos.Y);
            //Camera.HitPivotPoint = _modelRoot.ObjectHitPoint(_mouseDownPos.Value);
            //base.OnMouseDown(e);
            //RenderRequest();
        }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            var pos = e.GetPosition(this);
            var action = Camera.MoveAction.None;
            if(_mouseDownPos != null)
            {
                _mouseCurrentPos = new Vector2((float)pos.X, (float)pos.Y);
                if(e.LeftButton == MouseButtonState.Pressed)
                    action = Camera.MoveAction.Rotate;
                if(e.MiddleButton == MouseButtonState.Pressed)
                    action = Camera.MoveAction.Pan;
                Camera.MouseMoved(_mouseDownPos.Value, _mouseCurrentPos.Value, action);
                _mouseDownPos = _mouseCurrentPos;
            }
            base.OnMouseMove(e);
            if(action != Camera.MoveAction.None)
                RenderRequest(RepaintReason.CameraChanged);
            //RenderRequest( action == Camera.MoveAction.None ? RepaintReason.MouseMoved : RepaintReason.CameraChanged);
        }
        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            var pos = e.GetPosition(this);
            var currentPos = new Vector2((float)pos.X, (float)pos.Y);
            Camera.MouseWheelChanged(currentPos, e.Delta);
            base.OnMouseWheel(e);
            RenderRequest(RepaintReason.CameraChanged);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            var camera = Camera.State;
            Vector3 xDir = Vector3.Cross(camera.UpDir, camera.LookDir);
            xDir.Normalize();
            Vector3 yDir = Vector3.Cross(xDir, camera.UpDir);
            yDir.Normalize();
            vfloat dx = 0, dy = 0, step = camera.Width / 50;
            if(e.Key == Key.Left)
                dx = -step;
            if(e.Key == Key.Right)
                dx = step;
            if(e.Key == Key.Up)
                dy = -step;
            if(e.Key == Key.Down)
                dy = step;
            if(dx != 0 || dy != 0)
            {
                var move = xDir * dx + yDir * dy;
                camera.Position += move;
                camera.Target += move;
            }
            vfloat zoom = 0;
            if(e.Key == Key.OemPlus || e.Key == Key.Add)
                zoom = 60;
            if(e.Key == Key.OemMinus || e.Key == Key.Subtract)
                zoom = -60;
            var currentPos = new Vector2((float)ActualWidth / 2, (float)ActualHeight / 2);
            Camera.ZoomAtPoint(currentPos, zoom);

            base.OnKeyDown(e);
            RenderRequest();
        }
        #endregion
    }

}
