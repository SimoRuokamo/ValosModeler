using AvalonDock.Layout;
using Enterprixe.WPF.Tools.Localization;
using Epx.BIM;
using Epx.BIM.Models;
using ValosService;
using Epx.SDK;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Ribbon;
using System.Windows.Input;
using ValosModeler.Infrastructure;
using ValosModeler.Views;

namespace ValosModeler
{
	public class MainWindowViewModel : Infrastructure.ViewModelBase
	{
		/// <summary>
		/// The _session information
		/// </summary>
		private ValosSessionStartup _sessionInformation;
		/// <summary>
		/// The _open file name
		/// </summary>
		private string _openFileName = null;

		/// <summary>
		/// Initializes a new instance of the <see cref="MainWindowViewModel"/> class.
		/// </summary>
		public MainWindowViewModel() : this(null)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="MainWindowViewModel"/> class.
		/// </summary>
		/// <param name="sessionInformation">The session information.</param>
		public MainWindowViewModel(ValosSessionStartup sessionInformation)
		{
			RegisterToMediator();
			//AppCommonActionsViewModel.Initialize();
			AppModelInstanceManager.Initialize();
			ModelInstanceManager.Instance.InitializeService(Properties.Settings.Default.ServiceUsername, EncryptTool.Decrypt(Properties.Settings.Default.ServicePassword), Properties.Settings.Default.ApplicationID, Properties.Settings.Default.ServiceAddress);
			RibbonTabs.RibbonTabMappings.Initialize();
			Views.ViewMappings.Initialize();
			var featureManager = new FeatureManager();

			if (sessionInformation != null)
			{
				_openFileName = sessionInformation.ModelName;
				_sessionInformation = sessionInformation;
				AppModelInstanceManager.Instance.SessionService.StartSession(_sessionInformation, true);
			}

			_initialDate = DateTime.Now;
			WorldClockDisplay = _initialDate;
		}

		/// <summary>
		/// Flag to disable load event after initial loading of program. For example when resuming from
		/// system sleep state loaded events are fired again.
		/// </summary>
		/// <value><c>true</c> if [initial load completed]; otherwise, <c>false</c>.</value>
		public bool InitialLoadCompleted { get; set; }


#if DEBUG
		//code to open last used model on startup
		public bool OpenLastModel
		{
			get => Properties.Settings.Default.OpenLastModel;
			set
			{
				Properties.Settings.Default.OpenLastModel = value;
				Properties.Settings.Default.Save();
			}
		}


#endif
		/// <summary>
		/// Loads the main window.
		/// </summary>
		public void MainWindowLoaded()
		{
			try
			{
#if DEBUG
				if (!string.IsNullOrEmpty(Properties.Settings.Default.LastModelPath))
				{
					var fInfo = new FileInfo(Properties.Settings.Default.LastModelPath);
					if (OpenLastModel && fInfo.Exists)
					{
						StartModelOpenProcess(Properties.Settings.Default.LastModelPath);
						_openFileName = Properties.Settings.Default.LastModelPath;
					}
				}
#endif
				if (_sessionInformation != null && !string.IsNullOrEmpty(_sessionInformation.ModelName))
				{
					if (AppModelInstanceManager.Instance.OpenSessionModel())
						NewDatabaseOpened();
					else
					{
						OnRequestClose();
					}
				}
				else if (string.IsNullOrEmpty(_openFileName))
				{
					NewDesignDatabaseCommand.Execute(null);
				}
				else
				{
					// open local non session model from startup args?
				}
			}
			catch (Exception exception)
			{
				if (exception is ServerServiceException)
				{
					MessageBox.Show(App.Current.MainWindow, (exception as ServerServiceException).ErrorType.ToString(), "Server Service Error", MessageBoxButton.OK, MessageBoxImage.Error);
				}
			}

			InitialLoadCompleted = true;
		}

		/// <summary>
		/// Gets the window title.
		/// </summary>
		/// <value>The window title.</value>
		public string GetWindowTitle
		{
			get
			{
				var modelManager = AppModelInstanceManager.Instance;
				string projectName = !string.IsNullOrEmpty(modelManager.CurrentDesignFilePath) ? Path.GetFileName(modelManager.CurrentDesignFilePath) : modelManager.CurrentModel != null ? modelManager.CurrentModel.Name : string.Empty;
				if (!string.IsNullOrEmpty(projectName) && modelManager.SaveRequired) projectName += "*";
				string title = !string.IsNullOrEmpty(projectName) ? projectName + " - " + App.AppName + " " + "" : App.AppName + " " + "";
				return title;
			}
		}

		//private Type _currentModule = null;
		//public Type CurrentModule
		//{
		//	get { return _currentModule; }
		//	set
		//	{
		//		if (_currentModule != value)
		//		{
		//			_currentModule = value;
		//			OnPropertyChanged();
		//		}
		//	}
		//}

		#region Ribbon

		[MediatorMessageSink(Infrastructure.Events.ShowRibbonTabs.MessageID)]
		public void GenerateTabs(Type nodeType)
		{
			_isEditingTabSource = true;
			foreach (RibbonTab tab in RibbonTabSource)
			{
				if (tab.DataContext is ViewModelBase && !(tab.DataContext is MainWindowViewModel))
				{
					// dispose to unsubscribe from events
					if ((tab.DataContext as ViewModelBase).DisposeOnRibbonTabGeneration)
						(tab.DataContext as ViewModelBase).Dispose();
				}
			}
			////_previouslySelectedTabType = SelectedRibbonTab != null ? SelectedRibbonTab.GetType() : null;
			RibbonTabSource.Clear();

			// Add Home tab
			//RibbonTabSource.Add(ServiceLocator.Current.GetInstance<RibbonTabs.HomeTab>());
			RibbonTabSource.Add(new RibbonTabs.HomeTab() { DataContext = this });

			if (nodeType == null)
			{
				_isEditingTabSource = false;
				return;
			}

			// Module tabs
			var viewMapper = Infrastructure.DataNodeEventToViewMapper.Instance;
			//var key = Tuple.Create(typeof(Infrastructure.Events.RequestNavigateModule), nodeType);

			//Type value;
			//if (viewMapper.GetView(key, out value))
			//{
			//	// TODO maybe too slow?
			//	Infrastructure.IRibbonTabContainer tabContainer = Activator.CreateInstance(value) as Infrastructure.IRibbonTabContainer;

			//	if (tabContainer == null)
			//	{
			//		_isEditingTabSource = false;
			//		return;
			//	}

			//	IEnumerable<RibbonTab> tabsToAdd = tabContainer.Tabs;
			//	RibbonTab selectedTab = null;
			//	foreach (RibbonTab tab in tabsToAdd)
			//	{
			//		RibbonTabSource.Add(tab);
			//		if (tab.GetType() == GetPreviouslySelectedTabType()) selectedTab = tab;
			//	}

			//	if (selectedTab != null) SelectedRibbonTab = selectedTab;
			//	//else SelectedRibbonTab = tabsToAdd.ElementAt(0);
			//}
			_isEditingTabSource = false;
		}

		bool _wasContextualTabSelected = false;
		Type _previouslySelectedTabType = null;
		//Dictionary<Type, Type> _modulePreviuoslySelectedTabType = new Dictionary<Type, Type>();
		bool _isEditingTabSource = false;

		//[MediatorMessageSink(MediatorMessages.SetContextualTab)]
		//public void GenerateContextualTabs(BaseDataNode node)
		//{
		//	GenerateContextualTabs(node);
		//}

		[MediatorMessageSink(Infrastructure.Events.ShowRibbonContextualTabs.MessageID)]
		public void GenerateContextualTabs(object tabObject)
		{
			_isEditingTabSource = true;
			if (tabObject == null) _wasContextualTabSelected = false;

			// remove previous contextual tabs
			List<RibbonTab> removeList = new List<RibbonTab>();
			foreach (RibbonTab tab in RibbonTabSource)
			{
				if (!string.IsNullOrEmpty((string)tab.ContextualTabGroupHeader))
				{
					removeList.Add(tab);
				}
			}
			foreach (RibbonTab tab in removeList)
			{
				if (tab.IsSelected) _wasContextualTabSelected = true;
				if (tab.DataContext is ViewModelBase)
				{
					// dispose to unsubscribe from events
					(tab.DataContext as ViewModelBase).Dispose();
				}
				RibbonTabSource.Remove(tab);
			}
			// clear old group
			RibbonContextualTabGroupSource.Clear();

			if (tabObject == null)
			{
				// select the previously selected tab
				foreach (RibbonTab tab in RibbonTabSource)
				{
					if (tab.GetType() == GetPreviouslySelectedTabType())
					{
						SelectedRibbonTab = tab;
						break;
					}
				}
				_isEditingTabSource = false;
				return;
			}
			////if(!_wasContextualTabSelected) _previouslySelectedTabType = SelectedRibbonTab != null ? SelectedRibbonTab.GetType() : null;

			var viewMapper = Infrastructure.DataNodeEventToViewMapper.Instance;
			Type tabObjectType = tabObject.GetType();
			var key = Tuple.Create(typeof(Infrastructure.Events.ShowRibbonContextualTabs), tabObjectType);

			// exact matches only
			if (viewMapper.GetView(key, out Type value))
			{
				IRibbonContextualTabContainer tabContainer = Activator.CreateInstance(value, tabObject) as IRibbonContextualTabContainer;
				AddContextualTab(tabContainer);
			}
			// base type match system
			//var views = viewMapper.GetViews(key);
			//foreach (var view in views)
			//{
			//	IRibbonContextualTabContainer tabContainer = Activator.CreateInstance(view, tabObject) as IRibbonContextualTabContainer;
			//	AddContextualTab(tabContainer);
			//}

			_wasContextualTabSelected = false;
			_isEditingTabSource = false;
		}

		private void AddContextualTab(IRibbonContextualTabContainer tabContainer)
		{
			if (tabContainer == null)
			{
				_isEditingTabSource = false;
				return;
			}

			foreach (RibbonTab tab in RibbonTabSource)
			{
				if (tab.ContextualTabGroupHeader == tabContainer.ContextualTabGroup.Header)
				{
					// TODO what was this for?
					//_isEditingTabSource = false;
					//return;
				}
			}

			// add new group
			RibbonContextualTabGroupSource.Add(tabContainer.ContextualTabGroup);

			// add new tabs
			foreach (RibbonTab tab in tabContainer.ContextualTabs)
			{
				RibbonTabSource.Add(tab);

				// reselect new contextual tab if it was selected
				if (_wasContextualTabSelected)
				{
					SelectedRibbonTab = tab;
					_wasContextualTabSelected = false;
				}
			}
		}

		private ObservableCollection<RibbonTab> _RibbonTabSource = new ObservableCollection<RibbonTab>();
		public ObservableCollection<RibbonTab> RibbonTabSource
		{
			get { return _RibbonTabSource; }
			set
			{
				if (_RibbonTabSource != value)
				{
					_RibbonTabSource = value;
					OnPropertyChanged(nameof(this.RibbonTabSource));
				}
			}
		}

		private ObservableCollection<RibbonContextualTabGroup> _RibbonContextualTabGroupSource = new ObservableCollection<RibbonContextualTabGroup>();
		public ObservableCollection<RibbonContextualTabGroup> RibbonContextualTabGroupSource
		{
			get { return _RibbonContextualTabGroupSource; }
			set
			{
				if (_RibbonContextualTabGroupSource != value)
				{
					_RibbonContextualTabGroupSource = value;
					OnPropertyChanged(nameof(this.RibbonContextualTabGroupSource));
				}
			}
		}

		private RibbonTab _selectedRibbonTab = null;
		public RibbonTab SelectedRibbonTab
		{
			get { return _selectedRibbonTab; }
			set
			{
				if (_selectedRibbonTab != value)
				{
					_selectedRibbonTab = value;
					if (_selectedRibbonTab != null && !_isEditingTabSource && string.IsNullOrEmpty(_selectedRibbonTab.ContextualTabGroupHeader as string))
					{
						//if (CurrentModule != null)
						//	_modulePreviuoslySelectedTabType[CurrentModule] = _selectedRibbonTab.GetType();
						_previouslySelectedTabType = _selectedRibbonTab.GetType();
					}
					OnPropertyChanged(nameof(this.SelectedRibbonTab));
				}
			}
		}

		private void SelectRibbonTab(Type ribbonTabType)
		{
			foreach (RibbonTab tab in RibbonTabSource)
			{
				if (tab.GetType() == ribbonTabType)
				{
					SelectedRibbonTab = tab;
					break;
				}
			}
		}

		private Type GetPreviouslySelectedTabType()
		{
			Type selectedType = null;
			//if (CurrentModule != null)
			//{
			//	_modulePreviuoslySelectedTabType.TryGetValue(CurrentModule, out selectedType);
			//}
			selectedType = _previouslySelectedTabType;
			return selectedType;
		}

		#endregion //Ribbon

		#region DockingManager

		[MediatorMessageSink(Infrastructure.Events.OpenView.MessageID)]
		private void OpenView(Infrastructure.Events.OpenViewPayload payload)
		{
			try
			{
				if (DockingManager == null) return;

				if (payload.ControlView != null)
				{
					AddView(payload.ControlView, payload.OpenAsFloating);
				}
				else
				{
					var viewMapper = Infrastructure.DataNodeEventToViewMapper.Instance;
					Type contextType = payload.ViewType != null ? payload.ViewType : payload.DataNode.GetType();
					if (contextType == null) return;
					var key = Tuple.Create(typeof(Infrastructure.Events.OpenView), contextType);
					Type value;
					if (viewMapper.GetView(key, out value))
					{
						bool viewFound = false;

						if (!payload.CreateNewView)
						{
							// try to find existing view
							var documents = DockingManager.Layout.Descendents().OfType<LayoutDocument>();

							foreach (LayoutDocument doc in documents)
							{
								if (payload.ViewType == null)
								{
									if (doc.Content != null && doc.Content.GetType() == value &&
										((doc.Content as FrameworkElement).DataContext as ViewModelBase).DataNode == payload.DataNode)
									{
										ActiveContent = doc.Content;
										viewFound = true;
										break;
									}
								}
								else if (payload.ViewType != null)
								{
									if (payload.DataNode != null)
									{
										if (doc.Content != null && doc.Content.GetType() == value &&
											((doc.Content as FrameworkElement).DataContext as ViewModelBase).DataNode == payload.DataNode)
										{
											ActiveContent = doc.Content;
											viewFound = true;
											break;
										}
									}
									else
									{
										if (doc.Content != null && doc.Content.GetType() == value)
										{
											ActiveContent = doc.Content;
											viewFound = true;
											break;
										}
									}
								}
							}
						}

						if (payload.CreateNewView || (!payload.CreateNewView && !viewFound))
						{
							Control view = Activator.CreateInstance(value) as Control;
							if (payload.ViewType != null)
							{
								if (payload.DataNode != null)
									view.DataContext = Activator.CreateInstance(viewMapper.GetViewModel(key), payload.DataNode);
								else
									view.DataContext = Activator.CreateInstance(viewMapper.GetViewModel(key));
							}
							else if (payload.DataNode != null)
								view.DataContext = Activator.CreateInstance(viewMapper.GetViewModel(key), payload.DataNode);
							
							AddView(view, payload.OpenAsFloating);
						}
					}
					else
					{
						//if (CurrentModule != null)
						{
							//key = Tuple.Create(typeof(Infrastructure.Events.OpenView), CurrentModule);
							key = Tuple.Create(typeof(Infrastructure.Events.OpenView), typeof(App));
							Type viewType;
							if (viewMapper.GetView(key, out viewType))
							{
								bool viewFound = false;

								if (!payload.CreateNewView)
								{
									// try to find existing view
									var documents = DockingManager.Layout.Descendents().OfType<LayoutDocument>();

									foreach (LayoutDocument doc in documents)
									{
										if (doc.Content != null && doc.Content.GetType() == value &&
											((doc.Content as FrameworkElement).DataContext as ViewModelBase).DataNode == payload.DataNode)
										{
											ActiveContent = doc.Content;
											viewFound = true;
											break;
										}
									}
								}

								if (payload.CreateNewView || (!payload.CreateNewView && !viewFound))
								{
									// TODO maybe too slow?
									Control view = Activator.CreateInstance(viewType) as Control;
									view.DataContext = Activator.CreateInstance(viewMapper.GetViewModel(key), payload.DataNode);
									AddView(view, payload.OpenAsFloating);
								}
							}
						}
					}
				}
			}
			catch(System.MissingMethodException)
			{
				System.Diagnostics.Debug.Fail("Open view failed: constructor mismatch in viewmodel.");
			}
			catch// (Exception ex)
			{
				var node = "??";
				if (payload.DataNode != null)
					node = payload.DataNode.ToString();
				var action = "Open view for: " + node;
				//ExecLogger.LogActionError(ex, action);
			}
		}


		//private static FloatingData _defaultFloating = new FloatingData(400, 600);
		private void AddView(Control view, bool openAsFloating)
		{
			if (view != null)
			{
				LayoutDocument doc = CreateLayoutDocument(view);
				AddLayoutDocument(doc);
				if (openAsFloating)
				{
					//if (doc.Content is IFloatingWindow)
					//	(doc.Content as IFloatingWindow).Floating.ToContent(doc);
					//else
					//	_defaultFloating.ToContent(doc);
					doc.Float();
				}
				ActiveContent = view;
			}
		}

		private bool AddLayoutDocument(LayoutDocument doc)
		{
			if (DockingManager == null) return false;

			bool retval = false;
			LayoutDocumentPane documentPane = null;

			if (DockingManager.Layout.LastFocusedDocument != null)
			{
				documentPane = DockingManager.Layout.LastFocusedDocument.Parent as LayoutDocumentPane;
			}

			if (documentPane == null)
			{
				documentPane = DockingManager.Layout.Descendents().OfType<LayoutDocumentPane>().FirstOrDefault();
			}

			if (documentPane != null)
			{
				documentPane.Children.Add(doc);
				retval = true;
			}

			return retval;
		}

		/// <summary>
		/// Currently not used.
		/// </summary>
		/// <param name="anchorable"></param>
		/// <returns></returns>
		private bool AddLayoutAnchorable(LayoutAnchorable anchorable)
		{
			if (DockingManager == null) return false;

			bool retval = false;
			LayoutAnchorablePane anchorablePane = null;

			if (anchorablePane == null)
			{
				anchorablePane = DockingManager.Layout.Descendents().OfType<LayoutAnchorablePane>().FirstOrDefault();
			}

			if (anchorablePane != null)
			{
				anchorablePane.Children.Add(anchorable);
				retval = true;
			}

			return retval;
		}
		/// <summary>
		/// Currently not used.
		/// </summary>
		/// <param name="content"></param>
		/// <returns></returns>
		//private LayoutAnchorable CreateLayoutAnchorable(Control content)
		//{
		//	LayoutAnchorable anchorable = new LayoutAnchorable();
		//	anchorable.Content = content;
		//	AttachDockableClosedHandler(anchorable);
		//	return anchorable;
		//}

		private LayoutDocument CreateLayoutDocument(Control content)
		{
			LayoutDocument document = new LayoutDocument();
			document.Content = content;
			AttachDockableClosedHandler(document);
			return document;
		}

		private void AttachDockableClosedHandler(LayoutContent document)
		{
			if ((document.Content as FrameworkElement).DataContext is ViewModelBase)
			{
				document.Closing += ((document.Content as FrameworkElement).DataContext as ViewModelBase).OnLayoutDocumentClosing;
				document.Closed += ((document.Content as FrameworkElement).DataContext as ViewModelBase).OnLayoutDocumentClosed;
			}
		}

		/// <summary>
		/// Only after project file Open.
		/// </summary>
		public void AttachDockableClosedHandlers()
		{
			foreach (var lc in DockingManager.Layout.Descendents().OfType<LayoutContent>().ToList())
			{
				if (lc.Content == null)
				{
					lc.Close();
				}
				else /*if (lc.Content.GetType() != typeof(Views.Navigator) && lc.Content.GetType() != typeof(Views.PropertiesView) && lc.Content.GetType() != typeof(Views.ApplicationLog))*/
				{
					AttachDockableClosedHandler(lc);
				}
			}
		}

		private object _activeDocument = null;
		private object _activeContent = null;
		/// <summary>
		/// The active view/control.
		/// </summary>
		public object ActiveContent
		{
			get { return _activeContent; }
			set
			{
				if (_activeContent != value)
				{
					BaseDataNode node0 = null, node1 = null;
					if (_activeContent != null && (_activeContent as FrameworkElement).DataContext is ViewModelBase)
					{
						var activeContentViewModel = (_activeContent as FrameworkElement).DataContext as ViewModelBase;
						activeContentViewModel.IsActiveContentChanged(false);
						node0 = activeContentViewModel.DataNode;
					}

					_activeContent = value;
					OnPropertyChanged();

					if (_activeContent == null)
					{
						// publish event instead of calling local function so that other parts of UI are updated accordingly.
						//EventAggregator.GetEvent<Infrastructure.Events.RequestNavigateModule>().Publish(CurrentModule);
						//EventAggregator.GetEvent<Infrastructure.Events.RequestNavigatePropertiesView>().Publish(null);
					}
					else if (_activeDocument == _activeContent)
					{
						// the active LayoutDocument did not change
					}
					else
					{
						var viewMapper = Infrastructure.DataNodeEventToViewMapper.Instance;
						Type nodeType = viewMapper.GetNodeType(_activeContent.GetType());

						//if (nodeType != null)
						//{
						//	var key = Tuple.Create(typeof(Infrastructure.Events.GetModuleBaseType), nodeType);
						//	Type moduleType;
						//	if (viewMapper.GetView(key, out moduleType))
						//	{
						//		// publish event instead of calling local function so that other parts of UI are updated accordingly.
						//		EventAggregator.GetEvent<Infrastructure.Events.RequestNavigateModule>().Publish(moduleType);
						//	}
						//	else
						//	{
						//		EventAggregator.GetEvent<Infrastructure.Events.RequestNavigateModule>().Publish(CurrentModule);
						//	}
						//}

						var activeContentViewModel = (_activeContent as FrameworkElement).DataContext as ViewModelBase;
						if (activeContentViewModel != null)
						{
							activeContentViewModel.IsActiveContentChanged(true);
							node1 = activeContentViewModel.DataNode;
						}
						//need to change selected tree node if view switched from one storey view to another
						//if ((node0 == null && node1 != null) || (node0 is Model.ModelParts.ModelStorey && node1 is Model.ModelParts.ModelStorey))
						//	EventAggregator.GetEvent<Infrastructure.Events.RequestNavigatePropertiesView>().Publish(node1);
					}

					if (DockingManager.Layout.ActiveContent is LayoutDocument && _activeDocument != _activeContent)
					{
						_activeDocument = _activeContent;
						ViewModelBase.LastActiveModelView = (_activeDocument as FrameworkElement)?.DataContext as ViewModelBase;
					}
				}
			}
		}

		[MediatorMessageSink("SetActiveContent")]
		private void SetActiveContent(ViewModelBase viewModel)
		{
			LayoutContent content = DockingManager.Layout.Descendents().OfType<LayoutContent>().First(c => (c.Content as FrameworkElement).DataContext == viewModel);
			if (content != null)
			{
				ActiveContent = content.Content;
			}
		}

		HashSet<BaseDataNode> _openedViews = new HashSet<BaseDataNode>();
		private void LoadLayoutDeserializationCallBack(object sender, AvalonDock.Layout.Serialization.LayoutSerializationCallbackEventArgs e)
		{
			if (e.Model.ContentId == "ModelExplorerViewModel")
			{
				if (e.Content == null || e.Content.GetType() != typeof(ModelExplorer))
					e.Content = new ModelExplorer() { DataContext = new Views.ModelExplorerViewModel(AppModelInstanceManager.Instance.CurrentModel) };// ServiceLocator.Current.GetInstance<Views.Navigator>();
			}
			else if (e.Model.ContentId == "GLModelViewViewModel")
			{
				e.Content = new Views.Model3DView.GLModelView(AppModelInstanceManager.Instance.CurrentModel);
				if (ViewModelBase.LastActiveModelView == null)
					ViewModelBase.LastActiveModelView = (e.Content as FrameworkElement).DataContext as ViewModelBase;
			}
			else if (e.Model.ContentId == "ModelViewViewModel")
			{
				if (e.Content == null || e.Content.GetType() != typeof(Views.Model3DView.ModelView))
				{
					e.Content = new Views.Model3DView.ModelView()
					{ DataContext = new Views.Model3DView.ModelViewViewModel(AppModelInstanceManager.Instance.CurrentModel) { WindowTitle = e.Model.Title } };// ServiceLocator.Current.GetInstance<Views.Navigator>();
					if (ViewModelBase.LastActiveModelView == null)
						ViewModelBase.LastActiveModelView = (e.Content as FrameworkElement).DataContext as ViewModelBase;
				}
			}
			//else if (e.Model.ContentId == PropertiesViewModel.PropertiesContentId)
			//{
			//	if (e.Content == null || e.Content.GetType() != typeof(Views.PropertiesView))
			//		e.Content = ServiceLocator.Current.GetInstance<Views.PropertiesView>();
			//}
			//else if (e.Model.ContentId == ApplicationLogViewModel.ApplicationLogContentId)
			//{
			//	if (e.Content == null || e.Content.GetType() != typeof(Views.ApplicationLog))
			//		e.Content = ServiceLocator.Current.GetInstance<Views.ApplicationLog>();
			//}
			//else 
			if (e.Model.ContentId == ViewModelBase.NoSaveContentId || e.Model.ContentId == null)
			{
				e.Model.Close();
			}
			else if (!string.IsNullOrWhiteSpace(e.Model.ContentId))
			{
				try
				{
					string[] splitString = e.Model.ContentId.Split('|');
					if (Guid.TryParse(splitString[0], out Guid nodeUID))
					{
						var modelManager = Infrastructure.AppModelInstanceManager.Instance;
						BaseDataNode node = modelManager.CurrentModel.GetDescendantNode<BaseDataNode>(nodeUID); // can be slow
						if (node != null && !_openedViews.Contains(node))
						{
							var view = OpenDefaultViewForNode(node, node.GetType());
							//if (view == null && splitString.Count() > 1)
							//{
							//	Type module = BaseDataNode.GetType(splitString[1]);
							//	view = OpenDefaultViewForNode(node, module);
							//}
							e.Content = view;
							_openedViews.Add(node);
						}
					}
				}
				catch //(Exception ex)
				{
					var action = "Open view for: " + e.Model.ContentId;
					//ExecLogger.LogActionError(ex, action);
				}
			}
		}

		private Control OpenDefaultViewForNode(BaseDataNode node, Type nodeType)
		{
			Control view = null;
			var viewMapper = Infrastructure.DataNodeEventToViewMapper.Instance;
			var key = Tuple.Create(typeof(Infrastructure.Events.OpenView), nodeType);
			Type viewType;
			if (viewMapper.GetView(key, out viewType))
			{
				view = Activator.CreateInstance(viewType) as Control;
				view.DataContext = Activator.CreateInstance(viewMapper.GetViewModel(key), node);
			}
			return view;
		}

		private void CloseDockWindows()
		{
			if (DockingManager == null) return;

			IEnumerable<LayoutContent> layoutContents = DockingManager.Layout.Descendents().OfType<LayoutContent>().ToList<LayoutContent>();

			foreach (var content in layoutContents)
			{
				//if (!(content.Content is Views.Navigator || content.Content is Views.PropertiesView || content.Content is Views.ApplicationLog))
				{
					content.Close();
				}
			}
		}

#endregion //DockingManager

#region Open/Save/New

		/// <summary>
		/// Previews the new database opened.
		/// </summary>
		private void PreviewNewDatabaseOpened()
		{
			// Reset undo chain
			// Dispose viewmodels (to unregister from Mediator).
			CloseDockWindows();
			//if (ViewModelRibbon != null) ViewModelRibbon.Dispose();
			//if (ViewModelProjectExplorer != null) ViewModelProjectExplorer.Dispose();
			//if (ViewModelModelWindow != null) ViewModelModelWindow.Dispose();
		}

		/// <summary>
		/// News the database opened.
		/// </summary>
		private void NewDatabaseOpened()
		{
			LoadDefaultLayoutCommand.Execute(null);
			AttachDockableClosedHandlers();
			GenerateTabs(null);
			//ViewModelRibbon = new View.RibbonViewModel(ModelInstanceManager.Instance.CurrentModel);
			//ViewModelModelWindow = new View.ModelWindowViewModel(ModelInstanceManager.Instance.CurrentModel);
			//ViewModelProjectExplorer = new ProjectExplorerViewModelBase(ModelInstanceManager.Instance.CurrentModel);
			WindowTitle = GetWindowTitle;
		}

#region NewDesignDatabaseCommand
		/// <summary>
		/// The _new design database command
		/// </summary>
		RelayCommand _newDesignDatabaseCommand;
		/// <summary>
		/// Gets the new design database command.
		/// </summary>
		/// <value>The new design database command.</value>
		public ICommand NewDesignDatabaseCommand
		{
			get
			{
				if (_newDesignDatabaseCommand == null)
					_newDesignDatabaseCommand = new RelayCommand(execute => this.ExecuteNewDesignDatabase(execute), canexecute => this.CanExecuteNewDesignDatabase(canexecute));
				return _newDesignDatabaseCommand;
			}
		}

		/// <summary>
		/// Determines whether this instance [can execute new design database] the specified parameter.
		/// </summary>
		/// <param name="parameter">The parameter.</param>
		/// <returns><c>true</c> if this instance [can execute new design database] the specified parameter; otherwise, <c>false</c>.</returns>
		private bool CanExecuteNewDesignDatabase(object parameter)
		{
			return true;
		}

		/// <summary>
		/// Executes the new design database.
		/// </summary>
		/// <param name="parameter">The parameter.</param>
		private void ExecuteNewDesignDatabase(object parameter)
		{
			if (QuerySave(QuerySaveState.NewModel))
			{
				PreviewNewDatabaseOpened();
				StartNewModelProcess();
				NewDatabaseOpened();
			}
		}

		/// <summary>
		/// Starts the new model process.
		/// </summary>
		private void StartNewModelProcess()
		{
			_querySaveState = QuerySaveState.None;
			var modelManager = AppModelInstanceManager.Instance;
			if (modelManager.CreateNewModel() == null)
			{
				this.OnRequestClose(); // Could not start a session.
			}
			Project project = new Project(true);
			ModelFolderNode folder = new ModelFolderNode("Folder");
			project.AddChild(folder);
			modelManager.CurrentModel.AddChild(project);
			var ucs = new Epx.BIM.GridMesh.GridUCS("UCS", true);
			folder.AddChild(ucs);
			folder.IsTarget = true;
			
			ucs.IsCurrentGrid = true;
			
			
		}
#endregion //NewDesignDatabaseCommand

#region OpenDesignDatabaseCommand
		/// <summary>
		/// The _open design database command
		/// </summary>
		RelayCommand _openDesignDatabaseCommand;
		/// <summary>
		/// Gets the open design database command.
		/// </summary>
		/// <value>The open design database command.</value>
		public ICommand OpenDesignDatabaseCommand
		{
			get
			{
				if (_openDesignDatabaseCommand == null)
					_openDesignDatabaseCommand = new RelayCommand(execute => this.ExecuteOpenDesignDatabase(execute), canexecute => this.CanExecuteOpenDesignDatabase(canexecute));
				return _openDesignDatabaseCommand;
			}
		}

		/// <summary>
		/// Determines whether this instance [can execute open design database] the specified parameter.
		/// </summary>
		/// <param name="parameter">The parameter.</param>
		/// <returns><c>true</c> if this instance [can execute open design database] the specified parameter; otherwise, <c>false</c>.</returns>
		private bool CanExecuteOpenDesignDatabase(object parameter)
		{
			return true;
		}

		/// <summary>
		/// Executes the open design database.
		/// </summary>
		/// <param name="parameter">The parameter.</param>
		private void ExecuteOpenDesignDatabase(object parameter)
		{
			OpenDesignDatabase(QuerySaveState.OpenModel, parameter as string);
		}

		/// <summary>
		/// Opens the design database.
		/// </summary>
		/// <param name="saveState">State of the save.</param>
		/// <param name="path">The path.</param>
		private void OpenDesignDatabase(QuerySaveState saveState, string path = null)
		{
			// TODO integrate layout load/save
			if (QuerySave(saveState))
			{
				StartModelOpenProcess(path);
			}
		}

		/// <summary>
		/// Starts the model open process.
		/// </summary>
		/// <param name="path">The path.</param>
		private void StartModelOpenProcess(string path = null)
		{
			Mouse.OverrideCursor = Cursors.Wait;
			IsEnabled = false;

			_openedViews.Clear();
			//PreviewNewDatabaseOpened(); //SLe- cannot understand why the current view totally dismissed before any new model picked. Moved the call to opened event
			var modelManager = AppModelInstanceManager.Instance;
			modelManager.ModelOpenedEvent += new ModelOpenSaveEventHandler(modelManager_ModelOpenedEvent);
			modelManager.Open(path);
		}

		/// <summary>
		/// Handles the ModelOpenedEvent event of the modelManager control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="ModelOpenSaveEventArgs"/> instance containing the event data.</param>
		private void modelManager_ModelOpenedEvent(object sender, ModelOpenSaveEventArgs e)
		{
			IsEnabled = true;
			WindowTitle = GetWindowTitle;

			if (e.Result)
			{
				PreviewNewDatabaseOpened();
				NewDatabaseOpened();
#if DEBUG
				Properties.Settings.Default.LastModelPath = (sender as ModelInstanceManager).CurrentDesignFilePath;
				Properties.Settings.Default.Save();
#endif
			}
			else
			{
				if (!string.IsNullOrEmpty(e.Exception))
				{
					if (e.Exception == "File Not Found")
					{
						string emsg = CultureManager.GetLocalizedString("Error Opening Database") + Environment.NewLine + CultureManager.GetLocalizedString("Check if the file has been moved or deleted");
						MessageBox.Show(emsg, CultureManager.GetLocalizedString("File Not Found"));
					}
					else if (e.Exception == "Unable to start a session.")
					{
						string emsg = CultureManager.GetLocalizedString("Unable to start a session.") + Environment.NewLine + CultureManager.GetLocalizedString("Unable to start a session.");
						MessageBox.Show(emsg, CultureManager.GetLocalizedString("Unable to start a session"));
						this.OnRequestClose();
					}
					else
					{
						string emsg = "Error opening the database." + Environment.NewLine + Environment.NewLine + e.Exception;
						ModelInstanceManager.LogOpenSaveException(emsg, true, false);
					}
				}
			}

			(sender as ModelInstanceManager).ModelOpenedEvent -= new ModelOpenSaveEventHandler(modelManager_ModelOpenedEvent);
			_querySaveState = QuerySaveState.None;
			Mouse.OverrideCursor = null;
		}
#endregion //OpenDesignDatabaseCommand

#region SaveDesignDatabaseCommand
		/// <summary>
		/// The _save design database command
		/// </summary>
		RelayCommand _saveDesignDatabaseCommand;
		/// <summary>
		/// Gets the save design database command.
		/// </summary>
		/// <value>The save design database command.</value>
		public ICommand SaveDesignDatabaseCommand
		{
			get
			{
				if (_saveDesignDatabaseCommand == null)
					_saveDesignDatabaseCommand = new RelayCommand(execute => this.ExecuteSaveDesignDatabase(execute), canexecute => this.CanExecuteSaveDesignDatabase(canexecute));
				return _saveDesignDatabaseCommand;
			}
		}

		/// <summary>
		/// Determines whether this instance [can execute save design database] the specified parameter.
		/// </summary>
		/// <param name="parameter">The parameter.</param>
		/// <returns><c>true</c> if this instance [can execute save design database] the specified parameter; otherwise, <c>false</c>.</returns>
		private bool CanExecuteSaveDesignDatabase(object parameter)
		{
			return true;
		}

		/// <summary>
		/// Executes the save design database.
		/// </summary>
		/// <param name="parameter">The parameter.</param>
		private void ExecuteSaveDesignDatabase(object parameter)
		{
			var modelManager = ModelInstanceManager.Instance;
			InitializeSave();
			modelManager.Save();
		}

		/// <summary>
		/// Initializes the save.
		/// </summary>
		private void InitializeSave()
		{
			var modelManager = ModelInstanceManager.Instance;

			// TODO integrate layout load/save on user level
			//var layoutSerializer = new AvalonDock.Layout.Serialization.XmlLayoutSerializer(DockingManager);
			//string layout;
			//using (var stream = new System.IO.StringWriter())
			//{
			//	layoutSerializer.Serialize(stream);
			//	layout = stream.ToString();
			//}
			//modelManager.Instance.DockWindowLayout = layout;

			Mouse.OverrideCursor = Cursors.Wait;
			IsEnabled = false;

			modelManager.ModelSavedEvent += new ModelOpenSaveEventHandler(modelManager_ModelSavedEvent);
		}

		/// <summary>
		/// Handles the ModelSavedEvent event of the modelManager control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="ModelOpenSaveEventArgs"/> instance containing the event data.</param>
		private void modelManager_ModelSavedEvent(object sender, ModelOpenSaveEventArgs e)
		{
			IsEnabled = true;
			WindowTitle = GetWindowTitle;
			Mouse.OverrideCursor = null;
			(sender as ModelInstanceManager).ModelSavedEvent -= new ModelOpenSaveEventHandler(modelManager_ModelSavedEvent);

			if (e.Result)
			{
				if (_querySaveState == QuerySaveState.IsClosing)
				{
					Application.Current.Shutdown();
				}
				else if (_querySaveState == QuerySaveState.OpenModel)
				{
					StartModelOpenProcess();
				}
				//else if (_querySaveState == QuerySaveState.OpenMRU)
				//{
				//	StartModelOpenProcess(_mostRecentlyUsedOpenPath);
				//}
				else if (_querySaveState == QuerySaveState.NewModel)
				{
					StartNewModelProcess();
				}
				else if (e.Operation == ModelOpenSaveEventArgs.OpenSaveOperation.SaveAs)
				{
					NewDatabaseOpened();
				}
			}
			else
			{
				if (!string.IsNullOrEmpty(e.Exception))
				{
					string emsg = "Error saving the database." + Environment.NewLine + Environment.NewLine + e.Exception;
					ModelInstanceManager.LogOpenSaveException(emsg, false, false);

					if (_querySaveState == QuerySaveState.IsClosing)
					{
						IsClosing = false;
					}
				}
			}

			_querySaveState = QuerySaveState.None;
		}
#endregion //SaveDesignDatabaseCommand

#region SaveAsDesignDatabaseCommand
		/// <summary>
		/// The _save as design database command
		/// </summary>
		RelayCommand _saveAsDesignDatabaseCommand;
		/// <summary>
		/// Gets the save as design database command.
		/// </summary>
		/// <value>The save as design database command.</value>
		public ICommand SaveAsDesignDatabaseCommand
		{
			get
			{
				if (_saveAsDesignDatabaseCommand == null)
					_saveAsDesignDatabaseCommand = new RelayCommand((a) => this.ExecuteSaveAsDesignDatabase(a), (a) => this.CanExecuteSaveAsDesignDatabase(a));
				return _saveAsDesignDatabaseCommand;
			}
		}

		/// <summary>
		/// Determines whether this instance [can execute save as design database] the specified parameter.
		/// </summary>
		/// <param name="param">The parameter.</param>
		/// <returns><c>true</c> if this instance [can execute save as design database] the specified parameter; otherwise, <c>false</c>.</returns>
		private bool CanExecuteSaveAsDesignDatabase(object param)
		{
			return true;
		}

		/// <summary>
		/// Executes the save as design database.
		/// </summary>
		/// <param name="param">The parameter.</param>
		private void ExecuteSaveAsDesignDatabase(object param)
		{
			var modelManager = ModelInstanceManager.Instance;
			//PreviewNewDatabaseOpened();
			InitializeSave();
			modelManager.SaveAs();
		}
#endregion //SaveAsDesignDatabaseCommand

#region QuerySave

		/// <summary>
		/// Enum QuerySaveState
		/// </summary>
		public enum QuerySaveState
		{
			/// <summary>
			/// The is closing
			/// </summary>
			IsClosing,
			/// <summary>
			/// The open model
			/// </summary>
			OpenModel,
			/// <summary>
			/// The open MRU
			/// </summary>
			OpenMRU,
			/// <summary>
			/// The new model
			/// </summary>
			NewModel,
			/// <summary>
			/// The none
			/// </summary>
			None
		};
		/// <summary>
		/// The _query save state
		/// </summary>
		private QuerySaveState _querySaveState = QuerySaveState.None;

		/// <summary>
		/// Check and perform save if changes are pending.
		/// </summary>
		/// <param name="saveState">State of the save.</param>
		/// <returns>True if allowed to continue.</returns>
		public bool QuerySave(QuerySaveState saveState)
		{
			bool retval = false;
			var modelManager = AppModelInstanceManager.Instance;

			if (modelManager.SaveRequired)
			{
				MessageBoxResult msgboxres = MessageBox.Show(Application.Current.MainWindow, CultureManager.GetLocalizedString("Save changes to database") + ": '" + System.IO.Path.GetFileName(modelManager.CurrentDesignFilePathOrDatabaseName) + "'?", App.AppName, MessageBoxButton.YesNoCancel);
				if (msgboxres == MessageBoxResult.Yes)
				{
					_querySaveState = saveState;
					retval = false;
					if (saveState == QuerySaveState.IsClosing) IsClosing = true;

					InitializeSave();
					modelManager.Save();
				}
				else if (msgboxres == MessageBoxResult.No)
				{
					retval = true;
					if (saveState == QuerySaveState.IsClosing) IsClosing = true;
				}
				else if (msgboxres == MessageBoxResult.Cancel)
				{
					retval = false;
				}
			}
			else
			{
				retval = true;
			}

			return retval;
		}
#endregion //QuerySave

#region OpenServerModelCommand
		RelayCommand _openServerModelCommand;
		public ICommand OpenServerModelCommand
		{
			get
			{
				if (_openServerModelCommand == null)
					_openServerModelCommand = new RelayCommand(execute => this.ExecuteOpenServerModel(execute), canexecute => this.CanExecuteOpenServerModel(canexecute));
				return _openServerModelCommand;
			}
		}

		private bool CanExecuteOpenServerModel(object parameter)
		{
			var modelManager = AppModelInstanceManager.Instance;
			string username = modelManager.SessionService.ServiceUsername;
			string password = modelManager.SessionService.ServicePassword;
			if (modelManager.SessionService.CurrentSession == null && (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(modelManager.SessionService.ServiceAddress))) return false;

			return true;
		}

		private void ExecuteOpenServerModel(object parameter)
		{
			var modelManager = AppModelInstanceManager.Instance;
			modelManager.SessionService.AutoServerCheckEnabled = false;
			string userName = modelManager.SessionService.ServiceUsername;
			string password = modelManager.SessionService.ServicePassword;
			var applicationID = modelManager.SessionService.ServiceApplicationID;
			string serverAddress = modelManager.SessionService.CurrentSession != null ? modelManager.SessionService.CurrentSession.ServiceAddress : modelManager.SessionService.ServiceAddress;

			var dataModels = new List<ServerDataModel>();
			var result = UserLoginProject.GetUserModelsForApplication(serverAddress, userName, new Guid(applicationID), "", password, out dataModels);

			var userModelsViewModel = new Views.OpenServerModelViewModel(dataModels, result == null);
			ServerDataModel selectedModel = userModelsViewModel.ShowDialog();

			if (selectedModel != null)
			{
				Mouse.OverrideCursor = Cursors.Wait;
				bool wasSessionEnded = modelManager.SessionService.EndSession();

				if (modelManager.SessionService.StartSession(selectedModel.ModelId, null, selectedModel.Name, serverAddress))
				{
					if (modelManager.OpenSessionModel()) NewDatabaseOpened();
				}
				else
				{
					if (wasSessionEnded) this.OnRequestClose();
					//else // if not started with a server license then no session existed previously -> do nothing
				}
				Mouse.OverrideCursor = null;
			}

			modelManager.SessionService.AutoServerCheckEnabled = true;
		}
#endregion //OpenServerModelCommand

#endregion

		#region World Clock

		private DateTime _initialDate;
		public static DateTime WorldClock { get; set; }
		private DateTime _WorldClockDisplay = DateTime.MinValue;
		public DateTime WorldClockDisplay
		{
			get { return _WorldClockDisplay; }
			set
			{
				if (_WorldClockDisplay != value)
				{
					_WorldClockDisplay = value;
					OnPropertyChanged("WorldClockDisplay");
				}
			}
		}
		private int _ClockSliderValue = 0;
		public int ClockSliderValue
		{
			get { return _ClockSliderValue; }
			set
			{
				if (_ClockSliderValue != value)
				{
					_ClockSliderValue = value;
					OnPropertyChanged("ClockSliderValue");
					WorldClock = _initialDate.AddHours(value);
					WorldClockDisplay = WorldClock;
					Infrastructure.Events.Update3D.PublishAsync((BaseDataNode)null);
				}
			}
		}
		private int _ClockSliderMin = -4320;
		public int ClockSliderMin
		{
			get { return _ClockSliderMin; }
			set
			{
				if (_ClockSliderMin != value)
				{
					_ClockSliderMin = value;
					OnPropertyChanged("ClockSliderMin");
				}
			}
		}
		private int _ClockSliderMax = 4320;
		public int ClockSliderMax
		{
			get { return _ClockSliderMax; }
			set
			{
				if (_ClockSliderMax != value)
				{
					_ClockSliderMax = value;
					OnPropertyChanged("ClockSliderMax");
				}
			}
		}


		#endregion

		#region Closing, RequestClose [event]

		/// <summary>
		/// Raised when this VM wants to close.
		/// </summary>
		public event EventHandler RequestClose;

		/// <summary>
		/// Called when [request close].
		/// </summary>
		void OnRequestClose()
		{
			EventHandler handler = this.RequestClose;
			if (handler != null)
				handler(this, EventArgs.Empty);
		}

		/// <summary>
		/// Flag to tell that viewmodel has finished pre-close processes and is closing.
		/// </summary>
		/// <value><c>true</c> if this instance is closing; otherwise, <c>false</c>.</value>
		public bool IsClosing { get; set; }
		/// <summary>
		/// Application (main window) is being closed. True if allowed to close.
		/// </summary>
		/// <returns>True if allowed to close.</returns>
		public bool OnClosing()
		{
			var modelManager = AppModelInstanceManager.Instance;
			if (modelManager.IsSaveOpenInProgress) return false;
			bool allowClose = !(Application.Current as App).HasErrorHappened && QuerySave(QuerySaveState.IsClosing);
			if (allowClose)
			{
				if (!(Application.Current as App).HasErrorHappened) modelManager.DisposeAutoSave();
				this.Dispose();
			}
			return allowClose;
		}

		#endregion // Closing, RequestClose [event]

		#region Window ProgressBar

		public void StartWindowProgressBar() => WindowProgressBarState = System.Windows.Shell.TaskbarItemProgressState.Normal;
		public void EndWindowProgressBar() => WindowProgressBarState = System.Windows.Shell.TaskbarItemProgressState.None;
		public void WindowProgressBarProgressChangedHandler(object sender, System.ComponentModel.ProgressChangedEventArgs e)
		{
			App.Current.Dispatcher.BeginInvoke((Action)delegate
			{
				WindowProgressBarMessage = (string)e.UserState;
				WindowProgressBarValue = 0.01 * e.ProgressPercentage;
			}, System.Windows.Threading.DispatcherPriority.Send);
		}

		private System.Windows.Shell.TaskbarItemProgressState _WindowProgressBarState = System.Windows.Shell.TaskbarItemProgressState.None;
		/// <summary>
		/// The main window's task bar progress bar state.
		/// </summary>
		public System.Windows.Shell.TaskbarItemProgressState WindowProgressBarState
		{
			get { return _WindowProgressBarState; }
			set
			{
				if (_WindowProgressBarState != value)
				{
					_WindowProgressBarState = value;
					if (_WindowProgressBarState == System.Windows.Shell.TaskbarItemProgressState.None)
					{
						WindowProgressBarValue = -1;
						WindowProgressBarMessage = string.Empty;
					}
					OnPropertyChanged("WindowProgressBarState");
					OnPropertyChanged("WindowProgressBarVisibility");
					OnPropertyChanged("WindowProgressBarIsIndeterminate");
				}
			}
		}

		private double _WindowProgressBarValue = 0;
		/// <summary>
		/// The main window's task bar progress bar value. Range 0 to 1.
		/// </summary>
		public double WindowProgressBarValue
		{
			get { return _WindowProgressBarValue; }
			set
			{
				if (_WindowProgressBarValue != value)
				{
					_WindowProgressBarValue = value;
					OnPropertyChanged("WindowProgressBarValue");
					OnPropertyChanged("WindowProgressBarVisibility");
					OnPropertyChanged("WindowProgressBarIsIndeterminate");
				}
			}
		}
		/// <summary>
		/// Visibility property for regular progress bars.
		/// </summary>
		public Visibility WindowProgressBarVisibility
		{
			get { return _WindowProgressBarValue > 0 || WindowProgressBarState == System.Windows.Shell.TaskbarItemProgressState.Indeterminate ? Visibility.Visible : Visibility.Collapsed; }
		}
		/// <summary>
		/// Indeterminate property for regular progress bars.
		/// </summary>
		public bool WindowProgressBarIsIndeterminate
		{
			get { return _WindowProgressBarState == System.Windows.Shell.TaskbarItemProgressState.Indeterminate; }
		}

		private string _WindowProgressBarMessage = string.Empty;
		public string WindowProgressBarMessage
		{
			get { return _WindowProgressBarMessage; }
			set
			{
				if (_WindowProgressBarMessage != value)
				{
					_WindowProgressBarMessage = value;
					OnPropertyChanged("WindowProgressBarMessage");
				}
			}
		}


		#endregion // windows progressbar

		#region Commands

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
			return true;
		}

		private void ExecuteAddProject(object parameter)
		{
			string paramString = parameter as string;
			BaseDataNode newProject = new Project(); 
			bool canAdd = AppModelInstanceManager.Instance.CanExecuteAddNode(AppModelInstanceManager.Instance.CurrentModel, newProject);
			if (canAdd)
			{
				AppModelInstanceManager.Instance.ExecuteAddNode(AppModelInstanceManager.Instance.CurrentModel, newProject);
				Infrastructure.Events.OpenView.Publish(newProject);
			}
		}
		#endregion //AddProjectCommand

		#region SaveLayoutCommand

		private const string _dockLayoutSavedFileName = "AvalonDockSavedLayout.config";
		private const string _dockLayoutDefaultFileName = "ValosModeler.Themes.AvalonDockDefaultLayout.config";
//		private const string _dockLayoutDefaultFileName = "Themes.AvalonDockDefaultLayout.config";

		RelayCommand _saveLayoutCommand;
		public ICommand SaveLayoutCommand
		{
			get
			{
				if (_saveLayoutCommand == null)
					_saveLayoutCommand = new RelayCommand(execute => this.ExecuteSaveLayout(execute), canexecute => this.CanExecuteSaveLayout(canexecute));
				return _saveLayoutCommand;
			}
		}

		private bool CanExecuteSaveLayout(object parameter)
		{
			return DockingManager != null;
		}

		private void ExecuteSaveLayout(object parameter)
		{
			var layoutSerializer = new AvalonDock.Layout.Serialization.XmlLayoutSerializer(DockingManager);

			using (var stream = new System.IO.IsolatedStorage.IsolatedStorageFileStream(_dockLayoutSavedFileName, System.IO.FileMode.Create, Infrastructure.IsolatedStorageManager.StorageFile))
			{
				layoutSerializer.Serialize(stream);
			}
		}
#endregion //SaveLayoutCommand

#region LoadSavedLayoutCommand
		RelayCommand _loadSavedLayoutCommand;
		public ICommand LoadSavedLayoutCommand
		{
			get
			{
				if (_loadSavedLayoutCommand == null)
					_loadSavedLayoutCommand = new RelayCommand(execute => this.ExecuteLoadSavedLayout(execute), canexecute => this.CanExecuteLoadSavedLayout(canexecute));
				return _loadSavedLayoutCommand;
			}
		}

		private bool CanExecuteLoadSavedLayout(object parameter)
		{
			var isolatedStorage = Infrastructure.IsolatedStorageManager.StorageFile;
			return DockingManager != null && isolatedStorage.FileExists(_dockLayoutSavedFileName);
		}

		private void ExecuteLoadSavedLayout(object parameter)
		{
			var layoutSerializer = new AvalonDock.Layout.Serialization.XmlLayoutSerializer(DockingManager);
			layoutSerializer.LayoutSerializationCallback += new EventHandler<AvalonDock.Layout.Serialization.LayoutSerializationCallbackEventArgs>(LoadLayoutDeserializationCallBack);

			using (var stream = new System.IO.IsolatedStorage.IsolatedStorageFileStream(_dockLayoutSavedFileName, System.IO.FileMode.Open, Infrastructure.IsolatedStorageManager.StorageFile))
			{
				layoutSerializer.Deserialize(stream);
			}
		}
#endregion //LoadSavedLayoutCommand

#region LoadDefaultLayoutCommand
		RelayCommand _loadDefaultLayoutCommand;
		public ICommand LoadDefaultLayoutCommand
		{
			get
			{
				if (_loadDefaultLayoutCommand == null)
					_loadDefaultLayoutCommand = new RelayCommand(execute => this.ExecuteLoadDefaultLayout(execute), canexecute => this.CanExecuteLoadDefaultLayout(canexecute));
				return _loadDefaultLayoutCommand;
			}
		}

		private bool CanExecuteLoadDefaultLayout(object parameter)
		{
			return true;
		}

		private void ExecuteLoadDefaultLayout(object parameter)
		{
			var layoutSerializer = new AvalonDock.Layout.Serialization.XmlLayoutSerializer(DockingManager);
			layoutSerializer.LayoutSerializationCallback += new EventHandler<AvalonDock.Layout.Serialization.LayoutSerializationCallbackEventArgs>(LoadLayoutDeserializationCallBack);
			//string[] names = this.GetType().Assembly.GetManifestResourceNames();
			var stream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream(_dockLayoutDefaultFileName);
			layoutSerializer.Deserialize(stream);
		}
		#endregion //LoadDefaultLayoutCommand

		#region ImportIFCDataCommand
		RelayCommand _importIFCDataCommand;
		public ICommand ImportIFCDataCommand
		{
			get
			{
				if (_importIFCDataCommand == null)
					_importIFCDataCommand = new RelayCommand(execute => this.ExecuteImportIFCData(execute), canexecute => this.CanExecuteImportIFCData(canexecute));
				return _importIFCDataCommand;
			}
		}

		private bool CanExecuteImportIFCData(object parameter)
		{
			return true;
		}

		private async void ExecuteImportIFCData(object parameter)
		{
			var modelManager = ModelInstanceManager.Instance;
			var ifcPath = GetIfcFileName(null);
			if (!string.IsNullOrEmpty(ifcPath))
			{
				Mouse.OverrideCursor = Cursors.Wait;
				IsEnabled = false;
				StartWindowProgressBar();
				try
				{
					var loaded = await Task.Run(() =>
					{
						try
						{
							return Valos.Ifc.IfcDataLoader.LoadIfcData(ifcPath, WindowProgressBarProgressChangedHandler); // loader does not decide where the ifc model is inserted in the datamodel
						}
						catch 
						{
							return null; }
					});
					if (loaded != null)
					{
						modelManager.CurrentModel.GetDescendantNode<Project>().AddChild(loaded);
						Infrastructure.Events.ModelChanged.Publish(this);
						Infrastructure.Events.NodeSelected.Publish(loaded, this);
					}
				}
				catch
				{
					System.Diagnostics.Debugger.Break();
				}
				finally
				{
					EndWindowProgressBar();
					IsEnabled = true;
					Mouse.OverrideCursor = null;
				}
			}
		}

		private static string GetIfcFileName(string path)
		{
			if (string.IsNullOrEmpty(path))
			{
				Microsoft.Win32.OpenFileDialog fileDlg = new Microsoft.Win32.OpenFileDialog();
				fileDlg.Filter = "IFC Files (*.ifc)|*.ifc|All files (*.*)|*.*";
				fileDlg.DefaultExt = "ifc";
				if (fileDlg.ShowDialog(System.Windows.Application.Current.MainWindow) == true)
				{
					path = fileDlg.FileName;
				}
			}
			if (path != null)
			{
				var fInfo = new FileInfo(path);
				if (fInfo.Exists)
				{
					return path;
				}
			}
			return null;
		}

		#endregion //Import IFC Command

		#region Export IFC Command
		RelayCommand _exportIFCCommand;
		public ICommand ExportIFCCommand
		{
			get
			{
				if (_exportIFCCommand == null)
					_exportIFCCommand = new RelayCommand(execute => this.ExecuteExportIFC(execute));
				return _exportIFCCommand;
			}
		}

		private void ExecuteExportIFC(object parameter)
		{
			Mouse.OverrideCursor = Cursors.Wait;
			string path = parameter as string; // in debug can be like this "E:\\Development\\IFC\\Models\\ValosExportTest.ifc"
			var exporter = new ValosIFCExport.IFCExporter();
			if (path == null)
			{
				Application.Current.Dispatcher.Invoke((Action)delegate { });
				var fileDlg = new Microsoft.Win32.SaveFileDialog();
				fileDlg.Filter = "IFC Files (*.ifc)|*.ifc|All files (*.*)|*.*";
				fileDlg.DefaultExt = "ifc";
				fileDlg.FileName = ModelInstanceManager.Instance.CurrentModel.Name;
				if (fileDlg.ShowDialog(System.Windows.Application.Current.MainWindow) == true)
				{
					path = fileDlg.FileName;
				}
			}
			if(path != null)
				exporter.ExportIfcData(ModelInstanceManager.Instance.CurrentModel, path);

			Mouse.OverrideCursor = null;
		}
#endregion


#endregion //Commands

		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
		}
	}
}
