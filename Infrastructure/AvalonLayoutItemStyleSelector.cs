using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ValosModeler.Infrastructure
{
	/// <summary>
	/// Style selector for LayoutItems (to hook up Close and Hide Commands).
	/// </summary>
	public class AvalonLayoutItemStyleSelector : System.Windows.Controls.StyleSelector
    {
		public Style LayoutItemStyle { get; set; }
		public Style LayoutAnchorableItemStyle { get; set; }
		public Style LayoutDocumentItemStyle { get; set; }

		public override System.Windows.Style SelectStyle(object item, System.Windows.DependencyObject container)
		{
			if (container is AvalonDock.Controls.LayoutAnchorableItem)
				return LayoutAnchorableItemStyle;

			if (container is AvalonDock.Controls.LayoutDocumentItem)
				return LayoutDocumentItemStyle;

			if (container is AvalonDock.Controls.LayoutItem)
				return LayoutItemStyle;

			return base.SelectStyle(item, container);
		}
	}
}
