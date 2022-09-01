using System;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Input;
using Epx.BIM.Models;
using System.Windows.Threading;
namespace ValosModeler.Views.Model3DView
{
	/// <summary>
	/// Interaction logic for ModelView.xaml
	/// </summary>
	public partial class GLModelView : UserControl
	{
		public GLModelView()
		{
			InitializeComponent();
			if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(this))
				return;
			KeyDown += (s, e) => { _pointInputTool.DoKeyDown(ref e); };
		}
		public GLModelView(Epx.BIM.DataModel currentModel)
			: this()
		{
			DataContext = new ModelViewViewModel(currentModel) { RegenerateSelectedNode = false, WindowTitle = "GL-" + currentModel.Name };
		}
	}
}
