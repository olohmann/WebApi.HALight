namespace WebApi.HALight
{
    public interface IResource
    {
        /// <summary>
        /// The collection of link relations for this resource.
        /// </summary>
        Relations Relations { get; set; }
    }
}