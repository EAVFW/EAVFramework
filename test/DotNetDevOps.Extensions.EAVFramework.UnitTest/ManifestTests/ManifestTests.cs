﻿using Microsoft.EntityFrameworkCore.Infrastructure;
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

namespace DotNetDevOps.Extensions.EAVFramework.UnitTest.ManifestTests
{
    [TestClass]
    public class ManifestTests : BaseManifestTests
    {
      

       

    
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

            Assert.AreEqual(expectedSQL, sql);

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

            var sql = RunDBWithSchema("manifest_migrations", manifestB, manifestA);

            string expectedSQL = System.IO.File.ReadAllText(@"specs\CarsAndTrucksModel_AddEntity.sql");

            Assert.AreEqual(expectedSQL, sql);
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

            Assert.AreEqual(expectedSQL, sql);
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
