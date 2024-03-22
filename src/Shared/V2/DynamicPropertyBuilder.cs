
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace EAVFramework.Shared.V2
{
    public class ForeignKeyInfo
    {

    }

    public class DynamicPropertyBuilder : IDynamicPropertyBuilder
    {
        public bool HasParentProperty => this.dynamicTableBuilder.ContainsParentProperty(this.SchemaName);

        public string AttributeKey { get; }
        public string LogicalName { get; }
        public string SchemaName { get; }

        private readonly DynamicCodeService dynamicCodeService;
        private DynamicTableBuilder dynamicTableBuilder;

        public Type PropertyType { get; private set; }
        public Type DTOPropertyType { get; private set; }
        public string Type { get; }

        // public PropertyBuilder Builder { get; }
        public DynamicPropertyBuilder(DynamicCodeService dynamicCodeService,
            DynamicTableBuilder dynamicTableBuilder,
            string attributeKey, string propertyName, string logicalName, string type)
            : this(dynamicCodeService, dynamicTableBuilder, attributeKey, propertyName, logicalName, dynamicCodeService.TypeMapper.GetCLRType(type))
        {
            Type = type.ToLower();

        }
        public DynamicPropertyBuilder(DynamicCodeService dynamicCodeService, DynamicTableBuilder dynamicTableBuilder, string attributeKey, string propertyName, string logicalName, Type type)
        {
            this.dynamicCodeService = dynamicCodeService;
            this.dynamicTableBuilder = dynamicTableBuilder;
            SchemaName = propertyName;
            PropertyType = type;
            AttributeKey = attributeKey;
            LogicalName = logicalName;

        }
        private List<string> InverseProperties = new List<string>();


        public bool IsLookup { get; private set; }
        public DynamicTableBuilder ReferenceType { get; private set; }
        //  public ForeignKeyInfo ForeignKey { get; private set; }
        public object OnDeleteCascade { get; private set; }
        public object OnUpdateCascade { get; private set; }
        public DynamicPropertyBuilder LookupTo(DynamicTableBuilder related, object onDelete = null, object onUpdate = null)
        {
            IsLookup = true;
            ReferenceType = related;
            OnDeleteCascade = onDelete;
            OnUpdateCascade = onUpdate;

            this.dynamicTableBuilder.AddAsDependency(related);


            //   ForeignKey = foreignKey;
            var FKLogicalName = LogicalName;

            if (FKLogicalName.EndsWith("id", StringComparison.OrdinalIgnoreCase))
                FKLogicalName = FKLogicalName.Substring(0, FKLogicalName.Length - 2);

            var FKSchemaName = SchemaName;
            if (FKSchemaName.EndsWith("id", StringComparison.OrdinalIgnoreCase))
                FKSchemaName = FKSchemaName.Substring(0, FKSchemaName.Length - 2);

            related.AddInverseLookup(AttributeKey, FKSchemaName, dynamicTableBuilder);

            //var foreigh = manifest.SelectToken($"$.entities['{attributeDefinition.Value.SelectToken("$.type.referenceType").ToString()}']") as JObject;
            //  name= foreigh.SelectToken("$.pluralName")?.ToString()
            var foreighSchemaName = related.SchemaName;// foreigh.SelectToken("$.schemaName")?.ToString();

            //  var foreighEntityCollectionSchemaName = (foreigh.SelectToken("$.pluralName")?.ToString() ?? (foreigh.Parent as JProperty).Name).Replace(" ", "");



            dynamicTableBuilder.AddProperty(null, FKSchemaName, FKLogicalName, related.GetTypeInfo())
               .AddForeignKey(SchemaName);

            //var (attFKProp, attFKField) = CreateProperty(entityType, (FKSchemaName ??
            //    (foreigh.Parent as JProperty).Name).Replace(" ", ""), foreighSchemaName == entitySchameName ?
            //        entityType.Builder :
            //        GetRemoteTypeIfExist(foreigh) ??
            //        GetOrCreateEntityBuilder(myModule, foreighSchemaName, manifest, foreigh, (foreigh.Parent as JProperty).Name, false, nameof(CreateDTO) + "2").Builder as Type);


            //CustomAttributeBuilder ForeignKeyAttributeBuilder = new CustomAttributeBuilder(options.ForeignKeyAttributeCtor, new object[] { attProp.Name });

            //attFKProp.SetCustomAttribute(ForeignKeyAttributeBuilder);

            //CreateJsonSerializationAttribute(attributeDefinition, attFKProp, FKLogicalName);
            //CreateDataMemberAttribute(attFKProp, FKLogicalName);


            return this;
        }
        public string ForeignKey { get; private set; }
        public DynamicPropertyBuilder AddForeignKey(string schemaName)
        {
            ForeignKey = schemaName;
            return this;
        }

        public void Build()
        {
            if (PropertyType == null || dynamicTableBuilder.ContainsParentProperty(SchemaName))
            {
                return;
            }


            var (prop, field) = dynamicCodeService.EmitPropertyService.CreateProperty(this.TypeBuilder, SchemaName, DTOPropertyType ?? PropertyType);

            dynamicCodeService.EmitPropertyService.CreateDataMemberAttribute(prop, LogicalName,AttributeKey);

            dynamicCodeService.EmitPropertyService.CreateJsonSerializationAttribute(prop, LogicalName);

            foreach (var inverse in InverseProperties)
            {
                CustomAttributeBuilder ForeignKeyAttributeBuilder = new CustomAttributeBuilder(dynamicCodeService.Options.InverseAttributeCtor, new object[] { inverse });

                prop.SetCustomAttribute(ForeignKeyAttributeBuilder);
            }

            if (IsPrimaryField)
            {
                CustomAttributeBuilder PrimaryFieldAttributeBuilder = new CustomAttributeBuilder(typeof(PrimaryFieldAttribute).GetConstructor(new Type[] { }), new object[] { });

                prop.SetCustomAttribute(PrimaryFieldAttributeBuilder);


            }

            if (IsPrimaryKey)
            {
                CustomAttributeBuilder PrimaryKeyAttributeBuilder = new CustomAttributeBuilder(typeof(PrimaryKeyAttribute).GetConstructor(new Type[] { }), new object[] { });

                prop.SetCustomAttribute(PrimaryKeyAttributeBuilder);


            }

            if (Description != null)
            {
                CustomAttributeBuilder DescriptionAttributeBuilder = new CustomAttributeBuilder(typeof(DescriptionAttribute).GetConstructor(new Type[] { typeof(string) }), new object[] { Description });

                prop.SetCustomAttribute(DescriptionAttributeBuilder);


            }

            if (enumbuilder != null)
            {
                foreach (var values in Choices)
                    enumbuilder.DefineLiteral(string.IsNullOrWhiteSpace( values.Key) ? "Empty": values.Key, values.Value);

                DTOPropertyType = typeof(Nullable<>).MakeGenericType(enumbuilder.CreateTypeInfo()); ;
                //                }

                //                return ;
            }
            //    Builder = prop;

            if (!string.IsNullOrEmpty(ForeignKey))
            {
                CustomAttributeBuilder ForeignKeyAttributeBuilder = new CustomAttributeBuilder(this.dynamicCodeService.Options.ForeignKeyAttributeCtor, new object[] { ForeignKey });

                prop.SetCustomAttribute(ForeignKeyAttributeBuilder);
            }
        }
        public TypeBuilder TypeBuilder => this.dynamicTableBuilder.Builder;

        public bool IsPrimaryKey { get; private set; }
        public bool IsPrimaryField { get; private set; }
        public bool IsRequired { get; private set; }
        public bool IsRowVersion { get; private set; }

        public DynamicPropertyBuilder PrimaryKey()
        {
            this.PropertyType = Nullable.GetUnderlyingType(this.PropertyType) ?? this.PropertyType;
            IsPrimaryKey = true;
            return this;
        }
        public DynamicPropertyBuilder PrimaryField()
        {
            IsPrimaryField = true; return this;
        }
        public DynamicPropertyBuilder Required(bool required = true)
        {
            IsRequired = required;

            return this;
        }


        public DynamicPropertyBuilder RowVersion(bool rowversion = true)
        {
            IsRowVersion = rowversion; return this;
        }

        public bool HasScaleInfo { get; private set; }
        public int Precision { get; private set; } = 18;
        public int Scale { get; private set; } = 4;
        public string ExternalHash { get; private set; }
        public string ExternalTypeHash { get; private set; }
        public string Description { get; private set; }
        public IColumnPropertyResolver ColumnPropertyResolver { get; private set; }
        public int? MaxLength { get; private set; }
        public IndexInfo IndexInfo { get; internal set; }
        public bool IsManifestType => AttributeKey != null;

        public DynamicPropertyBuilder WithPrecision(int precision, int scale)
        {
            Precision = precision;
            Scale = scale;
            HasScaleInfo = true;
            return this;
        }

        public DynamicPropertyBuilder AddProperty(string attributekey, string propertyName, string logicalName, string type)
        {
            return this.dynamicTableBuilder.AddProperty(attributekey, propertyName, logicalName, type);
        }

        internal void AddInverseAttribute(string propertySchemaName)
        {
            InverseProperties.Add(propertySchemaName);
        }

        internal DynamicPropertyBuilder WithExternalHash(string v)
        {
            this.ExternalHash = v;
            return this;

        }

        internal DynamicPropertyBuilder WithExternalTypeHash(string v)
        {
            this.ExternalTypeHash = v;
            return this;
        }

        internal DynamicPropertyBuilder WithDescription(string v)
        {
            this.Description = v;
            return this;
        }

        internal DynamicPropertyBuilder WithMigrationColumnProvider(IColumnPropertyResolver columnPropertyResolver)
        {
            this.ColumnPropertyResolver = columnPropertyResolver;
            return this;
        }

        internal object GetColumnParam(string argName)
        {
            return ColumnPropertyResolver.GetValue(argName);
        }

        internal DynamicPropertyBuilder WithMaxLength(int? v)
        {
            this.MaxLength = v;
            return this;
        }

        internal DynamicPropertyBuilder WithIndex(IndexInfo indexInfo)
        {
            IndexInfo = indexInfo;
            return this;
        }
        public EnumBuilder enumbuilder { get; private set; }
        public Dictionary<string, int> Choices { get; private set; }
        public DynamicPropertyBuilder AddChoiceOptions(string enumName, Dictionary<string, int> choices)
        {

            enumbuilder = dynamicTableBuilder.DynamicAssemblyBuilder.Module.DefineEnum(enumName, TypeAttributes.Public, typeof(int));
            DTOPropertyType = typeof(Nullable<>).MakeGenericType(enumbuilder);// ; enumbuilder;
            Choices = choices;

            return this;
        }
    }
}
