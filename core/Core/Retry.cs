using System;
using System.Threading;

namespace Peach.Core
{
	public class Retry
	{
		public static readonly TimeSpan DefaultRetryDelay = TimeSpan.FromSeconds(0.5);
		public const int DefaultRetryCount = 3;

		public static void Execute(Action fn, int retryCount = DefaultRetryCount)
		{
			Execute(fn, DefaultRetryDelay, retryCount);
		}

		public static void Execute(Action fn, TimeSpan retryDelay, int retryCount = DefaultRetryCount)
		{
			int count = 0;
			while (true)
			{
				try
				{
					fn();
					break;
				}
				catch (Exception ex)
				{
					if (count++ == retryCount)
						throw ex;
					Thread.Sleep(retryDelay);
				}
			}
		}
	}
}
