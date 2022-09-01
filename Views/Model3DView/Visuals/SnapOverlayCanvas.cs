using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace ValosModeler.Views.Model3DView.Visuals
{
	public class SnapOverlayCanvas : Canvas
	{
		public List<FrameworkElement> ItemsSource
		{
			get { return (List<FrameworkElement>)GetValue(ItemsSourceProperty); }
			set { SetValue(ItemsSourceProperty, value); }
		}
		public static readonly DependencyProperty ItemsSourceProperty =
			DependencyProperty.Register("ItemsSource", typeof(List<FrameworkElement>), typeof(SnapOverlayCanvas), new UIPropertyMetadata(null, null, CoerceItemsCallback));

		protected static object CoerceItemsCallback(DependencyObject d, object value)
		{
			(d as SnapOverlayCanvas).RefreshChildren();
			return value;
		}

		private void RefreshChildren()
		{
			Children.Clear();
			if (ItemsSource != null)
			{
				foreach (FrameworkElement s in ItemsSource)
				{
					Children.Add(s);
				}
			}
		}
	}
}
