using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Force.AthWebClient.Stubs;
using Force.AthWebClient.TcpWrappers;

namespace Force.AthWebClient.Streams
{
	internal class RequestStream : WriteStubStream
	{
		private readonly Stream _innerStream;

		private readonly ITcpStreamWrapper _client;

		private long? _desiredLength;

		private long _bytesWritten;

		private bool _flushed = false;

		public RequestStream(ITcpStreamWrapper client, Stream innerStream, long? desiredLength)
		{
			_innerStream = innerStream;
			_client = client;
			_desiredLength = desiredLength;
		}

		public override void Flush()
		{
			_innerStream.Flush();
			_flushed = true;
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
			if (_desiredLength.HasValue && _bytesWritten + count > _desiredLength.Value)
			{
				_client.ErrorClose();
				throw new InvalidOperationException("Trying to write more than specified length (" + _desiredLength.Value + ")");
			}

			_innerStream.Write(buffer, offset, count);
			_bytesWritten += count;
			_flushed = false;
		}

		public override void Close()
		{
			ValidateFinalLength();

			if (!_flushed)
				Flush();
			// dont close here
		}

		public override Task CloseAsync()
		{
			ValidateFinalLength();

			if (!_flushed)
				return FlushAsync(new CancellationToken());
			// dont close here
			return new Task(() => { });
		}

		protected virtual void ValidateFinalLength()
		{
			if (_desiredLength.HasValue)
			{
				// -1: no real body, cannot write anything, no check here
				if (_desiredLength.Value >= 0)
				{
					if (_bytesWritten != _desiredLength.Value)
					{
						_client.ErrorClose();
						throw new InvalidOperationException(
							"Trying to write less than specified length (" + _desiredLength.Value + "). Should write additional "
							+ (_desiredLength.Value - _bytesWritten) + " bytes");
					}
				}
			}
		}
	}
}
