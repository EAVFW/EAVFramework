using EAVFW.Extensions.Manifest.SDK;
using System;
using System.Reflection;

namespace EAVFramework.Shared.V2
{
    public class GeoSpatialOptions
    {
        public Type PointGeomeryType {get;set;}
    }
    public class CodeGenerationOptions
    {

        public bool GenerateAbstractClasses { get; set; } = true;

        /// <summary>
        /// The constructor used for the Newtonsoft JsonPropertyAttribute when setting DTO Property Attributes.
        /// </summary>
        public ConstructorInfo JsonPropertyAttributeCtor { get; set; }


        /// <summary>
        /// The constructor used for the Microsoft JsonPropertyNameAttribute when setting DTO Property Attributes.
        /// </summary>
        public ConstructorInfo JsonPropertyNameAttributeCtor { get; set; }

        /// <summary>
        /// The inverse attribute set on loopup collection properties
        /// </summary>
        public ConstructorInfo InverseAttributeCtor { get; set; }

        /// <summary>
        /// All the interfaces that DTO classes can implement.
        /// </summary>
        public Type[] DTOBaseInterfaces { get; set; } = Array.Empty<Type>();

        public Type[] DTOBaseClasses { get; set; } = Array.Empty<Type>();
        public ConstructorInfo ForeignKeyAttributeCtor { get;  set; }

        /// <summary>
        /// The interface added to configuration class for the entity dto
        /// </summary>
        public Type EntityConfigurationInterface { get;  set; }
        public string EntityConfigurationConfigureName { get; set; }
        public Type EntityTypeBuilderType { get; set; }
        public MethodInfo EntityTypeBuilderToTable { get; set; }
        public MethodInfo UseTpcMappingStrategy { get; set; }
        

        /// <summary>
        /// Global schema to use when not specified per entity
        /// </summary>
        public string Schema { get;  set; }
        public MethodInfo EntityTypeBuilderHasKey { get; set; }
        public MethodInfo EntityTypeBuilderPropertyMethod { get; set; }

        public bool RequiredSupport { get; set; } = true;
        public MethodInfo IsRequiredMethod { get; set; }
        public MethodInfo IsRowVersionMethod { get; set; }
        public MethodInfo HasConversionMethod { get; set; }
        public MethodInfo HasPrecisionMethod { get; set; }
       // public string MigrationName { get; set; }

         
        public Type DynamicTableType { get;  set; }
        public Type DynamicTableArrayType { get; set; }
        public Type ColumnsBuilderType { get; set; }
        public Type CreateTableBuilderType { get; set; }
        public string CreateTableBuilderPrimaryKeyName { get; set; }
        public string CreateTableBuilderForeignKeyName { get; set; }
        public MethodInfo ColumnsBuilderColumnMethod { get; set; }
        public Type OperationBuilderAddColumnOptionType { get; set; }
        public MethodInfo MigrationBuilderDropTable { get; set; }
        public MethodInfo MigrationBuilderCreateTable { get; set; }
        public MethodInfo MigrationBuilderSQL { get; set; }
        public MethodInfo MigrationBuilderCreateIndex { get; set; }
        public MethodInfo MigrationBuilderDropIndex { get; set; }
        public MethodInfo MigrationsBuilderAddColumn { get; set; }
        public MethodInfo MigrationsBuilderAddForeignKey { get; set; }
        public MethodInfo MigrationsBuilderAlterColumn { get; set; }
        public MethodInfo MigrationsBuilderDropForeignKey { get; set; }
        public Type ReferentialActionType { get; set; }
        public CascadeAction ReferentialActionNoAction { get; set; }
        public MethodInfo LambdaBase { get; set; }
        public InversePropertyCollectionNamePattern InversePropertyCollectionName { get; set; } = InversePropertyCollectionNamePattern.ConcatWhenMultipleLookups;


        public GeoSpatialOptions GeoSpatialOptions { get; set; } = new GeoSpatialOptions();
    }
    public enum InversePropertyCollectionNamePattern
    {
        ConcatFieldNameAndLookupName,
        ConcatWhenMultipleLookups

    }
}
