using Enterprixe.WPF.Tools.Localization;
using Epx.BIM.Plugins;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ValosModeler
{
	/// <summary>
	/// Finds, lists and creates available features.
	/// </summary>
	public class FeatureManager
	{
		internal static List<PluginInfo> _PluginStructures;
		/// <summary>
		/// Initializes a new instance of the <see cref="Parametric3DPlugins"/> class.
		/// </summary>
		public FeatureManager()
		{
			_PluginStructures = new List<PluginInfo>();
			List<string> txtFiles = Directory.EnumerateFiles(System.AppDomain.CurrentDomain.BaseDirectory).ToList();
			this.AddPlugins(txtFiles);
			txtFiles.Clear();
			if (Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\" + App.AppName + "\\Plugins"))
			{
				txtFiles.AddRange(Directory.EnumerateFiles(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\" + App.AppName + "\\Plugins"));
			}
			else
			{
				System.IO.Directory.CreateDirectory(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\" + App.AppName + "\\Plugins");
			}
			this.AddExternalPlugins(txtFiles);

		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="files"></param>
		protected void AddPlugins(List<string> files)
		{
			foreach (string currentFile in files)
			{
				this.AddPluginClasses(currentFile, true);
			}
		}

		protected void AddExternalPlugins(List<string> files)
		{
			foreach (string currentFile in files)
			{
				this.AddPluginClasses(currentFile, false);
			}
		}
		/// <summary>
		/// Creates standard truss tool class in defined assembly
		/// </summary>
		/// <param name="assemblyPath">The assembly path.</param>
		/// <returns>null, if not found</returns>
		protected void AddPluginClasses(string assemblyPath, bool isInternal)
		{
			try
			{
				string extension = System.IO.Path.GetExtension(assemblyPath);
				if (extension.Trim().ToUpper() == ".DLL")
				{
					string assName = System.IO.Path.GetFileNameWithoutExtension(assemblyPath);
					if (isInternal && !assName.Contains("ValosModeler.Features")) return; // to avoid loading all assemblies from the directory
					else if (!isInternal)
					{
						// Check if assembly contains PluginTool based classes.
						// Get the array of runtime assemblies.
						string[] runtimeAssemblies = Directory.GetFiles(System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory(), "*.dll");
						// Create the list of assembly paths consisting of runtime assemblies and the inspected assembly.
						var paths = new List<string>();
						paths.Add(assemblyPath);
						paths.AddRange(Directory.GetFiles(Path.GetDirectoryName(assemblyPath), "*.dll"));
						paths.AddRange(Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, "*.dll"));
						paths.AddRange(Directory.GetFiles(System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory().Replace("NETCore", "WindowsDesktop"), "*.dll"));
//						paths.AddRange(Directory.GetFiles(Path.Combine(System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory(), "WPF"), "*.dll"));
						paths.AddRange(runtimeAssemblies);
						// Create PathAssemblyResolver that can resolve assemblies using the created list.
						var resolver = new System.Reflection.PathAssemblyResolver(paths);
						using (var mlc = new System.Reflection.MetadataLoadContext(resolver))
						{
							try
							{
								// Load assembly into MetadataLoadContext. Does not load into memory.
								System.Reflection.Assembly reflectionAssembly = mlc.LoadFromAssemblyPath(assemblyPath);
								Type pluginType = typeof(PluginTool);
								if (!reflectionAssembly.GetExportedTypes().Any(et => et.BaseType != null &&
								et.BaseType.FullName == pluginType.FullName || (et.BaseType != null && et.BaseType.BaseType != null && et.BaseType.BaseType.FullName == pluginType.FullName)))
									return;
							}
							catch 
							{ return; }
						}
					}

					//string fileName = System.IO.Path.ChangeExtension(assemblyPath, null);
					System.Reflection.Assembly assInstance = null;
					try
					{
						assInstance = System.Reflection.Assembly.LoadFrom(assemblyPath); // if an assembly is loaded it won't be loaded again
					}
					catch (FileLoadException)
					{
						// downloaded assemblies need to be unblocked in windows, show msgbox?
						MessageBox.Show(Application.Current.MainWindow, CultureManager.GetLocalizedString("Unable to load plugin. Make sure the file is not blocked by the operating system."), App.AppName, MessageBoxButton.OK, MessageBoxImage.Information);
					}

					//var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies();
					//Getting the assemblies referenced by another assembly:
					//var referencedAssemblies = someAssembly.GetReferencedAssemblies();
					//assInstance = loadedAssemblies.Where(a => a.GetName().Name == assName).FirstOrDefault();
					//if (assInstance == null) // assembly not loaded
					//{
					//    // if an assembly is loaded it won't be loaded again
					//    assInstance = System.Reflection.Assembly.LoadFrom(assemblyPath);
					//}

					if (assInstance != null)
					{
						System.Type[] toolTypes = assInstance.GetExportedTypes();
						if (toolTypes.Any())
						{
							bool assAdded = false;
							foreach (System.Type tt in toolTypes)
							{
								System.Type baseType = tt.BaseType;
								bool isCorrect = false;
								while (!isCorrect && baseType != null)
								{
									isCorrect = (baseType.Name == typeof(PluginTool).Name);
									if (!isCorrect) baseType = baseType.BaseType;
								}
								if (isCorrect)
								{
									PluginTool retVal = GetPluginTool(tt,null);
									if (retVal != null)
									{
										if (!assAdded)
										{
											string assemblyResxName = null;
											foreach (var t in assInstance.DefinedTypes)
											{
												if (t.FullName.Contains(".Strings.Strings"))
												{
													var split = t.FullName.Split(new string[] { ".Strings.Strings" }, StringSplitOptions.None);
													assemblyResxName = split[0] + ".Strings.Strings";
													break;
												}
											}
											if (!string.IsNullOrEmpty(assemblyResxName))
											{
												CultureManager.AddResourceDictionary(assemblyResxName);
												assAdded = true;
											}
										}

										retVal.CurrentUICulture = CultureManager.UICulture;
										foreach (List<string> startAttribs in retVal.GetStartAttributes)
										{
											retVal.StartAttributes = startAttribs;
											PluginInfo pp = new PluginInfo();
											pp.AssemblyName = assemblyPath;
											pp.AcceptedMasterType = retVal.AcceptedMasterNodeType;
											pp.FullClassName = tt.FullName;
											pp.NameForMenu = CultureManager.GetLocalizedString(retVal.NameForMenu);
											pp.PluginType = tt;
											pp.MenuToolTip = retVal.MenuToolTip is string ? CultureManager.GetLocalizedString(retVal.MenuToolTip as string) : retVal.MenuToolTip;
											pp.IconImageSource = retVal.IconImageSource;
											pp.AllowedViewModes = retVal.AllowedViewModes;
											pp.SupportsEditMode = retVal.SupportsEditMode;
											pp.IsContinuous = retVal.IsContinuous;
											pp.StartAttributes = startAttribs;
											_PluginStructures.Add(pp);
										}
									}
								}
							}
						}
						assInstance = null;
					}
				}
			}
			catch (Exception)
			{
			}
		}

		/// <summary>
		/// Gets the plugins.
		/// </summary>
		/// <value>The plugins.</value>
		public static List<PluginInfo> Features
		{
			get { return _PluginStructures; }
		}

		/// <summary>
		/// Gets the plugin tool.
		/// </summary>
		/// <param name="assemblyName">Name of the assembly.</param>
		/// <param name="fullClassName">Full name of the class.</param>
		/// <returns>Parametric3DPluginTool.</returns>
		public static PluginTool GetPluginTool(string assemblyName, string fullClassName, List<string> startAttribues)
		{
			System.Reflection.Assembly assInstance = null;
			try
			{
				assInstance = System.Reflection.Assembly.Load(assemblyName);
			}
			catch { }
			if (assInstance == null)//external plugin
			{
				string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\" + App.AppName + "\\Plugins";
				int charLocation = assemblyName.IndexOf(", ", StringComparison.Ordinal);
				string assemblyFileName = "";
				if (charLocation > 0)
				{
					assemblyFileName = assemblyName.Substring(0, charLocation) + ".dll";
				}
				string fileInPersonalFolder = Path.Combine(folderPath, assemblyFileName);
				if (!File.Exists(fileInPersonalFolder)) return null;
				{
					assInstance = System.Reflection.Assembly.LoadFrom(fileInPersonalFolder);
				}
			}
			if (assInstance != null)
			{
				System.Type toolType = assInstance.GetType(fullClassName, false);
				if (toolType != null)
				{
					return GetPluginTool(toolType, startAttribues);
				}
			}
			return null;
		}

		/// <summary>
		/// Gets the plugin tool.
		/// </summary>
		/// <param name="type">The type.</param>
		/// <returns>Parametric3DPluginTool.</returns>
		public static PluginTool GetPluginTool(Type type, List<string> startAttributes)
		{
			object luokka = Activator.CreateInstance(type);
			PluginTool retVal = luokka as PluginTool;
			if (retVal is PluginTool)
			{
				retVal.CurrentUICulture = CultureManager.UICulture;
				retVal.StartAttributes = startAttributes;
				return retVal;
			}
			else return null;
		}
	}
}
