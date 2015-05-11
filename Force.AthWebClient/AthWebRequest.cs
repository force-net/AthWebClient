using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

using Force.AthWebClient.Ssl;
using Force.AthWebClient.Streams;
using Force.AthWebClient.Stubs;

// proxy
// https +
// keep-alive
// request limit
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

		private readonly TcpClient _client;

		private Stream _stream;

		private readonly Uri _url;

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
					throw new InvalidOperationException("Content-Length already set by headers");
				_contentLength = value;
			}
		}

		public SslOptions SslOptions { get; private set; }

		public SslConnectionInfo SslConnectionInfo { get; private set; }

		public AthWebRequest(Uri url)
		{
			HttpVersion = HttpProtocolVersion.Http11;
			Method = "GET";

			if (url.Scheme != "http" && url.Scheme != "https")
				throw new NotSupportedException("Only http(s) scheme supported now");

			_url = url;
			_client = new TcpClient();
			if (url.Scheme == "https") SslOptions = SslOptions.CreateDefault();
		}

		public AthWebRequest(string url) : this(new Uri(url))
		{
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
					throw new InvalidOperationException("Content-Length already defined");
				_contentLengthSetByHeaders = true;
				_contentLength = Convert.ToInt64(value);
			}

			if (headerName == "Transfer-Encoding" && value == "chunked")
			{
				if (_contentLengthSetByHeaders)
					throw new InvalidOperationException("Content-Length already defined");
				_contentLengthSetByHeaders = true;
				_contentLength = null;
			}

			_headers.Add(new Tuple<string, string>(headerName, value));
		}

		private void ProcessGetStreamInternal()
		{
			_stream = _client.GetStream();
			if (_url.Scheme == "https")
			{
				var sslStream = new SslStream(
					_stream,
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
				_stream = sslStream;
				// TODO: host from headers
				var clientCertificates = SslOptions.ClientCertificate == null ? null : new X509CertificateCollection(new[] { SslOptions.ClientCertificate });
				sslStream.AuthenticateAsClient(_url.Host, clientCertificates, (SslProtocols)SslOptions.AllowedProtocols, SslOptions.CheckRevocation);
				SslConnectionInfo = new SslConnectionInfo(sslStream);
			}

			// TODO: async version
		}

		private void DoConnect()
		{
			if (_stream != null)
				throw new InvalidOperationException("Already connected");
			_client.Connect(_url.Host, _url.Port);
			ProcessGetStreamInternal();
		}

		private Task DoConnectAsync()
		{
			if (_stream != null)
				throw new InvalidOperationException("Already connected");

			return Task.Factory.FromAsync(_client.BeginConnect, _client.EndConnect, _url.Host, _url.Port, null)
				.ContinueWith(_ => ProcessGetStreamInternal());
		}

		private void WriteHeaders()
		{
			{
				// NO Using or Close!! We use StreamWriter as simple wrapper for strings writing
				var writer = new StreamWriter(_stream, Encoding.ASCII, 65536);
				var versionString = HttpVersion == HttpProtocolVersion.Http10 ? "1.0" : "1.1";
				writer.WriteLine((Method ?? "GET").ToUpperInvariant() + " " + _url.PathAndQuery + " " + "HTTP/" + versionString);
				if (!_isHostSet)
					writer.WriteLine("Host: " + _url.Host + (_url.IsDefaultPort ? string.Empty : ":" + _url.Port));
				writer.WriteLine("Connection: close");
				if (!_contentLengthSetByHeaders)
				{
					// manually add header
					if (_contentLength.HasValue && _contentLength.Value >= 0)
						writer.WriteLine("Content-Length: " + _contentLength.Value.ToString(CultureInfo.InvariantCulture));
					else
					{
						if (!_contentLength.HasValue)
							writer.WriteLine("Transfer-Encoding: chunked");
					}
				}

				foreach (var header in _headers)
				{
					writer.WriteLine(header.Item1 + ": " + header.Item2);
				}

				writer.WriteLine();
				writer.Flush();
			}
		}

		private WriteStubStream _requestStream;

		private Stream CreateRequestStreamInternal()
		{
			if (_contentLength == null) 
				return _requestStream = new ChunkedRequestStream(_stream);
			else 
				return _requestStream = new RequestStream(_stream, _contentLength);
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

			var response = new AthWebResponse(_client, _stream);
			response.ReadHeaders();
			return response;
		}

		public Task<AthWebResponse> GetResponseAsync()
		{
			return Task.Factory.StartNew(() => GetResponse());
		}

		public void Abort()
		{
			_stream.Close();
			//_client.Close();
		}
	}
}
