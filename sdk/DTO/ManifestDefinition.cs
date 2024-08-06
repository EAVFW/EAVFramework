using EAVFW.Extensions.Manifest.SDK.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace EAVFW.Extensions.Manifest.SDK
{
    public class ManifestDefinition
    {
        [JsonPropertyName("entities")] 
        public Dictionary<string, EntityDefinition> Entities { get; set; }
        [JsonPropertyName("version")]
        public string Version { get; set; }
    }


    public class ManifestDefinitionCollection
    {
        public List<ManifestDefinition> Manifests { get; set; } = new List<ManifestDefinition>();
        public List<MigrationDefinition> Migrations { get; set; } = new List<MigrationDefinition>();

        public void Add(ManifestDefinition manifestDefinition)
        {
            Migrations.Add(new MigrationDefinition
            {
                Source = Manifests.LastOrDefault(),
                Target = manifestDefinition
            });

            Manifests.Add(manifestDefinition);

        }

    }

    public class MigrationAttributeDefinition
    {
        public AttributeObjectDefinition Source { get; set; }
        public AttributeObjectDefinition Target { get; set; }
        public string Key { get; set; }
        public MigrationEntityDefinition Entity { get; set; }
        
        public bool HasCascadeChanges()
        {

            if (Source.AttributeType.Cascades == null && Target.AttributeType.Cascades != null)
                return true;
            if (Source.AttributeType.Cascades != null && Target.AttributeType.Cascades == null)
                return true;
            if (Source.AttributeType.Cascades != null && !Source.AttributeType.Cascades.Equals(Target.AttributeType.Cascades))
                return true;

            return false;
        }
        public bool HasChanged()
        {
            if ((Source.IsRequired??false) != (Target.IsRequired??false))
                return true;
            if(Source.AttributeType.Type != Target.AttributeType.Type)
                return true;
            if (Source.AttributeType.MaxLength != Target.AttributeType.MaxLength)
                return true;


            //if (Source.AttributeType.Cascades == null && Target.AttributeType.Cascades != null)
            //    return true;
            //if (Source.AttributeType.Cascades != null && Target.AttributeType.Cascades == null)
            //    return true;
            //if (Source.AttributeType.Cascades != null && !Source.AttributeType.Cascades.Equals(Target.AttributeType.Cascades))
            //    return true;


            if (Source.AttributeType.SqlOptions == null && Target.AttributeType.SqlOptions != null)
                return true;
            if (Source.AttributeType.SqlOptions != null && Target.AttributeType.SqlOptions == null)
                return true;

            if (Source.AttributeType.SqlOptions != null)
            {
                if (Source.AttributeType.SqlOptions.Count != Target.AttributeType.SqlOptions.Count)
                    return true;
                if (Source.AttributeType.SqlOptions.Keys.Except(Target.AttributeType.SqlOptions.Keys).Any())
                    return true;
                if (Target.AttributeType.SqlOptions.Keys.Except(Source.AttributeType.SqlOptions.Keys).Any())
                    return true;
                if (Target.AttributeType.SqlOptions.Any(kv => Source.AttributeType.SqlOptions[kv.Key].Equals(kv.Value)))
                    return true;
            }
          


            
             
            return false;
        }
    }

    public enum MappingStrategyChangeEnum
    {

        None,
        TPT2TPC,
        TPC2TPT
    }

    public class MigrationEntityDefinition
    {
        public EntityDefinition Source { get; set; }
        public EntityDefinition Target { get; set; }
        public MigrationDefinition MigrationDefinition { get;  set; }



        //public MappingStrategyChangeEnum MappingStrategyChange => Source.MappingStrategy switch
        //{
        //    null when Target.MappingStrategy == null => MappingStrategyChangeEnum.None,
        //    MappingStrategy.TPC when Target.MappingStrategy == MappingStrategy.TPC => MappingStrategyChangeEnum.None,
        //    MappingStrategy.TPT when Target.MappingStrategy == MappingStrategy.TPT => MappingStrategyChangeEnum.None,
        //    MappingStrategy.TPT when Target.MappingStrategy == MappingStrategy.TPC => MappingStrategyChangeEnum.TPT2TPC,
        //    MappingStrategy.TPC when Target.MappingStrategy == MappingStrategy.TPT => MappingStrategyChangeEnum.TPC2TPT,

        //    _ => throw new NotImplementedException(),
        //};




        public IEnumerable<MigrationAttributeDefinition> GetExistingFields()
        {
            foreach (var target in Target.Attributes)
            {
                if (Source.Attributes.TryGetValue(target.Key, out var source) && source is AttributeObjectDefinition sfield && target.Value is AttributeObjectDefinition tfield)
                {
                    yield return new MigrationAttributeDefinition { Entity = this, Key = target.Key, Source = sfield, Target = tfield };
                }
            }
        }

    }


    public class MigrationDefinition
    {
        public ManifestDefinition Source { get; set; }
        public ManifestDefinition Target { get; set; }


        public IEnumerable<KeyValuePair<string, EntityDefinition>> GetNewEntities()
        {
            return Target.Entities.Where(e => !(Source?.Entities.ContainsKey(e.Key) ?? false));

        }
        public Dictionary<string, EntityDefinition> Entities => Target.Entities;

        public IEnumerable<MigrationEntityDefinition> GetModifiedEntities()
        {
            foreach (var target in Target.Entities)
            {
                if (Source.Entities.TryGetValue(target.Key, out var source))
                {
                    yield return new MigrationEntityDefinition { Source = source, Target = target.Value };
                }
            }
        }

        public MigrationEntityDefinition GetEntityMigration(string entityKey)
        {
            return new MigrationEntityDefinition
            {
                MigrationDefinition = this,
                Source = (Source?.Entities.ContainsKey(entityKey) ??false) ? Source.Entities[entityKey] : null,
                
                Target = Target.Entities[entityKey]
            };
        }

        public bool IsTableNew(string entityKey)
        {
            return !(Source?.Entities?.ContainsKey(entityKey) ?? false);
        }
    }
}
