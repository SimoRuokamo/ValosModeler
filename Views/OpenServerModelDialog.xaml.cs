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
using System.Windows.Shapes;

namespace ValosModeler.Views
{
	/// <summary>
	/// Interaction logic for OpenServerModelDialog.xaml
	/// </summary>
	public partial class OpenServerModelDialog : Window
	{
		public OpenServerModelDialog()
		{
			InitializeComponent();
		}

		private void _ButtonOK_Click(object sender, RoutedEventArgs e)
		{
			this.DialogResult = true;
		}
	}
}
