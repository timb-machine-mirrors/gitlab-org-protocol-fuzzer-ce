//
// Process.h
//
// $Id: //poco/1.4/Foundation/include/Poco/Process.h#4 $
//
// Library: Foundation
// Package: Processes
// Module:  Process
//
// Definition of the Process class.
//
// Copyright (c) 2004-2006, Applied Informatics Software Engineering GmbH.
// and Contributors.
//
// Permission is hereby granted, free of charge, to any person or organization
// obtaining a copy of the software and accompanying documentation covered by
// this license (the "Software") to use, reproduce, display, distribute,
// execute, and transmit the Software, and to prepare derivative works of the
// Software, and to permit third-parties to whom the Software is furnished to
// do so, all subject to the following:
// 
// The copyright notices in the Software and this entire statement, including
// the above license grant, this restriction and the following disclaimer,
// must be included in all copies of the Software, in whole or in part, and
// all derivative works of the Software, unless such copies or derivative
// works are solely in the form of machine-executable object code generated by
// a source language processor.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE, TITLE AND NON-INFRINGEMENT. IN NO EVENT
// SHALL THE COPYRIGHT HOLDERS OR ANYONE DISTRIBUTING THE SOFTWARE BE LIABLE
// FOR ANY DAMAGES OR OTHER LIABILITY, WHETHER IN CONTRACT, TORT OR OTHERWISE,
// ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.
//


#ifndef Foundation_Process_INCLUDED
#define Foundation_Process_INCLUDED


#include "Poco/Foundation.h"


#if defined(POCO_OS_FAMILY_WINDOWS) && defined(POCO_WIN32_UTF8)
#if defined(_WIN32_WCE)
#include "Process_WINCE.h"
#else
#include "Poco/Process_WIN32U.h"
#endif
#elif defined(POCO_OS_FAMILY_WINDOWS)
#include "Poco/Process_WIN32.h"
#elif defined(POCO_VXWORKS)
#include "Poco/Process_VX.h"
#elif defined(POCO_OS_FAMILY_UNIX)
#include "Poco/Process_UNIX.h"
#else
#include "Poco/Process_VMS.h"
#endif


namespace Poco {


class Pipe;


class Foundation_API ProcessHandle
	/// A handle for a process created with Process::launch().
	///
	/// This handle can be used to determine the process ID of
	/// the newly created process and it can be used to wait for
	/// the completion of a process.
{
public:
	typedef ProcessImpl::PIDImpl PID;

	ProcessHandle(const ProcessHandle& handle);
		/// Creates a ProcessHandle by copying another one.
		
	~ProcessHandle();
		/// Destroys the ProcessHandle.
		
	ProcessHandle& operator = (const ProcessHandle& handle);
		/// Assigns another handle.
		
	PID id() const;
		/// Returns the process ID.
		
	int wait() const;
		/// Waits for the process to terminate
		/// and returns the exit code of the process.
		
protected:
	ProcessHandle(ProcessHandleImpl* pImpl);
	
private:
	ProcessHandle();

	ProcessHandleImpl* _pImpl;
	
	friend class Process;
};


class Foundation_API Process: public ProcessImpl
	/// This class provides methods for working with processes.
{
public:
	typedef PIDImpl  PID;
	typedef ArgsImpl Args;
	typedef EnvImpl  Env;
	
	static PID id();
		/// Returns the process ID of the current process.
		
	static void times(long& userTime, long& kernelTime);
		/// Returns the number of seconds spent by the
		/// current process in user and kernel mode.
		
	static ProcessHandle launch(const std::string& command, const Args& args);
		/// Creates a new process for the given command and returns
		/// a ProcessHandle of the new process. The given arguments are
		/// passed to the command on the command line.

	static ProcessHandle launch(
		const std::string& command, 
		const Args& args, 
		const std::string& initialDirectory);
		/// Creates a new process for the given command and returns
		/// a ProcessHandle of the new process. The given arguments are
		/// passed to the command on the command line.
		/// The process starts executing in the specified initial directory.

	static ProcessHandle launch(
		const std::string& command, 
		const Args& args, 
		Pipe* inPipe, 
		Pipe* outPipe, 
		Pipe* errPipe);
		/// Creates a new process for the given command and returns
		/// a ProcessHandle of the new process. The given arguments are
		/// passed to the command on the command line.
		///
		/// If inPipe, outPipe or errPipe is non-null, the corresponding
		/// standard input, standard output or standard error stream
		/// of the launched process is redirected to the Pipe.
		/// PipeInputStream or PipeOutputStream can be used to
		/// send receive data from, or send data to the process.
		///
		/// Note: the same Pipe can be used for both outPipe and errPipe.
		///
		/// After a Pipe has been passed as inPipe, only write operations
		/// are valid. After a Pipe has been passed as outPipe or errPipe,
		/// only read operations are valid.
		///
		/// It is forbidden to pass the same pipe as inPipe and outPipe or errPipe.
		///
		/// Usage example:
		///     Pipe outPipe;
		///     Process::Args args;
		///     ProcessHandle ph(launch("/bin/ps", args, 0, &outPipe, 0));
		///     PipeInputStream istr(outPipe);
		///     ... // read output of ps from istr
		///     int rc = ph.wait();

	static ProcessHandle launch(
		const std::string& command, 
		const Args& args, 
		const std::string& initialDirectory,
		Pipe* inPipe, 
		Pipe* outPipe, 
		Pipe* errPipe);
		/// Creates a new process for the given command and returns
		/// a ProcessHandle of the new process. The given arguments are
		/// passed to the command on the command line.
		/// The process starts executing in the specified initial directory.
		///
		/// If inPipe, outPipe or errPipe is non-null, the corresponding
		/// standard input, standard output or standard error stream
		/// of the launched process is redirected to the Pipe.
		/// PipeInputStream or PipeOutputStream can be used to
		/// send receive data from, or send data to the process.
		///
		/// Note: the same Pipe can be used for both outPipe and errPipe.
		///
		/// After a Pipe has been passed as inPipe, only write operations
		/// are valid. After a Pipe has been passed as outPipe or errPipe,
		/// only read operations are valid.
		///
		/// It is forbidden to pass the same pipe as inPipe and outPipe or errPipe.
		///
		/// Usage example:
		///     Pipe outPipe;
		///     Process::Args args;
		///     ProcessHandle ph(launch("/bin/ps", args, 0, &outPipe, 0));
		///     PipeInputStream istr(outPipe);
		///     ... // read output of ps from istr
		///     int rc = ph.wait();
		
	static ProcessHandle launch(
		const std::string& command, 
		const Args& args, 
		Pipe* inPipe, 
		Pipe* outPipe, 
		Pipe* errPipe,
		const Env& env);
		/// Creates a new process for the given command and returns
		/// a ProcessHandle of the new process. The given arguments are
		/// passed to the command on the command line.
		///
		/// If inPipe, outPipe or errPipe is non-null, the corresponding
		/// standard input, standard output or standard error stream
		/// of the launched process is redirected to the Pipe.
		///
		/// The launched process is given the specified environment variables.

	static ProcessHandle launch(
		const std::string& command, 
		const Args& args, 
		const std::string& initialDirectory,
		Pipe* inPipe, 
		Pipe* outPipe, 
		Pipe* errPipe,
		const Env& env);
		/// Creates a new process for the given command and returns
		/// a ProcessHandle of the new process. The given arguments are
		/// passed to the command on the command line.
		/// The process starts executing in the specified initial directory.
		/// If inPipe, outPipe or errPipe is non-null, the corresponding
		/// standard input, standard output or standard error stream
		/// of the launched process is redirected to the Pipe.
		/// The launched process is given the specified environment variables.

	static int wait(const ProcessHandle& handle);
		/// Waits for the process specified by handle to terminate
		/// and returns the exit code of the process.
		
	static void kill(ProcessHandle& handle);
		/// Kills the process specified by handle.
		///
		/// This is preferable on Windows where process IDs
		/// may be reused.

	static void kill(PID pid);
		/// Kills the process with the given pid.
		
	static void requestTermination(PID pid);
		/// Requests termination of the process with the give PID.
		///
		/// On Unix platforms, this will send a SIGINT to the
		/// process and thus work with arbitrary processes.
		///
		/// On other platforms, a global event flag
		/// will be set. Setting the flag will cause 
		/// Util::ServerApplication::waitForTerminationRequest() to
		/// return. Therefore this will only work with applications
		/// based on Util::ServerApplication.
};


//
// inlines
//
inline Process::PID Process::id()
{
	return ProcessImpl::idImpl();
}

	
inline void Process::times(long& userTime, long& kernelTime)
{
	ProcessImpl::timesImpl(userTime, kernelTime);
}


} // namespace Poco


#endif // Foundation_Process_INCLUDED
