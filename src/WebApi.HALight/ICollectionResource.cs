namespace WebApi.HALight
{
    using System.Collections.Generic;

    public interface ICollectionResource<TResource> : IResource 
        where TResource : IResource
    {
        ICollection<TResource> Items { get; set; }
    }
}
