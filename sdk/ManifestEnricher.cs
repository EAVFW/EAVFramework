using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace EAVFW.Extensions.Manifest.SDK
{

    public interface IManifestPermissionGenerator
    {

        Task<string> CreateInitializationScript(JToken model, string systemUserEntity);

    }
    public interface IParameterGenerator
    {
        string GetParameter(string name, bool escape=true);
    }
    public class SQLClientParameterGenerator : IParameterGenerator
    {
        public string GetParameter(string name, bool escape)
        {
            if(escape)
                return $"'$({name})'";
            return $"$({name})";
        }
    }
    public class DataClientParameterGenerator : IParameterGenerator
    {
        public string GetParameter(string name, bool escape)
        {
            return $"@{name}";
        }
    }
    public class ManifestPermissionGenerator : IManifestPermissionGenerator
    {
        private readonly IParameterGenerator parameterGenerator;

        public ManifestPermissionGenerator(IParameterGenerator parameterGenerator)
        {
            this.parameterGenerator = parameterGenerator ?? throw new ArgumentNullException(nameof(parameterGenerator));
        }
        public async Task<string> CreateInitializationScript(JToken model, string systemUserEntity)
        {

            var sb = new StringBuilder();
            var adminSGId = parameterGenerator.GetParameter("SystemAdminSecurityGroupId");// "$(SystemAdminSecurityGroupId)";

            sb.AppendLine("DECLARE @adminSRId uniqueidentifier");
            sb.AppendLine("DECLARE @permissionId uniqueidentifier");
            sb.AppendLine($"SET @adminSRId = ISNULL((SELECT s.Id FROM [{parameterGenerator.GetParameter("DBName",false)}].[{parameterGenerator.GetParameter("DBSchema",false)}].[SecurityRoles] s WHERE s.Name = 'System Administrator'),'{Guid.NewGuid()}')");
            sb.AppendLine($"IF NOT EXISTS(SELECT * FROM [{parameterGenerator.GetParameter("DBName",false)}].[{parameterGenerator.GetParameter("DBSchema",false)}].[Identities] WHERE [Id] = {adminSGId})");
            sb.AppendLine("BEGIN");
            sb.AppendLine($"INSERT INTO [{parameterGenerator.GetParameter("DBName",false)}].[{parameterGenerator.GetParameter("DBSchema",false)}].[Identities] (Id, Name, ModifiedOn,CreatedOn,CreatedById,ModifiedById,OwnerId) VALUES({adminSGId}, 'System Administrator Group', CURRENT_TIMESTAMP,CURRENT_TIMESTAMP,{adminSGId},{adminSGId},{adminSGId})");
            sb.AppendLine($"INSERT INTO [{parameterGenerator.GetParameter("DBName",false)}].[{parameterGenerator.GetParameter("DBSchema",false)}].[SecurityGroups] (Id) VALUES({adminSGId})");
            sb.AppendLine($"INSERT INTO [{parameterGenerator.GetParameter("DBName",false)}].[{parameterGenerator.GetParameter("DBSchema",false)}].[Identities] (Id, Name,ModifiedOn,CreatedOn,CreatedById,ModifiedById,OwnerId) VALUES ({parameterGenerator.GetParameter("UserGuid")}, {parameterGenerator.GetParameter("UserName")}, CURRENT_TIMESTAMP,CURRENT_TIMESTAMP,{adminSGId},{adminSGId},{adminSGId})");
            sb.AppendLine($"INSERT INTO [{parameterGenerator.GetParameter("DBName",false)}].[{parameterGenerator.GetParameter("DBSchema",false)}].[{systemUserEntity}] (Id,Email) VALUES ({parameterGenerator.GetParameter("UserGuid")}, {parameterGenerator.GetParameter("UserEmail")});");
            sb.AppendLine($"INSERT INTO [{parameterGenerator.GetParameter("DBName",false)}].[{parameterGenerator.GetParameter("DBSchema",false)}].[SecurityRoles] (Name, Description, Id,ModifiedOn,CreatedOn,CreatedById,ModifiedById,OwnerId) VALUES('System Administrator', 'Access to all permissions', @adminSRId, CURRENT_TIMESTAMP,CURRENT_TIMESTAMP,{adminSGId},{adminSGId},{adminSGId})");
            sb.AppendLine($"INSERT INTO [{parameterGenerator.GetParameter("DBName",false)}].[{parameterGenerator.GetParameter("DBSchema",false)}].[SecurityRoleAssignments] (IdentityId, SecurityRoleId, Id,ModifiedOn,CreatedOn,CreatedById,ModifiedById,OwnerId) VALUES({adminSGId}, @adminSRId, '{Guid.NewGuid()}',CURRENT_TIMESTAMP,CURRENT_TIMESTAMP,{adminSGId},{adminSGId},{adminSGId})");
            sb.AppendLine($"INSERT INTO [{parameterGenerator.GetParameter("DBName",false)}].[{parameterGenerator.GetParameter("DBSchema",false)}].[SecurityGroupMembers] (IdentityId, SecurityGroupId, Id,ModifiedOn,CreatedOn,CreatedById,ModifiedById,OwnerId) VALUES({parameterGenerator.GetParameter("UserGuid")}, {adminSGId}, '{Guid.NewGuid()}',CURRENT_TIMESTAMP,CURRENT_TIMESTAMP,{adminSGId},{adminSGId},{adminSGId})");
            sb.AppendLine("END;");
            foreach (var entitiy in model.SelectToken("$.entities").OfType<JProperty>())
            {
                WritePermissionStatement(sb, entitiy, "ReadGlobal", "Global Read", adminSGId,true);
                WritePermissionStatement(sb, entitiy, "Read", "Read", adminSGId);
                WritePermissionStatement(sb, entitiy, "UpdateGlobal", "Global Update", adminSGId, true);
                WritePermissionStatement(sb, entitiy, "Update", "Update", adminSGId);
                WritePermissionStatement(sb, entitiy, "CreateGlobal", "Global Create", adminSGId, true);
                WritePermissionStatement(sb, entitiy, "Create", "Create", adminSGId);
                WritePermissionStatement(sb, entitiy, "DeleteGlobal", "Global Delete", adminSGId, true);
                WritePermissionStatement(sb, entitiy, "Delete", "Delete", adminSGId);
                WritePermissionStatement(sb, entitiy, "ShareGlobal", "Global Share", adminSGId, true);
                WritePermissionStatement(sb, entitiy, "Share", "Share", adminSGId);
                WritePermissionStatement(sb, entitiy, "AssignGlobal", "Global Assign", adminSGId, true);
                WritePermissionStatement(sb, entitiy, "Assign", "Assign", adminSGId);
            }

            return sb.ToString();
        }
        private void WritePermissionStatement(StringBuilder sb, JProperty entitiy, string permission, string permissionName, string adminSGId, bool adminSRId1 = false)
        {
            sb.AppendLine($"SET @permissionId = ISNULL((SELECT s.Id FROM [{parameterGenerator.GetParameter("DBName",false)}].[{parameterGenerator.GetParameter("DBSchema",false)}].[Permissions] s WHERE s.Name = '{entitiy.Value.SelectToken("$.collectionSchemaName")}{permission}'),'{Guid.NewGuid()}')");
            sb.AppendLine($"IF NOT EXISTS(SELECT * FROM [{parameterGenerator.GetParameter("DBName",false)}].[{parameterGenerator.GetParameter("DBSchema",false)}].[Permissions] WHERE [Name] = '{entitiy.Value.SelectToken("$.collectionSchemaName")}{permission}')");
            sb.AppendLine("BEGIN");
            sb.AppendLine($"INSERT INTO [{parameterGenerator.GetParameter("DBName",false)}].[{parameterGenerator.GetParameter("DBSchema",false)}].[Permissions] (Name, Description, Id, ModifiedOn,CreatedOn,CreatedById,ModifiedById,OwnerId) VALUES('{entitiy.Value.SelectToken("$.collectionSchemaName")}{permission}', '{permissionName} access to {entitiy.Value.SelectToken("$.pluralName")}', @permissionId, CURRENT_TIMESTAMP,CURRENT_TIMESTAMP,{adminSGId},{adminSGId},{adminSGId})");
            sb.AppendLine("END");
            if (adminSRId1)
            {
                sb.AppendLine($"IF NOT EXISTS(SELECT * FROM [{parameterGenerator.GetParameter("DBName",false)}].[{parameterGenerator.GetParameter("DBSchema",false)}].[SecurityRolePermissions] WHERE [Name] = 'System Administrator - {entitiy.Value.SelectToken("$.collectionSchemaName")} - {permission}')");
                sb.AppendLine("BEGIN");
                sb.AppendLine($"INSERT INTO [{parameterGenerator.GetParameter("DBName",false)}].[{parameterGenerator.GetParameter("DBSchema",false)}].[SecurityRolePermissions] (Name, PermissionId, SecurityRoleId, Id,ModifiedOn,CreatedOn,CreatedById,ModifiedById,OwnerId) VALUES('System Administrator - {entitiy.Value.SelectToken("$.collectionSchemaName")} - {permission}', @permissionId, @adminSRId, '{Guid.NewGuid()}', CURRENT_TIMESTAMP,CURRENT_TIMESTAMP,{adminSGId},{adminSGId},{adminSGId})");
                sb.AppendLine("END");
            }
        }
    }
    public class ManifestEnricher : IManifestEnricher
    {
        private readonly ISchemaNameManager schemaName;
        private readonly IManifestReplacmentRunner manifestReplacmentRunner;

        public ManifestEnricher(ISchemaNameManager schemaName, IManifestReplacmentRunner manifestReplacmentRunner)
        {
            this.schemaName = schemaName ?? throw new System.ArgumentNullException(nameof(schemaName));
            this.manifestReplacmentRunner = manifestReplacmentRunner ?? throw new ArgumentNullException(nameof(manifestReplacmentRunner));
        }

        protected virtual JObject Merge(JObject jToken, object obj)
        {

            jToken = (JObject)jToken.DeepClone();


            var jobj = JToken.FromObject(obj) as JObject;

            foreach (var p in jobj.Properties())
            {
                if (!(p.Value.Type == JTokenType.Null || p.Value.Type == JTokenType.Undefined))
                    jToken[p.Name] = p.Value;
            }

            if (!jToken.ContainsKey("schemaName"))
                jToken["schemaName"] = schemaName.ToSchemaName(jToken.SelectToken("$.displayName")?.ToString());
            if (!jToken.ContainsKey("logicalName"))
                jToken["logicalName"] = jToken.SelectToken("$.schemaName")?.ToString().ToLower();

            return jToken as JObject;
        }
        private object[] CreateOptions(params string[] args)
        {
            return args.Select((o, i) => new { label = o, value = i + 1 }).ToArray();
        }
        private JObject CreateAttribute(JObject attr, string displayName, object type, string? schemaName = null, object? additionalProps = null)
        {
            if (additionalProps != null)
                return Merge(Merge(attr, new { displayName, type, schemaName }), additionalProps);
            return Merge(attr, new { displayName, type, schemaName });
        }
        private string TrimId(string v)
        {
            if (string.IsNullOrEmpty(v))
                return v;

            if (v.EndsWith("id", StringComparison.OrdinalIgnoreCase))
                return v.Substring(0, v.Length - 2);

            return v;
        }
        public async Task<JsonDocument> LoadJsonDocumentAsync(JToken jsonraw, string customizationprefix, ILogger logger)
        {


            var entities = jsonraw.SelectToken("$.entities") as JObject;
            var insertMerges = jsonraw.SelectToken("$.variables.options.insertMergeLayoutVariable")?.ToObject<string>();

            
            foreach (var entitieP in (entities)?.Properties() ?? Enumerable.Empty<JProperty>())
            {
                var entity = (JObject)entitieP.Value;

                SetRequiredProps(entity, entitieP.Name);

                await EnrichEntity(jsonraw, customizationprefix, logger, insertMerges,  entity);

            }


            foreach (var entitieP in (entities)?.Properties().ToArray() ?? Enumerable.Empty<JProperty>())
            {
                foreach (var polyLookup in entitieP.Value.SelectToken("$.attributes").OfType<JProperty>().Where(c => c.Value.SelectToken("$.type.type")?.ToString().ToLower() == "polylookup").ToArray())
                {
                    var name = polyLookup.Value.SelectToken("$.type.name")?.ToString() ?? $"{entitieP.Name} {polyLookup.Name}";
                    var Key = name + " Reference";
                    var pluralName = name + " References"; //$"{entitieP.Value.SelectToken("$.displayName")} {polyLookup.Value.SelectToken("$.displayName")} References";
                    var reverse = polyLookup.Value.SelectToken("$.type.reverse")?.ToObject<bool>() ?? false;
                    var inline = polyLookup.Value.SelectToken("$.type.inline")?.ToObject<bool>() ?? false;
                    if (!entities.ContainsKey(Key))
                    { 
                        var attributes = polyLookup.Value.SelectToken("$.type.referenceTypes").ToObject<string[]>()
                            .ToDictionary(k => k, v => JToken.FromObject(new { type = new { type = "lookup", referenceType = v } }));

                        if (inline)
                        {
                            foreach (var attribute in attributes)
                            {
                                entitieP.Value["attributes"][attribute.Key] = attribute.Value;

                            }
                            polyLookup.Value["type"]["type"] = "polylookup";
                            //   polyLookup.Remove();
                        }
                        else
                        {


                            //  attributes["Id"] = JToken.FromObject(new { isPrimaryKey = true });
                            attributes["Name"] = JToken.FromObject(new { isPrimaryField = true });



                            entities[Key] = JToken.FromObject(new
                            {
                                pluralName = pluralName,
                                attributes = attributes
                            });


                            SetRequiredProps(entities[Key] as JObject, Key);
                        }
                    }



                    if (!inline)
                    {
                        var entity = entities[Key] as JObject;
                        polyLookup.Value["type"]["foreignKey"] = JToken.FromObject(new
                        {
                            principalTable = entity["logicalName"].ToString(),
                            principalColumn = "id",
                            principalNameColumn = "name",
                            name = TrimId(polyLookup.Value.SelectToken("$.logicalName")?.ToString()) // jsonraw.SelectToken($"$.entities['{ attr["type"]["referenceType"] }'].logicalName").ToString().Replace(" ", ""),
                        });
                        polyLookup.Value["type"]["referenceType"] = Key;

                        if (reverse)
                        {
                            entities[Key]["attributes"][polyLookup.Name] = JToken.FromObject(new
                            {
                                type = new
                                {
                                    type = "lookup",
                                    referenceType = entitieP.Name
                                }
                            });

                            // polyLookup.Remove();
                        }


                        //  polyLookup.Value["type"]["type"] = "lookup";

                        await EnrichEntity(jsonraw, customizationprefix, logger, insertMerges, entity);
                    }
                    else
                    {

                        await EnrichEntity(jsonraw, customizationprefix, logger, insertMerges, entitieP.Value as JObject);
                    }
                }
            }


            await manifestReplacmentRunner.RunReplacements(jsonraw, customizationprefix, logger);


            foreach (var entitieP in (jsonraw.SelectToken("$.entities") as JObject)?.Properties() ?? Enumerable.Empty<JProperty>())
            {
                var attributes = entitieP.Value.SelectToken("$.attributes") as JObject;

                foreach (var attributeDefinition in attributes.Properties())
                {
                    var attr = attributeDefinition.Value;

                    switch (attr.SelectToken("$.type.type")?.ToString()?.ToLower())
                    {
                        case "lookup" when string.IsNullOrEmpty(jsonraw.SelectToken($"$.entities['{attr["type"]["referenceType"]}'].logicalName")?.ToString()):
                            throw new KeyNotFoundException($"The lookup entity does not exists: '{attr["type"]["referenceType"]}'");
                        case "lookup":

                            
                                var columns = jsonraw.SelectToken($"$.entities['{attr["type"]["referenceType"]}'].attributes").OfType<JProperty>()
                                        .Concat(jsonraw.SelectToken($"$.entities['{attr["type"]["referenceType"]}'].TPT") == null ? Enumerable.Empty<JProperty>() : jsonraw.SelectToken($"$.entities['{jsonraw.SelectToken($"$.entities['{attr["type"]["referenceType"]}'].TPT")}'].attributes").OfType<JProperty>())
                                        .GroupBy(k => k.Name).Select(g => g.First())
                                        .ToArray();

                                var principalTable = jsonraw.SelectToken($"$.entities['{attr["type"]["referenceType"]}'].logicalName").ToString();
                                var principalColumn = columns
                                        .FirstOrDefault(a => a.Value.SelectToken("$.isPrimaryKey")?.ToObject<bool>() ?? false)
                                        ?.Value.SelectToken("$.logicalName").ToString()
                                        ?? throw new InvalidOperationException($"Cant find principalColumn for lookup {entitieP.Name}.{attributeDefinition.Name}"); ;
                            
                                var principalNameColumn = columns
                                        .FirstOrDefault(a => a.Value.SelectToken("$.isPrimaryField")?.ToObject<bool>() ?? false)
                                        ?.Value.SelectToken("$.logicalName").ToString()
                                        ?? throw new InvalidOperationException($"Cant find principalNameColumn for lookup {entitieP.Name}.{attributeDefinition.Name}");
                                
                                attr["type"]["foreignKey"] = JToken.FromObject(new
                                {
                                    principalTable = principalTable,
                                    principalColumn = principalColumn,
                                    principalNameColumn = principalNameColumn,
                                    name = TrimId(attr.SelectToken("$.logicalName")?.ToString()) // jsonraw.SelectToken($"$.entities['{ attr["type"]["referenceType"] }'].logicalName").ToString().Replace(" ", ""),
                                });

                            
                            break;
                        case "float":
                        case "decimal":
                            if (attr.SelectToken("$.type.sql") == null)
                            {
                                attr["type"]["sql"] = JToken.FromObject(new { precision = 18, scale = 4 });
                            }
                            if (attr.SelectToken("$.type.sql.precision") == null)
                            {
                                attr["type"]["sql"]["precision"] = 18;
                            }
                            if (attr.SelectToken("$.type.sql.scale") == null)
                            {
                                attr["type"]["sql"]["scale"] = 4;
                            }
                            break;

                    }


                }
            }



            var defaultControls = jsonraw.SelectToken("$.controls");
            if (defaultControls != null)
            {
                logger.LogInformation("Replacing default Controls");

                foreach (var defaultControl in defaultControls.OfType<JProperty>())
                {
                    logger.LogInformation("Replacing default Controls : {Type}", defaultControl.Name);

                    foreach (var entity in jsonraw.SelectToken("$.entities")?.OfType<JProperty>() ?? Enumerable.Empty<JProperty>())
                    {
                        foreach (var attribute in entity.Value.SelectToken("$.attributes")?.OfType<JProperty>() ?? Enumerable.Empty<JProperty>())
                        {
                            var attributeType = (attribute.Value.SelectToken("$.type.type") ?? attribute.Value.SelectToken("$.type")).ToString();

                            if (string.Equals(attributeType, defaultControl.Name, StringComparison.OrdinalIgnoreCase))
                            {
                                logger.LogInformation("Replacing default Controls for {entity} {attribute} : {type}", entity.Name, attribute.Name, defaultControl.Name);

                                var formFields = (entity.Value.SelectToken($"$.forms")?.OfType<JProperty>() ?? Enumerable.Empty<JProperty>())
                                    .Select(c => c.Value.SelectToken($"$.columns['{attribute.Name}']")).Where(c => c != null).ToArray();

                                {

                                    foreach (var formField in formFields)
                                    {
                                        var control = formField.SelectToken("$.control");

                                        if (control == null)
                                        {
                                            var replacement = defaultControl.Value.DeepClone(); ;
                                            formField["control"] = replacement;
                                            var q = new Queue<JToken>(new[] { replacement });
                                            while (q.Any())
                                            {
                                                var e = q.Dequeue();
                                                if (e is JObject obj)
                                                {
                                                    foreach (var prop in e.OfType<JProperty>())
                                                    {
                                                        q.Enqueue(prop);
                                                    }
                                                }
                                                else if (e is JProperty prop)
                                                {
                                                    q.Enqueue(prop.Value);
                                                }
                                                else if (e is JArray array)
                                                {
                                                    foreach (var ee in array)
                                                    {
                                                        q.Enqueue(ee);
                                                    }
                                                }
                                                else if (e.Type == JTokenType.String)
                                                {
                                                    var str = e.ToString();
                                                    if (str.StartsWith("[[") && str.EndsWith("]]"))
                                                    {
                                                        e.Replace(str.Substring(1, str.Length - 2));
                                                    }

                                                }
                                            }


                                            logger.LogInformation("Replacing default Controls for {entity} {attribute} {formname}: {type}", entity.Name, attribute.Name, (formField.Parent.Parent.Parent as JProperty)?.Name, defaultControl.Name);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            await manifestReplacmentRunner.RunReplacements(jsonraw, customizationprefix, logger);


            foreach (var (entityDefinition, attributeDefinition2) in jsonraw.SelectToken("$.entities").OfType<JProperty>()
                .SelectMany(e => e.Value.SelectToken("$.attributes").OfType<JProperty>().Select(p => (e, p)))
                .Where(a => a.p.Value.SelectToken("$.type.type")?.ToString().ToLower() == "choices")
                .ToArray())
            {




                var nentity = $"{attributeDefinition2.Value.SelectToken("$.type.name")}";


                jsonraw["entities"][nentity] = JToken.FromObject(
                   new
                   {
                       pluralName = $"{attributeDefinition2.Value.SelectToken("$.type.pluralName")}",
                       displayName = nentity,
                       logicalName = $"{attributeDefinition2.Value.SelectToken("$.type.name")}".Replace(" ", "").ToLower(),
                       schemaName = $"{attributeDefinition2.Value.SelectToken("$.type.name")}".Replace(" ", ""),
                       collectionSchemaName = $"{attributeDefinition2.Value.SelectToken("$.type.pluralName")}".Replace(" ", ""),
                       keys = new Dictionary<string, object>
                       {
                           [$"IX_{entityDefinition.Name}Value"] = new[] { entityDefinition.Name, nentity + " Value" }
                       },
                       attributes = new Dictionary<string, object>
                       {
                           ["Id"] = new
                           {
                               displayName = "Id",
                               logicalName = "id",
                               schemaName = "Id",
                               type = new { type = "guid" },
                               isPrimaryKey = true,
                           },
                           [entityDefinition.Name] = new
                           {
                               displayName = entityDefinition.Value.SelectToken("$.displayName"),
                               logicalName = entityDefinition.Value.SelectToken("$.logicalName") + "id",
                               schemaName = entityDefinition.Value.SelectToken("$.schemaName") + "Id",
                               type = new
                               {
                                   type = "lookup",
                                   referenceType = entityDefinition.Name,
                               },
                           },
                           [nentity + " Value"] = new
                           {

                               displayName = nentity + " Value",
                               logicalName = $"{attributeDefinition2.Value.SelectToken("$.type.name")}".Replace(" ", "").ToLower(),
                               schemaName = $"{attributeDefinition2.Value.SelectToken("$.type.name")}".Replace(" ", "") + "Value",
                               //   isPrimaryKey = true,
                               type = new
                               {
                                   type = "choice",
                                   name = $"{attributeDefinition2.Value.SelectToken("$.type.name")}".Replace(" ", "") + "Value",
                                   options = attributeDefinition2.Value.SelectToken("$.type.options")
                               }
                           }
                       }
                   });
                //attributeDefinition2.Value.SelectToken("$.type").Replace(JToken.FromObject(
                //  new
                //  {
                //      type = "lookup",
                //      referenceType = $"{attributeDefinition2.Value.SelectToken("$.type.name")}"
                //  }
                // ));

                attributeDefinition2.Value["type"]["logicalName"] = $"{attributeDefinition2.Value.SelectToken("$.type.name")}".Replace(" ", "").ToLower();
                attributeDefinition2.Value["type"]["schemaName"] = $"{attributeDefinition2.Value.SelectToken("$.type.name")}".Replace(" ", "");
                attributeDefinition2.Value["type"]["collectionSchemaName"] = $"{attributeDefinition2.Value.SelectToken("$.type.pluralName")}".Replace(" ", "");
                attributeDefinition2.Value["type"]["principalColumn"] = entityDefinition.Value.SelectToken("$.logicalName") + "id";
                //  attributeDefinition2.Remove();

            }


            //Lets sort them according to TPT
            var qque = new Queue<JProperty>(jsonraw.SelectToken("$.entities").OfType<JProperty>());

            while (qque.Count > 0)
            {
                var entity = qque.Dequeue();

                var tpt = entity.Value.SelectToken("$.TPT")?.ToString();
                if (!string.IsNullOrEmpty(tpt))
                {
                    var baseentity = jsonraw.SelectToken($"$.entities['{tpt}']").Parent as JProperty;
                    entity.Remove();
                    baseentity.AddAfterSelf(entity);


                }
            }


            var json = JsonDocument.Parse(jsonraw.ToString(), new JsonDocumentOptions
            {
                CommentHandling = JsonCommentHandling.Skip
            });
            Directory.CreateDirectory("obj");
            File.WriteAllText("obj/manifest.g.json", jsonraw.ToString(Newtonsoft.Json.Formatting.Indented));


            ///For loop over jsonraw.selectToken("$.entities")
            ///write a file to obj/specs/<entity.logicalName>.spec.g.json
            ///containing a json schema file for the entity attributes. Sadly there is no strict type map of possible types.
            ///Types can be anything random that i later maps to something in dynamics. (use tolower)
            /// Currently from AttributeTypeCodeConverter - currency,customer,datetime,multilinetext,memo,int,integer,timezone,phone,float,guid,string,text,boolean,bool,
            /// and type.type can be autonumber,choice,picklist,choices,state,status,lookup,string,text

            bool ConvertToSchemaType(JToken attrType, out JToken type)
            {
                type = null;

                var inp = attrType?.ToString();
                if (!(attrType.Type == JTokenType.String))
                {
                    inp = attrType.SelectToken("$.type")?.ToString();
                }

                switch (inp.ToLower())
                {
                    case "point":
                        type = JToken.FromObject(new
                        {
                            type = "object",
                           properties=new
                           {

                           }
                        });
                        return true;
                    case "binary":
                        type = JToken.FromObject(new
                        {
                            type = "string",
                            contentEncoding = "base64"
                        });
                        return true;
                    case "datetime":
                        type = "datetime";
                        return true;
                    case "time":
                        type = "time";
                        return true;

                    case "customer":
                    case "polylookup":
                        return false;
                    case "string":
                    case "text":
                    case "multilinetext":
                        type = "string";
                        return true;
                    case "integer":
                        type = "integer";
                        return true;
                    case "decimal":
                        type = "number";
                        return true;
                    case "boolean":
                        type = "boolean";
                        return true;
                    case "lookup":
                   

                        var foreignTable = jsonraw.SelectToken($"$.entities['{attrType.SelectToken("$.referenceType")}']");
                        var fatAttributes = foreignTable.SelectToken("$.attributes");
                        var fat = fatAttributes.OfType<JProperty>().Where(c => c.Value.SelectToken("$.isPrimaryKey")?.ToObject<bool>() ?? false)
                            .Select(a => a.Value.SelectToken("$.type")).Single();
                        if (fat.Type == JTokenType.Object)
                            fat = fat.SelectToken("$.type");

                        ConvertToSchemaType(fat?.ToString(), out type);

                        type["x-foreign-key"] = JToken.FromObject(new
                        {
                            table = new
                            {
                                logicalName = foreignTable.SelectToken("$.logicalName"),
                                schemaName = foreignTable.SelectToken("$.schemaName"),
                                pluralName = foreignTable.SelectToken("$.pluralName"),
                            },
                            columns = fatAttributes.OfType<JProperty>().Where(c => c.Value.SelectToken("$.isPrimaryKey")?.ToObject<bool>() ?? false)
                            .Select(a => new
                            {
                                logicalName = a.SelectToken("$.logicalName"),
                                schemaName = a.SelectToken("$.schemaName"),

                            })
                        });

                        return true;

                    case "guid":
                        type = JToken.FromObject(new
                        {
                            type = "string",
                            format = "uuid"
                        });
                        return true;
                    case "choices":

                        type = JToken.FromObject(new
                        {
                            type = "array",
                            items = new
                            {
                                type = "integer",
                                @enum = attrType.SelectToken("$.options").OfType<JProperty>().Select(c => c.Value.ToObject<int>())
                            }
                        });
                        return true;
                    case "choice":
                        type = JToken.FromObject(new
                        {
                            type = "integer",
                            @enum = attrType.SelectToken("$.options").OfType<JProperty>().Select(c => c.Value.Type == JTokenType.Object ? c.Value.SelectToken("$.value") : c.Value).Select(v => v.ToObject<int>())
                        });
                        return true;
                    default:
                        throw new NotImplementedException(inp);
                }


            }

            Directory.CreateDirectory("obj/models");
            foreach (var entity in (jsonraw.SelectToken("$.entities") as JObject)?.Properties() ?? Enumerable.Empty<JProperty>())
            {
                try
                {
                    var entityValue = entity.Value as JObject;
                    var schema = new JObject
                    {
                        ["title"] = entity.Name,
                        ["$schema"] = "http://json-schema.org/draft-07/schema#",
                        ["type"] = "object",
                    };
                    var properties = new JObject();

                    foreach (var attr in (entityValue.SelectToken("$.attributes") as JObject)?.Properties() ?? Enumerable.Empty<JProperty>())
                    {
                        if(!(attr.Value is JObject attrValue)) continue;
                        
                        var attrType = attrValue.SelectToken("$.type");
                       
                        if (!ConvertToSchemaType(attrType, out var type)) continue;

                        var propValues = new JObject();
                        var logicalName = attrValue.SelectToken("$.logicalName").ToString();
                        var displayName = attrValue.SelectToken("$.displayName").ToString();
                        propValues["title"] = displayName;
                        propValues["type"] = type;
                        properties[logicalName] = propValues;
                    }

                    schema["properties"] = properties;

                    var filePath = $"obj/models/{entityValue["logicalName"]}.spec.g.json";
                    File.WriteAllText(filePath, schema.ToString(Newtonsoft.Json.Formatting.Indented));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: Failed to generate jsonschema for {entity.Name}");
                    Console.Write(ex);
                }
            }

            return json;
        }

        private async Task EnrichEntity(JToken jsonraw, string customizationprefix, ILogger logger, string insertMerges, JObject entity)
        {
            JObject SetDefault(JToken obj, JObject localeEnglish)
            {
                var value = new JObject(new JProperty("1033", localeEnglish));
                obj["locale"] = value;
                return value;
            }
            var entityLocaleEnglish = new JObject(new JProperty("displayName", entity["displayName"]), new JProperty("pluralName", entity["pluralName"]));
            var entityLocale = entity.SelectToken("$.locale") as JObject ?? SetDefault(entity, entityLocaleEnglish);
            if (!entityLocale.ContainsKey("1033"))
                entityLocale["1033"] = entityLocaleEnglish;


            var attributes = entity.SelectToken("$.attributes") as JObject;

            if (attributes == null)
            {
                entity["attributes"] = attributes = new JObject();
            }

            if (attributes != null)
            {
                if (!attributes.Properties().Any(p => p.Value.SelectToken("$.isPrimaryKey")?.ToObject<bool>() ?? false))
                {
                    attributes["Id"] = JToken.FromObject(new { isPrimaryKey = true, type = new { type = "guid" } });
                }


                //Replace string attributes
                foreach (var attr in attributes.Properties().ToArray())
                {
                    if (attr.Name == "[merge()]")
                    {
                        await manifestReplacmentRunner.RunReplacements(jsonraw, customizationprefix, logger, attr);
                    }
                    else if (attr.Value.Type == JTokenType.String)
                    {
                        await manifestReplacmentRunner.RunReplacements(jsonraw, customizationprefix, logger, attr.Value);
                    }
                }

                var queue =
                    new Queue<JObject?>(
                        attributes.Properties()
                            .Select(c => c.Value as JObject)
                            .Where(x => x != null)
                    );

                foreach (var attribute in attributes.Properties())
                {
                    if (!string.IsNullOrEmpty(insertMerges))
                    {
                        var value = attribute.Value as JObject;
                        if (!value?.ContainsKey("[merge()]") ?? false)
                            value.Add(new JProperty("[merge()]", $"[variables('{insertMerges}')]"));
                        queue.Enqueue(value);
                    }
                }


                while (queue.Count > 0)
                {
                    var attr = queue.Dequeue();



                    if (!attr.ContainsKey("displayName"))
                        attr["displayName"] = (attr.Parent as JProperty)?.Name;

                    if (attr["type"]?.ToString() == "address")
                    {

                        var displayName = attr.SelectToken("$.displayName")?.ToString();

                        attr["__unroll__path"] = attr.Path;

                        var unrolls = new[] {
                            Merge(attr,new { displayName=$"{displayName}: Address Type", type=new { type ="picklist",
                                isGlobal=false,
                                name=$"{displayName}: Address Type",
                                options=CreateOptions("Bill To","Ship To","Primary","Other")
                            } }),
                            Merge(attr,new { displayName=$"{displayName}: City", type="string"}),
                            Merge(attr,new { displayName=$"{displayName}: Country", type="string", schemaName=schemaName.ToSchemaName( $"{displayName}: Country")}),
                            Merge(attr,new { displayName=$"{displayName}: County", type="string"}),
                            Merge(attr,new { displayName=$"{displayName}: Fax", type="string"}),
                            Merge(attr,new { displayName=$"{displayName}: Freight Terms", schemaName=schemaName.ToSchemaName( $"{displayName}: Freight Terms Code"), type=new { type="picklist",
                                isGlobal=false,
                                name=$"{displayName}: Freight Terms",
                                options=CreateOptions("FOB","No Charge")
                            } }),
                           // Merge(attr,new { displayName=$"{displayName}: Id",schemaName=ToSchemaName( $"{displayName}: AddressId"),type ="guid"}),
                            CreateAttribute(attr,$"{displayName}: Latitude","float"),
                            CreateAttribute(attr,$"{displayName}: Longitude","float"),
                            CreateAttribute(attr,$"{displayName}: Name","string",null, new { isPrimaryField = !attributes.Properties().Any(p=>p.Value.SelectToken("$.isPrimaryField") != null) }),
                            CreateAttribute(attr,$"{displayName}: Phone","phone", schemaName.ToSchemaName( $"{displayName}: Telephone 1")),
                            CreateAttribute(attr,$"{displayName}: Telephone 2","phone", schemaName.ToSchemaName( $"{displayName}: Telephone 2")),
                            CreateAttribute(attr,$"{displayName}: Telephone 3","phone", schemaName.ToSchemaName( $"{displayName}: Telephone 3")),
                            CreateAttribute(attr,$"{displayName}: Post Office Box","string"),
                            CreateAttribute(attr,$"{displayName}: Primary Contact Name","string"),
                            CreateAttribute(attr,$"{displayName}: Shipping Method",new { type="picklist",
                                isGlobal=false,
                                name=$"{displayName}: Shipping Method",
                                options=CreateOptions("Airborne","DHL","FedEx","UPS","Postal Mail","Full Load","Will Call"),
                            }, schemaName.ToSchemaName( $"{displayName}: Shipping Method Code")),
                            CreateAttribute(attr,$"{displayName}: State/Province","string"),
                            CreateAttribute(attr,$"{displayName}: Street 1","string",schemaName.ToSchemaName( $"{displayName}: line1")),
                            CreateAttribute(attr,$"{displayName}: Street 2","string",schemaName.ToSchemaName( $"{displayName}: line2")),
                            CreateAttribute(attr,$"{displayName}: Street 3","string",schemaName.ToSchemaName( $"{displayName}: line3")),
                            CreateAttribute(attr,$"{displayName}: UPS Zone","string"),
                            CreateAttribute(attr,$"{displayName}: UTC Offset","timezone"),
                            CreateAttribute(attr,$"{displayName}: ZIP/Postal Code","string",schemaName.ToSchemaName( $"{displayName}: Postal Code")),
                            CreateAttribute(attr,$"{displayName}: State/Province","string"),

                        };

                        attr["schemaName"] = displayName.Replace(" ", "").Replace(":", "_") + "_Composite";
                        attr["type"] = "MultilineText";


                        foreach (var unroll in unrolls)
                        {
                            queue.Enqueue(unroll);
                        }

                        //if(!attributes.Properties().Any(p=>p.Value.SelectToken("$.isPrimaryField") != null))
                        //{
                        //    attr["type"] = JObject.FromObject(new { type = "string", maxLength = 1024 });
                        //    attr["isPrimaryField"] = true;
                        //}

                    }


                    if (!attr.ContainsKey("schemaName"))
                    {

                        attr["schemaName"] = schemaName.ToSchemaName(attr.SelectToken("$.displayName").ToString());

                        await manifestReplacmentRunner.RunReplacements(jsonraw, customizationprefix, logger, attr);

                        switch (attr.SelectToken("$.type.type")?.ToString()?.ToLower())
                        {
                            case "lookup":
                            case "polylookup":
                            case "customer":
                                if (!attr["schemaName"].ToString().EndsWith("Id"))
                                    attr["schemaName"] = $"{schemaName.ToSchemaName(attr.SelectToken("$.displayName").ToString())}Id";



                                break;

                        }
                    }


                    if (!attr.ContainsKey("logicalName"))
                        attr["logicalName"] = attr.SelectToken("$.schemaName").ToString().ToLower();

                    if (!attr.ContainsKey("type"))
                        attr["type"] = "string";

                    if (attr.Parent == null && !(attributes.ContainsKey(attr["logicalName"].ToString()) || attributes.ContainsKey(attr["schemaName"].ToString()) || attributes.ContainsKey(attr["displayName"].ToString())))
                        attributes[attr["logicalName"].ToString()] = attr;

                    if (attr.SelectToken("$.type").Type == JTokenType.String)
                    {
                        attr["type"] = JToken.FromObject(new { type = attr.SelectToken("$.type") });
                    }



                }

                foreach (var attr in attributes.Properties().Where(x => x.Value.Type == JTokenType.Object))
                {
                    var attributeLocaleEnglish = new JObject(new JProperty("displayName", attr.Value["displayName"]));
                    var attributeLocale = attr.Value.SelectToken("$.locale") as JObject ?? SetDefault(attr.Value, attributeLocaleEnglish);
                    if (!attributeLocale.ContainsKey("1033"))
                        attributeLocale["1033"] = attributeLocaleEnglish;
                }


            }
        }

        private void SetRequiredProps(JObject entity, string key)
        {
            if (!entity.ContainsKey("displayName"))
                entity["displayName"] = key;
            if (!entity.ContainsKey("schemaName"))
                entity["schemaName"] = entity.SelectToken("$.displayName")?.ToString().Replace(" ", "");
            if (!entity.ContainsKey("logicalName"))
                entity["logicalName"] = entity.SelectToken("$.schemaName")?.ToString().ToLower();

            if (!entity.ContainsKey("collectionSchemaName"))
                entity["collectionSchemaName"] = schemaName.ToSchemaName(entity["pluralName"]?.ToString());
        }
    }
}
