namespace Force.AthWebClient.TcpWrappers
{
	internal static class TcpStreamFactory
	{
		internal static ITcpStreamWrapper Get(AthWebRequest request)
		{
			if (request.LimitPolicy.HostRequestLimit <= 0 && request.LimitPolicy.TotalRequestLimit <= 0)
				return new SimpleTcpStreamWrapper();
			return new LimitedTcpStreamWrapper(request.LimitPolicy);
		}
	}
}
