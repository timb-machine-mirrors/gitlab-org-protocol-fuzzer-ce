
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
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using NLog;
using Peach.Core;
using Peach.Core.IO;
using Encoding = Peach.Core.Encoding;
using Logger = NLog.Logger;

namespace Peach.Pro.Core.Publishers
{
	[Publisher("Http")]
	[Parameter("Method", typeof(string), "Method type")]
	[Parameter("Url", typeof(string), "Url")]
	[Parameter("BaseUrl", typeof(string), "Optional BaseUrl for authentication", "")]
	[Parameter("Username", typeof(string), "Optional username for authentication", "")]
	[Parameter("Password", typeof(string), "Optional password for authentication", "")]
	[Parameter("Domain", typeof(string), "Optional domain for authentication", "")]
	[Parameter("Cookies", typeof(bool), "Track cookies (defaults to true)", "true")]
	[Parameter("CookiesAcrossIterations", typeof(bool), "Track cookies across iterations (defaults to false)", "false")]
	[Parameter("Timeout", typeof(int), "How many milliseconds to wait for data/connection (default 3000)", "3000")]
	[Parameter("IgnoreCertErrors", typeof(bool), "Allow https regardless of cert status (defaults to true)", "true")]
	public class HttpPublisher : Peach.Core.Publishers.BufferedStreamPublisher
	{
		private static readonly Logger logger = LogManager.GetCurrentClassLogger();
		protected override Logger Logger { get { return logger; } }

		public string Url { get; protected set; }
		public string Method { get; protected set; }
		public string Username { get; protected set; }
		public string Password { get; protected set; }
		public string Domain { get; protected set; }
		public string BaseUrl { get; protected set; }
		public bool Cookies { get; protected set; }
		public bool CookiesAcrossIterations { get; protected set; }
		public bool IgnoreCertErrors { get; protected set; }

		protected CookieContainer CookieJar = new CookieContainer();
		protected HttpWebResponse Response { get; set; }
		protected string Query { get; set; }
		protected Dictionary<string, string> Headers = new Dictionary<string, string>();
		protected CredentialCache Credentials = null;

		public HttpPublisher(Dictionary<string, Variant> args)
			: base(args)
		{
			if (!string.IsNullOrWhiteSpace(Username) && !string.IsNullOrWhiteSpace(Password))
			{
				var baseUrl = new Uri(Url);

				if (!string.IsNullOrWhiteSpace(BaseUrl))
					baseUrl = new Uri(BaseUrl);

				Credentials = new CredentialCache
				{
					{baseUrl, "Basic", new NetworkCredential(Username, Password)}
				};

				if (!string.IsNullOrWhiteSpace(Domain))
				{
					Credentials.Add(baseUrl, "NTLM", new NetworkCredential(Username, Password, Domain));
					Credentials.Add(baseUrl, "Digest", new NetworkCredential(Username, Password, Domain));
				}
			}
			if (IgnoreCertErrors)
			{
				logger.Info("Ignoring Certificate Validation Check Errors");
				ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
			}
		}

		protected static string ReadString(BitwiseStream data)
		{
			data.Seek(0, SeekOrigin.Begin);
			var rdr = new BitReader(data);
			try
			{
				var str = rdr.ReadString(Encoding.UTF8);
				return str;
			}
			catch (Exception ex)
			{
				// Eat up encoding exception.
				throw new SoftException("HTTP Publisher skips test cases with incorrect UTF8", ex);
			}
		}

		protected override Variant OnCall(string method, List<BitwiseStream> args)
		{

			switch (method)
			{
				case "Query":
					Query = ReadString(args[0]);
					break;
				case "Header":
					var key = CleanHeaderValue(ReadString(args[0]));
					var value = ReadString(args[1]);
					Headers[key] = value;
					break;
			}

			return null;
		}

		static readonly char[] InvalidParamChars =
		{ 
			'(', ')', '<', '>', '@', ',', ';', ':', 
			'\\', '"', '\'', '/', '[', ']', '?', '=', 
			'{', '}', ' ', '\t', '\r', '\n'
		};


		/// <summary>
		/// Remove characters not allowed in header fields.
		/// </summary>
		/// <param name="str"></param>
		/// <returns></returns>
		protected string CleanHeaderValue(string str)
		{
			var sb = new StringBuilder(str.Length);

			foreach (var c in str)
			{
				if (Array.IndexOf(InvalidParamChars, c) == -1 &&
					!((c == '\x007f') || ((c < ' ') && (c != '\t'))))
				{
					sb.Append(c);
				}
			}

			return sb.ToString();
		}

		protected override void OnInput()
		{
			if (Response == null)
				CreateClient(null);

			base.OnInput();
		}

		/// <summary>
		/// Send data
		/// </summary>
		/// <param name="data">Data to send/write</param>
		protected override void OnOutput(BitwiseStream data)
		{
			lock (_clientLock)
			{
				if (_client != null)
					CloseClient();
			}

			CreateClient(data);
		}

		private void CreateClient(BitwiseStream data)
		{
			if (Response != null)
			{
				Response.Close();
				Response = null;
			}

			// Send request with data as body.
			Uri url;

			try
			{
				url = new Uri(Url);
				if (!string.IsNullOrWhiteSpace(Query))
					url = new Uri(Url + "?" + Query);
			}
			catch (UriFormatException ex)
			{
				throw new SoftException(ex);
			}

			SoftException caught = null;

			try
			{
				Retry.TimedBackoff(TimeSpan.FromSeconds(1), TimeSpan.FromMilliseconds(Timeout), () =>
				{
					try
					{
						_client = TryCreateClient(url, data);
					}
					catch (SoftException ex)
					{
						caught = ex;
					}
					catch (ProtocolViolationException ex)
					{
						// Happens if we try to write data and the method doesn't support it
						caught = new SoftException(ex);
					}
					catch (WebException ex)
					{
						if (ex.Status == WebExceptionStatus.Timeout || ex.Status == WebExceptionStatus.ConnectFailure)
							throw new TimeoutException(ex.Message, ex);

						caught = new SoftException(ex);
					}
				});
			}
			catch (TimeoutException ex)
			{
				caught = new SoftException("Timed out connecting to {0}".Fmt(SafeUrlString(url)), ex);
			}

			if (caught != null)
				throw new SoftException(caught);

			_clientName = SafeUrlString(url);

			StartClient();
		}

		private static string SafeUrlString(Uri url)
		{
			return "{0}://{1}:{2}".Fmt(url.Scheme, url.Host, url.Port);
		}

		static bool HeaderCompare(string lhs, string rhs)
		{
			return 0 == string.Compare(lhs, rhs, StringComparison.OrdinalIgnoreCase);
		}

		static void CheckBadChars(string value)
		{
			//First, check for correctly formed multi-line value
			//Second, check for absenece of CTL characters
			var crlf = 0;
			foreach (var t in value)
			{
				var c = (char)(0x000000ff & (uint)t);
				switch (crlf)
				{
					case 0:
						if (c == '\r')
						{
							crlf = 1;
						}
						else if (c == '\n')
						{
							// Technically this is bad HTTP.  But it would be a breaking change to throw here.
							// Is there an exploit?
							crlf = 2;
						}
						else if (c == 127 || (c < ' ' && c != '\t'))
						{
							throw new ArgumentException("Specified value has invalid Control characters.", "value");
						}
						break;

					case 1:
						if (c == '\n')
						{
							crlf = 2;
							break;
						}
						throw new ArgumentException("Specified value has invalid CRLF characters.", "value");

					case 2:
						if (c == ' ' || c == '\t')
						{
							crlf = 0;
							break;
						}
						throw new ArgumentException("Specified value has invalid CRLF characters.", "value");
				}
			}
		}

		private Stream TryCreateClient(Uri url, BitwiseStream data)
		{
			Logger.Trace("TryCreateClient> {0} {1}", Method, SafeUrlString(url));

			var request = (HttpWebRequest)WebRequest.Create(url);
			request.Method = Method;
			request.Timeout = Timeout;
			request.ServicePoint.Expect100Continue = false;

			if (Cookies)
				request.CookieContainer = CookieJar;

			if (Credentials != null)
				request.Credentials = Credentials;

			foreach (var kv in Headers)
			{
				try
				{
					// Mono doesn't do this but Microsoft does.
					// Ensure both behave the same way.
					CheckBadChars(kv.Value);

					if (HeaderCompare(kv.Key, "Accept"))
						request.Accept = kv.Value;
					else if (HeaderCompare(kv.Key, "Connection"))
						request.Connection = kv.Value;
					else if (HeaderCompare(kv.Key, "Content-Type"))
						request.ContentType = kv.Value;
					else if (HeaderCompare(kv.Key, "Date"))
						request.Date = DateTime.Parse(kv.Value);
					else if (HeaderCompare(kv.Key, "Expect"))
						request.Expect = kv.Value;
					else if (HeaderCompare(kv.Key, "If-Modified-Since"))
						request.IfModifiedSince = DateTime.Parse(kv.Value);
					else if (HeaderCompare(kv.Key, "Referer"))
						request.Referer = kv.Value;
					else if (HeaderCompare(kv.Key, "Transfer-Encoding"))
						request.TransferEncoding = kv.Value;
					else if (HeaderCompare(kv.Key, "User-Agent"))
						request.UserAgent = kv.Value;
					else if (!string.IsNullOrWhiteSpace(kv.Key))
						request.Headers[kv.Key] = kv.Value;
				}
				catch (ArgumentException ex)
				{
					throw new SoftException("Unable to set the '{0}' HTTP header to '{1}'.".Fmt(kv.Key, kv.Value), ex);
				}
			}

			if (data != null)
			{
				if (Logger.IsDebugEnabled)
					Logger.Debug("\n\n" + Utilities.HexDump(data));

				using (var sout = request.GetRequestStream())
				{
					data.CopyTo(sout);
				}
			}
			else
			{
				request.ContentLength = 0;
			}

			Response = (HttpWebResponse)request.GetResponse();

			return Response.GetResponseStream();
		}

		protected override void OnClose()
		{
			base.OnClose();

			if (Cookies && !CookiesAcrossIterations)
				CookieJar = new CookieContainer();

			if (Response != null)
			{
				Response.Close();
				Response = null;
			}

			Query = null;
			Headers.Clear();
		}

	}
}
