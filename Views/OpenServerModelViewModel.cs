using Enterprixe.WPF.Tools.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ValosService;

namespace ValosModeler.Views
{
	public class OpenServerModelViewModel : Infrastructure.ViewModelBase
	{
		/// <summary>
		/// The _data models
		/// </summary>
		private List<ServerDataModel> _dataModels = new List<ServerDataModel>();
		/// <summary>
		/// The _connected to server
		/// </summary>
		bool _connectedToServer;

		/// <summary>
		/// Initializes a new instance of the <see cref="UserDatabasesViewViewModel"/> class.
		/// </summary>
		/// <param name="dataModels">The data models.</param>
		/// <param name="connectedToServer">if set to <c>true</c> [connected to server].</param>
		public OpenServerModelViewModel(List<ServerDataModel> dataModels, bool connectedToServer)
			: base()
		{
			_dataModels = dataModels;
			_connectedToServer = connectedToServer;
		}

		/// <summary>
		/// Gets the list of data models.
		/// </summary>
		/// <value>The data models.</value>
		public List<ServerDataModel> DataModels
		{
			get
			{
				return _dataModels;
			}
		}

		/// <summary>
		/// The _selected model
		/// </summary>
		ServerDataModel _selectedModel = null;
		/// <summary>
		/// Gets or sets the selected model.
		/// </summary>
		/// <value>The selected model.</value>
		public ServerDataModel SelectedModel
		{
			get
			{
				return _selectedModel;
			}
			set
			{
				if (value != _selectedModel)
				{
					_selectedModel = value;
					OnPropertyChanged("SelectedModel");
				}
			}
		}

		/// <summary>
		/// Shows the dialog.
		/// </summary>
		/// <returns>ServerDataModel.</returns>
		public ServerDataModel ShowDialog()
		{
			var _modelDialog = new Views.OpenServerModelDialog();
			System.Windows.Window mainWindow = System.Windows.Application.Current.MainWindow as MainWindow;
			_modelDialog.Owner = mainWindow;
			_modelDialog.DataContext = this;
			bool result = (bool)_modelDialog.ShowDialog();
			return result ? SelectedModel : null;
		}

		/// <summary>
		/// Indicates whether it was possible to conenct to the server.
		/// </summary>
		/// <value>The connection result.</value>
		public string ConnectionResult
		{
			get
			{
				string retVal = "";
				if (_connectedToServer)
					retVal = CultureManager.GetLocalizedString("Connected to Server");
				else
					retVal = CultureManager.GetLocalizedString("Unable to connect to server");
				return retVal;
			}
		}
	}
}
