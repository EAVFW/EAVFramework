using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;

namespace EAVFramework.UnitTest.ManifestTests
{


    [TestClass]
    public class RowVersionTests : BaseManifestTests
    {

        [TestMethod]
        [DeploymentItem(@"ManifestTests/Specs/RowVersionShouldGenerateRowversionColumn.sql", "Specs")]
        public async Task RowVersionShouldGenerateRowversionColumn()
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
                            rowversion = new
                            {
                                schemaName = "RowVersion",
                                logicalName = "rowversion",
                                type = new
                                {
                                    type = "binary",
                                    sql = new
                                    {
                                        rowVersion = true,
                                    },
                                },

                                isRowVersion = true,
                                isRequired = true,
                            }
                        }
                    }
                }
            });



            var sql = RunDBWithSchema("manifest_rowversion", manifest);

            //Assure

            string expectedSQL = System.IO.File.ReadAllText(@"Specs/RowVersionShouldGenerateRowversionColumn.sql");

            MigrationAssert.AreEqual(expectedSQL, sql);
        }
    }
}
