using System.Threading;
using System.Threading.Tasks;

using Force.AthWebClient.Stubs;

namespace Force.AthWebClient.Streams
{
	internal class ResponseStream : ReadStubStream
	{
		private readonly NetworkReadStream _innerStream;

		internal ResponseStream(NetworkReadStream innerStream)
		{
			_innerStream = innerStream;
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			return _innerStream.Read(buffer, offset, count);
		}

		public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
		{
			return _innerStream.ReadAsync(buffer, offset, count, cancellationToken);
		}

		public override void Close()
		{
			base.Close();
			_innerStream.Close();
		}
	}
}
