using System;
using System.Collections.Generic;
using System.Threading;

namespace Force.AthWebClient
{
	public class ConnectionLimitPolicy
	{
		public int HostRequestLimit { get; private set; }

		public int TotalRequestLimit { get; private set; }

		private ConnectionLimitPolicy(int hostLimit, int totalLimit)
		{
			if (hostLimit > totalLimit && totalLimit > 0)
				throw new InvalidOperationException("Host limit should be less than or equal to totalLimit");
			HostRequestLimit = hostLimit;
			TotalRequestLimit = totalLimit;
		}

		public static ConnectionLimitPolicy CreateUnlimited()
		{
			return new ConnectionLimitPolicy(0, 0);
		}

		public static ConnectionLimitPolicy Create(int hostLimit, int totalLimit = 0)
		{
			return new ConnectionLimitPolicy(hostLimit, totalLimit);
		}

		private readonly Dictionary<AthEndPoint, int> _hostRequests = new Dictionary<AthEndPoint, int>();

		private int _totalRequests;

		internal void GetNewSlot(AthEndPoint host)
		{
			while (true)
			{
				lock (_hostRequests)
				{
					if (_totalRequests < TotalRequestLimit || TotalRequestLimit <= 0)
					{
						if (HostRequestLimit > 0)
						{
							int currentHostRequests;
							if (_hostRequests.TryGetValue(host, out currentHostRequests))
							{
								if (currentHostRequests < HostRequestLimit)
								{
									_hostRequests[host] = currentHostRequests + 1;
									_totalRequests++;
									return;
								}
							}
							else
							{
								_hostRequests.Add(host, 1);
								_totalRequests++;
								return;
							}
						}
						else
						{
							_totalRequests++;
							return;
						}
					}

					Monitor.Wait(_hostRequests);
				}
			}
		}

		internal void ReleaseSlot(AthEndPoint host)
		{
			lock (_hostRequests)
			{
				_totalRequests--;
				int value;
				if (_hostRequests.TryGetValue(host, out value)) _hostRequests[host] = value - 1;
				Monitor.PulseAll(_hostRequests);
			}
		}
	}
}
