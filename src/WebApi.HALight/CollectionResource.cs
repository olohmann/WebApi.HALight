namespace WebApi.HALight
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;

    public class CollectionResource<TResource> : ICollectionResource<TResource>
        where TResource : IResource
    {
        public CollectionResource()
        {
            Relations = new Relations();
            Items = new Collection<TResource>();
        }

        /// <summary>
        /// The collection of relations for the resource.
        /// </summary>
        public Relations Relations { get; set; }

        /// <summary>
        /// A collection of sub-resources.
        /// </summary>
        public ICollection<TResource> Items { get; set; }

        /// <summary>
        /// Used by conversion routines.
        /// </summary>
        internal void AddItem(object item)
        {
            Items.Add((TResource)item);
        }
    }
}