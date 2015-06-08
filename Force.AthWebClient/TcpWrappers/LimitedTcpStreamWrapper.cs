using System;

namespace Force.AthWebClient.TcpWrappers
{
	public class LimitedTcpStreamWrapper : SimpleTcpStreamWrapper
	{
		private readonly ConnectionLimitPolicy _policy;

		private AthEndPoint _host;

		internal LimitedTcpStreamWrapper(ConnectionLimitPolicy policy)
		{
			_policy = policy;
		}

		public override void Connect(AthEndPoint endpoint, TimeSpan connectTimeout, TimeSpan sendTimeout, TimeSpan receiveTimeout)
		{
			_host = endpoint;
			_policy.GetNewSlot(_host);
			base.Connect(endpoint, connectTimeout, sendTimeout, receiveTimeout);
		}

		public override void Release()
		{
			ReleaseSlot();
			base.Release();
		}

		public override void ErrorClose()
		{
			ReleaseSlot();
			base.ErrorClose();
		}

		private void ReleaseSlot()
		{
			if (_host != null)
			{
				_policy.ReleaseSlot(_host);
				_host = null;
			}
		}

		~LimitedTcpStreamWrapper()
		{
			ReleaseSlot();
		}
	}
}
