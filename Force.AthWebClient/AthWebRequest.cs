using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

using Force.AthWebClient.Ssl;
using Force.AthWebClient.Streams;
using Force.AthWebClient.Stubs;
using Force.AthWebClient.TcpWrappers;

// proxy
// https +
// keep-alive
// request limit +
// chunked +
// gzip +
// timeouts
// cancelling
// api improvements
// AthWebClient
// expect 100 continue
// .net 4.5

namespace Force.AthWebClient
{
	public class AthWebRequest
	{
		public enum HttpProtocolVersion
		{
			Http10,
			Http11
		}

		private ITcpStreamWrapper _client;

		private readonly AthEndPoint _connectEndPoint;

		private readonly List<Tuple<string, string>> _headers = new List<Tuple<string, string>>();

		private bool _isHostSet;

		public string Method { get; set; }

		public HttpProtocolVersion HttpVersion { get; set; }

		private long? _contentLength;

		private bool _contentLengthSetByHeaders;

		public long? ContentLength
		{
			get
			{
				return _contentLength;
			}

			set
			{
				if (_contentLengthSetByHeaders)
					ThrowError("Content-Length already set by headers");
				_contentLength = value;
			}
		}

		public SslOptions SslOptions { get; private set; }

		public SslConnectionInfo SslConnectionInfo { get; private set; }

		public TimeSpan ConnectTimeout { get; set; }

		public TimeSpan SendTimeout { get; set; }

		public TimeSpan ReceiveTimeout { get; set; }

		public ConnectionLimitPolicy LimitPolicy { get; set; }

		public AthWebRequest(string url)
			: this(AthEndPoint.FromUrl(url))
		{
		}

		public AthWebRequest(Uri url) 
			: this(AthEndPoint.FromUrl(url))
		{
		}

		public AthWebRequest(string scheme, EndPoint endPoint, string pathAndQuery)
			: this(AthEndPoint.FromEndPoint(scheme, endPoint, pathAndQuery))
		{
		}

		public AthWebRequest(AthEndPoint endPoint)
		{
			HttpVersion = HttpProtocolVersion.Http11;
			Method = "GET";

			_connectEndPoint = endPoint;
			LimitPolicy = ConnectionLimitPolicy.CreateUnlimited();
			if (_connectEndPoint.Scheme == AthEndPoint.SchemeType.Https) 
				SslOptions = SslOptions.CreateDefault();
		}

		public void AddHeader(string headerName, string value)
		{
			if (headerName == "Host") _isHostSet = true;
			// no keep-alive now
			if (headerName == "Connection" || headerName == "Keep-Alive")
				return;
			if (headerName == "Content-Length")
			{
				if (_contentLengthSetByHeaders)
					ThrowError("Content-Length is already defined");
				_contentLengthSetByHeaders = true;
				_contentLength = Convert.ToInt64(value);
			}

			if (headerName == "Transfer-Encoding" && value == "chunked")
			{
				if (_contentLengthSetByHeaders)
					ThrowError("Content-Length is already defined");
				_contentLengthSetByHeaders = true;
				_contentLength = null;
			}

			_headers.Add(new Tuple<string, string>(headerName, value));
		}

		private void ProcessGetStreamInternal()
		{
			_client.CreateStream(ProcessWrapStreamInternal);
		}

		private Stream ProcessWrapStreamInternal(Stream stream)
		{
			if (_connectEndPoint.Scheme == AthEndPoint.SchemeType.Https)
			{
				var sslStream = new SslStream(
					stream,
					false,
					(sender, certificate, chain, errors) =>
					{
						if (SslOptions.ServerCertificateValidationCallback != null) 
							return SslOptions.ServerCertificateValidationCallback(sender, certificate, chain, errors);
						else
						{
							if (SslOptions.ServerCertificateValidationPolicy == SslOptions.CertificateValidationPolicy.AllowAll) return true;
							else if (SslOptions.ServerCertificateValidationPolicy == SslOptions.CertificateValidationPolicy.AllowValid)
							{
								return errors == SslPolicyErrors.None;
							}
						}

						// by default
						return false;
					},
					null,
					SslOptions.EncryptionPolicy);
				// TODO: host from headers
				var clientCertificates = SslOptions.ClientCertificate == null ? null : new X509CertificateCollection(new[] { SslOptions.ClientCertificate });

				// set target host, only if by host name, and not by ip
				string targetHost = _connectEndPoint.IsHostIpAddress ? string.Empty : _connectEndPoint.Host;

				sslStream.AuthenticateAsClient(targetHost, clientCertificates, (SslProtocols)SslOptions.AllowedProtocols, SslOptions.CheckRevocation);
				SslConnectionInfo = new SslConnectionInfo(sslStream);

				return sslStream;
			}
			else
			{
				// http, does not modify
				return stream;
			}

			// TODO: async version
		}

		private void DoConnect()
		{
			if (_client == null)
				_client = TcpStreamFactory.Get(this);

			if (_client.GetStream() != null)
				ThrowError("Already connected");

			_client.Connect(_connectEndPoint, ConnectTimeout, SendTimeout, ReceiveTimeout);
			ProcessGetStreamInternal();
		}

		private Task DoConnectAsync()
		{
			if (_client == null)
				_client = TcpStreamFactory.Get(this);

			if (_client.GetStream() != null)
				ThrowError("Already connected");

			return _client.ConnectAsync(_connectEndPoint, SendTimeout, ReceiveTimeout)
				.ContinueWith(_ => ProcessGetStreamInternal());
		}

		private void WriteHeaders()
		{
			// NO Using or Close!! We use StreamWriter as simple wrapper for strings writing
			var writer = new StreamWriter(_client.GetStream(), Encoding.ASCII, 65536);
			var versionString = HttpVersion == HttpProtocolVersion.Http10 ? "1.0" : "1.1";
			writer.WriteLine(
				(Method ?? "GET").ToUpperInvariant() + " " + _connectEndPoint.GetPathAndQueryEscaped() + " " + "HTTP/"
				+ versionString);
			if (!_isHostSet)
				writer.WriteLine(
					"Host: " + _connectEndPoint.Host + (_connectEndPoint.IsDefaultPort ? string.Empty : ":" + _connectEndPoint.Port));
			writer.WriteLine("Connection: close");
			if (!_contentLengthSetByHeaders)
			{
				// manually add header
				if (_contentLength.HasValue && _contentLength.Value >= 0) writer.WriteLine("Content-Length: " + _contentLength.Value.ToString(CultureInfo.InvariantCulture));
				else
				{
					if (!_contentLength.HasValue) writer.WriteLine("Transfer-Encoding: chunked");
				}
			}

			foreach (var header in _headers)
			{
				writer.WriteLine(header.Item1 + ": " + header.Item2);
			}

			writer.WriteLine();
			writer.Flush();
		}

		private WriteStubStream _requestStream;

		private Stream CreateRequestStreamInternal()
		{
			// servers dont likes chunked requests, better to use usual, if not specified chunked by header
			if (_contentLength == null)
				return _requestStream = new ChunkedRequestStream(_client.GetStream(), !_contentLengthSetByHeaders);
			else
				return _requestStream = new RequestStream(_client, _client.GetStream(), _contentLength);
		}

		public Stream GetRequestStream()
		{
			if (_requestStream != null) return _requestStream;
			
			DoConnect();
			WriteHeaders();
			
			return CreateRequestStreamInternal();
		}

		public Task<Stream> GetRequestStreamAsync()
		{
			if (_requestStream != null) return Task.Factory.StartNew(() => (Stream)_requestStream);

			return DoConnectAsync().ContinueWith(_ => WriteHeaders()).ContinueWith(_ => (Stream)_requestStream);
		}

		public AthWebResponse GetResponse()
		{
			if (_requestStream == null)
			{
				if (!_contentLengthSetByHeaders && _contentLength == null) _contentLength = 0;
				GetRequestStream();
			}

			_requestStream.Close();

			var response = new AthWebResponse(_client);
			response.ReadHeaders();
			return response;
		}

		public Task<AthWebResponse> GetResponseAsync()
		{
			return Task.Factory.StartNew(() => GetResponse());
		}

		public void Abort()
		{
			_client.ErrorClose();
		}

		private void ThrowError(string text)
		{
			_client.ErrorClose();
			throw new InvalidOperationException(text);
		}
	}
}
