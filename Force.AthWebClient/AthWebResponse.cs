using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Sockets;
using System.Text;

using Force.AthWebClient.Streams;

namespace Force.AthWebClient
{
	public class AthWebResponse
	{
		private readonly NetworkReadStream _stream;

		private Stream _responseStream;

		private readonly List<Tuple<string, string>> _headers = new List<Tuple<string, string>>();

		public int StatusCode { get; private set; }

		private bool _autoDecompressResponse;

		public bool AutoDecompressResponse
		{
			get
			{
				return _autoDecompressResponse;
			}

			set
			{
				if (_responseStream != null)
					throw new InvalidOperationException("ResponseStream already retrieved.");
				_autoDecompressResponse = value;
			}
		}

		public List<Tuple<string, string>> Headers
		{
			get
			{
				return _headers;
			}
		}

		internal AthWebResponse(TcpClient client, Stream stream)
		{
			_stream = new NetworkReadStream(client, stream);
		}

		internal void ReadHeaders()
		{
			_stream.ReadSomething();
			ReadFirstLine();
			ReadHeadersInternal();
		}

		public Stream GetResponseStream()
		{
			if (_responseStream == null)
			{
				if (_headers.Any(x => x.Item1 == "Transfer-Encoding" && x.Item2 == "chunked"))
					_responseStream = new ChunkedResponseStream(_stream);
				else
					_responseStream = new ResponseStream(_stream);

				if (AutoDecompressResponse)
				{
					var encoding = _headers.FirstOrDefault(x => x.Item1 == "Content-Encoding");
					if (encoding != null)
					{
						if (encoding.Item2 == "deflate") _responseStream = new DeflateStream(_responseStream, CompressionMode.Decompress, false);
						else if (encoding.Item2 == "gzip") _responseStream = new GZipStream(_responseStream, CompressionMode.Decompress, false);
					}
				}
			}

			return _responseStream;
		}

		private void ReadFirstLine()
		{
			var r = _stream.ReadString(256);

			if (!r.Item1)
				throw new InvalidOperationException("Incorrect http answer");

			string line = r.Item2;
			if (!line.StartsWith("HTTP/"))
				throw new InvalidOperationException("Non-http response");

			var idxSp1 = line.IndexOf(' ');
			if (idxSp1 < 0)
				throw new InvalidOperationException("Incorrect http answer");

			var b = new StringBuilder();
			for (var i = idxSp1 + 1; i < line.Length; i++)
			{
				if (line[i] >= '0' && line[i] < (byte)'9')
					b.Append(line[i]);
				else
					break;
			}

			if (b.Length != 3)
				throw new InvalidOperationException("Incorrect http answer");

			// ok, we already have 3 digits
			StatusCode = Convert.ToInt32(b.ToString());
		}

		private void ReadHeadersInternal()
		{
			while (true)
			{
				var res = _stream.ReadString(65536);
				if (!res.Item1)
					throw new InvalidOperationException("Very big header");
				var header = res.Item2;

				if (header.Length == 0) return;

				var idx = header.IndexOf(':');

				if (idx < 0)
					throw new InvalidOperationException("Invalid response header: " + header);
				_headers.Add(new Tuple<string, string>(header.Substring(0, idx), header.Remove(0, idx + 1).Trim()));
			}
		}
	}
}
