﻿
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
	[Parameter("IgnoreCertErrors", typeof(bool), "Allow https regardless of cert status (defaults to false)", "false")]
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
					var value = CleanHeaderValue(ReadString(args[1]));
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

			Exception caught = null;
			Retry.Backoff(TimeSpan.FromSeconds(1), 30, () =>
			{
				try
				{
					_client = TryCreateClient(url, data);
				}
				catch(SoftException softEx)
				{
					caught = softEx;
				}
			});

			if (caught != null)
				throw caught;

			_clientName = url.ToString();

			StartClient();
		}

		private Stream TryCreateClient(Uri url, BitwiseStream data)
		{
			Logger.Debug("TryCreateClient> {0} {1}", Method, url);

			var request = (HttpWebRequest)WebRequest.Create(url);
			request.Method = Method;

			if (Cookies)
				request.CookieContainer = CookieJar;

			if (Credentials != null)
				request.Credentials = Credentials;

			foreach (var kv in Headers)
			{
				if (0 == string.Compare("Content-Type", kv.Key, StringComparison.OrdinalIgnoreCase))
					request.ContentType = kv.Value;
				else if (!string.IsNullOrWhiteSpace(kv.Key))
					request.Headers[kv.Key] = kv.Value;
			}

			if (data != null)
			{
				if (Logger.IsDebugEnabled)
					Logger.Debug("\n\n" + Utilities.HexDump(data));

				try
				{
					using (var sout = request.GetRequestStream())
					{
						data.CopyTo(sout);
					}
				}
				catch (ProtocolViolationException ex)
				{
					throw new SoftException(ex);
				}
			}
			else
			{
				request.ContentLength = 0;
			}

			try
			{
				Response = (HttpWebResponse)request.GetResponse();
			}
			catch (WebException ex)
			{
				throw new SoftException(ex);
			}

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
