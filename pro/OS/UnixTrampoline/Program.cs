using Mono.Unix;
using Mono.Unix.Native;

namespace PeachTrampoline
{
	public class Program
	{
		static int Main(string[] args)
		{
			var ret = Syscall.fcntl(3, FcntlCommand.F_SETFD, 1);
			UnixMarshal.ThrowExceptionForLastErrorIf(ret);
	
			ret = Syscall.execvp(args[0], args);
			UnixMarshal.ThrowExceptionForLastErrorIf(ret);

			return 0;
		}
	}
}
