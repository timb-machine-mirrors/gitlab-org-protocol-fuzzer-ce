using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Peach.Enterprise.Pits
{
	public enum IpSecv6_Mode
	{
		Tunnel,
		Transport
	}

	public enum IpSecv6_Encryption
	{
		Aes128,
		TripleDes
	}

	public enum IpSecv6_HMAC { HMACSHA1, HMACMD5, HMACRIPEMD160, HMACSHA256, HMACSHA384, HMACSHA512, MACTripleDES };

}
