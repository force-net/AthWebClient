using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Force.AthWebClient.Stubs
{
	internal abstract class WriteStubStream : Stream
	{
		public override long Seek(long offset, SeekOrigin origin)
		{
			throw new NotSupportedException();
		}

		public override void SetLength(long value)
		{
			throw new NotSupportedException();
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			throw new NotSupportedException();
		}

#if NET45
		public override Task FlushAsync(CancellationToken cancellationToken)
		{
			throw new NotSupportedException();
		}
#else
		public virtual Task FlushAsync(CancellationToken cancellationToken)
		{
			return Task.Factory.StartNew(Flush);
		}

		public virtual Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
		{
			return Task.Factory.StartNew(() => Write(buffer, offset, count), cancellationToken);
		}
#endif

		public virtual Task CloseAsync()
		{
			return Task.Factory.StartNew(Close);
		}

		public override bool CanRead
		{
			get
			{
				return false;
			}
		}

		public override bool CanSeek
		{
			get
			{
				return false;
			}
		}

		public override bool CanWrite
		{
			get
			{
				return true;
			}
		}

		public override long Length
		{
			get
			{
				return -1;
			}
		}

		public override long Position
		{
			get
			{
				throw new NotSupportedException();
			}

			set
			{
				throw new NotSupportedException();
			}
		}
	}
}
