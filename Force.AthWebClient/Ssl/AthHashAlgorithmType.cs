using System.Security.Authentication;

namespace Force.AthWebClient.Ssl
{
	/// <summary>
	/// Wrapper for <see cref="HashAlgorithmType"/> (SHA2 is missing here)
	/// </summary>
	public enum AthHashAlgorithmType
	{
		None = 0,
		Crc32 = 1,
		Md5 = 32771,
		Sha1 = 32772,
		Ripemd160 = 32775,
		Sha256 = 32780,
		Sha384 = 32781,
		Sha512 = 32782
	}
}
