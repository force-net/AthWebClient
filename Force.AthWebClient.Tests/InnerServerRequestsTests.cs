using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

using NUnit.Framework;

namespace Force.AthWebClient.Tests
{
	[TestFixture]
	public class InnerServerRequestsTests
	{
		private const string SERVER_URL = "http://localhost:11000/";

		private MiniServer StartServer(Action<HttpListenerContext> processAction)
		{
			var ms = new MiniServer();
			ms.StartInThread(SERVER_URL, processAction);
			return ms;
		}

		[Test]
		public void Chunked_Post_Should_Be_Correct()
		{
			Action<HttpListenerContext> processAction = context =>
			{
				var stream = context.Request.InputStream;
				var i = 0;
				var isFail = false;
				while (true)
				{
					var b = stream.ReadByte();
					if (b < 0)
						break;
					if (b != i % 256) isFail = true;
					i++;
				}

				context.Response.StatusCode = isFail ? 500 : 200;
			};
			using (StartServer(processAction))
			{
				var r = new AthWebRequest(SERVER_URL);
				r.Method = "POST";
				r.AddHeader("Transfer-Encoding", "chunked");

				var stream = r.GetRequestStream();
				// chunked by default
				Assert.That(stream.GetType().Name, Is.EqualTo("ChunkedRequestStream"));
				for (var i = 0; i < 200000; i++)
				{
					stream.WriteByte((byte)(i % 256));
				}

				var response = r.GetResponse();

				Console.WriteLine(response.StatusCode);
				Assert.That(response.StatusCode, Is.EqualTo(200));
			}
		}

		[Test]
		public void Invalid_Server_Should_Cause_Normal_Exception()
		{
			var r = new AthWebRequest("http://localhost:44557");
			Assert.Throws<SocketException>(() => r.GetResponse());
		}

		[Test]
		public void Read_Timeout_Should_Throw()
		{
			HttpListener listener = null;
			var endEvt = new AutoResetEvent(false);
			var thread = new Thread(() =>
				{
					listener = new HttpListener();
					listener.Prefixes.Add(SERVER_URL);
					listener.Start();
					// no get context!!
					endEvt.WaitOne(TimeSpan.FromSeconds(10));
				});
			thread.Start();
			var r = new AthWebRequest(SERVER_URL);
			r.ReceiveTimeout = TimeSpan.FromSeconds(1);
			try
			{
				Assert.Throws<IOException>(() => r.GetResponse());
			}
			finally 
			{
				if (listener != null)
					listener.Abort();
				endEvt.Set();
				thread.Join(TimeSpan.FromSeconds(1));
				thread.Abort();
			}
		}

		[Test]
		public void Send_Timeout_Should_Throw()
		{
			HttpListener listener = null;
			var endEvt = new AutoResetEvent(false);
			var thread = new Thread(() =>
			{
				listener = new HttpListener();
				listener.Prefixes.Add(SERVER_URL);
				listener.Start();
				listener.GetContext();
				// no read!!
				endEvt.WaitOne(TimeSpan.FromSeconds(10));
			});
			thread.Start();
			var r = new AthWebRequest(SERVER_URL);
			r.Method = "POST";
			r.SendTimeout = TimeSpan.FromSeconds(1);
			try
			{
				var stream = r.GetRequestStream();
				Assert.Throws<IOException>(() =>
					{
						var buf = new byte[65536];
						// ensuring, data not in local cache
						stream.Write(buf, 0, buf.Length);
						stream.Write(buf, 0, buf.Length);
						stream.Write(buf, 0, buf.Length);
						stream.Write(buf, 0, buf.Length);
						stream.Write(buf, 0, buf.Length);
						stream.Write(buf, 0, buf.Length);
						stream.Write(buf, 0, buf.Length);
						stream.Write(buf, 0, buf.Length);
					});
			}
			finally
			{
				if (listener != null)
					listener.Abort();
				endEvt.Set();
				thread.Join(TimeSpan.FromSeconds(1));
				thread.Abort();
			}
		}
	}
}
