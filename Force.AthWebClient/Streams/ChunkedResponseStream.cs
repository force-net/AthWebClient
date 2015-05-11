using System;
using System.Globalization;

using Force.AthWebClient.Stubs;

namespace Force.AthWebClient.Streams
{
	internal class ChunkedResponseStream : ReadStubStream
	{
		private readonly NetworkReadStream _innerStream;

		private long _chunkSize;

		private bool _isFullyRead;

		private bool _isFirstChunk = true;

		internal ChunkedResponseStream(NetworkReadStream innerStream)
		{
			_innerStream = innerStream;
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			if (_isFullyRead) return 0;

			if (_chunkSize > 0)
			{
				var toRead = (int)Math.Min(count, _chunkSize);
				var readed = _innerStream.Read(buffer, offset, toRead);
				_chunkSize -= readed;
				return readed;
			}
			else
			{
				if (!_isFirstChunk)
				{
					var emptyLine = _innerStream.ReadString(2);
					if (!emptyLine.Item1 || emptyLine.Item2.Length > 0)
						throw new InvalidOperationException("Invalid line separator: " + emptyLine.Item2);
				}

				var str = _innerStream.ReadString(16);
				_isFirstChunk = false;
				if (!str.Item1)
					throw new InvalidOperationException("Invalid chunk");
				if (!long.TryParse(str.Item2, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out _chunkSize))
					throw new InvalidOperationException("Invalid chunk: " + str.Item2);
				// final chunk
				if (_chunkSize == 0)
				{
					var emptyLine = _innerStream.ReadString(2);
					if (!emptyLine.Item1 || emptyLine.Item2.Length > 0)
						throw new InvalidOperationException("Invalid final chunk: " + emptyLine.Item2);
					_isFullyRead = true;
				}

				return Read(buffer, offset, count);
			}
		}

		// todo: normal async read

		public override void Close()
		{
			_innerStream.Close();
		}
	}
}
