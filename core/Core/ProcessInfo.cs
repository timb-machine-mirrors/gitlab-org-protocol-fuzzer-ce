using System.Diagnostics;

namespace Peach.Core
{
	/// <summary>
	/// Helper class to get information on a process.  The built in 
	/// methods on mono aren't 100% implemented.
	/// </summary>
	
	public interface IProcessInfo
	{
		/// <summary>
		/// Returns a populated ProcessInfo instance.
		/// throws ArgumentException if the Process is not valid.
		/// </summary>
		/// <param name="p">Process to obtain info about.</param>
		/// <returns>Information about the process.</returns>
		ProcessInfo Snapshot(Process p);

		/// <summary>
		/// Returns a list of Processes that match the current name.
		/// </summary>
		/// <remarks>
		/// this works around mono compatibility issues on linux/osx.
		/// </remarks>
		/// <param name="name">Name of process.</param>
		/// <returns>List of processes.</returns>
		Process[] GetProcessesByName(string name);

		/// <summary>
		/// Kill the process and wait indefinitely for the process to exit.
		/// </summary>
		/// <remarks>
		/// this works around mono compatibility issues on linux/osx.
		/// </remarks>
		/// <param name="p">Process to kill.</param>
		void Kill(Process p);

		/// <summary>
		/// Kill the process and wait the specified time for the process to exit.
		/// </summary>
		/// <remarks>
		/// this works around mono compatibility issues on linux/osx.
		/// </remarks>
		/// <param name="p">Process to kill.</param>
		/// <param name="milliseconds">The amount of time to wait for the process to exit.</param>
		/// <returns>True if process was killed, false if it didn't exit within the timeout.</returns>
		bool Kill(Process p, int milliseconds);
	}

	public class ProcessInfo : StaticPlatformFactory<IProcessInfo>
	{
		public int Id;
		public string ProcessName;
		public bool Responding;

		public ulong TotalProcessorTicks;
		public ulong UserProcessorTicks;
		public ulong PrivilegedProcessorTicks;

		public long PeakVirtualMemorySize64;
		public long PeakWorkingSet64;
		public long PrivateMemorySize64;
		public long VirtualMemorySize64;
		public long WorkingSet64;
	}
}
