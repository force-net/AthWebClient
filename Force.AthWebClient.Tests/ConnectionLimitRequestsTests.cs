using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using NUnit.Framework;

namespace Force.AthWebClient.Tests
{
	[TestFixture]
	public class ConnectionLimitRequestsTests
	{
		private const string SERVER_URL1 = "http://localhost:11000/";
		
		private const string SERVER_URL2 = "http://127.0.0.1:11001/";

		private MiniServer StartServer(Action<HttpListenerContext> processAction, string url)
		{
			var ms = new MiniServer();
			ms.StartInThread(url, processAction);
			return ms;
		}

		[Test]
		[TestCase(2, 0, 4, 2, null)]
		[TestCase(2, 2, 4, 2, null)]
		[TestCase(0, 2, 4, 2, null)] // no host limit, but max limit
		[TestCase(0, 0, 4, 4, null)] // no limit
		[TestCase(30, 30, 4, 4, null)] // big limit
		[TestCase(2, 3, 4, 3, new[] { SERVER_URL1, SERVER_URL2 })] // different hosts
		public void HostRequestLimit_Should_Be_Applied(int hostLimit, int totalLimit, int requestCount, int checkCount, string[] hostNames = null)
		{
			var activeCount = 0;
			var maxCount = 0;
			Action<HttpListenerContext> processAction = context =>
				{
					activeCount++;
					Thread.Sleep(300);
					maxCount = Math.Max(maxCount, activeCount);
					activeCount--;
					context.Response.StatusCode = 200;
					context.Response.Close();
				};

			var connectionLimitPolicy = ConnectionLimitPolicy.Create(hostLimit, totalLimit);
			using (StartServer(processAction, SERVER_URL1))
			using (StartServer(processAction, SERVER_URL2))
			{
				var requests = new AthWebRequest[requestCount];
				var tasks = new Task[requestCount];
				for (int i = 0; i < requests.Length; i++)
				{
					var hostName = hostNames == null ? SERVER_URL1 : hostNames[i % hostNames.Length];
					var r = requests[i] = new AthWebRequest(hostName);
					r.LimitPolicy = connectionLimitPolicy;
					tasks[i] = new Task(() => r.GetResponse().GetResponseStream().Close());
					tasks[i].Start();
				}

				Task.WaitAll(tasks);

				Assert.That(maxCount, Is.EqualTo(checkCount));
			}
		}
	}
}
