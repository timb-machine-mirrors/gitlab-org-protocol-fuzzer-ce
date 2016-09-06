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
			var cert = ProxyServer.LoadPemCert(new StringReader(CertChain));
			var key = ProxyServer.LoadPemKey(new StringReader(PrivateKey));

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

		public const string CertChain = @"-----BEGIN CERTIFICATE-----
MIIDKjCCAhICAQIwDQYJKoZIhvcNAQEEBQAwWTELMAkGA1UEBhMCQVUxEzARBgNV
BAgMClNvbWUtU3RhdGUxITAfBgNVBAoMGEludGVybmV0IFdpZGdpdHMgUHR5IEx0
ZDESMBAGA1UEAwwJY2EucGYuY29tMB4XDTE1MDgyNjIzMjYxOVoXDTE2MDgyNTIz
MjYxOVowXTELMAkGA1UEBhMCQVUxEzARBgNVBAgMClNvbWUtU3RhdGUxITAfBgNV
BAoMGEludGVybmV0IFdpZGdpdHMgUHR5IEx0ZDEWMBQGA1UEAwwNY2xpZW50LnBm
LmNvbTCCASIwDQYJKoZIhvcNAQEBBQADggEPADCCAQoCggEBANH6ejDk4BWsI5GP
PVC5D5sytZRwxZr3Zh5BOwfRM+IWC9ijIF/H8kXwUJmHiPvmd+d7aThLC/V458tj
DnW9Ag6MJ55QfSbD2eO5Bd4EcxEKSRcgAqDQuzeTF5tKbuPuzcK869siBGSpRmSk
4epi/geGTubTfyJC5udyR+mYrgrSZgjtUP9qRPIX56g+bImjMz3++Gu/eS28+PNy
eKSnSooAca9jSCy2Qt7nkQOlxnNfaqIzIO4Ugfa8QQF/zslU+p8caUrAmVW7qzg3
R0L3mLhSWXJjwbHyqmPN8Q0ISKRV8rTLHscx2gzj4za5rJNV+wvzJIZNFvHQjd4z
IMAnQ7MCAwEAATANBgkqhkiG9w0BAQQFAAOCAQEAUbVQOnvnKlUOVEymOkHr8sYe
L0yjUF69oyxwwpBgmDhOLsp/tujj+V92VrrqTK42r9te8zoPfAIsZ0kwSNf0dF8i
+VKh97DBbzNySO8jR7uG1N6YN90yGsDmiXSzBorlAfU2lJP888q/iacSWu8jdJtx
38YvEN/cUwWktopzifgqLr4rkN/6fnNmyCxgVr+Mqj8Nmdyps11mZzB/n/Fbu0ZN
TiTPXB1crUprWHjivn7IKFvdzKYht57GmcKLhcMYaQxM3uWv2b9Ykk7EqUwwrKcn
hkQspOhvqi+FAxt5nnr6bhne8qHKMwcD2/GS9SLAwvjqjWfjXmRb3CygdiUt1w==
-----END CERTIFICATE-----
-----BEGIN CERTIFICATE-----
MIIDhTCCAm2gAwIBAgIJAOvc2BXLHI8gMA0GCSqGSIb3DQEBCwUAMFkxCzAJBgNV
BAYTAkFVMRMwEQYDVQQIDApTb21lLVN0YXRlMSEwHwYDVQQKDBhJbnRlcm5ldCBX
aWRnaXRzIFB0eSBMdGQxEjAQBgNVBAMMCWNhLnBmLmNvbTAeFw0xNTA4MjYyMzI1
NTZaFw0xNjA4MjUyMzI1NTZaMFkxCzAJBgNVBAYTAkFVMRMwEQYDVQQIDApTb21l
LVN0YXRlMSEwHwYDVQQKDBhJbnRlcm5ldCBXaWRnaXRzIFB0eSBMdGQxEjAQBgNV
BAMMCWNhLnBmLmNvbTCCASIwDQYJKoZIhvcNAQEBBQADggEPADCCAQoCggEBAKtK
ijmxHr3WHPwaKye09YpDo4Smz9DQb7Ojj8QFun9pctaLqvy4C21cuhOFJ/E320AO
pMDjL9zfmMf0ivBwduGnG+pGhhYg1N/DoX9XwTPMrQuaNNRAJuiYHIqIXF3p2JeW
Xe+yJCX7GiPimDA+/UcITraIIpmRiX6ikp+SRIpF9TepfXquHWa/GnSkoHcBoZ/K
FTLcrjWAwas3aLXRv89Se5g96gyDHn8JWGNKjyEGpB2eHiUGdWbgoB6bqSx4VbFF
CK7i9/rvSIYvk3A23SYJlrVBb5WrHcrbANWRmKTQ2ZkOYniV7KboBRXXqkLfkTvY
l5FgzW1LH5BZQleP2aUCAwEAAaNQME4wHQYDVR0OBBYEFEaJmxkn7SYHIxhKaJq9
vHnOSsPoMB8GA1UdIwQYMBaAFEaJmxkn7SYHIxhKaJq9vHnOSsPoMAwGA1UdEwQF
MAMBAf8wDQYJKoZIhvcNAQELBQADggEBAF/0Ml3xyZcgzyhRHiHnruUNx+ZR9KNk
zXnx6DU/98C93gORVJNeNF4eHpwuXHgK2/Bbeu2qznTKZhYnSGIY2rWodY2Gw83j
FKRh/kXkHlTkZCJTb2HErR4+yd81BmvefEfndxIvi5CSY7UZFljouVYjYihFFaRp
yZly9leAWeZMTrx/S5LlTf8rKSDeoLH6eUw2FeM67iXGrjqJ/JV7Ei0tgNesLAq9
Sf6S0NF7IFIfHHjPegMm1wARiLEwlmiZXGd2AvdEdpk2S7cxrIqt3B2BHs59d2zk
cgjocQ/BfVfMWJwn20LUtAsJobxsRylEhi/4Ua1AJqB1XYsCLAXvWpg=
-----END CERTIFICATE-----";

		public const string PrivateKey = @"-----BEGIN PRIVATE KEY-----
MIIEvwIBADANBgkqhkiG9w0BAQEFAASCBKkwggSlAgEAAoIBAQDR+now5OAVrCOR
jz1QuQ+bMrWUcMWa92YeQTsH0TPiFgvYoyBfx/JF8FCZh4j75nfne2k4Swv1eOfL
Yw51vQIOjCeeUH0mw9njuQXeBHMRCkkXIAKg0Ls3kxebSm7j7s3CvOvbIgRkqUZk
pOHqYv4Hhk7m038iQubnckfpmK4K0mYI7VD/akTyF+eoPmyJozM9/vhrv3ktvPjz
cnikp0qKAHGvY0gstkLe55EDpcZzX2qiMyDuFIH2vEEBf87JVPqfHGlKwJlVu6s4
N0dC95i4UllyY8Gx8qpjzfENCEikVfK0yx7HMdoM4+M2uayTVfsL8ySGTRbx0I3e
MyDAJ0OzAgMBAAECggEBAMf48ZHLBxXcwGsJrW1JMXGuk3txAuAYqOo7WUqSlATG
1jVU9aoCM9sjiL8OcwjjbmHICGcSIat/n9D033qFrIXKOJjvMqFOeB4FwW61chhS
YVViqN4aq5G7xgmgk4cDkHXbkgs5lFGBXIbQ+E5ORHtKD66I0VUBvgHBtnbjbyzh
hCU9yZUrlLGebzOHQHcavea9rfi3YXQxh1AZUgTzeaSgHLoWdiYiMG4ukNiQIuDz
iw0tV3DcfJFsrigPJbGSHOvJJvuysFE5+rWrDuINp0LFsNkDIWqhwdGJq0ZZxktX
WU98VlL+gO8Uj4v0NaLBBmbHKLDT8KVYk95j4DzLM1ECgYEA++mBq1UMphHA6TEk
nTGMOP0OYTEZLIMhEkank4x5NZypJnKInl1SBId7GGoIkgje7dVCh486jGHkSff+
3CfdBXZvja04FULjgt2U9DabXyGUUDlpTtjxoBZOEk/hAaCQNRWWUFjxQjlAgp08
HOmEbAx8ZgfjNYhPy1GxtdXuJeUCgYEA1WLFD0EYqhGjD0pTTor2KyGH+KZUIF7U
yOHVGWwPidkhDFef85A9xI3QzSFbqata+U6dpRmyry5hIkYL/H8QlgR/mODNM7TT
RMvZPj+WbAEmxvQahY9y1ZuSgS4oWXFk+Nihd8g24XPy7AuO5XtxVEvFyw2XWGVB
wPQUz83uqbcCgYEA2X2i7E+DmiWdzjcVi3npvJIOxi7jsyCLfwHtUGBpjbXx/DXK
ah/b3fkyd8OkHqD2B5Sl2e/49pbVyF8KdP0dv8efTEyPLRwQ3T19itNSlXGiYRkQ
KHj5M+POB9VbSFJd7tbybfA8a5aOcbZa3gNmxvqItHZ+3gsQCVRA/+Lxt1kCgYB2
Kk/99op2A3ZMzVf8sq9HizYE1/bWRi2HYKflgBXKvFa0wwFsm3/ZDL+IGOTtvFqQ
nJxoBScSI5jepfOVS7gfmzYdrr9z1rr36frPod7myGx02FCnjZyF9bTXnHvzq/vC
YhRohJL/nnnzhSMIqadEiwFpRIl/BfO3qnKr8NotWQKBgQDwQk+9I0LmOSoO2lRq
kgACyYpFBnp0sdIqDufxBWEygJ0lxQ1YDpO2NlZaGLaE3Y5y+jdsikE0Y3PLhFtg
YmBXFPUWufXeoxR4xuKuYWb6X+jAgTlQ5H7FIR35+KxMQeaDwuQ0Fw96eCxSSYy2
Azv4NVvusCKgKkOqxMRm7qoq/Q==
-----END PRIVATE KEY-----";
	}
}