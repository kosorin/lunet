using System;
using System.Net;
using System.Net.Sockets;

namespace Lure.Net
{
    public class InternetEndPoint : IEndPoint, IEquatable<InternetEndPoint>
    {
        public InternetEndPoint(IPEndPoint endPoint)
        {
            EndPoint = endPoint;
        }

        public InternetEndPoint(string host, int port, AddressFamily addressFamily)
        {
            var hostAddress = NetHelper.ResolveAddress(host, addressFamily);
            if (hostAddress == null)
            {
                throw new NetException($"Could not resolve host '{host}'");
            }
            EndPoint = new IPEndPoint(hostAddress, port);
        }

        public InternetEndPoint(EndPoint endPoint) : this((IPEndPoint)endPoint)
        {
        }

        public InternetEndPoint(IPAddress hostAddress, int port) : this(new IPEndPoint(hostAddress, port))
        {
        }

        public InternetEndPoint(string host, int port) : this(host, port, AddressFamily.InterNetwork)
        {
        }


        public IPEndPoint EndPoint { get; }


        public bool Equals(InternetEndPoint other)
        {
            if (ReferenceEquals(other, this))
            {
                return true;
            }
            if (ReferenceEquals(other, null))
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
            if (ReferenceEquals(obj, null))
            {
                return false;
            }
            return obj is InternetEndPoint other && EqualsCore(other);
        }

        public static bool operator ==(InternetEndPoint left, InternetEndPoint right)
        {
            if (ReferenceEquals(left, right))
            {
                return true;
            }
            if (ReferenceEquals(left, null))
            {
                return false;
            }
            if (ReferenceEquals(right, null))
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
