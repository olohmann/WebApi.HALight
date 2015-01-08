namespace WebApi.HALight.Converters
{
    using System;
    using System.Linq;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    internal class RelationsConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var relations = (Relations)value;

            writer.WriteStartObject();

            var groupedByRel = relations.Links
                .GroupBy(_ => _.Rel)
                .ToDictionary(_ => _.Key, _ => _.ToArray());

            var allRelsExceptWellKnown = groupedByRel.Keys
                .Where(key => key != Link.CuriesRel && key != Link.SelfRel).ToList();

            // Not strictly required, but we will write the nodes in a nice order.
            if (groupedByRel.ContainsKey(Link.SelfRel))
            {
                WriteLinks(writer, serializer, Link.SelfRel, links: groupedByRel[Link.SelfRel]);
            }

            if (groupedByRel.ContainsKey(Link.CuriesRel))
            {
                WriteLinks(writer, serializer, Link.CuriesRel, links: groupedByRel[Link.CuriesRel]);
            }

            foreach (var rel in allRelsExceptWellKnown)
            {
                WriteLinks(writer, serializer, rel, links: groupedByRel[rel]);
            }

            writer.WriteEndObject();
        }        

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var relations = new Relations();
            
            var jToken = JToken.ReadFrom(reader);
            foreach (JProperty jProperty in jToken.Where(_ => _.GetType() == typeof(JProperty)).Cast<JProperty>())
            {
                if (jProperty.Value.GetType() == typeof (JArray))
                {
                    var links = serializer.Deserialize<Link[]>(jProperty.Value.CreateReader());
                    foreach (var link in links)
                    {
                        relations.Add(link);
                        link.Rel = jProperty.Name;
                    }
                }
                else
                {
                    var link = serializer.Deserialize<Link>(jProperty.Value.CreateReader());
                    link.Rel = jProperty.Name;
                    relations.Add(link);
                }
            }

            return relations;
        }

        public override bool CanConvert(Type objectType)
        {
            return typeof(Relations) == objectType;
        }

        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanWrite
        {
            get { return true; }
        }

        private static void WriteLinks(JsonWriter writer, JsonSerializer serializer, string rel, Link[] links)
        {
            writer.WritePropertyName(rel);

            // Special case for Curies: always write an array.
            if (links.Length > 1 || rel == Link.CuriesRel)
            {
                serializer.Serialize(writer, links);
            }
            else if (links.Length == 1)
            {
                serializer.Serialize(writer, links[0]);
            }
            else
            {
                throw new InvalidOperationException(string.Format(
                    "Unexpected: Found an empty array for Link group: {0}", rel));
            }
        }
    }
}
