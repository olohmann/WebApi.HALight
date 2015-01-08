namespace WebApi.HALight
{
    public class Resource : IResource
    {
        public Resource()
        {
            Relations = new Relations();
        }

        /// <summary>
        /// The collection of link relations for this resource.
        /// </summary>
        public Relations Relations { get; set; }
    }
}