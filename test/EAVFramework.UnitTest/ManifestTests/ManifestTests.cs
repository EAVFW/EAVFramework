using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Reflection;

namespace EAVFramework.UnitTest.ManifestTests
{
    public static class MigrationAssert
    {


        public static void AreEqual(string expected, string actual)
        {
            var version = typeof(DbContext).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;

            expected = expected.Replace("{{VERSION}}", version);

            Assert.AreEqual(expected, actual);

        }
    }
    [TestClass]
    public class ManifestTests : BaseManifestTests
    {


        [TestMethod]
        [DeploymentItem(@"ManifestTests/specs/CascadingTest.manifest.json", "specs")]
        [DeploymentItem(@"ManifestTests/specs/CascadingTest.sql", "specs")]
        public async Task TestCascading()
        {
            //Arrange
            var manifest = JToken.Parse(File.ReadAllText(@"specs\CascadingTest.manifest.json"));


            //Act
            var sql = RunDBWithSchema("manifest_migrations", manifest);


            //Assure

            string expectedSQL = System.IO.File.ReadAllText(@"specs\CascadingTest.sql");

            MigrationAssert.AreEqual(expectedSQL, sql);

        }

       
       


        [TestMethod]
        [DeploymentItem(@"ManifestTests/specs/CarsAndTrucksModel.sql","specs")]
        public async Task CarsAndTrucksModelTest()
        {
            //Arrange
            var manifest = JToken.FromObject(new
            {
                version = "1.0.0",
                entities = new
                {
                    Car = CreateCustomEntity("Car", "Cars"),
                    Truck = CreateCustomEntity("Truck", "Trucks")
                }
            });


            //Act
            var sql = RunDBWithSchema("manifest_migrations", manifest);


            //Assure

            string expectedSQL = System.IO.File.ReadAllText(@"specs\CarsAndTrucksModel.sql");

            MigrationAssert.AreEqual(expectedSQL, sql);

        }



        [TestMethod]
        [DeploymentItem(@"ManifestTests/specs/CarsAndTrucksModel_AddEntity.sql", "specs")]
        public async Task CarsAndTrucksModel_AddEntityTest()
        {
            var manifestA = JToken.FromObject(new
            {
                version = "1.0.0",
                entities = new
                {
                    Car = CreateCustomEntity("Car", "Cars"),
                }
            });

           

            var manifestB = JToken.FromObject(new
            {
                version = "1.0.1",
                entities = new
                {
                    Car = CreateCustomEntity("Car", "Cars"),
                    Truck = CreateCustomEntity("Truck", "Trucks")
                }
            });
 

            var sql = RunDBWithSchema("manifest_migrations", manifestB, manifestA);

            string expectedSQL = System.IO.File.ReadAllText(@"specs\CarsAndTrucksModel_AddEntity.sql");

            MigrationAssert.AreEqual(expectedSQL, sql);
        }


        [TestMethod]
        [DeploymentItem(@"ManifestTests/specs/CarsAndTrucksModel_ChangePropertyTextLength.sql", "specs")]
        public async Task CarsAndTrucksModel_ChangePropertyTextLength()
        {
            var manifestA = JToken.FromObject(new
            {
                version = "1.0.0",
                entities = new
                {
                    Car = CreateCustomEntity("Car", "Cars"),
                }
            });

            AppendAttribute(manifestA, "Car", "FullName", new
            {
                schemaName = "FullName",
                logicalName = "fullname",
                type = new
                {
                    type = "Text",
                    maxLength = 100

                },

            });

            var manifestB = JToken.FromObject(new
            {
                version = "1.0.1",
                entities = new
                {
                    Car = CreateCustomEntity("Car", "Cars"),
                    Truck = CreateCustomEntity("Truck", "Trucks")
                }
            });

            AppendAttribute(manifestB, "Car", "FullName", new
            {
                schemaName = "FullName",
                logicalName = "fullname",
                type = new
                {
                    type = "Text",
                    maxLength = 255

                },

            });

            var sql = RunDBWithSchema("manifest_migrations", manifestB, manifestA);

            string expectedSQL = System.IO.File.ReadAllText(@"specs\CarsAndTrucksModel_ChangePropertyTextLength.sql");

            MigrationAssert.AreEqual(expectedSQL, sql);
        }

        [TestMethod]
        [DeploymentItem(@"ManifestTests/specs/CarsAndTrucksModel_ChangePropertyTextLengthTwice.sql", "specs")]
        public async Task CarsAndTrucksModel_ChangePropertyTextLengthTwice()
        {
            var manifestA = JToken.FromObject(new
            {
                version = "1.0.0",
                entities = new
                {
                    Car = CreateCustomEntity("Car", "Cars"),
                }
            });

            AppendAttribute(manifestA, "Car", "FullName", new
            {
                schemaName = "FullName",
                logicalName = "fullname",
                type = new
                {
                    type = "Text",
                    maxLength = 100

                },

            });

            var manifestB = JToken.FromObject(new
            {
                version = "1.0.1",
                entities = new
                {
                    Car = CreateCustomEntity("Car", "Cars"),
                    Truck = CreateCustomEntity("Truck", "Trucks")
                }
            });

            AppendAttribute(manifestB, "Car", "FullName", new
            {
                schemaName = "FullName",
                logicalName = "fullname",
                type = new
                {
                    type = "Text",
                    maxLength = 255

                },

            });


            var manifestC = JToken.FromObject(new
            {
                version = "1.0.10",
                entities = new
                {
                    Car = CreateCustomEntity("Car", "Cars"),
                    Truck = CreateCustomEntity("Truck", "Trucks")
                }
            });

            AppendAttribute(manifestC, "Car", "FullName", new
            {
                schemaName = "FullName",
                logicalName = "fullname",
                type = new
                {
                    type = "Text",
                    maxLength = 555

                },

            });

            var sql = RunDBWithSchema("manifest_migrations", manifestC, manifestB, manifestA);

            string expectedSQL = System.IO.File.ReadAllText(@"specs\CarsAndTrucksModel_ChangePropertyTextLengthTwice.sql");

            MigrationAssert.AreEqual(expectedSQL, sql);
        }

        [TestMethod]
        [DeploymentItem(@"ManifestTests/specs/CarsAndTrucksModel_AddLookup.sql", "specs")]
        public async Task CarsAndTrucksModel_AddLookup()
        {
            var manifestA = JToken.FromObject(new
            {
                version = "1.0.0",
                entities = new
                {
                    Car = CreateCustomEntity("Car", "Cars"),
                    Garage = CreateCustomEntity("Garage", "Garages")
                }
            });

            var manifestB = JToken.FromObject(new
            {
                version = "1.0.1",
                entities = new
                {
                    Car = CreateCustomEntity("Car", "Cars"),
                    Garage = CreateCustomEntity("Garage", "Garages")
                }
            });

         

            AppendAttribute(manifestB, "Car", "GarageToPark", new
            {
                schemaName = "GarageToParkId",
                logicalName = "GarageToParkId",
                type = new
                {
                    type = "lookup",
                    referenceType = "Garage"
                },

            });

            var sql = RunDBWithSchema("manifest_migrations", manifestB, manifestA);

            string expectedSQL = System.IO.File.ReadAllText(@"specs\CarsAndTrucksModel_AddLookup.sql");

            MigrationAssert.AreEqual(expectedSQL, sql);
        }

        [TestMethod]
        [DeploymentItem(@"ManifestTests/specs/CarsAndTrucksModel_AddLookup_WithCascade.sql", "specs")]
        public async Task CarsAndTrucksModel_AddLookupWithCascade()
        {
            var manifestA = JToken.FromObject(new
            {
                version = "1.0.0",
                entities = new
                {
                    Car = CreateCustomEntity("Car", "Cars"),
                    Garage = CreateCustomEntity("Garage", "Garages")
                }
            });

            var manifestB = JToken.FromObject(new
            {
                version = "1.0.1",
                entities = new
                {
                    Car = CreateCustomEntity("Car", "Cars"),
                    Garage = CreateCustomEntity("Garage", "Garages")
                }
            });



            AppendAttribute(manifestB, "Car", "GarageToPark", new
            {
                schemaName = "GarageToParkId",
                logicalName = "GarageToParkId",
                type = new
                {
                    type = "lookup",
                    referenceType = "Garage"
                },

            });

            var manifestC = JToken.FromObject(new
            {
                version = "1.0.10",
                entities = new
                {
                    Car = CreateCustomEntity("Car", "Cars"),
                    Garage = CreateCustomEntity("Garage", "Garages")
                }
            });



            AppendAttribute(manifestC, "Car", "GarageToPark", new
            {
                schemaName = "GarageToParkId",
                logicalName = "GarageToParkId",
                type = new
                {
                    type = "lookup",
                    referenceType = "Garage",
                    cascade = new
                    {
                        delete = "cascade"
                    }
                },
               
            });

            var sql = RunDBWithSchema("manifest_migrations", manifestC,manifestB, manifestA);

            string expectedSQL = System.IO.File.ReadAllText(@"specs\CarsAndTrucksModel_AddLookup_WithCascade.sql");

            MigrationAssert.AreEqual(expectedSQL, sql);
        }

        [TestMethod]
        [DeploymentItem(@"ManifestTests/specs/CarsAndTrucksModel_AddAttribute.sql", "specs")]
        public async Task CarsAndTrucksModel_AddAttributeTest()
        {
            var manifestA = JToken.FromObject(new
            {
                version = "1.0.0",
                entities = new
                {
                    Car = CreateCustomEntity("Car", "Cars")
                }
            });

            var manifestB = JToken.FromObject(new
            {
                version = "1.0.1",
                entities = new
                {
                    Car = CreateCustomEntity("Car", "Cars"),
                    Truck = CreateCustomEntity("Truck", "Trucks")
                }
            });

            var manifestC = JToken.FromObject(new
            {
                version = "1.0.2",
                entities = new
                {
                    Car = CreateCustomEntity("Car", "Cars"),
                    Truck = CreateCustomEntity("Truck", "Trucks")
                }
            });
            AppendAttribute(manifestC, "Truck", "Version", new
            {
                schemaName = "Version",
                logicalName = "version",
                type = "string",

            });

            var sql = RunDBWithSchema("manifest_migrations", manifestC, manifestB, manifestA);
           
            string expectedSQL = System.IO.File.ReadAllText(@"specs\CarsAndTrucksModel_AddAttribute.sql");

            MigrationAssert.AreEqual(expectedSQL, sql);
        }

      
      

       // [TestMethod(]
        public async Task MutualReferenceTest()
        {
            var manifest = JToken.FromObject(new
            {
                entities = new
                {
                    Account = new
                    {
                        schemaName = "AccountEntity",
                        logicalName = "accountentity",
                        pluralName = "accountentities",
                        collectionSchemaName = "AccountEntities",
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
                            PrimaryAccountCode = new
                            {
                                schemaName = "PrimaryAccountCode",
                                logicalName = "primaryaccountcode",
                                type = new
                                {
                                    type = "lookup",
                                    referenceType = "AccountCode"

                                },
                                isRequired = true,
                            }
                        }
                    },
                    AccountCode = new
                    {
                        schemaName = "AccountCodeEntity",
                        logicalName = "accountcodeentity",
                        pluralName = "accountcodeentities",
                        collectionSchemaName = "AccountCodeEntities",
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
                            Account = new
                            {
                                schemaName = "account",
                                logicalName = "account",
                                type = new
                                {
                                    type = "lookup",
                                    referenceType = "Account"

                                },
                                isRequired = true,
                            }
                        }
                    }
                }
            });

            var sql = RunDBWithSchema("MutualReferenceTest", manifest);
        }

    }
}
