using System;
using System.Security.Authentication;

namespace Force.AthWebClient.Ssl
{
	/// <summary>
	/// Wrapper for <see cref="SslProtocols"/> (in 4.0 tls 1.1 and 1.2 are missing)
	/// </summary>
	[Flags]
	public enum AthSslProtocols
	{
		None = 0,
		Ssl2 = 12,
		Ssl3 = 48,
		Tls = 192,
		Tls11 = 768,
		Tls12 = 3072,
		Default = Tls | Ssl3,
	}
}
