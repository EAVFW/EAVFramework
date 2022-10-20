using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace EAVFramework.UnitTest.ManifestTests
{
    [TestClass]
    public class BinaryTests: BaseManifestTests
    {

        [TestMethod]
        [DeploymentItem(@"ManifestTests/specs/BinaryShouldGenerateBinaryColumn.sql", "specs")]
        public async Task BinaryShouldGenerateBinaryColumn()
        {
            var manifest = JToken.FromObject(new
            {
                entities = new
                {
                    CustomEntity = new
                    {
                        schemaName = "CustomEntity",
                        logicalName = "customEntity",
                        pluralName = "customentities",
                        collectionSchemaName = "CustomEntities",
                        attributes = new
                        {
                            id = new
                            {
                                isPrimaryKey = true,
                                schemaName = "Id",
                                logicalName = "id",
                                type = "guid"
                            },
                            name = new
                            {
                                schemaName = "Name",
                                logicalName = "name",
                                type = "string",
                                isPrimaryField = true
                            },
                            blob = new
                            {
                                schemaName = "Data",
                                logicalName = "Data",
                                type = "binary"
                            }
                        }
                    }
                }
            });



          var sql=  RunDBWithSchema("manifest_binary", manifest);

            //Assure

            string expectedSQL = System.IO.File.ReadAllText(@"specs/BinaryShouldGenerateBinaryColumn.sql");

            MigrationAssert.AreEqual(expectedSQL, sql);
        }

    }
}
