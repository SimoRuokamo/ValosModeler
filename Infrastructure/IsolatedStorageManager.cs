using System;
using System.Collections.Generic;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ValosModeler.Infrastructure
{
	public static class IsolatedStorageManager
	{
		static IsolatedStorageFile _storageFile;

		public static IsolatedStorageFile StorageFile
		{
			get
			{
				try
				{
#if DEBUG
					_storageFile = IsolatedStorageFile.GetUserStoreForAssembly();
#else
					_storageFile = IsolatedStorageFile.GetUserStoreForApplication();
#endif
				}
				catch (IsolatedStorageException)
				{
					_storageFile = IsolatedStorageFile.GetUserStoreForAssembly();
				}
				return _storageFile;
			}
		}
	}
}
