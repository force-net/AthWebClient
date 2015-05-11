using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace Force.AthWebClient.Ssl
{
	public class SslOptions
	{
		public enum CertificateValidationPolicy
		{
			AllowValid = 0,
			AllowAll,
			// useless 
			// DenyAll
		}

		public CertificateValidationPolicy ServerCertificateValidationPolicy { get; set; }

		public bool CheckRevocation { get; set; }

		public EncryptionPolicy EncryptionPolicy { get; set; }

		public X509Certificate ClientCertificate { get; set; }

		public RemoteCertificateValidationCallback ServerCertificateValidationCallback { get; set; }

		public AthSslProtocols AllowedProtocols { get; set; }

		public static SslOptions CreateDefault()
		{
			return new SslOptions
						{
							ServerCertificateValidationPolicy = CertificateValidationPolicy.AllowValid,
							CheckRevocation = true,
							EncryptionPolicy = EncryptionPolicy.RequireEncryption,
							AllowedProtocols = AthSslProtocols.Tls | AthSslProtocols.Tls11 | AthSslProtocols.Tls12
						};
		}
	}
}
