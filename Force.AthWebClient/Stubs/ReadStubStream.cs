using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Force.AthWebClient.Stubs
{
	internal abstract class ReadStubStream : Stream
	{
		public override void Flush()
		{
			throw new NotSupportedException();
		}

#if NET45
		public override Task FlushAsync(CancellationToken cancellationToken)
		{
			throw new NotSupportedException();
		}
#endif

		public override long Seek(long offset, SeekOrigin origin)
		{
			throw new NotSupportedException();
		}

		public override void SetLength(long value)
		{
			throw new NotSupportedException();
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			throw new NotSupportedException();
		}

#if NET45
		public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
		{
			throw new NotSupportedException();
		}
#endif

#if !NET45
		public virtual Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
		{
			return Task.Factory.StartNew(() => Read(buffer, offset, count), cancellationToken);
		}
#endif

		public override bool CanRead
		{
			get
			{
				return true;
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
				return false;
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
