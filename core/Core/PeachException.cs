
//
// Copyright (c) Michael Eddington
//
// Permission is hereby granted, free of charge, to any person obtaining a copy 
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights 
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell 
// copies of the Software, and to permit persons to whom the Software is 
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in	
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
//

// Authors:
//   Michael Eddington (mike@dejavusecurity.com)

// $Id$

using System;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;

namespace Peach.Core
{
	/// <summary>
	/// Unrecoverable error.  Causes Peach to exit with an error
	/// message, but no stack trace.
	/// </summary>
	[Serializable]
	public class PeachException : ApplicationException
	{
		public PeachException(string message)
			: base(message)
		{
		}

		public PeachException(string message, Exception innerException)
			: base(message, innerException)
		{
		}

		protected PeachException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}
	}

	/// <summary>
	/// Thrown to stop current test case and move to next.
	/// </summary>
	/// <remarks>
	/// Used to indicate an error that should stop the current test case, but not the fuzzing job.
	/// </remarks>
	[Serializable]
	public class SoftException : ApplicationException
	{
		public SoftException(string message, Exception innerException)
			: base(message, innerException)
		{
		}

		public SoftException(string message)
			: base(message)
		{
		}

		public SoftException(Exception innerException)
			: base(innerException.Message, innerException)
		{
		}

		protected SoftException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}
	}

	/// <summary>
	/// Thrown when peach catches an exception from an agent.
	/// </summary>
	[Serializable]
	public class AgentException : ApplicationException
	{
		public AgentException(string message, Exception innerException)
			: base(message, innerException)
		{
		}

		public AgentException(string message)
			: base(message)
		{
		}

		public AgentException(Exception innerException)
			: base(innerException.Message, innerException)
		{
		}

		protected AgentException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}
	}

	/// <summary>
	/// Thrown to indicate a fault has occured.
	/// </summary>
	/// <remarks>
	/// This exception can be thrown by Publishers or Scripting code to
	/// indicate a fault has occured.  The exception extends from
	/// SoftException, so normal cleanup code will run when this exception
	/// is thrown.
	/// </remarks>
	[Serializable]
	public class FaultException : SoftException
	{
		/// <summary>
		/// One line title of fault
		/// </summary>
		public string Title;

		/// <summary>
		/// Description field of fault
		/// </summary>
		public string Description;

		/// <summary>
		/// Major hash of fault.
		/// </summary>
		public string MajorHash;

		/// <summary>
		/// Minor hash of fault
		/// </summary>
		public string MinorHash;

		/// <summary>
		/// Exploitability of fault
		/// </summary>
		public string Exploitablity = "Unknown";

		/// <summary>
		/// Detection source for fault, typically the class name
		/// </summary>
		/// For the Rest publisher the detection name is the name attribute while
		/// detection source is the Publisher attribute.
		public string DetectionSource = "Unknown";

		/// <summary>
		/// Name of detection source
		/// </summary>
		/// <remarks>
		/// For the Rest publisher the detection name is the name attribute while
		/// detection source is the Publisher attribute.
		/// </remarks>
		public string DetectionName = "Unknown";

		/// <summary>
		/// Name of agent fault was reported by.
		/// </summary>
		/// <remarks>
		/// Only used when fault generated via agent, otherwise null.
		/// </remarks>
		public string AgentName = "Internal";

		/// <summary>
		/// Constructor for use in Publishers or when base class sets detectionSource
		/// </summary>
		/// <remarks>
		/// Expects base class to set detectionSource
		/// </remarks>
		/// <param name="title">Title of fault</param>
		/// <param name="description">Description of fault</param>
		/// <param name="majorHash">Major hash for fault. Set to null or empty string to skip bucketing.</param>
		/// <param name="minorHash">Minor hash for fault. Set to null or empty string to skip bucketing.</param>
		/// <param name="exploitability">Exploitability for fault</param>
		public FaultException(string title, string description, string majorHash, string minorHash, string exploitability)
			: base("Fault: " + title)
		{
			Title = title;
			Description = description;
			MajorHash = majorHash;
			MinorHash = minorHash;
			Exploitablity = exploitability;
		}

		/// <summary>
		/// Constructor for non-agents
		/// </summary>
		/// <param name="title">Title of fault</param>
		/// <param name="description">Description of fault</param>
		/// <param name="majorHash">Major hash for fault. Set to null or empty string to skip bucketing.</param>
		/// <param name="minorHash">Minor hash for fault. Set to null or empty string to skip bucketing.</param>
		/// <param name="exploitability">Exploitability for fault</param>
		/// <param name="detectionSource">Detection source. For Publishers set to publisher attribute name.</param>
		/// <param name="detectionName">Detection source. For Publishers set to name attribute.</param>
		public FaultException(string title, string description, string majorHash, string minorHash, string exploitability, string detectionSource, string detectionName)
			: base("Fault: " + title)
		{
			Title = title;
			Description = description;
			MajorHash = majorHash;
			MinorHash = minorHash;
			Exploitablity = exploitability;
			DetectionSource = detectionSource;
			DetectionName = detectionName;
		}

		/// <summary>
		/// Constructor for agents
		/// </summary>
		/// <param name="title">Title of fault</param>
		/// <param name="description">Description of fault</param>
		/// <param name="majorHash">Major hash for fault. Set to null or empty string to skip bucketing.</param>
		/// <param name="minorHash">Minor hash for fault. Set to null or empty string to skip bucketing.</param>
		/// <param name="exploitability">Exploitability for fault</param>
		/// <param name="detectionSource">Detection source, such as Monitor class attribute.</param>
		/// <param name="detectionName">Detection name, such as name attribute</param>
		/// <param name="agentName">Name of agent fault was reported by</param>
		public FaultException(string title, string description, string majorHash, string minorHash, string exploitability, string detectionSource, string detectionName, string agentName)
			: base("Fault: " + title)
		{
			Title = title;
			Description = description;
			MajorHash = majorHash;
			MinorHash = minorHash;
			Exploitablity = exploitability;
			DetectionSource = detectionSource;
			DetectionName = detectionName;
			AgentName = agentName;
		}

		/// <summary>
		/// Compute the hash of a value for use as either
		/// the MajorHash or MinorHash.
		/// </summary>
		/// <param name="value">String value to hash</param>
		/// <returns>The first 4 bytes of the md5 has as a hex string</returns>
		public static string Hash(string value)
		{
			using (var md5 = MD5.Create())
			{
				const int hashLen = 4;

				var data = md5.ComputeHash(Encoding.UTF8.GetBytes(value));
				var sb = new StringBuilder(hashLen * 2);

				for (var i = 0; i < hashLen; i++)
					sb.Append(data[i].ToString("X2"));

				return sb.ToString();
			}
		}

	}
}

// end
