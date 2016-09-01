using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using NUnit.Framework;
using Titanium.Web.Proxy;
using Titanium.Web.Proxy.Models;

namespace Peach.Pro.Test.WebProxy
{
	[TestFixture]
	public class SslTests
	{
		private readonly ProxyServer _proxy = new ProxyServer();

		private int Port { get { return _proxy.ProxyEndPoints[0].Port; } }

		[SetUp]
		public void SetUp()
		{
			_proxy.Logger.TraceSource.Switch.Level = SourceLevels.All;
			_proxy.AddEndPoint(new ExplicitProxyEndPoint(IPAddress.Any, 0));
			_proxy.Start();
		}

		[TearDown]
		public void TearDown()
		{
			_proxy.Stop();
		}

		[Test]
		public void TestGoogle()
		{
			var cookies = new CookieContainer();
			var handler = new WebRequestHandler
			{
				CookieContainer = cookies,
				UseCookies = true,
				UseDefaultCredentials = false,
				Proxy = new System.Net.WebProxy("http://127.0.0.1:" + Port, false, new string[] { }),
				UseProxy = true,
				ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true
			};

			var cli = new HttpClient(handler);
			var result = cli.GetStringAsync("https://www.google.com/").Result;

			Assert.NotNull(result);
		}

		[Test]
		public void TestClientCert()
		{
			const string certChain = @"-----BEGIN CERTIFICATE-----
MIIDmjCCAlKgAwIBAgIEUqKc4DANBgkqhkiG9w0BAQsFADAjMSEwHwYDVQQDExhC
b3VuY3lDYXN0bGUgVExTIFRlc3QgQ0EwHhcNMTMxMjA3MDM1ODI0WhcNMzMxMjAy
MDM1ODI0WjAjMSEwHwYDVQQDExhCb3VuY3lDYXN0bGUgVGVzdCBTZXJ2ZXIwggFS
MA0GCSqGSIb3DQEBAQUAA4IBPwAwggE6AoIBMQCzL883ng/tmQfbcUeO2Bm7OnIZ
rzj5hk4zeyeKR6brrSj3RaxOq4wy1c14BA8YAVSm7ZDGjjXqiCiWqq1NdgnP2qyn
94O/OU0Ik3scpvkWDbweIJx0zHYBHTeqUaTEjdawI/EWxfzfOvPzBlK+s7uONLX9
Z8lbW9iZ76SS1hyD3T7mknTmEQAjAVT+aH0qdVFV2cg0JwKp2fCkDV8A9cvCo1h1
GiVpzNpAUjWaXxhs6AKd5/O7F5K32rP/tOQEhNW8F/cAfl9QbpR7M3GZrzmjtvU/
hAk2JYYDu7CHuIAGkhHvv3kWTx5s5JQP2Vn2KqjQLNcEMCLAl7e7NIsEhOUXTFvK
94f4guPuwqlrQKRX7nJnUKYHjbqWW64GVjFuLbB8CA3xqt9C2dOhuX+b4KWtAgMB
AAGjdjB0MAwGA1UdEwEB/wQCMAAwEwYDVR0lBAwwCgYIKwYBBQUHAwEwDwYDVR0P
AQH/BAUDAwegADAdBgNVHQ4EFgQUlILGBt8EzhYtlNrOPrhfwi0s6bkwHwYDVR0j
BBgwFoAU9mOvr6Pi7+4O5bKxRihqeCkyHukwDQYJKoZIhvcNAQELBQADggExAMQd
MLOlWKWJxh6IP7sRlWKjpWZyu43eSOfvEpVljW8VaRxJC8UdhpFxsXS6Ml7wEMUC
BkVNHGMxho/GJMXUBV7OsQSv0et1o45bmkN+KKisSVReSgcj6Drp/BRcUcybPtcJ
aDW1txh/suHWppVmtkIkZIF/3IR2qFekDdCLoluiEOvbNn3YjUnQLm6Eo0pBxgpb
W5MF3/19UckP1sLrs5vFk1dtDBZ/agpI9I0psv+6OsjosvrdpjIPHjwmoZ+oYtKc
4Q30vzLCVtGGyzXWBZ+Z6AbmZpJPDQtul522XKE2vE8GA3+X/RXVAZB8a86DWtzq
J1O6D+KOyA9zwe1CO+VJ5fMkjSNXY6WDzEXqyKEBP8tkkvSByiM546CXtNDbEwBe
PtYQf223mpK56XTFq4k=
-----END CERTIFICATE-----
-----BEGIN CERTIFICATE-----
MIIDZzCCAh+gAwIBAgIEUqKcyzANBgkqhkiG9w0BAQsFADAjMSEwHwYDVQQDExhC
b3VuY3lDYXN0bGUgVExTIFRlc3QgQ0EwHhcNMTMxMjA3MDM1ODAzWhcNMzMxMjAy
MDM1ODAzWjAjMSEwHwYDVQQDExhCb3VuY3lDYXN0bGUgVExTIFRlc3QgQ0EwggFS
MA0GCSqGSIb3DQEBAQUAA4IBPwAwggE6AoIBMQDMhzecH5G7Hux5I8B4ftDYKQfB
EpGBlFB2Yvbn3JIbtEpnY3utJokWGdbTY5oXn8amSRZFP9ZJlBDPrAyop//UfuJ0
A1n2wDiFHUcPMc1Dg67uH44fGib59tnOV4a0w4xF18FVgPH++2Vy/ZY/VjSAIfMd
U3nznh1p744dsEjTqj4euJjcy9CCvpW7A0i0ZuXztkkNZvcVnskCrvuHKshAZoPo
dtIW1G66evZQCGQIJHLyASAifQFe1c8VlJ8U4Z5zQeJe26DjMRF5IrYJWl43IFYr
DfFC4x+9EnVKdE2g95D9mTkWAwX8/y5eWzPBj7uauLdc36CPfJcn6Q0shGxMbn+O
j2/mrF8cq9hXBe0cuRLH8F7k6wGxzVzx4wizMysCKJoXYVnEw9AP3uSZqXNDAgMB
AAGjQzBBMA8GA1UdEwEB/wQFMAMBAf8wDwYDVR0PAQH/BAUDAwcEADAdBgNVHQ4E
FgQU9mOvr6Pi7+4O5bKxRihqeCkyHukwDQYJKoZIhvcNAQELBQADggExAKyMiFmj
YxzjXpQBD5dvRI7xZn79vH3lo13XRBXj/sbPXDXWIv21iLfutXn/RGGsq8piPXHG
5UY3cpZR6gOq6QO1dJ91K0ViAJBFQdkhhtfbhqGY4jvj0vGO6zenG/WrjH26nCT7
8S4L6ZoF6Y0EfQXluP50vEitTaZ6x/rung9h2JQ8rYKiRRVCA+tgBWK/CNhQ9LXy
k3GU0mKLik0AkEFS17C0NWePIPEs/Kxv9iTEFacAN9wVHjZcMYnYtWaPNX0LWV8s
2V2DMJxrmgCEcoXgJxlyEmvyqwpjB+2AiIQVIuWcwPqgBQoKHThT2zJcXV+bMhMs
6cGvaIdvPxttduQsP349GcmUIlV6zFJq+HcMjfa8hZNIkuGBpUzdRQnu1+vYTkwz
eVOPEIBZLzg9e2k=
-----END CERTIFICATE-----";

			const string privateKey = @"-----BEGIN RSA PRIVATE KEY-----
MIIFfAIBAAKCATEAsy/PN54P7ZkH23FHjtgZuzpyGa84+YZOM3snikem660o90Ws
TquMMtXNeAQPGAFUpu2Qxo416ogolqqtTXYJz9qsp/eDvzlNCJN7HKb5Fg28HiCc
dMx2AR03qlGkxI3WsCPxFsX83zrz8wZSvrO7jjS1/WfJW1vYme+kktYcg90+5pJ0
5hEAIwFU/mh9KnVRVdnINCcCqdnwpA1fAPXLwqNYdRolaczaQFI1ml8YbOgCnefz
uxeSt9qz/7TkBITVvBf3AH5fUG6UezNxma85o7b1P4QJNiWGA7uwh7iABpIR7795
Fk8ebOSUD9lZ9iqo0CzXBDAiwJe3uzSLBITlF0xbyveH+ILj7sKpa0CkV+5yZ1Cm
B426lluuBlYxbi2wfAgN8arfQtnTobl/m+ClrQIDAQABAoIBMAEZ6kKNFNbIeBqT
24dTDIUsj+DCbGPsR86wsG5FOgFRa96K0qcAS53nBAXDynKFl6igJcXs4WfLiRnN
QGcgob+cIgM6pT8rDnlZT/291pH9mD/MwnyHDwak7C7dGXrRojWQl1TGZqjJS0f5
enaN5lUnXwROZqsxUYohFkmtj1dpjxTqUGZ0In1JdG2mDmunMgjkJ/UqtCp0QFCp
xtd1u5/913RBFC+n/CJl1+TnNQ4WNDRtkHZfJpVp+dOldQr028CJU3C5u1JB9ped
JK+dywUFlqH1Tdk2v5CQV5esvbFpXX7FIlHuId+JOwFyvkR5+7NmyQ2uAz/Yeg8K
ItdRhNQCJpYPlPH9Yb1JooEhPYrQX6oz/q2R3qiFdtc1cMZW3KCtIALZk30lW/o+
B4EiysECgZkA1cYcdgNKy5K1O/IOyhr1eYK2us738Z3A57gC1y+m58D02dzmwacg
HKZI9Cz98L8yEyORkdxpg86xi+NIcdU15BXXioFgMI5ZSfNVBcO4rv8G/QCxEWDr
gnXe7FYuLlOKBbVXx7fJ0IXRUVbUlH5ZqE2HlFbSdPJmlaKtbxxyT7xhhWkf/MuI
hRn+MFvbXvC+JqL+FS141kECgZkA1pS74hc7H5blq0J51E8EDtgU5m770VlQKhbW
Z1D0oXVfB19DD3SX4xu22/6XlKXLlQDx4ssxVw11VIkd9WhkIrqq4U644XL5xXmf
PMsR+fG2o+Y7MY4TNy+4qcuOK17n6R2pxR4zQoVnZs/qL4s5jPKMsC76C4Udfxfh
yup0eFEJ+jPQdWYWQ6uX3UF0rA5x0Tb200aKbG0CgZkAkwaoeHoXLR//yfTXOyWD
g0jliGHkoabQEA681WcOsgJB5L1LcBETwuCS+G0hUj0NoaAq9FjVsTOtZPqyzqfH
YtGq5rXIhFzDCFt1NHvCP4ljMwsQvVUdZSLQaVd0d6Q5H2fzsYa0JNiEeB7yIhcs
btaz0tBL+ubkqzGxeuPjsvdrUyhUObd6c6DG9FeY7xlAjq43djVKEIECgZhITde9
SDyo2UzMV1r72iAw7EimmPELSsADXqyiJZo4qXb64fOTyqK/aQBFwtTKxs8Bh076
L6ORhLxrXsSUg7dyKFoaD0+mz/ovu1qXvolxIix7r8F0Yj5BUzgzJp7iKFmWqGMj
Q5jcKl18PETZ/lzHDJexajLhHNqij6aKnFPgktX80+bDGEIaTUCf0kWBEGDzsUSc
TmGoRQKBmQDVFQ6EAOXXNp2bMLZ78qnxmS+NZPijBDeVu1cXMYdRSBoldrtSHKUM
NFjPfesz4IRbEePK5s0vgM88QCDaspy8aLj//gh9YuqijbHOfhvMUP6MqzU/jJN7
MDAxcGbcoFqdWTP1HB9qeX91UNRwN+2/xfO6SrLbfTvG4v/sntr8YfVYya2EuAa9
qLk0TB3QXaoHknsz7EhRnw==
-----END RSA PRIVATE KEY-----";

			var cert = ProxyServer.LoadPemCert(new StringReader(certChain));
			var key = ProxyServer.LoadPemKey(new StringReader(privateKey));

			_proxy.Logger.TraceSource.Listeners.Add(new ConsoleTraceListener());

			_proxy.SelectClientCertificate += (sender, args) =>
			{
				args.ClientCert = cert;
				args.ClientPrivateKey = key;
			};

			var cookies = new CookieContainer();
			var handler = new WebRequestHandler
			{
				CookieContainer = cookies,
				UseCookies = true,
				UseDefaultCredentials = false,
				Proxy = new System.Net.WebProxy("http://127.0.0.1:" + Port, false, new string[] { }),
				UseProxy = true,
				ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true
			};

			var cli = new HttpClient(handler);
			var result = cli.GetStringAsync("https://www.google.com/").Result;

			Assert.NotNull(result);

			Console.WriteLine(result);
		}
	}
}