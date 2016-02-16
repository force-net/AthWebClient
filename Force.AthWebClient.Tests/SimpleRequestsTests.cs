using System;
using System.IO;
using System.Linq;

using NUnit.Framework;

namespace Force.AthWebClient.Tests
{
	[TestFixture]
	public class SimpleRequestsTests
	{
		[Test]
		public void Lenta_Request_Should_Return_Ok()
		{
			var r = new AthWebRequest("https://lenta.ru/");
			r.Method = "GET";
			// r.ContentLength = 0;
			var response = r.GetResponse();
			Console.WriteLine(response.StatusCode);
			Assert.That(response.StatusCode, Is.EqualTo(200));

			var server = response.Headers.First(x => x.Item1 == "Server").Item2;
			Assert.That(server, Is.EqualTo("nginx"));

			//foreach (var header in response.Headers)
			//{
			//	Console.WriteLine(header.Item1 + ":" + header.Item2);
			//}

			var answer = new StreamReader(response.GetResponseStream()).ReadToEnd();
			// Console.WriteLine(answer);
			Assert.That(answer, Is.StringStarting("<!DOCTYPE html>"));
		}

		[Test, Ignore("Manual")]
		public void Localhost_Request_Should_Return_Ok()
		{
			var r = new AthWebRequest("http://localhost:8221/");
			r.Method = "GET";
			var rs = r.GetRequestStream();
			rs.Close();
			// r.ContentLength = 0;
			var response = r.GetResponse();
			Console.WriteLine(response.StatusCode);
			Assert.That(response.StatusCode, Is.EqualTo(200));

			var answer = new StreamReader(response.GetResponseStream()).ReadToEnd();
			Assert.That(answer, Is.StringStarting("<!DOCTYPE html>"));
		}

		[Test]
		public void Chunked_Response_Should_Be_Processed()
		{
			var r = new AthWebRequest("http://code.jquery.com/jquery-1.11.2.min.js");
			r.AddHeader("Accept-Encoding", "gzip");
			var response = r.GetResponse();

			Console.WriteLine(response.StatusCode);
			Assert.That(response.StatusCode, Is.EqualTo(200));

			// var server = response.Headers.First(x => x.Item1 == "Server").Item2;
			// Assert.That(server, Is.EqualTo("nginx"));

			foreach (var header in response.Headers)
			{
				Console.WriteLine(header.Item1 + ":" + header.Item2);
			}

			response.AutoDecompressResponse = true;
			var answer = new StreamReader(response.GetResponseStream()).ReadToEnd();
			// Console.WriteLine(answer);
			Assert.That(answer, Is.StringStarting("/*! jQuery v1.11.2"));
		}

		[Test]
		public void Google_Https_Request_Should_Return_Ok()
		{
			var r = new AthWebRequest("https://google.com/");
			var isValidated = false;
			r.SslOptions.ServerCertificateValidationCallback += (sender, certificate, chain, errors) => isValidated = true;
			var response = r.GetResponse();
			var ssl = r.SslConnectionInfo;
			Console.WriteLine("CheckCertRevocationStatus: " + ssl.CheckCertRevocationStatus);
			Console.WriteLine("CipherAlgorithm: " + ssl.CipherAlgorithm);
			Console.WriteLine("CipherStrength: " + ssl.CipherStrength);
			Console.WriteLine("HashAlgorithm: " + ssl.HashAlgorithm);
			Console.WriteLine("HashStrength: " + ssl.HashStrength);
			Console.WriteLine("KeyExchangeAlgorithm: " + ssl.KeyExchangeAlgorithm);
			Console.WriteLine("KeyExchangeStrength: " + ssl.KeyExchangeStrength);
			Console.WriteLine("SslPotocol: " + ssl.SslPotocol);
			Console.WriteLine("IsEncrypted: " + ssl.IsEncrypted);
			Console.WriteLine("IsSigned: " + ssl.IsSigned);
			Console.WriteLine("Certificate: " + ssl.ServerCertificate.Subject);

			Assert.That(isValidated, Is.True);
			Console.WriteLine(response.StatusCode);
			Assert.That(response.StatusCode, Is.EqualTo(200).Or.EqualTo(302));

			// invalid now
			/*var server = response.Headers.First(x => x.Item1 == "Server").Item2;
			Assert.That(server, Is.EqualTo("GFE/2.0"));*/

			var answer = new StreamReader(response.GetResponseStream()).ReadToEnd();
			// Console.WriteLine(answer);
			Assert.That(answer, Is.StringStarting("<HTML>"));
		}

		[Test]
		public void Chunked_Lenta_Ru_Should_Be_Processed()
		{
			var r = new AthWebRequest("http://lenta.ru/");
			// r.AddHeader("Host", "www.lenta.ru");
			var response = r.GetResponse();

			Console.WriteLine(response.StatusCode);
			Assert.That(response.StatusCode, Is.EqualTo(200));

			// var server = response.Headers.First(x => x.Item1 == "Server").Item2;
			// Assert.That(server, Is.EqualTo("nginx"));

			foreach (var header in response.Headers)
			{
				Console.WriteLine(header.Item1 + ":" + header.Item2);
			}

			// response.AutoDecompressResponse = true;
			var answer = new StreamReader(response.GetResponseStream()).ReadToEnd();
			// Console.WriteLine(answer);
			Assert.That(answer, Is.StringStarting("<!DOCTYPE html>"));
		}
	}
}
