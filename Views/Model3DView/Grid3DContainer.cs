using Enterprixe.ValosUITools.Elements3D;
using Epx.BIM.GridMesh;
using Epx.BIM.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media.Media3D;
using ValosModeler.Views.Model3DView.Visuals;

namespace ValosModeler.Views.Model3DView
{
	/// <summary>
	/// Must be disposed.
	/// </summary>
	public class Grid3DContainer : AppModelPart3DContainer
	{
#if !NETCoreOnly
		protected override ModelVisual3DBase CreateVisual(ModelBaseNode node)
		{
			GridMesh3D grid3d = new GridMesh3D();
			Views.Model3DView.Node3DViewModel grid3dvm = new Views.Model3DView.Node3DViewModel(node);
			//grid3d.ViewModel = grid3dvm;
			Binding binding = new Binding("Node");
			binding.Source = grid3dvm;
			BindingOperations.SetBinding(grid3d, ModelVisual3DBase.NodeProperty, binding);
			return grid3d;
		}
#endif
	}
}
