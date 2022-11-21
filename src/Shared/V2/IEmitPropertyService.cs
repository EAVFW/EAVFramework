using System;
using System.Reflection;
using System.Reflection.Emit;

namespace EAVFramework.Shared.V2
{
    public interface IEmitPropertyService
    {
        void AddInterfaces(DynamicTableBuilder dynamicTableBuilder);
        void CreateJsonSerializationAttribute(PropertyBuilder attProp, string logicalName);
        void CreateDataMemberAttribute(PropertyBuilder attProp, string logicalName, string entityKey);
        (PropertyBuilder, FieldBuilder) CreateProperty(TypeBuilder builder, string name, Type type, PropertyAttributes props = PropertyAttributes.None,
            MethodAttributes methodAttributes = MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig | MethodAttributes.Virtual);

        void EmitNullable(ILGenerator entityCtorBuilderIL, Action p, ParameterInfo arg1);
        void WriteLambdaExpression(ModuleBuilder builder, ILGenerator il, Type clrType, params MethodInfo[] getters);
        void CreateTableImpl(string entityCollectionName, string schema, Type columnsCLRType, MethodBuilder columsMethod, MethodBuilder ConstraintsMethod, ILGenerator UpMethodIL);
        void AddForeignKey(string EntityCollectionSchemaName, string schema, ILGenerator UpMethodIL, DynamicPropertyBuilder dynamicPropertyBuilder);
           
    }
}
