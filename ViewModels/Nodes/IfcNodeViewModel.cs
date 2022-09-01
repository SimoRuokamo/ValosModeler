using AvalonDock.Layout;
using Enterprixe.WPF.Tools.Viewport;
using Epx.BIM;
using Epx.BIM.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls.Ribbon;
using System.Windows.Input;
using ValosModeler.Infrastructure;
using ValosModeler.Infrastructure.Events;
using ValosModeler.Views.Model3DView;

namespace ValosModeler.ViewModels.Nodes
{
	public class IfcNodeViewModel : GeneralNodeViewModel
	{
		Valos.Ifc.ValosIfcSpatialNode _ifcSpatialNode;
		public IfcNodeViewModel(BaseDataNode node, bool isSelected)
			: base(node, isSelected)
		{
			_ifcSpatialNode = node as Valos.Ifc.ValosIfcSpatialNode;
		}
		private int _objToSearch = 0;
		public int SearchObject 
		{
			get => _objToSearch;
			set 
			{
				if(_objToSearch != value)
				{
					_objToSearch = value;
					SearchIfcObject(_objToSearch);
				}
			}
		}
		IEnumerable<Valos.Ifc.ValosIfcSpatialNode> allIfcNodes = null;
		private void SearchIfcObject(int ifcId)
		{
			if(_ifcSpatialNode != null)
			{
				var model = _ifcSpatialNode.GetParent<Valos.Ifc.IfcModel>();
				if(allIfcNodes == null)
					allIfcNodes= model.GetDescendantNodes<Valos.Ifc.ValosIfcSpatialNode>().Where(n=> n.IfcSpatialObject != null);
				var found = allIfcNodes.FirstOrDefault(n => n.IfcSpatialObject.IfcId == _objToSearch);
				if(found != null)
				{
					Infrastructure.Events.NodeSelected.Publish(found, this);
				}

			}

		}

		Valos.Ifc.ValosIfcSpatialNode _selectedObject = null;
		[MediatorMessageSink(Infrastructure.Events.NodeSelected.MessageID)]
		public void OnObjectSelected(Infrastructure.Events.GeneralNodePayload param)
		{
			if(param.Sender == this) return;
			_selectedObject = param.DataNodes.FirstOrDefault() as Valos.Ifc.ValosIfcSpatialNode;
			//if(_showSelected == null)
			//	_showSelected = false;
			OnPropertyChanged("ShowSelected");
		}

		bool _showSelected = false;
		public bool ShowSelected
		{
			get => _showSelected;
			set 
			{
				if(value != _showSelected)
				{
					var model = _ifcSpatialNode.GetParent<Valos.Ifc.IfcModel>();
					_showSelected = value;
					if(allIfcNodes == null)
						allIfcNodes = model.GetDescendantNodes<Valos.Ifc.ValosIfcSpatialNode>().Where(n => n.IfcSpatialObject != null);
					foreach(var node in allIfcNodes)
					{
						node.IsShownIn3D = !value;
						if(node.Parent == _ifcSpatialNode)
							node.IsShownIn3D = true;
					}
					_ifcSpatialNode.IsShownIn3D = true;
					Infrastructure.Events.Update3D.PublishAsync(allIfcNodes);
				}
				_showSelected = value;
			}
		}

	}
}
