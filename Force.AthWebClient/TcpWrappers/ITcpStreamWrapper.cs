using System;
using System.IO;
using System.Threading.Tasks;

namespace Force.AthWebClient.TcpWrappers
{
	public interface ITcpStreamWrapper
	{
		void Connect(AthEndPoint endpoint, TimeSpan connectTimeout, TimeSpan sendTimeout, TimeSpan receiveTimeout);

		Task ConnectAsync(AthEndPoint endpoint, TimeSpan sendTimeout, TimeSpan receiveTimeout);

		Stream CreateStream(Func<Stream, Stream> wrapper);

		Stream GetStream();

		void Release();

		void ErrorClose();
	}
}
