namespace WebApi.HALight
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Text.RegularExpressions;
    using Newtonsoft.Json;

    /// <summary>
    /// Read the specification of Link Objects for details:
    /// https://tools.ietf.org/html/draft-kelly-json-hal-06#page-4
    /// </summary>
    public class Link : IEquatable<Link>
    {
        private static readonly Regex _IsTemplatedRegex = new Regex(@"{.+}", RegexOptions.Compiled);

        public const string SelfRel = "self";
        public const string CuriesRel = "curies";
        public const string CuriesRelPattern = "{rel}";
        public const char HrefCurieSeparator = ':';
        public static readonly string HrefCurieSeparatorStr = new string(HrefCurieSeparator, 1);

        /// <summary>
        /// Tool usage only.
        /// </summary>
        protected Link()
        {
        }

        protected Link(string rel, string href)
        {
            if (!IsHrefValid(href))
            {
                throw new ArgumentException("invalid href", "href");
            }

            if (!IsRelValid(rel))
            {
                throw new ArgumentException("invalid rel", "rel");
            }

            Rel = rel;
            Href = href;
        }

        /// <summary>
        /// Create a CURIE link.
        /// </summary>
        /// <param name="href">The href for the link. Requires a placeholder <c>{rel}</c>.</param>
        /// <param name="name">The CURIE name.</param>
        /// <returns>A CURIE link.</returns>
        public static Link CreateCuriesLink(string href, string name)
        {
            if (!IsHrefValid(href))
            {
                throw new ArgumentException("invalid href", "href");
            }

            if (href.IndexOf(CuriesRelPattern, StringComparison.OrdinalIgnoreCase) == 0)
            {
                throw new ArgumentException("invalid href: no curie template present", "href");                
            }

            if (href.IndexOf(CuriesRelPattern, StringComparison.OrdinalIgnoreCase)
                != href.LastIndexOf(CuriesRelPattern, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("invalid href: multiple curie templates present", "href");
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("name must be defined for curie", "name");
            }

            return new Link(CuriesRel, href) { Name = name };
        }

        /// <summary>
        /// Create a self link relation.
        /// </summary>
        /// <param name="href">The href for self.</param>
        /// <returns>A self link relation.</returns>
        public static Link CreateSelfLink(string href)
        {
            if (!IsHrefValid(href))
            {
                throw new ArgumentException("invalid href", "href");
            }

            return new Link(SelfRel, href);
        }

        /// <summary>
        /// Creates a link for the given rel and href.
        /// </summary>
        /// <param name="rel">The relation.</param>
        /// <param name="href">The href.</param>
        /// <returns>A link.</returns>
        public static Link CreateLink(string rel, string href)
        {            
            return new Link(rel, href);
        }

        /// <summary>
        /// Returns an empty string if not curie prefix is set.
        /// </summary>
        [JsonIgnore]
        public string CuriePrefix
        {
            get
            {
                if (IsRelValid(Rel))
                {
                    throw new InvalidOperationException("Invalid object state: Href is not valid.");
                }

                return Rel.Substring(0, Rel.IndexOf(":", StringComparison.OrdinalIgnoreCase));
            }
        }

        /// <summary>
        /// Rel. 
        /// </summary>
        [JsonIgnore]
        public string Rel { get; set; }
        
        /// <summary>
        /// Section 5.1. REQUIRED.
        /// </summary>
        [JsonProperty("href")]
        public string Href { get; set; } 

        /// <summary>
        /// Section 5.2. OPTIONAL.
        /// </summary>
        [JsonProperty("templated")]
        public bool IsTemplated 
        {
            get { return _IsTemplatedRegex.IsMatch(Href); }
        }

        /// <summary>
        /// Section 5.3. OPTIONAL.
        /// </summary>
        [JsonProperty("type")]
        public string Type { get; set; }

        /// <summary>
        /// Section 5.4. OPTIONAL.
        /// </summary>
        [JsonProperty("deprecation")]
        public bool IsDeprecated { get; set; }

        /// <summary>
        /// Section 5.5. OPTIONAL.
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set;  }

        /// <summary>
        /// Section 5.6. OPTIONAL.
        /// </summary>
        [JsonProperty("profile")]
        public string Profile { get; set; }

        /// <summary>
        /// Section 5.7. OPTIONAL.
        /// </summary>
        [JsonProperty("title")]
        public string Title { get; set; }

        /// <summary>
        /// Section 5.8. OPTIONAL.
        /// </summary>
        [JsonProperty("hreflang")]
        public string HrefLang { get; set; }

        private static bool IsHrefValid(string href)
        {
            return !string.IsNullOrWhiteSpace(href);
        }

        private static bool IsRelValid(string rel)
        {
            // Rel is valid if it is flat or has a Curie Prefix.
            return
                !string.IsNullOrWhiteSpace(rel)
                && !rel.StartsWith(HrefCurieSeparatorStr)
                && rel.Count(_ => _ == HrefCurieSeparator) <= 1;
        }

        #region Equals Support

        public override bool Equals(object other)
        {
            return Equals(other as Link);
        }

        public override int GetHashCode()
        {
            IEnumerable<FieldInfo> fields = GetFieldsIncludingBaseTypes(GetType());

            const int startValue = 13;
            const int multiplier = 7;

            unchecked
            {
                int hashCode = startValue;

                foreach (FieldInfo field in fields)
                {
                    object value = field.GetValue(this);

                    if (value != null)
                    {
                        hashCode = hashCode * multiplier + value.GetHashCode();
                    }
                }

                return hashCode;
            }
        }

        public virtual bool Equals(Link other)
        {
            return AreEqual(this, other);
        }

        public static bool operator ==(Link x, Link y)
        {
            return Object.Equals(x, y);
        }

        public static bool operator !=(Link x, Link y)
        {
            return !(x == y);
        }

        private static bool AreEqual(Link left, Link right)
        {
            if (object.ReferenceEquals(left, null))
            {
                return right == null;
            }

            if (object.ReferenceEquals(right, null))
            {
                return false;
            }

            Type typeLeft = left.GetType();
            Type typeRight = right.GetType();
            if (typeLeft != typeRight)
            {
                return false;
            }

            IEnumerable<FieldInfo> fields = GetFieldsIncludingBaseTypes(typeLeft);
            foreach (FieldInfo field in fields)
            {
                object value1 = field.GetValue(left);
                object value2 = field.GetValue(right);

                if (object.ReferenceEquals(value1, null))
                {
                    if (!object.ReferenceEquals(value2, null))
                    {
                        return false;
                    }
                }
                else if (!value1.Equals(value2))
                {
                    return false;
                }
            }

            return true;
        }

        private static IEnumerable<FieldInfo> GetFieldsIncludingBaseTypes(Type t)
        {
            var fields = new List<FieldInfo>();

            while (t != typeof(object))
            {
                fields.AddRange(t.GetRuntimeFields());
                t = t.GetTypeInfo().BaseType;
            }

            return fields;
        }
        #endregion
    }
}