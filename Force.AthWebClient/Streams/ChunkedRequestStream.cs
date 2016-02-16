using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Force.AthWebClient.Stubs;

namespace Force.AthWebClient.Streams
{
	internal class ChunkedRequestStream : WriteStubStream
	{
		private const int BUFFER_START = 32;

		private const int MAX_BUFFER_LENGTH = 65536 - (BUFFER_START * 2);

		private readonly Stream _innerStream;

		private readonly byte[] _buffer = new byte[65536];

		private int _bufferLength;

		private bool _doNothingOnEmpty;

		public ChunkedRequestStream(Stream innerStream, bool doNothingOnEmpty)
		{
			_innerStream = innerStream;
			_doNothingOnEmpty = doNothingOnEmpty;
		}

		public override void Flush()
		{
			WriteBlock();
			_innerStream.Flush();
		}

		public override Task FlushAsync(CancellationToken cancellationToken)
		{
			// TODO: .NET 4.5
			return WriteBlockAsync().ContinueWith(_ => _innerStream.Flush());
		}

		public override int WriteTimeout
		{
			get
			{
				return _innerStream.WriteTimeout;
			}

			set
			{
				_innerStream.WriteTimeout = value;
			}
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			if (_isClosed)
				throw new ObjectDisposedException("Stream already closed");
			if (count == 0)
				return;
			_doNothingOnEmpty = false;
			while (true)
			{
				var cnt = Math.Min(MAX_BUFFER_LENGTH - _bufferLength, count);
				Buffer.BlockCopy(buffer, offset, _buffer, _bufferLength + BUFFER_START, cnt);
				_bufferLength += cnt;
				count -= cnt;
				offset += cnt;
				if (_bufferLength == MAX_BUFFER_LENGTH)
					WriteBlock();
				if (count == 0)
					return;
			}
		}

		public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
		{
			if (count == 0)
				return Task.Factory.StartNew(() => { });
			return Task.Factory.StartNew(() => Write(buffer, offset, count), cancellationToken);
		}

		private bool _isClosed;

		public override void Close()
		{
			if (_isClosed)
				return;
			// empty stream without data. do not write final empty block
			if (_doNothingOnEmpty)
				return;
			WriteBlock();
			WriteFinalBlock();
			Flush();
			_isClosed = true;

			// dont close here
		}

		public override Task CloseAsync()
		{
			if (_isClosed)
			{
				var t = new Task(() => { });
				t.Start();
				return t;
			}

			_isClosed = true;
			return WriteBlockAsync()
				.ContinueWith(_ => WriteFinalBlockAsync())
				.ContinueWith(_ => FlushAsync(new CancellationToken()));
			// dont close here
		}

		private void WriteBlock()
		{
			// dont write empty block here
			if (_bufferLength == 0)
				return;

			var stringLength = _bufferLength.ToString("x", CultureInfo.InvariantCulture);
			var bytesLength = Encoding.ASCII.GetBytes(stringLength);
			var start = BUFFER_START - bytesLength.Length - 2;
			Buffer.BlockCopy(bytesLength, 0, _buffer, start, bytesLength.Length);
			_buffer[BUFFER_START - 2] = 13;
			_buffer[BUFFER_START - 1] = 10;
			_buffer[BUFFER_START + _bufferLength] = 13;
			_buffer[BUFFER_START + _bufferLength + 1] = 10;
			 _innerStream.Write(_buffer, start, _bufferLength + bytesLength.Length + 4);
			_bufferLength = 0;
		}

		private Task WriteBlockAsync()
		{
			return Task.Factory.StartNew(() => WriteBlockAsync());
		}

		private void WriteFinalBlock()
		{
			_innerStream.Write(new byte[] { (byte)'0', 13, 10, 13, 10 }, 0, 5);
		}

		private Task WriteFinalBlockAsync()
		{
			return Task.Factory.StartNew(() => WriteFinalBlockAsync());
		}
	}
}
