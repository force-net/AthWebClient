using System;
using System.Net;

namespace Force.AthWebClient
{
	public class AthEndPoint
	{
		public enum SchemeType
		{
			Http,
			Https
		}

		public SchemeType Scheme { get; private set; }

		public bool IsHostIpAddress { get; private set; }

		public string Host { get; private set; }

		public string PathAndQuery { get; private set; }

		public int Port { get; private set; }

		public bool IsDefaultPort { get; private set; }

		private IPAddress _ipAddress;

		public IPAddress Address
		{
			get
			{
				if (!IsHostIpAddress)
					throw new InvalidOperationException("Cannot Get IPAddress of dns host");

				return _ipAddress;
			}
		}

		/// <summary>
		/// returns escaped path and query, if source data looks like unescaped
		/// </summary>
		public string GetPathAndQueryEscaped()
		{
			var shouldEscape = false;
			foreach (var c in PathAndQuery)
			{
				if (c <= 32 || c >= 127)
				{
					shouldEscape = true;
				}
			}

			if (shouldEscape) 
				return Uri.EscapeDataString(PathAndQuery);
			return PathAndQuery;
		}

		private AthEndPoint()
		{
		}

		public static AthEndPoint FromUrl(string url)
		{
			return FromUrl(new Uri(url));
		}

		public static AthEndPoint FromUrl(Uri url)
		{
			var isIp = url.HostNameType == UriHostNameType.IPv4 || url.HostNameType == UriHostNameType.IPv6;
			return FromEndPoint(
				url.Scheme,
				isIp ? (EndPoint)new IPEndPoint(IPAddress.Parse(url.Host), url.Port) : new DnsEndPoint(url.Host, url.Port),
				url.PathAndQuery);
		}

		public static AthEndPoint FromEndPoint(string scheme, EndPoint endPoint, string pathAndQuery)
		{
			int defaultPort;
			var ep = new AthEndPoint();
			if (scheme == "http")
			{
				ep.Scheme = SchemeType.Http;
				defaultPort = 80;
			}
			else if (scheme == "https")
			{
				ep.Scheme = SchemeType.Https;
				defaultPort = 443;
			}
			else throw new NotSupportedException("Only http(s) scheme supported now");

			var ipEndPoint = endPoint as IPEndPoint;
			var dnsEndPoint = endPoint as DnsEndPoint;

			if (ipEndPoint != null)
			{
				ep.IsHostIpAddress = true;
				ep._ipAddress = ipEndPoint.Address;
				ep.Host = ipEndPoint.Address.ToString();
				ep.Port = ipEndPoint.Port;
			}
			else if (dnsEndPoint != null)
			{
				ep.IsHostIpAddress = false;
				ep.Host = dnsEndPoint.Host;
				ep.Port = dnsEndPoint.Port;
			}
			else
			{
				throw new NotSupportedException("Invalid endPoint type");
			}

			ep.PathAndQuery = pathAndQuery ?? "/";
			ep.IsDefaultPort = ep.Port == defaultPort;

			return ep;
		}

		protected bool Equals(AthEndPoint other)
		{
			return Scheme == other.Scheme && string.Equals(Host, other.Host) && Port == other.Port;
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj))
				return false;

			if (ReferenceEquals(this, obj))
				return true;
			
			if (obj.GetType() != this.GetType())
				return false;

			return Equals((AthEndPoint)obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				var hashCode = (int)Scheme;
				hashCode = (hashCode * 397) ^ Host.GetHashCode();
				hashCode = (hashCode * 397) ^ Port;
				return hashCode;
			}
		}
	}
}
