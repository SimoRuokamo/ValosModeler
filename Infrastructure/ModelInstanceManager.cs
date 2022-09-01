using Enterprixe.WPF.Tools.Localization;
using Epx.BIM;
using Epx.BIM.Drawings;
using Epx.BIM.Models;
using ValosService;
using Epx.SDK;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using ValosModeler.Infrastructure.Events;

namespace ValosModeler.Infrastructure
{
	/// <summary>
	/// Delegate ModelOpenSaveEventHandler
	/// </summary>
	/// <param name="sender">The sender.</param>
	/// <param name="e">The <see cref="ModelOpenSaveEventArgs"/> instance containing the event data.</param>
	public delegate void ModelOpenSaveEventHandler(object sender, ModelOpenSaveEventArgs e);

	/// <summary>
	///  A class which represents open/save events.
	/// </summary>
	/// <seealso cref="System.EventArgs" />
	public class ModelOpenSaveEventArgs : EventArgs
	{
		/// <summary>
		/// Enum OpenSaveOperation
		/// </summary>
		public enum OpenSaveOperation
		{
			/// <summary>
			/// The new
			/// </summary>
			New,
			/// <summary>
			/// The open
			/// </summary>
			Open,
			/// <summary>
			/// The save
			/// </summary>
			Save,
			/// <summary>
			/// The save as
			/// </summary>
			SaveAs
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ModelOpenSaveEventArgs"/> class.
		/// </summary>
		/// <param name="result">if set to <c>true</c> [result].</param>
		/// <param name="exception">The exception.</param>
		/// <param name="operation">The operation.</param>
		public ModelOpenSaveEventArgs(bool result, string exception, OpenSaveOperation operation)
		{
			Result = result;
			Exception = exception;
			Operation = operation;
		}

		/// <summary>
		/// Gets or sets a value indicating whether this <see cref="ModelOpenSaveEventArgs"/> is result.
		/// </summary>
		/// <value><c>true</c> if result; otherwise, <c>false</c>.</value>
		public bool Result { get; set; }
		/// <summary>
		/// Gets or sets the exception.
		/// </summary>
		/// <value>The exception.</value>
		public string Exception { get; set; }
		/// <summary>
		/// Gets or sets the operation.
		/// </summary>
		/// <value>The operation.</value>
		public OpenSaveOperation Operation { get; set; }
	}

	/// <summary>
	/// A class which manages access to data model instances. This class Includes the methods needed to handle the data model, including the local save and communication with the Server model.
	/// Operations ont he data model which are supported include Save/Open, Load/remove nodes, RemoveNode, check in, checkout, get server events.
	/// The class also contains some commands which can be used to support actions such as check in and check out.
	/// </summary>
	/// <seealso cref="ViewModelBase" />
	public class ModelInstanceManager : ViewModelBase
	{
		/// <summary>
		/// Occurs when [model opened event].
		/// </summary>
		public event ModelOpenSaveEventHandler ModelOpenedEvent;
		/// <summary>
		/// Occurs when [model saved event].
		/// </summary>
		public event ModelOpenSaveEventHandler ModelSavedEvent;

		/// <summary>
		/// The _instance
		/// </summary>
		protected static ModelInstanceManager _instance;
		/// <summary>
		/// Gets the instance. Will always create a new instance if does not exist.
		/// </summary>
		/// <value>The instance.</value>
		public static ModelInstanceManager Instance
		{
			get
			{
				if (_instance == null)
				{
					_instance = new ModelInstanceManager();
				}
				return _instance;
			}
		}

		/// <summary>
		/// Gets a value indicating whether this instance has been created.
		/// </summary>
		/// <value><c>true</c> if this instance has instance; otherwise, <c>false</c>.</value>
		public static bool HasInstance { get { return _instance != null; } }


		/// <summary>
		/// Initializes a new instance of the <see cref="ModelInstanceManager"/> class.
		/// </summary>
		protected ModelInstanceManager()
		{
			_currentModel = new DataModel("DesignDatabase", true);
			SessionService = new Enterprixe.ValosUITools.Infrastructure.SessionServiceViewModel();
		}

		/// <summary>
		/// Gets the server session service.
		/// </summary>
		/// <value>
		/// The session service.
		/// </value>
		public Enterprixe.ValosUITools.Infrastructure.SessionServiceViewModel SessionService { get; private set; }

		/// <summary>
		/// Initializes the service.
		/// </summary>
		/// <param name="username">The username.</param>
		/// <param name="password">The password.</param>
		/// <param name="applicationID">The application identifier.</param>
		/// <param name="serverAddress">The server address.</param>
		public void InitializeService(string username, string password, string applicationID, string serverAddress)
		{
			SessionService.ServiceUsername = username;
			SessionService.ServicePassword = password;
			SessionService.ServiceApplicationID = applicationID;
			SessionService.ServiceAddress = serverAddress;
		}

		/// <summary>
		/// The _current model
		/// </summary>
		DataModel _currentModel;

		/// <summary>
		/// The current model instance.
		/// </summary>
		/// <value>The current model.</value>
		public DataModel CurrentModel
		{
			get { return _currentModel; }
		}
		/// <summary>
		/// Set current model. Use only with special case.
		/// </summary>
		/// <param name="newCurrentModel">The new current model.</param>
		private void SetCurrentModel(DataModel newCurrentModel)
		{
			_currentModel = newCurrentModel;
		}
		/// <summary>
		/// Creates a new instance of the data model.
		/// </summary>
		/// <param name="name">The name.</param>
		/// <returns>DataModel.</returns>
		public DataModel CreateNewModel(string name = "DesignDatabase")
		{
			DataModel newModel = null;

			// Need to know if the program was started from Valos or local license system.
			if (SessionService.IsStartedWithServerLicense)
			{
				if (SessionService.IsSessionModelConnected) // model specific session
				{
					SessionService.EndSession();
					if (SessionService.StartSession(Guid.Empty, null, string.Empty))
					{
						newModel = GetNewModel(name);
					}
					else
					{
						// return null;
					}
				}
				else
				{
					newModel = GetNewModel(name);
				}
			}
			else
			{
				if (SessionService.IsSessionModelConnected) // model specific session
				{
					SessionService.EndSession();
				}
				newModel = GetNewModel(name);
			}

			return newModel;
		}

		private DataModel GetNewModel(string name)
		{
			_currentModel = new DataModel(name, true);
			CurrentDesignFilePath = string.Empty;
			SaveRequired = false;
			//SaveStateAction = null;
			return _currentModel;
		}

		#region Save/Open

		/// <summary>
		/// Gets or sets a value indicating whether this instance is save open in progress.
		/// </summary>
		/// <value><c>true</c> if this instance is save open in progress; otherwise, <c>false</c>.</value>
		public bool IsSaveOpenInProgress { get; set; }

		/// <summary>
		/// The _ save required
		/// </summary>
		private bool _SaveRequired = false;
		/// <summary>
		/// Gets or sets a value indicating whether [save required].
		/// </summary>
		/// <value><c>true</c> if [save required]; otherwise, <c>false</c>.</value>
		public bool SaveRequired
		{
			get => _SaveRequired;
			set => _SaveRequired = value;
		}

		/// <summary>
		/// Opens the model.
		/// </summary>
		/// <param name="valosSession">The session information.</param>
		/// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
		/// <exception cref="ServerServiceException"> Throws as server service Exception if there is a fault in the communication with the server.</exception>
		public bool OpenSessionModel()
		{
			ValosSessionStartup valosSession = SessionService.CurrentSession;
			if (valosSession == null) return false;
			bool returnResult = true;
			ServerServiceError error = null;
			DataModel dataModel = new DataModel();

			string openFileName = "";
			if (!UserLoginProject.LocalSaveForModelExists(valosSession.ServiceAddress, valosSession.SessionID, out openFileName))
			{
				//CurrentModel.Name = valosSession.ModelName;
				openFileName = GetSaveFilePath(valosSession.ModelName);
				if (string.IsNullOrEmpty(openFileName)) // cancel clicked
				{
					//IsSaveOpenInProgress = false;
					SessionService.EndSession();
					return false;
				}
			}

			error = UserLoginProject.OpenModel(valosSession.ServiceAddress,
				valosSession.SessionID, ref openFileName, out dataModel);

			if (error == null)
			{
				SetCurrentModel(dataModel);
				CurrentDesignFilePath = openFileName;
				SaveRequired = false;

			}
			else
			{
				returnResult = false;
				throw new ServerServiceException(error);
			}
			return returnResult;
		}

		/// <summary>
		/// Open a database file.
		/// </summary>
		/// <param name="openPath">The open path.</param>
		/// <returns>Null if not opened.</returns>
		public void Open(string openPath = null)
		{
			string path = string.IsNullOrEmpty(openPath) ? GetOpenFilePath() : openPath;
			bool result = false;

			if (!string.IsNullOrEmpty(path))
			{
				bool wasSessionEnded = SessionService.EndSession();
				string errorMessage = string.Empty;

				BackgroundWorker worker = new BackgroundWorker();
				worker.DoWork += delegate (object s, DoWorkEventArgs args)
				{
					if (!path.Contains(_autosaveSuffix)) DisposeAutoSave();
					IsSaveOpenInProgress = true;
					SessionService.AutoServerCheckEnabled = false;

					if (System.IO.File.Exists(path))
					{
						DataModel newInstance = null;
						var settings = Properties.Settings.Default;
						newInstance = Epx.SDK.DataStorageFunctions.Open(path, out errorMessage);

#if DEBUG && LOCAL_ONLY
						if (newInstance != null && string.IsNullOrEmpty(errorMessage))
						{
							_currentModel = newInstance;
							result = true;
						}
#else

					// If the datamodel is connected to the server try to open a session. If it fails cannot open the file.
						if (newInstance != null && string.IsNullOrEmpty(errorMessage) && newInstance.ConnectedToServer)
						{
							if (SessionService.StartSession(newInstance.UniqueID, newInstance, newInstance.Name, newInstance.ServerServiceAddress))
							{
								SessionService.SetModelSessionData(newInstance);
								_currentModel = newInstance;
								result = true;
							}
							else
							{
								newInstance = null;
								errorMessage = "Unable to start a session.";
								result = false;
							}
						}
						else if (newInstance != null && string.IsNullOrEmpty(errorMessage))
						{
							_currentModel = newInstance;
							result = true;
						}
#endif
					}
				};

				worker.RunWorkerCompleted += delegate (object s, RunWorkerCompletedEventArgs args)
				{
					if (result)
					{
						CheckBackwardCompatability();
						CurrentDesignFilePath = path;
						SaveRequired = false;
						if (ModelOpenedEvent != null) ModelOpenedEvent(this, new ModelOpenSaveEventArgs(result, "", ModelOpenSaveEventArgs.OpenSaveOperation.Open));
					}
					else
					{
						if (ModelOpenedEvent != null) ModelOpenedEvent(this, new ModelOpenSaveEventArgs(result, errorMessage, ModelOpenSaveEventArgs.OpenSaveOperation.Open));
					}

					IsSaveOpenInProgress = false;
					SessionService.AutoServerCheckEnabled = true;
				};
				worker.RunWorkerAsync();
			}
			else
			{
				if (ModelOpenedEvent != null) ModelOpenedEvent(this, new ModelOpenSaveEventArgs(result, "", ModelOpenSaveEventArgs.OpenSaveOperation.Open));
			}

		}

		/// <summary>
		/// The _save operation
		/// </summary>
		private ModelOpenSaveEventArgs.OpenSaveOperation _saveOperation = ModelOpenSaveEventArgs.OpenSaveOperation.Save;
		/// <summary>
		/// Saves this instance.
		/// </summary>
		public void Save()
		{
			string path;
			if (!string.IsNullOrEmpty(CurrentDesignFilePath) && Directory.Exists(Path.GetDirectoryName(CurrentDesignFilePath)))
			{
				path = CurrentDesignFilePath;
			}
			else
			{
				path = GetSaveFilePath();
			}

			string ext = Path.GetExtension(path);
			//if (ext == ".sdf" || ext == ".SDF")
			//	path = GetSaveFilePath();

			//Need to check that user hasn't manually changed file extension in filesavedialog
			//ext = Path.GetExtension(path);
			//if (ext != ".mdf")
			//	path = path + ".mdf";

			_saveOperation = ModelOpenSaveEventArgs.OpenSaveOperation.Save;
			DoSave(path);
		}

		/// <summary>
		/// Saves as.
		/// </summary>
		public void SaveAs()
		{
			string path = GetSaveFilePath();

			//Need to check that user hasn't manually changed file extension in filesavedialog
			//string ext = Path.GetExtension(path);
			//if (ext != null && ext != ".mdf")
			//	path = path + ".mdf";

			_saveOperation = ModelOpenSaveEventArgs.OpenSaveOperation.SaveAs;
			DoSave(path);
		}
		/// <summary>
		/// Does the save.
		/// </summary>
		/// <param name="path">The path.</param>
		private void DoSave(string path)
		{
			bool result = false;
			string errorMessage = string.Empty;
			if (!string.IsNullOrEmpty(path) && !IsSaveOpenInProgress)
			{
				BackgroundWorker worker = new BackgroundWorker();
				worker.DoWork += delegate (object s, DoWorkEventArgs args)
				{
					IsSaveOpenInProgress = true;
					SessionService.AutoServerCheckEnabled = false;

					// create a copy to generate new Guids for all nodes
					ServerServiceError err;
					if (_saveOperation == ModelOpenSaveEventArgs.OpenSaveOperation.Save)
					{
						err = LoadSave.Save(_currentModel, path, false);
					}
					else
					{
						err = LoadSave.Save(_currentModel, path);
						//err = null;
					}
					if (err != null) errorMessage = err.ErrorType.ToString();// +" | " + App.GenerateErrorString(err.OtherException);
					result = err == null;
				};

				worker.RunWorkerCompleted += delegate (object s, RunWorkerCompletedEventArgs args)
				{
					if (result)
					{
						CurrentDesignFilePath = path;
						SaveRequired = false;
					}
					else
					{
					}
					IsSaveOpenInProgress = false;
					SessionService.AutoServerCheckEnabled = true;
					if (ModelSavedEvent != null) ModelSavedEvent(this, new ModelOpenSaveEventArgs(result, errorMessage, _saveOperation));
				};
				worker.RunWorkerAsync();
			}
			else
			{
				if (ModelSavedEvent != null) ModelSavedEvent(this, new ModelOpenSaveEventArgs(result, errorMessage, _saveOperation));
			}
		}

#region AutoSave

		/// <summary>
		/// The _autosave suffix
		/// </summary>
		private const string _autosaveSuffix = "_AutoSave_ValosApp";
		/// <summary>
		/// Process specific.
		/// </summary>
		/// <value>The autosave suffix.</value>
		private string AutosaveSuffix
		{
			get { return _autosaveSuffix + "_" + System.Diagnostics.Process.GetCurrentProcess().Id.ToString(); }
		}
		/// <summary>
		/// The auto save renamed string.
		/// </summary>
		private const string _autoSaveRenamed = "_Save_ValosApp";
		/// <summary>
		/// Process specific.
		/// </summary>
		/// <value>The auto save renamed property.</value>
		private string AutoSaveRenamed
		{
			get { return _autoSaveRenamed + "_" + System.Diagnostics.Process.GetCurrentProcess().Id.ToString(); }
		}

		/// <summary>
		/// For auto save thread. Path is not checked for correctness.
		/// </summary>
		public void AutoSave()
		{
			if (!IsSaveOpenInProgress)
			{
				IsSaveOpenInProgress = true;
				SessionService.AutoServerCheckEnabled = false;
				string filename = !string.IsNullOrEmpty(CurrentDesignFilePath) ? Path.GetFileName(CurrentDesignFilePath) : string.Empty;
				string caseName = _currentModel.Name;
				string directory;

				if (!string.IsNullOrEmpty(CurrentDesignFilePath) && Directory.Exists(Path.GetDirectoryName(CurrentDesignFilePath)))
					directory = Path.GetDirectoryName(CurrentDesignFilePath);
				else
					directory = ModelInstanceManager.GetLatestDirectory();

				filename = !string.IsNullOrEmpty(filename) ? filename : caseName;
				string autosavepath = directory + "/" + Path.GetFileNameWithoutExtension(filename) + AutosaveSuffix + ".valosdb";

				string errorMessage;
				Epx.SDK.DataStorageFunctions.SaveAs(_currentModel, autosavepath, CurrentDesignFilePath, out errorMessage);
				bool result = string.IsNullOrEmpty(errorMessage);

				IsSaveOpenInProgress = false;
				SessionService.AutoServerCheckEnabled = true;
				if (!result)
				{
					string emsg = "Unable to autosave backup file.";
					emsg += Environment.NewLine;
					emsg += Environment.NewLine;
					emsg += errorMessage;
					Application.Current.Dispatcher.BeginInvoke(new Action(() => LogOpenSaveException(emsg, false, true)));
				}
			}
		}
		/// <summary>
		/// Deletes all autosave files.
		/// </summary>
		public void DisposeAutoSave()
		{
			if (!IsSaveOpenInProgress)
			{
				string directory;
				if (!string.IsNullOrEmpty(CurrentDesignFilePath) && Directory.Exists(CurrentDesignFilePath))
				{
					directory = Path.GetDirectoryName(CurrentDesignFilePath);
				}
				else
				{
					directory = ModelInstanceManager.GetLatestDirectory();
				}
				foreach (string file in Directory.EnumerateFiles(directory))
				{
					if (file.Contains(AutosaveSuffix))
						File.Delete(file);
				}
			}
		}

		/// <summary>
		/// Renames the old automatic save file.
		/// </summary>
		public static void RenameOldAutoSaveFile()
		{
			try // can prevent program from starting
			{
				string oldPath = ModelInstanceManager.GetLatestDirectory() + "\\dummy.valosdb";
				if (!string.IsNullOrEmpty(oldPath))
				{
					string directory = Path.GetDirectoryName(oldPath);
					foreach (string file in Directory.EnumerateFiles(directory))
					{
						if (file.Contains(_autosaveSuffix))
						{
							string newfilepath = file.Replace(_autosaveSuffix, _autoSaveRenamed);
							int i = 1;
							string prevSuffix = _autoSaveRenamed;
							while (File.Exists(newfilepath) && i < 100)
							{
								newfilepath = newfilepath.Replace(prevSuffix, _autoSaveRenamed + i.ToString());
								prevSuffix = _autoSaveRenamed + i.ToString();
								i++;
							}
							System.IO.File.Move(file, newfilepath);
							File.SetAttributes(newfilepath, FileAttributes.Normal);
						}
					}
				}
			}
			catch (Exception e)
			{
				string emessage = e.Message;
				if (e.InnerException != null) emessage += "\n" + e.InnerException.Message;
				if (e.InnerException != null && e.InnerException.InnerException != null) emessage += "\n" + e.InnerException.InnerException.Message;
				MessageBox.Show(CultureManager.GetLocalizedString("Unable to rename autosave files. Please check the previous working directory for autosaved design files.") + "\n\n" + emessage, "Autosave " + CultureManager.GetLocalizedString("Warning"), MessageBoxButton.OK, MessageBoxImage.Warning);
			}
		}
#endregion //AutoSave

		/// <summary>
		/// Logs the open save exception.
		/// </summary>
		/// <param name="errorMessage">The error message.</param>
		/// <param name="isOpen">if set to <c>true</c> [is open].</param>
		/// <param name="isAutoSave">if set to <c>true</c> [is automatic save].</param>
		public static void LogOpenSaveException(string errorMessage, bool isOpen, bool isAutoSave)
		{
			string message0;
			if (isAutoSave) message0 = CultureManager.GetLocalizedString("Autosave was unable to save the design file. To ensure design file integrity, undo changes until you are able to save the design file.");
			else message0 = isOpen ? CultureManager.GetLocalizedString("Unable to open the design file.") : CultureManager.GetLocalizedString("Unable to save the design file. To save your work, undo changes until you are able to save the design file.");
			string message1 = CultureManager.GetLocalizedString("To help solve the problem, please save the error to a file and send it to support.");
			string message2 = CultureManager.GetLocalizedString("Error log saved to");

			MessageBoxResult msgBoxRes = MessageBox.Show(message0 + " " + message1, CultureManager.GetLocalizedString("Open/Save Error"), MessageBoxButton.OKCancel, isAutoSave ? MessageBoxImage.Warning : MessageBoxImage.Error);

			if (msgBoxRes == MessageBoxResult.OK)
			{
				string filePath;
				Microsoft.Win32.SaveFileDialog fileDlg = new Microsoft.Win32.SaveFileDialog();
				fileDlg.Filter = "Log Files (*.log)|*.log";
				fileDlg.DefaultExt = "log";
				fileDlg.InitialDirectory = ModelInstanceManager.GetDefaultDirectory();
				fileDlg.FileName = "ErrorLog_" + string.Format("{0:yyyy-MM-dd_HH-mm-ss}", DateTime.Now) + "." + fileDlg.DefaultExt;

				if (fileDlg.ShowDialog() == true)
				{
					filePath = fileDlg.FileName;

					if (!string.IsNullOrEmpty(filePath))
					{
						System.IO.StreamWriter sw = new System.IO.StreamWriter(filePath);
						try
						{

						}
						catch
						{
							//deployment valid only when actually deployed.
						}
						sw.WriteLine("Error date: " + DateTime.Now.ToLocalTime());
						sw.WriteLine();
						sw.Write(errorMessage);
						sw.Close();
					}
					MessageBox.Show(message2 + ": " + filePath + "\n ", CultureManager.GetLocalizedString("Open/Save Error"), MessageBoxButton.OK, MessageBoxImage.Information);
				}
			}
		}

		/// <summary>
		/// Stores latest open/save path during execution of app and between executions.
		/// </summary>
		private string _currentDesignFilePath;
		/// <summary>
		/// Gets or sets the current design file path.
		/// </summary>
		/// <value>The current design file path.</value>
		public string CurrentDesignFilePath
		{
			get
			{
				return _currentDesignFilePath;
			}
			set
			{
				_currentDesignFilePath = value;
			}
		}

		/// <summary>
		/// Gets the name of the current design file path or database.
		/// </summary>
		/// <value>The name of the current design file path or database.</value>
		public string CurrentDesignFilePathOrDatabaseName
		{
			get { return !string.IsNullOrEmpty(_currentDesignFilePath) ? _currentDesignFilePath : _currentModel.Name; }
		}

		/// <summary>
		/// Gets the latest directory.
		/// </summary>
		/// <returns>System.String.</returns>
		public static string GetLatestDirectory()
		{
			string path = ModelInstanceManager.GetDefaultDirectory();
			return path;

		}

		/// <summary>
		/// Gets the default name of the directory.
		/// </summary>
		/// <value>The default name of the directory.</value>
		public virtual string DefaultDirectoryName
		{
			get { return "Valos Modeler"; }
		}

		/// <summary>
		/// Gets the default name of the sub directory.
		/// </summary>
		/// <value>The default name of the sub directory.</value>
		public virtual string DefaultSubDirectoryName
		{
			get { return null; }
		}

		/// <summary>
		/// Gets the default directory path (...Documents\Valos\ValosApp) and creates it if it does not exist.
		/// </summary>
		/// <param name="mainDirName">Name of the main dir.</param>
		/// <param name="subDirName">If specified will create a sub directory in the default directory.</param>
		/// <returns>Empty string if unable to create directory.</returns>
		public static string GetDefaultDirectory()
		{
			string mainDirName = Instance.DefaultDirectoryName;
			string subDirName = Instance.DefaultSubDirectoryName;
			string userDoc = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
			string defaultDir = System.IO.Path.Combine(userDoc, mainDirName);

			if (!string.IsNullOrEmpty(subDirName))
			{
				defaultDir = System.IO.Path.Combine(defaultDir, subDirName);
			}

			try
			{
				if (!System.IO.Directory.Exists(defaultDir))
				{
					System.IO.DirectoryInfo dirInfo = System.IO.Directory.CreateDirectory(defaultDir);
				}
			}
			catch
			{
				defaultDir = string.Empty;
			}

			return defaultDir;
		}

		/// <summary>
		/// Gets the open file path.
		/// </summary>
		/// <returns>System.String.</returns>
		public string GetOpenFilePath()
		{
			string filePath = null;
			string filename = !string.IsNullOrEmpty(CurrentDesignFilePath) ? Path.GetFileName(CurrentDesignFilePath) : string.Empty;
			string caseName = _currentModel.Name;
			Microsoft.Win32.OpenFileDialog fileDlg = new Microsoft.Win32.OpenFileDialog();
			fileDlg.Filter = "Valos Database Files (*.valosdb)|*.valosdb|All files (*.*)|*.*";
			fileDlg.DefaultExt = "valosdb";

			if (!string.IsNullOrEmpty(CurrentDesignFilePath) && Directory.Exists(CurrentDesignFilePath))
			{
				fileDlg.InitialDirectory = Path.GetDirectoryName(CurrentDesignFilePath);
			}
			else
			{
				fileDlg.InitialDirectory = ModelInstanceManager.GetLatestDirectory();
			}

			fileDlg.FileName = !string.IsNullOrEmpty(filename) ? filename : string.Empty;
			if (fileDlg.ShowDialog(Application.Current.MainWindow) == true)
			{
				filePath = fileDlg.FileName;
			}
			return filePath;
		}

		/// <summary>
		/// Gets the save file path.
		/// </summary>
		/// <returns>System.String.</returns>
		public string GetSaveFilePath(string initialFileName = null)
		{
			string filePath = null;
			string filename = !string.IsNullOrEmpty(CurrentDesignFilePath) ? Path.GetFileName(CurrentDesignFilePath) : string.Empty;
			string caseName = Path.GetFileNameWithoutExtension(_currentModel.Name);
			Microsoft.Win32.SaveFileDialog fileDlg = new Microsoft.Win32.SaveFileDialog();
			fileDlg.Filter = "Valos Database Files (*.valosdb)|*.valosdb"/*|All files (*.*)|*.*"*/;
			fileDlg.DefaultExt = "valosdb";
			fileDlg.AddExtension = true;

			if (!string.IsNullOrEmpty(CurrentDesignFilePath) && Directory.Exists(CurrentDesignFilePath))
				fileDlg.InitialDirectory = Path.GetDirectoryName(CurrentDesignFilePath);
			else
				fileDlg.InitialDirectory = ModelInstanceManager.GetLatestDirectory();

			fileDlg.FileName = !string.IsNullOrEmpty(initialFileName) ? initialFileName : !string.IsNullOrEmpty(filename) ? filename : caseName + "." + fileDlg.DefaultExt;

			if (fileDlg.ShowDialog(Application.Current.MainWindow) == true)
				filePath = fileDlg.FileName;

			return filePath;
		}


		/// <summary>
		/// Temporary, will be removed/cleared with major release. Check old projects for compatability with current data model structure to prevent program crashes
		/// with missing nodes etc..
		/// </summary>
		protected virtual void CheckBackwardCompatability()
		{

		}

#endregion

#region Class Types
		/// <summary>
		/// Create class by type
		/// </summary>
		/// <param name="typeId">The type identifier.</param>
		/// <param name="id">The identifier.</param>
		/// <returns>BaseDataNode.</returns>
		public static BaseDataNode CreateClass(string typeId, Guid id)
		{
			BaseDataNode retVal = null;
			Type typ = ModelInstanceManager.GetType(typeId);
			if (typ != null)
			{
				retVal = Activator.CreateInstance(typ) as BaseDataNode;
			}
			//if (retVal == null) retVal = new ProjectTreeNode();
			if (retVal != null) retVal.UniqueID = id;
			return retVal;
		}
		/// <summary>
		/// Creates the class.
		/// </summary>
		/// <param name="typeId">The type identifier.</param>
		/// <returns>BaseDataNode.</returns>
		public static BaseDataNode CreateClass(string typeId)
		{
			BaseDataNode retVal = null;
			Type typ = ModelInstanceManager.GetType(typeId);
			if (typ != null)
			{
				retVal = Activator.CreateInstance(typ) as BaseDataNode;
			}
			return retVal;
		}

		/// <summary>
		/// Searches all assemblies.
		/// </summary>
		/// <param name="typeName">Name of the type.</param>
		/// <returns>Type.</returns>
		public static Type GetType(string typeName)
		{
			foreach (System.Reflection.Assembly currentassembly in AppDomain.CurrentDomain.GetAssemblies())
			{
				Type t = currentassembly.GetType(typeName, false, true);
				if (t != null) { return t; }
			}
			return null;
		}
#endregion

#region Node operations

		/// <summary>
		/// The _remove node command
		/// </summary>
		RelayCommand _removeNodeCommand;
		/// <summary>
		/// The _add folder command
		/// </summary>
		RelayCommand _addFolderCommand;
		/// <summary>
		/// The _cut node command
		/// </summary>
		RelayCommand _cutNodeCommand;
		/// <summary>
		/// The _copy node command
		/// </summary>
		RelayCommand _copyNodeCommand;
		/// <summary>
		/// The _paste node command
		/// </summary>
		RelayCommand _pasteNodeCommand;
		/// <summary>
		/// The _cut node list
		/// </summary>
		List<BaseDataNode> _cutNodeList = new List<BaseDataNode>();
		/// <summary>
		/// The _copy node list
		/// </summary>
		List<BaseDataNode> _copyNodeList = new List<BaseDataNode>();
		/// <summary>
		/// Gets the cut node list.
		/// </summary>
		/// <value>The cut node list.</value>
		public List<BaseDataNode> CutNodeList
		{
			get { return _cutNodeList; }
		}
		/// <summary>
		/// Gets the copy node list.
		/// </summary>
		/// <value>The copy node list.</value>
		public List<BaseDataNode> CopyNodeList
		{
			get { return _copyNodeList; }
		}

		/// <summary>
		/// Gets the remove node command.
		/// </summary>
		/// <value>The remove node command.</value>
		public ICommand RemoveNodeCommand
		{
			get
			{
				if (_removeNodeCommand == null)
					_removeNodeCommand = new RelayCommand(execute => this.ExecuteRemoveNode(execute), canexecute => this.CanExecuteRemoveNode(canexecute));

				return _removeNodeCommand;
			}
		}
		/// <summary>
		/// Determines whether this instance [can execute remove node] the specified parameter.
		/// </summary>
		/// <param name="param">The parameter.</param>
		/// <returns><c>true</c> if this instance [can execute remove node] the specified parameter; otherwise, <c>false</c>.</returns>
		private bool CanExecuteRemoveNode(object param)
		{
			BaseDataNode node = param as BaseDataNode;
			if (node != null && !node.CanRemove())
				return false;
			else if (node != null && !node.IsEditable)
				return false;
			else if (node != null && node is Project)
				return true;
			else if (node != null && node is UnloadedNode)
				return false;

			else if (node != null && (_cutNodeList.Count() > 0 || _copyNodeList.Count > 0))
			{
				bool retVal = true;
				foreach (BaseDataNode cutcopynode in _cutNodeList)
				{
					if (cutcopynode == node)
						retVal = false;
				}
				foreach (BaseDataNode cutcopynode in _copyNodeList)
				{
					if (cutcopynode == node)
						retVal = false;
				}
				return retVal;
			}
			else
				return true;
		}
		/// <summary>
		/// Executes the remove node.
		/// </summary>
		/// <param name="param">The parameter.</param>
		private void ExecuteRemoveNode(object param)
		{
			List<BaseDataNode> nodes = param as List<BaseDataNode>;
			if (nodes != null)
			{
				if (nodes != null && nodes.All(n => n.Parent != null))
				{
					ResetCutCopyStatus();

					//BaseDataNode parentNode = node.Parent;
					List<BaseDataNode> selectedNodes = new List<BaseDataNode>(nodes);

					List<BaseDataNode> otherNodesToRemove = new List<BaseDataNode>();
					PreviewRemoveNode(selectedNodes, ref otherNodesToRemove);

					CallMethodAction(
					delegate
					{
						foreach (var n in selectedNodes)
						{
							n.Parent.RemoveChild(n);
						}
						foreach (var n in otherNodesToRemove)
						{
							n.Parent.RemoveChild(n);
						}

						//if (node.Parent.RemoveChild(node))
						{
							var allNodes = new List<BaseDataNode>();
							allNodes.AddRange(selectedNodes);
							allNodes.AddRange(otherNodesToRemove);
							SaveRequired = true;
							Infrastructure.Events.ModelChanged.PublishNodeRemoved(allNodes, this);
							Infrastructure.Events.NodeSelected.Clear(this);
							Infrastructure.Events.ShowRibbonContextualTabs.Publish(null);
						}
					},
					delegate // undo
					{
						foreach (var n in selectedNodes)
						{
							n.Parent.AddChild(n);
						}
						foreach (var n in otherNodesToRemove)
						{
							n.Parent.AddChild(n);
						}

						//if (parentNode.AddChild(node))
						{
							var allNodes = new List<BaseDataNode>();
							allNodes.AddRange(selectedNodes);
							allNodes.AddRange(otherNodesToRemove);
							Infrastructure.Events.ModelChanged.PublishNodeAdded(allNodes, this);
							Infrastructure.Events.NodeSelected.Publish(selectedNodes, this);
							Infrastructure.Events.ShowRibbonContextualTabs.Publish(null);
						}
					}, selectedNodes, "Remove node", "Remove node");
				}
			}
		}
		/// <summary>
		/// Previews the remove node.
		/// </summary>
		/// <param name="nodes">The nodes.</param>
		/// <param name="otherNodesToRemove">The other nodes to remove.</param>
		protected virtual void PreviewRemoveNode(List<BaseDataNode> nodes, ref List<BaseDataNode> otherNodesToRemove)
		{
		}

#region Add Folder

		/// <summary>
		/// Gets the add folder command.
		/// </summary>
		/// <value>The add folder command.</value>
		public ICommand AddFolderCommand
		{
			get
			{
				if (_addFolderCommand == null)
					_addFolderCommand = new RelayCommand(execute => this.ExecuteAddFolder(execute), canexecute => this.CanExecuteAddFolder(canexecute));

				return _addFolderCommand;
			}
		}
		/// <summary>
		/// Determines whether this instance [can execute add folder] the specified parameter.
		/// </summary>
		/// <param name="param">The parameter.</param>
		/// <returns><c>true</c> if this instance [can execute add folder] the specified parameter; otherwise, <c>false</c>.</returns>
		private bool CanExecuteAddFolder(object param)
		{
			BaseDataNode node = param as BaseDataNode;
			if (node != null)
			{
				if ((node is Project ||
					(node is ModelBaseNode) ||
					(node is FolderNode && node.HasParent<Project>()))
					&& node.IsEditable)
				{
					if (node is ModelBaseNode && node.IsOriginalType && (node as ModelBaseNode).CanBeTarget)
						return true;
					else 
						return true;
				}
			}
			return false;
		}
		/// <summary>
		/// Executes the add folder.
		/// </summary>
		/// <param name="param">The parameter.</param>
		private void ExecuteAddFolder(object param)
		{
			BaseDataNode node = param as BaseDataNode;
			if (node != null && node != null)
			{
				BaseDataNode nodeToBeAdded = null;
				if (node is ModelBaseNode || node is Project || node.HasParent<Project>()) nodeToBeAdded = new ModelFolderNode();
				else nodeToBeAdded = new FolderNode();

				ResetCutCopyStatus();
				ExecuteAddNode(node, nodeToBeAdded);
			}
		}

#endregion

#region Add Node

		/// <summary>
		/// Determines whether this instance [can execute add node] the specified parent.
		/// </summary>
		/// <param name="parent">The parent.</param>
		/// <param name="child">The child.</param>
		/// <returns><c>true</c> if this instance [can execute add node] the specified parent; otherwise, <c>false</c>.</returns>
		public bool CanExecuteAddNode(BaseDataNode parent, BaseDataNode child)
		{
			if (child != null && parent != null && parent.IsEditable)
			{
				return child.IsValidParent(parent);
			}
			else
			{
				return false;
			}
		}

		/// <summary>
		/// Executes the add node.
		/// </summary>
		/// <param name="parent">The parent.</param>
		/// <param name="child">The child.</param>
		public void ExecuteAddNode(BaseDataNode parent, BaseDataNode child)
		{
			CallMethodAction(
				delegate
				{
					if (parent.AddChild(child))
					{
						SaveRequired = true;
						Infrastructure.Events.ModelChanged.PublishNodeAdded(child, this);
					}
				},
				delegate // undo
				{
					if (child.Parent != null && child.Parent.RemoveChild(child))
					{
						Infrastructure.Events.ModelChanged.PublishNodeRemoved(child, this);
					}
				}, child, "Add Node", "Add Node");
		}

#endregion // Add Node

#region AddProjectCommand
		RelayCommand _addProjectCommand;
		public ICommand AddProjectCommand
		{
			get
			{
				if (_addProjectCommand == null)
					_addProjectCommand = new RelayCommand(execute => this.ExecuteAddProject(execute), canexecute => this.CanExecuteAddProject(canexecute));
				return _addProjectCommand;
			}
		}

		private bool CanExecuteAddProject(object parameter)
		{
			BaseDataNode node = parameter as BaseDataNode;
			if (node != null)
			{
				if ((node is DataModel ||
					(node is FolderNode) )
					&& node.IsEditable)
				{
					return true;
				}
			}
			return false;
		}

		private void ExecuteAddProject(object parameter)
		{
			BaseDataNode node = parameter as BaseDataNode;
			if (node != null)
			{
				var nodeToBeAdded = new Project();

				ResetCutCopyStatus();
				ExecuteAddNode(node, nodeToBeAdded);
			}
		}
#endregion //AddProjectCommand

		/// <summary>
		/// Gets the cut node command.
		/// </summary>
		/// <value>The cut node command.</value>
		public ICommand CutNodeCommand
		{
			get
			{
				if (_cutNodeCommand == null)
					_cutNodeCommand = new RelayCommand(execute => this.ExecuteCutNode(execute), canexecute => this.CanExecuteCutNode(canexecute));

				return _cutNodeCommand;
			}
		}
		/// <summary>
		/// Determines whether this instance [can execute cut node] the specified parameter.
		/// </summary>
		/// <param name="param">The parameter.</param>
		/// <returns><c>true</c> if this instance [can execute cut node] the specified parameter; otherwise, <c>false</c>.</returns>
		private bool CanExecuteCutNode(object param)
		{
			List<BaseDataNode> nodes = param as List<BaseDataNode>;
			bool returnValue = false;
			if (nodes != null)
			{
				if (nodes.Count() > 0)
				{
					returnValue = true;
				}
				BaseDataNode target = param as BaseDataNode;
				if (target != null && target != null && target.IsEditable)
				{
					foreach (BaseDataNode node in nodes)
					{
						returnValue = returnValue && node.CanCopy() && node.IsEditable;
					}
				}
				else
				{
					returnValue = false;
				}
			}
			return returnValue;
		}
		/// <summary>
		/// Executes the cut node.
		/// </summary>
		/// <param name="param">The parameter.</param>
		private void ExecuteCutNode(object param)
		{
			List<BaseDataNode> nodes = param as List<BaseDataNode>;
			ResetCutCopyStatus();
			foreach (BaseDataNode node in nodes)
			{
				if (node != null)
				{
					_cutNodeList.Add(node);
				}
			}
		}

		/// <summary>
		/// Gets the copy node command.
		/// </summary>
		/// <value>The copy node command.</value>
		public ICommand CopyNodeCommand
		{
			get
			{
				if (_copyNodeCommand == null)
					_copyNodeCommand = new RelayCommand(execute => this.ExecuteCopyNode(execute), canexecute => this.CanExecuteCopyNode(canexecute));

				return _copyNodeCommand;
			}
		}
		/// <summary>
		/// Determines whether this instance [can execute copy node] the specified parameter.
		/// </summary>
		/// <param name="param">The parameter.</param>
		/// <returns><c>true</c> if this instance [can execute copy node] the specified parameter; otherwise, <c>false</c>.</returns>
		private bool CanExecuteCopyNode(object param)
		{
			List<BaseDataNode> nodes = param as List<BaseDataNode>;
			bool returnValue = false;
			if (nodes != null)
			{
				if (nodes.Count() > 0)
				{
					returnValue = true;
				}
				BaseDataNode target = param as BaseDataNode;
				if (target != null)
				{
					foreach (BaseDataNode node in nodes)
					{
						returnValue = returnValue && node.CanCopy();
					}
				}
				else
				{
					returnValue = false;
				}
			}
			return returnValue;
		}
		/// <summary>
		/// Executes the copy node.
		/// </summary>
		/// <param name="param">The parameter.</param>
		private void ExecuteCopyNode(object param)
		{
			List<BaseDataNode> nodes = param as List<BaseDataNode>;
			ResetCutCopyStatus();
			foreach (BaseDataNode node in nodes)
			{
				if (node != null)
				{
					_copyNodeList.Add(node);
				}
			}
		}

		/// <summary>
		/// Gets the paste node command.
		/// </summary>
		/// <value>The paste node command.</value>
		public ICommand PasteNodeCommand
		{
			get
			{
				if (_pasteNodeCommand == null)
					_pasteNodeCommand = new RelayCommand(execute => this.ExecutePasteNode(execute), canexecute => this.CanExecutePasteNode(canexecute));

				return _pasteNodeCommand;
			}
		}
		/// <summary>
		/// Determines whether this instance [can execute paste node] the specified parameter.
		/// </summary>
		/// <param name="param">The parameter.</param>
		/// <returns><c>true</c> if this instance [can execute paste node] the specified parameter; otherwise, <c>false</c>.</returns>
		private bool CanExecutePasteNode(object param)
		{
			bool retval = false;
			BaseDataNode node = param as BaseDataNode;
			if (node != null && (_copyNodeList.Count > 0 || _cutNodeList.Count > 0) && node.IsEditable)
			{
				retval = true;
				foreach (BaseDataNode cutNode in _cutNodeList)
				{
					retval = retval && node.CanPasteNode(cutNode) && node != cutNode.Parent;
					//if (retval && (cutNode is BIM.ProjectData.Drawings.DrawingPage || cutNode is BIM.ProjectData.Drawings.DrawingBaseBlock))
					//{
					//	if (cutNode.HasParent<TrussFrame>() && !node.HasParent<TrussFrame>()) retval = false; // prevent truss drawing blocks etc from being pasted into building drawings.
					//	else if (!cutNode.HasParent<TrussFrame>() && node.HasParent<TrussFrame>()) retval = false; // prevent building drawing blocks etc from being pasted into truss drawings.
					//}
				}
				foreach (BaseDataNode cutNode in _copyNodeList)
				{
					retval = retval && node.CanPasteNode(cutNode) && node != cutNode.Parent;
					//if (retval && (cutNode is BIM.ProjectData.Drawings.DrawingPage || cutNode is BIM.ProjectData.Drawings.DrawingBaseBlock))
					//{
					//	if (cutNode.HasParent<TrussFrame>() && !node.HasParent<TrussFrame>()) retval = false; // prevent truss drawing blocks etc from being pasted into building drawings.
					//	else if (!cutNode.HasParent<TrussFrame>() && node.HasParent<TrussFrame>()) retval = false; // prevent building drawing blocks etc from being pasted into truss drawings.
					//}
				}
			}
			return retval;
		}
		/// <summary>
		/// Executes the paste node.
		/// </summary>
		/// <param name="param">The parameter.</param>
		private void ExecutePasteNode(object param)
		{
			BaseDataNode node = param as BaseDataNode;
			if (node != null)
			{
				if (node != null)
				{
					BaseDataNode dataNode = node;
					List<BaseDataNode> copiedNodes = new List<BaseDataNode>();

					//Key = cuttedNode, Value = node old parent
					Dictionary<BaseDataNode, BaseDataNode> cuttedNodes = new Dictionary<BaseDataNode, BaseDataNode>();
					List<BaseDataNode> toReplacedNodes = new List<BaseDataNode>();

					foreach (BaseDataNode copyNode in _copyNodeList)
					{
						bool add = true;

						if (copyNode is DrawingsGroupNode)
						{
							if (node.HasDrawingSupport && node.HasDescendant<DrawingsGroupNode>(true))
							{
								MessageBoxResult resultMB = MessageBox.Show(CultureManager.GetLocalizedString("Drawings group node already exists. Do you want to replace it?"), "", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
								if (resultMB == MessageBoxResult.No || resultMB == MessageBoxResult.Cancel)
								{
									add = false;
								}
							}
						}
						else
						{
							bool replace;
							add = PreviewPasteNode(false, copyNode, out replace);
							if (replace) toReplacedNodes.Add(copyNode);
						}

						if (add)
						{
							copiedNodes.Add(Epx.BIM.CopyMoveTools.CopyMoveTool.CopyTree(copyNode));
						}
					}
					foreach (BaseDataNode cutNode in _cutNodeList)
					{
						bool add = true;
						if (cutNode is DrawingsGroupNode)
						{
							if (node.HasDrawingSupport && node.HasDescendant<DrawingsGroupNode>(true))
							{
								MessageBoxResult resultMB = MessageBox.Show(CultureManager.GetLocalizedString("Drawings group node already exists. Do you want to replace it?"), "", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
								if (resultMB == MessageBoxResult.No || resultMB == MessageBoxResult.Cancel)
								{
									add = false;
								}
							}
						}
						else
						{
							bool replace;
							add = PreviewPasteNode(true, cutNode, out replace);
							if (replace) toReplacedNodes.Add(cutNode);
						}

						if (add)
						{
							cuttedNodes.Add(cutNode, cutNode.Parent);
						}
					}

					ResetCutCopyStatus();//Clears _copyNodeList and _cutNodeList

					List<BaseDataNode> replacedNodes = new List<BaseDataNode>();
					foreach (BaseDataNode cutNode in cuttedNodes.Keys)
					{
						if (cutNode is DrawingsGroupNode)
						{
							DrawingsGroupNode replaced = node.GetChildNode<DrawingsGroupNode>();
							if (replaced != null)
							{
								replacedNodes.Add(replaced);
							}
						}
						else if (toReplacedNodes.Contains(cutNode))
						{
							BaseDataNode replaced = node.Children.Where(c => c.GetType() == cutNode.GetType()).FirstOrDefault();
							if (replaced != null)
							{
								replacedNodes.Add(replaced);
							}
						}
					}
					foreach (BaseDataNode copiedNode in copiedNodes)
					{
						if (copiedNode is DrawingsGroupNode)
						{
							DrawingsGroupNode replaced = node.GetChildNode<DrawingsGroupNode>();
							if (replaced != null)
							{
								replacedNodes.Add(replaced);
							}
						}
						else if (toReplacedNodes.Contains(copiedNode))
						{
							BaseDataNode replaced = node.Children.Where(c => c.GetType() == copiedNode.GetType()).FirstOrDefault();
							if (replaced != null)
							{
								replacedNodes.Add(replaced);
							}
						}
					}

					CallMethodAction(
						delegate // do
						{
							List<Drawing> copiedCuttedDrawings = cuttedNodes.Keys.Where(n => n is DrawingPage || n is DrawingBaseBlock).Select(n => n.GetParent<Drawing>()).ToList();// get old drawings
							foreach (BaseDataNode replaced in replacedNodes)
							{
								dataNode.RemoveChild(replaced);
							}
							foreach (BaseDataNode cutNode in cuttedNodes.Keys)
							{
								if (cutNode != null)
								{
									if (dataNode.AddChild(cutNode))
										cuttedNodes[cutNode].RemoveChild(cutNode);
								}
							}
							foreach (BaseDataNode copiedNode in copiedNodes)
							{
								dataNode.AddChild(copiedNode);
							}

							copiedCuttedDrawings.AddRange(cuttedNodes.Keys.Where(n => n is DrawingPage || n is DrawingBaseBlock).Select(n => n.GetParent<Drawing>()));//new drawings
							copiedCuttedDrawings.AddRange(copiedNodes.Where(n => n is DrawingPage || n is DrawingBaseBlock).Select(n => n.GetParent<Drawing>()));
							copiedCuttedDrawings = copiedCuttedDrawings.Distinct().ToList();
							copiedCuttedDrawings.RemoveAll(d => d == null);
							foreach (Drawing d in copiedCuttedDrawings)
							{
								//Mediator.NotifyColleagues<Drawing>(MediatorMessages.UpdateDrawingView, d);//cannot be async, handled by NodeAdded/Removed
							}

							SaveRequired = true;
							var addedNodes = new List<BaseDataNode>();
							addedNodes.AddRange(cuttedNodes.Keys);
							addedNodes.AddRange(copiedNodes);
							var removedNodes = new List<BaseDataNode>(addedNodes);
							removedNodes.AddRange(replacedNodes);
							Infrastructure.Events.ModelChanged.PublishNodeRemoved(removedNodes, this);
							Infrastructure.Events.ModelChanged.PublishNodeAdded(addedNodes, this);
							Infrastructure.Events.ModelChanged.Publish(this);
						},
						delegate // undo
						{
							List<Drawing> copiedCuttedDrawings = cuttedNodes.Keys.Where(n => n is DrawingPage || n is DrawingBaseBlock).Select(n => n.GetParent<Drawing>()).ToList();// get old drawings
							foreach (BaseDataNode cutNode in cuttedNodes.Keys)
							{
								if (cutNode != null)
								{
									if (cuttedNodes[cutNode].AddChild(cutNode))
										dataNode.RemoveChild(cutNode);
								}
							}

							foreach (BaseDataNode copiedNode in copiedNodes)
							{
								dataNode.RemoveChild(copiedNode);
							}
							foreach (BaseDataNode replaced in replacedNodes)
							{
								dataNode.AddChild(replaced);
							}

							copiedCuttedDrawings.AddRange(cuttedNodes.Keys.Where(n => n is DrawingPage || n is DrawingBaseBlock).Select(n => n.GetParent<Drawing>()));//new drawings
							copiedCuttedDrawings.AddRange(copiedNodes.Where(n => n is DrawingPage || n is DrawingBaseBlock).Select(n => n.GetParent<Drawing>()));
							copiedCuttedDrawings = copiedCuttedDrawings.Distinct().ToList();
							copiedCuttedDrawings.RemoveAll(d => d == null);
							foreach (Drawing d in copiedCuttedDrawings)
							{
								//Mediator.NotifyColleagues<Drawing>(MediatorMessages.UpdateDrawingView, d);//cannot be async
							}

							SaveRequired = true;
							var addedNodes = new List<BaseDataNode>();
							addedNodes.AddRange(cuttedNodes.Keys);
							addedNodes.AddRange(copiedNodes);
							var removedNodes = new List<BaseDataNode>(addedNodes);
							removedNodes.AddRange(replacedNodes);
							Infrastructure.Events.ModelChanged.PublishNodeRemoved(addedNodes, this);
							Infrastructure.Events.ModelChanged.PublishNodeAdded(removedNodes, this);
							Infrastructure.Events.ModelChanged.Publish(this);
						}, dataNode, "Paste node", "Paste node");
				}
			}
		}
		/// <summary>
		/// Ask if the node needs to be replaced etc.
		/// </summary>
		/// <param name="isCut">else is copy</param>
		/// <param name="node">The node.</param>
		/// <param name="replaceNode">If true the child of the same type will be replaced.</param>
		/// <returns>True if can paste.</returns>
		protected virtual bool PreviewPasteNode(bool isCut, BaseDataNode node, out bool replaceNode)
		{
			replaceNode = false;
			return true;
		}
		/// <summary>
		/// Sets _copyNode etc to null.
		/// </summary>
		private void ResetCutCopyStatus()
		{
			_cutNodeList.Clear();
			_copyNodeList.Clear();
		}

		/// <summary>
		/// The _set target folder command
		/// </summary>
		RelayCommand _setTargetFolderCommand;
		/// <summary>
		/// Gets the set target folder command.
		/// </summary>
		/// <value>The set target folder command.</value>
		public ICommand SetTargetFolderCommand
		{
			get
			{
				if (_setTargetFolderCommand == null)
					_setTargetFolderCommand = new RelayCommand(execute => this.ExecuteSetTargetFolder(execute), canexecute => this.CanExecuteSetTargetFolder(canexecute));

				return _setTargetFolderCommand;
			}
		}
		/// <summary>
		/// Determines whether this instance [can execute set target folder] the specified parameter.
		/// </summary>
		/// <param name="param">The parameter.</param>
		/// <returns><c>true</c> if this instance [can execute set target folder] the specified parameter; otherwise, <c>false</c>.</returns>
		private bool CanExecuteSetTargetFolder(object param)
		{
			BaseDataNode item = param as BaseDataNode;
			if (item != null)
			{
				if (item is ModelBaseNode && (item as ModelBaseNode).IsOriginalType && (item as ModelBaseNode).CanBeTarget && !(item as ModelBaseNode).IsTarget)
					return true;
				else if (item is ModelBaseNode && !(item as ModelBaseNode).IsTarget)
					return true;
			}
			return false;
		}
		/// <summary>
		/// Executes the set target folder.
		/// </summary>
		/// <param name="param">The parameter.</param>
		private void ExecuteSetTargetFolder(object param)
		{
			BaseDataNode item = param as BaseDataNode;
			if (item is ModelBaseNode mbn)
			{
				mbn.IsTarget = true; // no need to undo, not a data operation
				Infrastructure.Events.NodePropertyChanged.Publish(item as ModelBaseNode, nameof(mbn.IsTarget), this);
			}
		}

#region SetCurrentGridCommand
		/// <summary>
		/// The _set current grid command
		/// </summary>
		RelayCommand _setCurrentGridCommand;
		/// <summary>
		/// Gets the set current grid command.
		/// </summary>
		/// <value>The set current grid command.</value>
		public ICommand SetCurrentGridCommand
		{
			get
			{
				if (_setCurrentGridCommand == null)
					_setCurrentGridCommand = new RelayCommand(execute => this.ExecuteSetCurrentGrid(execute), canexecute => this.CanExecuteSetCurrenGrid(canexecute));
				return _setCurrentGridCommand;
			}
		}

		/// <summary>
		/// Determines whether this instance [can execute set curren grid] the specified parameter.
		/// </summary>
		/// <param name="parameter">The parameter.</param>
		/// <returns><c>true</c> if this instance [can execute set curren grid] the specified parameter; otherwise, <c>false</c>.</returns>
		private bool CanExecuteSetCurrenGrid(object parameter)
		{
			//TEMPORARY CODE
			//return true;
			//
			BaseDataNode item = parameter as BaseDataNode;
			if (item != null)
			{
				Epx.BIM.GridMesh.GridMesh node = item as Epx.BIM.GridMesh.GridMesh;
				return node != null ? !node.IsCurrentGrid : false;
			}
			else
				return false;
		}

		/// <summary>
		/// Executes the set current grid.
		/// </summary>
		/// <param name="parameter">The parameter.</param>
		private void ExecuteSetCurrentGrid(object parameter)
		{
			BaseDataNode item = parameter as BaseDataNode;
			var currentGrid = item as Epx.BIM.GridMesh.GridMesh;
			Epx.BIM.GridMesh.GridMesh oldCurrent = null;
			var gridList = CurrentModel.GetDescendantNodes<Epx.BIM.GridMesh.GridMesh>().Where(g=>g.IsCurrentGrid);
			foreach (var grid in gridList)
			{
				if (grid != currentGrid)
					oldCurrent = grid;
				grid.IsCurrentGrid = false;
			}
			CallMethodAction(
			delegate // do
			{
				currentGrid.IsCurrentGrid = true;
				//if (oldCurrent != null)//oldCurrent == null if value == false
				//{
				//	oldCurrent.IsCurrentGrid = false;
				//	Infrastructure.Events.NodePropertyChanged.Publish(oldCurrent, nameof(oldCurrent.IsCurrentGrid), this);
				//}
				Infrastructure.Events.Update3D.Publish(currentGrid);
				Infrastructure.Events.NodePropertyChanged.Publish(currentGrid, nameof(currentGrid.IsCurrentGrid), this);
			},
			delegate // undo
			{
				if (oldCurrent != null)
				{
					currentGrid.IsCurrentGrid = false;
					oldCurrent.IsCurrentGrid = true;
					Infrastructure.Events.Update3D.Publish(oldCurrent);
					Infrastructure.Events.NodePropertyChanged.Publish(oldCurrent, nameof(oldCurrent.IsCurrentGrid), this);
					//Infrastructure.Events.NodePropertyChanged.Publish(currentGrid, nameof(currentGrid.IsCurrentGrid), this);
				}
			}, currentGrid, "IsCurrent", "Is Current Grid");
		}
#endregion //SetCurrenGridCommand

#endregion // Node operations

#region Node service operations

#region LoadTreeCommand
		/// <summary>
		/// The _load tree command
		/// </summary>
		RelayCommand _loadTreeCommand;
		/// <summary>
		/// Gets the load tree command.
		/// </summary>
		/// <value>The load tree command.</value>
		public ICommand LoadTreeCommand
		{
			get
			{
				if (_loadTreeCommand == null)
					_loadTreeCommand = new RelayCommand(execute => this.ExecuteLoadTree(execute), canexecute => this.CanExecuteLoadTree(canexecute));
				return _loadTreeCommand;
			}
		}

		/// <summary>
		/// Determines whether this instance [can execute load tree] the specified parameter.
		/// </summary>
		/// <param name="parameter">The parameter.</param>
		/// <returns><c>true</c> if this instance [can execute load tree] the specified parameter; otherwise, <c>false</c>.</returns>
		private bool CanExecuteLoadTree(object parameter)
		{
			BaseDataNode item = parameter as BaseDataNode;
			string filePath = CurrentDesignFilePath;
			if (item != null && !string.IsNullOrEmpty(filePath))
			{
				return LoadSave.IsLoadValid(item);
			}
			else
			{
				return false;
			}
		}

		/// <summary>
		/// Executes the load tree.
		/// </summary>
		/// <param name="parameter">The parameter.</param>
		private void ExecuteLoadTree(object parameter)
		{
			BaseDataNode item = parameter as BaseDataNode;
			string filePath = CurrentDesignFilePath;
			if (item != null && !string.IsNullOrEmpty(filePath))
			{
				BaseDataNode node = item;
				Mouse.OverrideCursor = Cursors.Wait;
				ServerServiceError sse = LoadSave.LoadTree(filePath, ref node);

				Mouse.OverrideCursor = null;
				OnTreeLoaded(node, sse);
			}
		}

		/// <summary>
		/// Called when [tree loaded].
		/// </summary>
		/// <param name="node">The node.</param>
		/// <param name="error">The error.</param>
		protected virtual void OnTreeLoaded(BaseDataNode node, ServerServiceError error)
		{
			if (error == null)
			{
				SaveRequired = true;
				Infrastructure.Events.NodePropertyChanged.Publish(node, nameof(node.IsDataLoaded), this);
				Infrastructure.Events.Update3D.Publish(new List<BaseDataNode>(1) { null });
			}
			else
			{
				MessageBox.Show(error.ToString() + ": " + error.ErrorType.ToString() + (error.OtherException != null ? (" (" + error.OtherException.ToString() + ")") : ""));
			}
		}
#endregion //LoadTreeCommand

#region UnloadTreeCommand
		/// <summary>
		/// The _unload tree command
		/// </summary>
		RelayCommand _unloadTreeCommand;
		/// <summary>
		/// Gets the unload tree command.
		/// </summary>
		/// <value>The unload tree command.</value>
		public ICommand UnloadTreeCommand
		{
			get
			{
				if (_unloadTreeCommand == null)
					_unloadTreeCommand = new RelayCommand(execute => this.ExecuteUnloadTree(execute), canexecute => this.CanExecuteUnloadTree(canexecute));
				return _unloadTreeCommand;
			}
		}

		/// <summary>
		/// Determines whether this instance [can execute unload tree] the specified parameter.
		/// </summary>
		/// <param name="parameter">The parameter.</param>
		/// <returns><c>true</c> if this instance [can execute unload tree] the specified parameter; otherwise, <c>false</c>.</returns>
		private bool CanExecuteUnloadTree(object parameter)
		{
			BaseDataNode item = parameter as BaseDataNode;
			string filePath = CurrentDesignFilePath;

			if (item != null && !string.IsNullOrEmpty(filePath))
			{
				return LoadSave.IsUnloadValid(item);
			}
			else
			{
				return false;
			}
		}

		/// <summary>
		/// Executes the unload tree.
		/// </summary>
		/// <param name="parameter">The parameter.</param>
		private void ExecuteUnloadTree(object parameter)
		{
			BaseDataNode item = parameter as BaseDataNode;
			string filePath = CurrentDesignFilePath;
			if (item != null && !string.IsNullOrEmpty(filePath))
			{
				System.Windows.MessageBoxResult result = System.Windows.MessageBox.Show(CultureManager.GetLocalizedString("Do you want to save node changes?"),
					CultureManager.GetLocalizedString("Project Explorer"), System.Windows.MessageBoxButton.YesNoCancel, System.Windows.MessageBoxImage.Question);
				if (result == System.Windows.MessageBoxResult.Cancel)
				{
					return;
				}
				else
				{
					bool bSave = (result != System.Windows.MessageBoxResult.No);
					var error = LoadSave.UnloadTree(filePath, item, bSave);
					Mouse.OverrideCursor = null;
					OnTreeUnLoaded(item, error);
				}
			}
		}
		/// <summary>
		/// Called when [tree un loaded].
		/// </summary>
		/// <param name="node">The node.</param>
		/// <param name="error">The error.</param>
		protected virtual void OnTreeUnLoaded(BaseDataNode node, ServerServiceError error)
		{
			if (error == null)
			{
				SaveRequired = true;
				Infrastructure.Events.NodePropertyChanged.Publish(node, nameof(node.IsDataLoaded), this);
			}
			else
			{
				MessageBox.Show(error.ToString() + ": " + error.ErrorType.ToString() + (error.OtherException != null ? (" (" + error.OtherException.ToString() + ")") : ""));
			}
		}
#endregion //UnloadTreeCommand

#region CheckOutCommand
		/// <summary>
		/// The _check out command
		/// </summary>
		RelayCommand _checkOutCommand;
		/// <summary>
		/// Gets the check out command.
		/// </summary>
		/// <value>The check out command.</value>
		public ICommand CheckOutCommand
		{
			get
			{
				if (_checkOutCommand == null)
					_checkOutCommand = new RelayCommand(execute => this.ExecuteCheckOut(execute), canexecute => this.CanExecuteCheckOut(canexecute));
				return _checkOutCommand;
			}
		}

		/// <summary>
		/// Determines whether this instance [can execute check out] the specified parameter.
		/// </summary>
		/// <param name="parameter">The parameter.</param>
		/// <returns><c>true</c> if this instance [can execute check out] the specified parameter; otherwise, <c>false</c>.</returns>
		private bool CanExecuteCheckOut(object parameter)
		{
			BaseDataNode item = parameter as BaseDataNode;
			string filePath = CurrentDesignFilePath;
			if (item != null && !string.IsNullOrEmpty(filePath))
			{
				return Reservation.IsCheckOutValid(item);
			}
			else
			{
				return false;
			}
		}

		/// <summary>
		/// Executes the check out.
		/// </summary>
		/// <param name="parameter">The parameter.</param>
		private void ExecuteCheckOut(object parameter)
		{
			BaseDataNode item = parameter as BaseDataNode;
			string filePath = CurrentDesignFilePath;
			if (item != null && !string.IsNullOrEmpty(filePath))
			{
				BaseDataNode node = item;
				Mouse.OverrideCursor = Cursors.Wait;
				ServerServiceError sse = Reservation.CheckOut(filePath, node);
				Mouse.OverrideCursor = null;
				OnCheckOutExecuted(node, sse);
			}
		}
		/// <summary>
		/// Called when [check out executed].
		/// </summary>
		/// <param name="node">The node.</param>
		/// <param name="error">The error.</param>
		protected virtual void OnCheckOutExecuted(BaseDataNode node, ServerServiceError error)
		{
			if (error == null)
			{
				Infrastructure.Events.NodePropertyChanged.Publish(node, nameof(node.IsDataLoaded), this);
			}
			else
			{
				MessageBox.Show(error.ToString() + ": " + error.ErrorType.ToString() + (error.OtherException != null ? (" (" + error.OtherException.ToString() + ")") : ""));
			}
		}
#endregion //CheckOutCommand

#region CheckInCommand
		/// <summary>
		/// The _check in command
		/// </summary>
		RelayCommand _checkInCommand;
		/// <summary>
		/// Gets the check in command.
		/// </summary>
		/// <value>The check in command.</value>
		public ICommand CheckInCommand
		{
			get
			{
				if (_checkInCommand == null)
					_checkInCommand = new RelayCommand(execute => this.ExecuteCheckIn(execute), canexecute => this.CanExecuteCheckIn(canexecute));
				return _checkInCommand;
			}
		}
		/// <summary>
		/// Determines whether this instance [can execute check in] the specified parameter.
		/// </summary>
		/// <param name="parameter">The parameter.</param>
		/// <returns><c>true</c> if this instance [can execute check in] the specified parameter; otherwise, <c>false</c>.</returns>
		private bool CanExecuteCheckIn(object parameter)
		{
			BaseDataNode item = parameter as BaseDataNode;
			string filePath = CurrentDesignFilePath;
			if (item != null && !string.IsNullOrEmpty(filePath))
			{
				return Reservation.IsCheckInValid(item);
				//mediator message is editable imet.DataNode
			}
			else
			{
				return false;
			}
		}
		/// <summary>
		/// Executes the check in.
		/// </summary>
		/// <param name="parameter">The parameter.</param>
		private void ExecuteCheckIn(object parameter)
		{
			BaseDataNode item = parameter as BaseDataNode;
			string filePath = CurrentDesignFilePath;
			if (item != null && !string.IsNullOrEmpty(filePath))
			{
				BaseDataNode node = item;
				Mouse.OverrideCursor = Cursors.Wait;
				ServerServiceError sse = Reservation.CheckIn(node, filePath, "Check in");
				Mouse.OverrideCursor = null;
				OnCheckInExecuted(node, sse); // do app specific actions after checkin
			}
		}
		/// <summary>
		/// Called when [check in executed].
		/// </summary>
		/// <param name="node">The node.</param>
		/// <param name="error">The error.</param>
		protected virtual void OnCheckInExecuted(BaseDataNode node, ServerServiceError error)
		{
			if (error == null)
			{
				//Mediator.NotifyColleaguesAsync<NodePropertyChangedPayload>(MediatorMessages.ViewModelNodePropertyChanged, new NodePropertyChangedPayload(node, "IsLoaded"));
				//Mediator.NotifyColleagues<NodePropertyChangedPayload>(MediatorMessages.ViewModelNodePropertyChanged, new NodePropertyChangedPayload(node, "IsEditable"));
				Infrastructure.Events.NodePropertyChanged.Publish(node, new List<string>(2) { nameof(node.IsDataLoaded), nameof(node.IsEditable) }, this);
			}
			else
			{
				MessageBox.Show(error.ToString() + ": " + error.ErrorType.ToString() + (error.OtherException != null ? (" (" + error.OtherException.ToString() + ")") : ""));
			}
		}
#endregion //CheckInCommand

#region Events
		/// <summary>
		/// Gets the latest events for a given model from the server.
		/// </summary>
		/// <param name="dataModel">The data model.</param>
		/// <returns>List&lt;ModelEvent&gt;.</returns>
		/// <exception cref="ServerServiceException"></exception>
		public virtual List<ModelEvent> GetEvents(DataModel dataModel)
		{
			var events = new List<ModelEvent>();
			ServerServiceError error = Epx.SDK.Events.GetEvents(dataModel, out events);// check changes
			if (error != null)
			{
				throw new ServerServiceException(error);
			}
			return events;
		}


		/// <summary>
		/// Updates the given events to local repository.
		/// </summary>
		/// <param name="dataModel">The data model.</param>
		/// <param name="events">The events.</param>
		/// <param name="eventsChangingData">A list of events that cause a change to the local repository data.</param>
		/// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
		/// <exception cref="ServerServiceException"></exception>
		public bool UpdateEventsToLocalRepository(DataModel dataModel, List<ModelEvent> events, out List<ModelEvent> eventsChangingData)
		{
			bool returnResult = true;
			ServerServiceError error = Epx.SDK.Events.UpdateEventsToLocalRepository(dataModel, CurrentDesignFilePath, events, out eventsChangingData);//load new data
			if (error != null)
			{
				returnResult = false;
				throw new ServerServiceException(error);
			}
			return returnResult;
		}
#endregion

#region Branch/Publish

		/// <summary>
		/// Branches the specified data node.
		/// </summary>
		/// <param name="dataNode">The data node.</param>
		/// <returns><c>true</c> if the branch action is successful, <c>false</c> otherwise.</returns>
		/// <exception cref="ServerServiceException">An exception is thrown if the branch failed.</exception>
		public virtual bool Branch(BaseDataNode dataNode)
		{
			bool returnValue = true;
			ServerServiceError error = BranchPublish.Branch(CurrentDesignFilePath, dataNode, null);
			if (error != null)
			{
				returnValue = false;
				throw new ServerServiceException(error);
			}
			return returnValue;
		}

		/// <summary>
		/// Publishes the specified data node.
		/// </summary>
		/// <param name="dataNode">The data node.</param>
		/// <returns><c>true</c> if the publish action is successful, <c>false</c> otherwise.</returns>
		/// <exception cref="ServerServiceException">An exception is thrown if the publish failed.</exception>
		public virtual bool Publish(BaseDataNode dataNode)
		{
			bool returnValue = true;
			ServerServiceError error = BranchPublish.Publish(dataNode, CurrentDesignFilePath);
			if (error != null)
			{
				returnValue = false;
				throw new ServerServiceException(error);
			}
			return returnValue;
		}
#endregion

#endregion // Node service operations

		protected override void Dispose(bool disposing)
		{
			SessionService.EndSession();

			base.Dispose(disposing);
		}
	}
}
