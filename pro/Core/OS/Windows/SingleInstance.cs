using System;
using System.Threading;
using Peach.Core;

namespace Peach.Pro.OS.Windows
{
	[PlatformImpl(Platform.OS.Windows)]
	public class SingleInstanceImpl : SingleInstance
	{
		object obj;
		Mutex mutex;
		bool locked;

		public SingleInstanceImpl(string name)
		{
			locked = false;
			obj = new object();
			var safeName = name.Replace("\\", "_").Replace("/", "_");
			mutex = new Mutex(false, "Global\\" + safeName);
		}

		public override void Dispose()
		{
			lock (obj)
			{
				if (mutex != null)
				{
					if (locked)
					{
						mutex.ReleaseMutex();
						locked = false;
					}

					mutex.Dispose();
					mutex = null;
				}
			}
		}

		public override bool TryLock()
		{
			lock (obj)
			{
				if (mutex == null)
					throw new ObjectDisposedException("SingleInstanceImpl");

				if (locked)
					return true;

				try
				{
					locked = mutex.WaitOne(0);
					return locked;
				}
				catch (AbandonedMutexException)
				{
					return TryLock();
				}
			}
		}

		public override void Lock()
		{
			lock (obj)
			{
				if (mutex == null)
					throw new ObjectDisposedException("SingleInstanceImpl");

				if (locked)
					return;

				try
				{
					mutex.WaitOne();
					locked = true;
				}
				catch (AbandonedMutexException)
				{
					Lock();
				}
			}
		}
	}
}
