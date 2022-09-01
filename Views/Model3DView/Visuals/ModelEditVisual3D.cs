using Enterprixe.WPF.Tools.Elements3D;
using Epx.BIM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace ValosModeler.Views.Model3DView.Visuals
{
	public abstract class ModelEditVisual3D : HighlightableVisual3D
	{
		static Material _editMaterial = null;
		protected static Material GetEditMaterial()
		{
			if (_editMaterial == null)
			{
				SolidColorBrush brush = new SolidColorBrush(Colors.LimeGreen);
				brush.Opacity = 0.7;
				brush.Freeze();
				_editMaterial = new DiffuseMaterial(brush);
				_editMaterial.Freeze();
			}
			return _editMaterial;
		}

		public ModelEditVisual3D()
		{
			IsScalingEnabled = true;
			IsXScaled = true;
			IsYScaled = true;
			IsZScaled = true;
		}

		protected double _thickness;
		protected BaseDataNode _attachedNode;

		public BaseDataNode AttachedNode
		{
			get { return _attachedNode; }
		}

		/// <summary>
		/// Updates Model.
		/// </summary>
		public double Thickness
		{
			get { return _thickness; }
			set
			{
				_thickness = value;
				CreateModel();
			}
		}

		/// <summary>
		/// Gets value which tells if this model represents the reference plane origin.
		/// </summary>
		public virtual bool IsOrigin { get; protected set; }

		/// <summary>
		/// Gets value which tells if this model represents the reference plane rotation.
		/// </summary>
		public virtual bool IsDirection { get; protected set; }

		public bool IsXAxisPoint { get; protected set; }

		public bool IsYAxisPoint { get; protected set; }

		public virtual string HoverText { get; }
	}
}
