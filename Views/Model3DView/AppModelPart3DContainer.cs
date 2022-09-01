
using Epx.BIM.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media.Media3D;
using Enterprixe.ValosUITools.Elements3D;
using ValosModeler.Views.Model3DView.Visuals;

namespace ValosModeler.Views.Model3DView
{
	/// <summary>
	/// Must be disposed.
	/// </summary>
	public class AppModelPart3DContainer : ModelPart3DContainer
	{
#if !NETCoreOnly
		/// <summary>
		/// Creates the visual.
		/// </summary>
		/// <param name="node">The node.</param>
		/// <returns>ModelVisual3DBase.</returns>
		protected override ModelVisual3DBase CreateVisual(ModelBaseNode node)
		{
			ModelObjectVisual visual = new ModelObjectVisual();
			Views.Model3DView.Node3DViewModel visualVM = new Views.Model3DView.Node3DViewModel(node);
			//visual.ViewModel = visualVM;

			Binding binding = new Binding("Viewport");
			binding.Source = this;
			BindingOperations.SetBinding(visual, ModelVisual3DBase.ViewportProperty, binding);

			binding = new Binding("Node");
			binding.Source = visualVM;
			BindingOperations.SetBinding(visual, ModelVisual3DBase.NodeProperty, binding);

			return visual;
		}
#endif
	}
}
