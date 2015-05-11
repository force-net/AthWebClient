using System;
using System.IO;
using System.Text;

using Force.AthWebClient.Stubs;
using Force.AthWebClient.TcpWrappers;

namespace Force.AthWebClient.Streams
{
	internal class NetworkReadStream : ReadStubStream
	{
		private readonly ITcpStreamWrapper _sourceClient;

		private readonly Stream _innerStream;

		private readonly byte[] _buffer = new byte[65536];

		private int _bufferStart;

		private int _bufferEnd;

		internal NetworkReadStream(ITcpStreamWrapper sourceClient)
		{
			_sourceClient = sourceClient;
			_innerStream = sourceClient.GetStream();
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			if (_bufferEnd - _bufferStart > 0)
			{
				var length = Math.Min(_bufferEnd - _bufferStart, count);
				Array.Copy(_buffer, _bufferStart, buffer, offset, length);
				SetBytesProcessed(_bufferStart + length);
				return length;
			}

			return _innerStream.Read(buffer, offset, count);
		}

		public override void Close()
		{
			_sourceClient.Release();
		}

		internal void ErrorClose()
		{
			_sourceClient.ErrorClose();
		}

		internal bool ReadSomething()
		{
			int toRead = _buffer.Length - _bufferEnd;
			if (toRead <= 0)
			{
				ErrorClose();
				throw new InvalidOperationException("Buffer underflow");
			}

			var readed = _innerStream.Read(_buffer, _bufferStart, toRead);
			if (readed == 0)
				return false;
			_bufferEnd += readed;

			return true;
		}

		internal void SetBytesProcessed(int processedBytes)
		{
			_bufferStart = processedBytes;

			if (_bufferStart == _bufferEnd)
			{
				_bufferStart = 0;
				_bufferEnd = 0;
			}
		}

		internal Tuple<bool, string> ReadString(int maxLength)
		{
			var b = new StringBuilder();

			while (true)
			{
				for (var i = _bufferStart; i < _bufferEnd; i++)
				{
					if (_buffer[i] == '\n')
					{
						if (b.Length > 0 && b[b.Length - 1] == '\r') b.Remove(b.Length - 1, 1);
						SetBytesProcessed(i + 1);
						return new Tuple<bool, string>(true, b.ToString());
					}

					b.Append((char)_buffer[i]);
					if (b.Length >= maxLength) return new Tuple<bool, string>(false, b.ToString());
				}
				
				if (!ReadSomething())
					return new Tuple<bool, string>(false, b.ToString());
			}
		}
	}
}
