using System;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Force.AthWebClient.TcpWrappers
{
	public class SimpleTcpStreamWrapper : ITcpStreamWrapper
	{
		private readonly TcpClient _client;

		private Stream _stream;

		private bool _isClosed;

		public SimpleTcpStreamWrapper()
		{
			_client = new TcpClient();
		}

		public void Connect(AthEndPoint endpoint, TimeSpan connectTimeout, TimeSpan sendTimeout, TimeSpan receiveTimeout)
		{
			CheckDisposed();

			_client.SendTimeout = (int)sendTimeout.TotalMilliseconds;
			_client.ReceiveTimeout = (int)receiveTimeout.TotalMilliseconds;

			if (connectTimeout == TimeSpan.Zero)
			{
				if (endpoint.IsHostIpAddress)
					_client.Connect(endpoint.Address, endpoint.Port);
				else
					_client.Connect(endpoint.Host, endpoint.Port);
			}
			else
			{
				var task = ConnectAsync(endpoint, sendTimeout, receiveTimeout);
				if (!task.Wait(connectTimeout))
				{
					ErrorClose();
					throw new TimeoutException("Cannot connect to server");
				}
			}
		}

		public Task ConnectAsync(AthEndPoint endpoint, TimeSpan sendTimeout, TimeSpan receiveTimeout)
		{
			CheckDisposed();

			_client.SendTimeout = (int)sendTimeout.TotalMilliseconds;
			_client.ReceiveTimeout = (int)receiveTimeout.TotalMilliseconds;


			if (!endpoint.IsHostIpAddress)
				return Task.Factory.FromAsync(_client.BeginConnect, _client.EndConnect, endpoint.Host, endpoint.Port, null);
			else
				return Task.Factory.FromAsync(_client.BeginConnect, _client.EndConnect, endpoint.Address, endpoint.Port, null);
		}

		public Stream CreateStream(Func<Stream, Stream> wrapper)
		{
			CheckDisposed();
			return _stream ?? (_stream = wrapper(_client.GetStream()));
		}

		public Stream GetStream()
		{
			CheckDisposed();
			return _stream;
		}

		private void CheckDisposed()
		{
			if (_isClosed)
				throw new ObjectDisposedException("TcpClient already disposed");
		}

		public void Release()
		{
			if (_stream != null)
				_stream.Close();

			_client.Close();
			_isClosed = true;
		}

		public void ErrorClose()
		{
			Release();
		}
	}
}
