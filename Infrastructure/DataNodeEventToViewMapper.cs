using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ValosModeler.Infrastructure
{
	public class DataNodeEventToViewMapper
	{
		private static DataNodeEventToViewMapper _instance;
		private static bool _instanceInitializing = false;
		/// <summary>
		/// Gets the instance. Will return null if attempted while the instance is being initialized to prevent infinite loop.
		/// </summary>
		/// <value>The instance.</value>
		public static DataNodeEventToViewMapper Instance
		{
			get
			{
				if (_instance == null && !_instanceInitializing)
				{
					_instanceInitializing = true;
					_instance = new DataNodeEventToViewMapper();
					_instanceInitializing = false;
				}
				return _instance;
			}
		}

		/// <summary>
		/// Maps Event + Node combinations to views and viewmodels.
		/// </summary>
		private DataNodeEventToViewMapper()
		{
			Views = new Dictionary<Tuple<Type, Type>, Type>();
			ViewModels = new Dictionary<Tuple<Type, Type>, Type>();
		}

		public void Register<TEvent, TNode>(Type view, Type viewModel)
		{
			if (!Views.ContainsKey(Tuple.Create(typeof(TEvent), typeof(TNode))))
			{
				Views.Add(Tuple.Create(typeof(TEvent), typeof(TNode)), view);
				ViewModels.Add(Tuple.Create(typeof(TEvent), typeof(TNode)), viewModel);
			}
			else
			{
				Debug.Fail("DataNodeEventToViewMapper: Model node + Event type has already been registered with the mapper.");
			}
		}

		public void Register<TEvent, TNode>(Type view)
		{
			if (!Views.ContainsKey(Tuple.Create(typeof(TEvent), typeof(TNode))))
			{
				Views.Add(Tuple.Create(typeof(TEvent), typeof(TNode)), view);
				ViewModels.Add(Tuple.Create(typeof(TEvent), typeof(TNode)), null);
			}
			else
			{
				Debug.Fail("DataNodeEventToViewMapper: Model node + Event type has already been registered with the mapper.");
			}
		}

		private Dictionary<Tuple<Type, Type>, Type> Views
		{
			get;
			set;
		}

		private Dictionary<Tuple<Type, Type>, Type> ViewModels
		{
			get;
			set;
		}

		/// <summary>
		/// Gets the view Type for the key. Checks base class registration also.
		/// </summary>
		/// <param name="key"></param>
		/// <param name="view"></param>
		/// <returns></returns>
		public bool GetView(Tuple<Type, Type> key, out Type view, bool checkBaseClasses = true)
		{
			Type value;

			if (Views.TryGetValue(key, out value))
			{
				view = value;
				return true;
			}
			else if (checkBaseClasses)
			{
				// check if base classes if they are registered
				foreach (var tuplekey in Views.Keys)
				{
					if (tuplekey.Item1 == key.Item1)
					{
						if (tuplekey.Item2.IsAssignableFrom(key.Item2))
						{
							view = Views[tuplekey];
							return true;
						}
					}
				}
			}

			view = null;
			return false;
		}
		/// <summary>
		/// Gets all views for the key. Optionally for base types also.
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		public List<Type> GetViews(Tuple<Type, Type> key, bool acceptBaseTypeMatch = true)
		{
			List<Type> allViews = new List<Type>();
			if (!acceptBaseTypeMatch && Views.TryGetValue(key, out Type value)) allViews.Add(value);
			// check if base classes if they are registered
			if (acceptBaseTypeMatch)
			{
				foreach (var tuplekey in Views.Keys)
				{
					if (tuplekey.Item1 == key.Item1)
					{
						if (tuplekey.Item2.IsAssignableFrom(key.Item2)) // this finds equal types also
						{
							allViews.Add(Views[tuplekey]);
						}
					}
				}
			}
			return allViews;
		}
		/// <summary>
		/// Gets the viewmodel Type for the key. Checks base class registration also.
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		public Type GetViewModel(Tuple<Type, Type> key, bool checkBaseClasses = true)
		{
			if (ViewModels.TryGetValue(key, out Type value))
			{
				return value;
			}
			else if (checkBaseClasses)
			{
				// check if base classes if they are registered
				foreach (var tuplekey in ViewModels.Keys)
				{
					if (tuplekey.Item1 == key.Item1)
					{
						if (tuplekey.Item2.IsAssignableFrom(key.Item2))
						{
							return ViewModels[tuplekey];
						}
					}
				}
			}

			return value;
		}
		/// <summary>
		/// Gets all viewmodels for the key.
		/// </summary>
		/// <param name="key"></param>
		/// <param name="acceptBaseTypeMatch"></param>
		/// <returns></returns>
		public List<Type> GetViewModels(Tuple<Type, Type> key, bool acceptBaseTypeMatch = true)
		{
			List<Type> allViews = new List<Type>();

			if (!acceptBaseTypeMatch && ViewModels.TryGetValue(key, out Type value))
			{
				allViews.Add(value);
			}
			if (acceptBaseTypeMatch)
			{
				// check if base classes if they are registered
				foreach (var tuplekey in ViewModels.Keys)
				{
					if (tuplekey.Item1 == key.Item1)
					{
						if (tuplekey.Item2.IsAssignableFrom(key.Item2))
						{
							allViews.Add(ViewModels[tuplekey]);
						}
					}
				}
			}

			return allViews;
		}

		public bool ContainsKey(Tuple<Type, Type> key)
		{
			return Views.ContainsKey(key);
		}

		/// <summary>
		/// Get the Node type registered with this view type.
		/// </summary>
		/// <param name="viewType"></param>
		/// <returns></returns>
		public Type GetNodeType(Type viewType)
		{
			foreach (var value in Views)
			{
				if (value.Value == viewType)
				{
					return value.Key.Item2;
				}
			}

			return null;
		}
	}
}
