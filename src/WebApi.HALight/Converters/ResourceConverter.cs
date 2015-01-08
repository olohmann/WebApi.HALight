namespace WebApi.HALight.Converters
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    internal class ResourceConverter : JsonConverter
    {
        public class ReservedPropertyNames
        {
            public const string Relations = "_links";
            public const string EmbeddedResources = "_embedded";

            public static IEnumerable<string> Names
            {
                get
                {
                    yield return Relations;
                    yield return EmbeddedResources;
                }
            }
        }
        
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            serializer.Converters.Remove(this);
            var root = new JObject();
            ConvertRecursively((IResource)value, root, serializer);
            root.WriteTo(writer);
            serializer.Converters.Add(this);
        }

        private void ConvertRecursively(IResource currentResource, JObject node, JsonSerializer serializer)
        {
            if (currentResource == null)
            {
                return;
            }

            var currentResourceType = currentResource.GetType();
            var readableProperties = GetReadablePropertyInfos(currentResourceType);

            var nonResourceProperties = readableProperties.Where(IsNeitherResourceOrReservedProperty).ToList();
            var resourceProperties = readableProperties.Where(IsResourceProperty).ToList();

            node.Add(ReservedPropertyNames.Relations, JObject.FromObject(currentResource.Relations, serializer));
            var embeddedResourceObject = new JObject();
            node.Add(ReservedPropertyNames.EmbeddedResources, embeddedResourceObject);

            foreach (var resourceProperty in resourceProperties)
            {                
                var embeddedResourceNodeValue = new JObject();

                ConvertRecursively((IResource)resourceProperty.GetValue(currentResource), embeddedResourceNodeValue, serializer);
                embeddedResourceObject.Add(ToCamelCase(resourceProperty.Name), embeddedResourceNodeValue);
            }

            if (IsCollectionResourceType(currentResourceType))
            {
                var currentResourceDynamic = (dynamic) currentResource;
                var jArray = new JArray();
                string name = "";
                foreach (IResource resourceItem in currentResourceDynamic.Items)
                {
                    var embeddedResourceNodeValue = new JObject();
                    ConvertRecursively(resourceItem, embeddedResourceNodeValue, serializer);
                    jArray.Add(embeddedResourceNodeValue);
                    name =  resourceItem.GetType().Name;
                }

                // Remove the "Resource" by convention.
                if (name.EndsWith("Resource"))
                {
                    name = name.Remove(name.LastIndexOf("Resource", StringComparison.Ordinal));
                }

                embeddedResourceObject.Add(ToCamelCase(name), jArray);
            }

            foreach (var nonResourceProperty in nonResourceProperties)
            {
                var value = nonResourceProperty.GetValue(currentResource);
                if (value != null && value.GetType().IsClass && value.GetType() != typeof(string))
                {
                    node.Add(ToCamelCase(nonResourceProperty.Name), JObject.FromObject(value));
                }
                else
                {
                    node.Add(ToCamelCase(nonResourceProperty.Name), new JValue(value));
                }                
            }
        }

        private static bool IsResourceProperty(PropertyInfo propertyInfo)
        {
            return IsResourceType(propertyInfo.PropertyType);
        }

        private static bool IsCollectionResourceType(Type type)
        {
            return
                type.GetInterfaces()
                    .Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(ICollectionResource<>));
        }

        private static bool IsResourceType(Type type)
        {
            return typeof(IResource).IsAssignableFrom(type);
        }

        private static bool IsNeitherResourceOrReservedProperty(PropertyInfo propertyInfo)
        {
            return !IsResourceProperty(propertyInfo) && !IsReservedProperty(propertyInfo);
        }

        private static List<PropertyInfo> GetReadablePropertyInfos(Type type)
        {
            return type.GetRuntimeProperties().Where(_ => _.CanRead).ToList();
        }

        private static bool IsReservedProperty(PropertyInfo propertyInfo)
        {
            var res = false;

            if (IsCollectionResourceType(propertyInfo.DeclaringType))
            {
                var interfacePropertyNames = typeof(ICollectionResource<>).GetRuntimeProperties().Where(_ => _.CanRead).Select(_ => _.Name).ToList();
                res |= interfacePropertyNames.Any(propertyName => propertyInfo.Name == propertyName);
            }

            if (IsResourceType(propertyInfo.DeclaringType))
            {
                var resourceProperties = typeof(IResource).GetRuntimeProperties().Where(_ => _.CanRead).Select(_ => _.Name).ToList();
                res |= resourceProperties.Any(propertyName => propertyInfo.Name == propertyName);
            }

            return res;
        }

        public static string ToCamelCase(string s)
        {
            if (string.IsNullOrEmpty(s))
                return s;

            if (!char.IsUpper(s[0]))
                return s;

            var chars = s.ToCharArray();

            for (var i = 0; i < chars.Length; i++)
            {
                var hasNext = (i + 1 < chars.Length);
                if (i > 0 && hasNext && !char.IsUpper(chars[i + 1]))
                    break;

                chars[i] = char.ToLowerInvariant(chars[i]);
            }

            return new string(chars);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var currentResourceInstance = (IResource)Activator.CreateInstance(objectType);
            var jToken = JToken.ReadFrom(reader);
            if (jToken[ReservedPropertyNames.Relations] != null)
            {
                currentResourceInstance.Relations = serializer.Deserialize<Relations>(jToken[ReservedPropertyNames.Relations].CreateReader());
            }

            var simpleProperties =
                jToken.Where(_ => _.GetType() == typeof (JProperty))
                    .Cast<JProperty>()
                    .Where(_ => !ReservedPropertyNames.Names.Contains(_.Name));

            // Create a new JObject that will receive all "simple" non-reserved properties.
            // This will be the vehicle to populate the resource instance.
            var populater = new JObject();
            foreach (JProperty jProperty in simpleProperties)
            {
                populater.Add(jProperty);
            }

            serializer.Populate(populater.CreateReader(), currentResourceInstance);

            if (jToken[ReservedPropertyNames.EmbeddedResources] != null)
            {
                JToken embeddedResourceObject = jToken[ReservedPropertyNames.EmbeddedResources];
                foreach (var embeddedObjectProperty in embeddedResourceObject.Where(_ => _.GetType() == typeof(JProperty)).Cast<JProperty>())
                {
                    if (embeddedObjectProperty.Value.GetType() == typeof (JArray))
                    {
                        if (objectType.Name.ToLower().Contains(embeddedObjectProperty.Name))
                        {
                            var dynamicCurrent = (dynamic)currentResourceInstance;
                            var itemsProperty = objectType.GetProperty("Items");
                            var collectionResourceType = itemsProperty.PropertyType.GetGenericArguments().First();

                            // Recursion
                            foreach (var arrayElem in ((JArray) embeddedObjectProperty.Value))
                            {
                                var resourceElem =
                                    (IResource)
                                        serializer.Deserialize(arrayElem.CreateReader(), collectionResourceType);
                            
                                dynamicCurrent.AddItem(resourceElem);
                            }
                        }
                    }
                    else
                    {
                        var assignableProperty = objectType.GetProperties()
                            .Where(_ => typeof (IResource).IsAssignableFrom(_.PropertyType))
                            .FirstOrDefault(_ => _.PropertyType.Name.ToLower().Contains(embeddedObjectProperty.Name));
                        if (assignableProperty != null)
                        {
                            // Recursion
                            var resourceElem = serializer.Deserialize(embeddedObjectProperty.Value.CreateReader(),
                                assignableProperty.PropertyType);
                            assignableProperty.SetValue(currentResourceInstance, resourceElem);
                        }
                    }
                }
            }

            return currentResourceInstance;
        }

        public override bool CanConvert(Type objectType)
        {
            return typeof(IResource).IsAssignableFrom(objectType);
        }

        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanWrite
        {
            get { return true; }
        }
    }
}