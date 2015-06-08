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

		[Test]
		public void HostRequestLimit_Should_Go_In_Correct_Order()
		{
			var requestsA = 0;
			var requestsB = 0;
			Action<HttpListenerContext> processActionA = context =>
			{
				requestsA++;
				Thread.Sleep(300);
				context.Response.StatusCode = 200;
				context.Response.Close();
			};

			Action<HttpListenerContext> processActionB = context =>
			{
				requestsB++;
				Thread.Sleep(100);
				context.Response.StatusCode = 200;
				context.Response.Close();
			};

			var connectionLimitPolicy = ConnectionLimitPolicy.Create(1);
			using (StartServer(processActionA, SERVER_URL1))
			using (StartServer(processActionB, SERVER_URL2))
			{
				var r1 = new AthWebRequest(SERVER_URL1);
				var r2 = new AthWebRequest(SERVER_URL1);
				var r3 = new AthWebRequest(SERVER_URL2);
				var r4 = new AthWebRequest(SERVER_URL2);
				r1.LimitPolicy = connectionLimitPolicy;
				r2.LimitPolicy = connectionLimitPolicy;
				r3.LimitPolicy = connectionLimitPolicy;
				r4.LimitPolicy = connectionLimitPolicy;

				var t1 = new Task(() => r1.GetResponse().GetResponseStream().Close());
				var t2 = new Task(() => r2.GetResponse().GetResponseStream().Close()); // queued
				var t3 = new Task(() => r3.GetResponse().GetResponseStream().Close());
				var t4 = new Task(() => r4.GetResponse().GetResponseStream().Close()); // queued and completed
				t1.Start();
				t2.Start();
				t3.Start();
				t4.Start();
				Task.WaitAll(t3, t4);

				// fast requests completed before slow
				Assert.That(requestsB, Is.EqualTo(2));
				Assert.That(requestsA, Is.EqualTo(1));

				Task.WaitAll(t1, t2);

				// checking, that all requests is completed
				Assert.That(requestsB + requestsA, Is.EqualTo(4));
			}
		}
	}
}
