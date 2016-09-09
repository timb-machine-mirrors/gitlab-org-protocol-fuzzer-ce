using System;
using System.IO;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using Mono.Unix;
using Mono.Unix.Native;

namespace Peach.Pro.Core.OS.Unix
{
	internal class SingleInstanceImpl : ISingleInstance
	{
		private static readonly Regex SanitizerRegex = new Regex(@"[\\/]");

		private readonly string _name;

		private int _fd;
		private bool _locked;

		public SingleInstanceImpl(string name)
		{
			_name = Path.Combine(Path.GetTempPath(), SanitizerRegex.Replace(name, "_"));
			_fd = Syscall.open(_name, OpenFlags.O_RDWR | OpenFlags.O_CREAT, FilePermissions.DEFFILEMODE);
			UnixMarshal.ThrowExceptionForLastErrorIf(_fd);
		}

		public void Dispose()
		{
			lock (_name)
			{
				if (_fd == -1)
					return;

				if (_locked)
				{
					var flock = new Flock { l_type = LockType.F_UNLCK };
					Syscall.fcntl(_fd, FcntlCommand.F_SETLK, ref flock);
					_locked = false;
				}

				Syscall.close(_fd);
				_fd = -1;

				Syscall.unlink(_name);
			}
		}

		public bool TryLock()
		{
			lock (_name)
			{
				if (_fd == -1)
					throw new ObjectDisposedException("SingleInstanceImpl");

				if (_locked)
					return true;

				var flock = new Flock { l_type = LockType.F_WRLCK };

				var ret = Syscall.fcntl(_fd, FcntlCommand.F_SETLK, ref flock);

				if (ret == -1)
				{
					// If EACCES or EAGAIN means the lock is held
					var errno = Stdlib.GetLastError();

					if (errno != Errno.EACCES && errno != Errno.EAGAIN)
						UnixMarshal.ThrowExceptionForError(errno);

					_locked = false;
				}
				else
				{
					_locked = true;
				}

				return _locked;
			}
		}

		public void Lock()
		{
			lock (_name)
			{
				if (_fd == -1)
					throw new ObjectDisposedException("SingleInstanceImpl");

				if (_locked)
					return;

				while (true)
				{
					var flock = new Flock { l_type = LockType.F_WRLCK };

					var ret = Syscall.fcntl(_fd, FcntlCommand.F_SETLKW, ref flock);

					Errno error;
					if (UnixMarshal.ShouldRetrySyscall(ret, out error))
						continue;

					UnixMarshal.ThrowExceptionForErrorIf(ret, error);

					_locked = true;
					return;
				}
			}
		}
	}
}
