using Lunet.Extensions;
using System;
using SystemNet_EndPoint = System.Net.EndPoint;
using SystemNet_IPEndPoint = System.Net.IPEndPoint;

namespace Lunet
{
    public class InternetEndPoint : IEquatable<InternetEndPoint>
    {
        internal InternetEndPoint(SystemNet_EndPoint endPoint) : this((SystemNet_IPEndPoint)endPoint)
        {
        }

        internal InternetEndPoint(SystemNet_IPEndPoint endPoint)
        {
            EndPoint = endPoint;

            Host = EndPoint.Address.ToString();
            Port = EndPoint.Port;
            IPVersion = EndPoint.AddressFamily.ToIPVersion();
        }

        public InternetEndPoint(string host, int port, IPVersion ipVersion = IPVersion.IPv4)
        {
            var hostAddress = IPAddressResolver.Resolve(host, ipVersion.ToAddressFamily());
            if (hostAddress == null)
            {
                throw new NetException($"Could not resolve host '{host}'");
            }
            EndPoint = new SystemNet_IPEndPoint(hostAddress, port);

            Host = host;
            Port = port;
            IPVersion = ipVersion;
        }


        public string Host { get; }

        public int Port { get; }

        public IPVersion IPVersion { get; }

        internal SystemNet_IPEndPoint EndPoint { get; }


        public bool Equals(InternetEndPoint other)
        {
            if (ReferenceEquals(other, this))
            {
                return true;
            }
            if (other is null)
            {
                return false;
            }
            return EqualsCore(other);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(obj, this))
            {
                return true;
            }
            return obj is InternetEndPoint other && EqualsCore(other);
        }

        public static bool operator ==(InternetEndPoint left, InternetEndPoint right)
        {
            if (ReferenceEquals(left, right))
            {
                return true;
            }
            if (left is null || right is null)
            {
                return false;
            }
            return left.EqualsCore(right);
        }

        public static bool operator !=(InternetEndPoint left, InternetEndPoint right)
        {
            return !(left == right);
        }

        protected virtual bool EqualsCore(InternetEndPoint other)
        {
            return EndPoint.Equals(other.EndPoint);
        }

        public override int GetHashCode()
        {
            return EndPoint.GetHashCode();
        }

        public override string ToString()
        {
            return EndPoint.ToString();
        }
    }
}
