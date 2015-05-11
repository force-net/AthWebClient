using System.Security.Authentication;

namespace Force.AthWebClient.Ssl
{
	/// <summary>
	/// Wrapper for <see cref="ExchangeAlgorithmType"/> (added some others)
	/// </summary>
	public enum AthExchangeAlgorithmType
	{
		None = 0,
		RsaSign = 9216,
		RsaKeyX = 41984,
		DiffieHellman = 43522,
		ECDHEphem = 44550
	}
}
