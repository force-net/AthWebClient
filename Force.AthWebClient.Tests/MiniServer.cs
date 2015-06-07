using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Force.AthWebClient.Tests
{
	internal class MiniServer : IDisposable
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
					if (!_isExiting)
						Console.WriteLine("MiniServer Context Error: " + ex.Message);
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
}