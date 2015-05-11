using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using NUnit.Framework;

namespace Force.AthWebClient.Tests
{
	[TestFixture]
	public class InnerServerRequestsTests
	{
		private class MiniServer : IDisposable
		{
			private Thread _thread;

			private bool _isExiting = false;

			private HttpListener _listener;

			public void StartInThread(string url, Action<HttpListenerContext> processAction)
			{
				_thread = new Thread(() => Start(url, processAction));
				_thread.Start();
			}

			private void Start(string url, Action<HttpListenerContext> processAction)
			{
				_isExiting = false;
				_listener = new HttpListener();
				_listener.Prefixes.Add(url);
				_listener.Start();

				while (!_isExiting)
				{
					try
					{
						var ctx = _listener.GetContext();
						var t = new Task(
							contextObject =>
							{
								var context = contextObject as HttpListenerContext;
								processAction(context);
								context.Response.Close();
							},
							ctx);
						t.Start();
					}
					catch (Exception ex)
					{
						Console.WriteLine(ex.Message);
						return;
					}
				}
			}

			private void Stop()
			{
				_isExiting = true;
				if (_thread != null)
				{
					_listener.Stop();
					if (!_thread.Join(100))
						_thread.Abort();
					_thread = null;
				}

				Thread.Sleep(100);
			}

			public void Dispose()
			{
				Stop();
			}
		}

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
	}
}
