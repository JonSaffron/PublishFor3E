using System;

namespace PublishFor3E
    {
    internal class Target : IEquatable<Target>
        {
        public readonly Uri BaseUri;
        public readonly string Environment;

        public static Target Parse(string url)
            {
            if (!TryParse(url, out Target? result, out string? reason))
                {
                throw new FormatException(reason);
                }

            return result!;
            }

        public static bool TryParse(string url, out Target? result, out string? reason)
            {
            result = null;

            if (string.IsNullOrWhiteSpace(url))
                {
                reason = "Cannot be null, an empty string, or white space";
                return false;
                }

            Uri uri;
            try
                {
                uri = new Uri(url);
                }
            catch (Exception ex)
                {
                reason = ex.Message;
                return false;
                }

            if (!uri.IsAbsoluteUri)
                {
                reason = "URL must be specified in full";
                return false;
                }

            if (!uri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) &&
                !uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
                {
                reason = "URL scheme must be http or https";
                return false;
                }

            if (uri.Segments.Length < 2)
                {
                reason = "URL is not long enough to identify a particular 3E environment";
                return false;
                }

            string environment = uri.AbsolutePath.Split('/')[1].ToUpperInvariant();
            if (!environment.StartsWith("TE_3E_", StringComparison.OrdinalIgnoreCase))
                {
                reason = "URL path does not look like a 3E environment - it must start TE_3E_";
                return false;
                }

            result = new Target(new Uri(uri, $"/{environment}/"), environment);
            reason = null;
            return true;
            }

        private Target(Uri baseUri, string environment)
            {
            this.BaseUri = baseUri;
            this.Environment = environment;
            }

        public bool Equals(Target? other)
            {
            if (other == null) return false;
            return this.BaseUri.Equals(other.BaseUri);
            }

        public override bool Equals(object? other)
            {
            if (other == null) 
                return false;
            if (other.GetType() != this.GetType()) 
                return false;
            return Equals((Target)other);
            }

        public override int GetHashCode()
            {
            return this.BaseUri.GetHashCode();
            }

        public static bool operator !=(Target? t1, Target? t2)
            {
            if (ReferenceEquals(t1, t2))
                return false;
            if ((object?) t1 == null || (object?) t2 == null)
                return true;
            return t1.BaseUri != t2.BaseUri;
            }

        public static bool operator ==(Target? t1, Target? t2)
            {
            if (ReferenceEquals(t1, t2))
                return true;
            if ((object?)t1 == null || (object?)t2 == null)
                return false;
            return t1.BaseUri == t2.BaseUri;
            }

        public override string ToString()
            {
            return $"{this.Environment}: {this.BaseUri}";
            }
        }
    }
