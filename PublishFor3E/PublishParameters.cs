using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace PublishFor3E
    {
    internal class PublishParameters: IEquatable<PublishParameters>
        {
        public Target Target { get; }

        private readonly HashSet<string> _wapis = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public PublishParameters(Target target)
            {
            this.Target = target ?? throw new ArgumentNullException(nameof(target));
            }

        public IEnumerable<string> Wapis => this._wapis;

        public void AddWapi(string wapi)
            {
            if (wapi == null)
                throw new ArgumentNullException(nameof(wapi), "Cannot be null");
            if (string.IsNullOrWhiteSpace(wapi))
                throw new ArgumentOutOfRangeException(nameof(wapi), "Invalid WAPI name");
            this._wapis.Add(wapi);
            }

        public void AddWapis(IEnumerable<string> wapiList)
            {
            if (wapiList == null)
                throw new ArgumentNullException(nameof(wapiList), "Cannot be null");
            var list = wapiList.ToList();
            if (list.Any(string.IsNullOrWhiteSpace))
                throw new ArgumentOutOfRangeException(nameof(wapiList), "Invalid WAPI name in list");
            this._wapis.UnionWith(list);
            }

        public NetworkCredential? WmiCredentials;

        public bool Equals(PublishParameters? other)
            {
            if (other == null) return false;
            return this.Target.Equals(other.Target) && this._wapis.SetEquals(other.Wapis);
            }

        public override bool Equals(object? other)
            {
            if (other == null) return false;
            if (other.GetType() != this.GetType()) return false;
            return Equals((Target)other);
            }

        public override int GetHashCode()
            {
            return this.Target.GetHashCode() ^ this.Wapis.GetHashCode();
            }

        public static bool operator !=(PublishParameters? t1, PublishParameters? t2)
            {
            if (ReferenceEquals(t1, t2))
                return false;
            if ((object?) t1 == null || (object?) t2 == null)
                return true;
            return t1.Target != t2.Target || !t1._wapis.SetEquals(t2.Wapis);
            }

        public static bool operator ==(PublishParameters? t1, PublishParameters? t2)
            {
            if (ReferenceEquals(t1, t2))
                return true;
            if ((object?)t1 == null || (object?)t2 == null)
                return false;
            return t1.Target == t2.Target && t1._wapis.SetEquals(t2.Wapis);
            }

        public override string ToString()
            {
            return $"{this.Target.Environment} ({string.Join(", ", this.Wapis)})";
            }
        }
    }
