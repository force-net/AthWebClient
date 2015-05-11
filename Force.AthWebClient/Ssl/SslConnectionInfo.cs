using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace Force.AthWebClient.Ssl
{
	public class SslConnectionInfo
	{
		private readonly SslStream _stream;

		public bool CheckCertRevocationStatus
		{
			get
			{
				return _stream.CheckCertRevocationStatus;
			}
		}

		public CipherAlgorithmType CipherAlgorithm
		{
			get
			{
				return _stream.CipherAlgorithm;
			}
		}

		public int CipherStrength
		{
			get
			{
				return _stream.CipherStrength;
			}
		}

		public AthHashAlgorithmType HashAlgorithm
		{
			get
			{
				return (AthHashAlgorithmType)_stream.HashAlgorithm;
			}
		}

		public int HashStrength
		{
			get
			{
				return _stream.HashStrength;
			}
		}

		public bool IsEncrypted
		{
			get
			{
				return _stream.IsEncrypted;
			}
		}

		public bool IsSigned
		{
			get
			{
				return _stream.IsSigned;
			}
		}

		public AthExchangeAlgorithmType KeyExchangeAlgorithm
		{
			get
			{
				return (AthExchangeAlgorithmType)_stream.KeyExchangeAlgorithm;
			}
		}

		public int KeyExchangeStrength
		{
			get
			{
				return _stream.KeyExchangeStrength;
			}
		}

		public X509Certificate ServerCertificate
		{
			get
			{
				return _stream.RemoteCertificate;
			}
		}

		public SslProtocols SslPotocol
		{
			get
			{
				return _stream.SslProtocol;
			}
		}

		internal SslConnectionInfo(SslStream stream)
		{
			_stream = stream;
		}
	}
}
