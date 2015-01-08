namespace WebApi.HALight.UnitTests
{
    using System.Linq;
    using Newtonsoft.Json;
    using NUnit.Framework;
    using WebApi.HALight.Converters;

    [TestFixture]
    public class RelationConverterFixture
    {
        [TestCase]
        public void VerifyRoundtrip()
        {
            // Arrange
            var relations = new Relations();
            relations.Add(Link.CreateSelfLink("http://localhost/users"));
            relations.Add(Link.CreateLink("dt:admins", "http://locahost/users/admins"));
            relations.Add(Link.CreateLink("admin", "http://locahost/users/admins/1"));
            relations.Add(Link.CreateLink("admin", "http://locahost/users/admins/2"));
            relations.Add(Link.CreateCuriesLink("http://locahost/doc/{rel}", "dt"));

            // Act
            var serializedRelations = JsonConvert.SerializeObject(relations, new RelationsConverter());
            var deserializedRelations = JsonConvert.DeserializeObject<Relations>(serializedRelations, new RelationsConverter());
            var t = JsonConvert.SerializeObject(deserializedRelations, new RelationsConverter());
            
            // Assert
            Assert.AreEqual(relations, deserializedRelations);
        }
    }
}
