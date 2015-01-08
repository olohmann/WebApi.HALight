namespace WebApi.HALight
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class Relations : IEquatable<Relations>
    {
        private readonly List<Link> _links;

        public Relations()
        {
            _links = new List<Link>();
        }

        public void Add(Link link)
        {
            _links.Add(link);    
        }

        public IEnumerable<Link> Links
        {
            get { return _links; }
        }

        #region Equals Support
        public override bool Equals(object other)
        {
            return Equals(other as Relations);
        }

        public virtual bool Equals(Relations other)
        {
            return AreEqual(this, other);
        }

        public override int GetHashCode()
        {
            const int startValue = 13;
            const int multiplier = 7;

            unchecked
            {
                int hashCode = startValue;

                foreach (var link in Links)
                {
                    hashCode = hashCode * multiplier + link.GetHashCode();
                }

                return hashCode;
            }
        }
       
        public static bool operator ==(Relations x, Relations y)
        {
            return Object.Equals(x, y);
        }

        public static bool operator !=(Relations x, Relations y)
        {
            return !(x == y);
        }

        private static bool AreEqual(Relations left, Relations right)
        {
            if (object.ReferenceEquals(left, null))
            {
                return right == null;
            }

            if (object.ReferenceEquals(right, null))
            {
                return false;
            }

            var leftLinks = left.Links.OrderBy(x => x.GetHashCode()).ToList();
            var rightLinks = right.Links.OrderBy(x => x.GetHashCode()).ToList();

            return leftLinks.SequenceEqual(rightLinks);
        }

       
        #endregion
    }
}