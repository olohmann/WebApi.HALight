namespace WebApi.HALight.Formatters
{
    using System;
    using System.Net.Http.Formatting;
    using System.Net.Http.Headers;
    using Newtonsoft.Json;
    using WebApi.HALight.Converters;

    public class SimpleHalFormatter : JsonMediaTypeFormatter
    {
        public const string HalMediaType = "application/hal+json";

        public SimpleHalFormatter()
        {
            SupportedMediaTypes.Add(new MediaTypeHeaderValue(HalMediaType));
            SerializerSettings.Converters.Add(new RelationsConverter());
            SerializerSettings.Converters.Add(new ResourceConverter());
            SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
            SerializerSettings.DefaultValueHandling = DefaultValueHandling.Ignore;
        }

        public bool IndentJson
        {
            get { return SerializerSettings.Formatting == Formatting.Indented; }
            set { SerializerSettings.Formatting = value ? Formatting.Indented : Formatting.None; }
        }

        public override bool CanReadType(Type type)
        {
            return typeof(IResource).IsAssignableFrom(type);
        }

        public override bool CanWriteType(Type type)
        {
            return typeof(IResource).IsAssignableFrom(type);
        }
    }
}
