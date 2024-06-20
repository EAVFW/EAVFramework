using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Microsoft.EntityFrameworkCore.Metadata.Builders
{
    public class PropertyBuilder
    {
        public virtual PropertyBuilder IsRequired(bool required = true) => throw new NotImplementedException();
         
        public virtual PropertyBuilder IsRowVersion() => throw new NotImplementedException();
        public virtual PropertyBuilder HasConversion<TProvider>() => throw new NotImplementedException();
        public virtual PropertyBuilder HasPrecision(int precision, int scale) => throw new NotImplementedException();
        public virtual PropertyBuilder ValueGeneratedOnAddOrUpdate() => throw new NotImplementedException();
        public virtual PropertyBuilder ValueGeneratedNever() => throw new NotImplementedException();
        
    }
    public class EntityTypeBuilder
    {
        public void Property(string name) => throw new NotImplementedException();

        public virtual KeyBuilder HasKey([NotNullAttribute] params string[] propertyNames) => throw new NotImplementedException();
        public virtual KeyBuilder HasAlternateKey([NotNullAttribute] params string[] propertyNames) => throw new NotImplementedException();


    }
    public static class RelationalEntityTypeBuilderExtensions
    {
        /// <summary>
        ///     Configures TPC as the mapping strategy for the derived types. Each type will be mapped to a different database object.
        ///     All properties will be mapped to columns on the corresponding object.
        /// </summary>
        /// <remarks>
        ///     See <see href="https://aka.ms/efcore-docs-inheritance">Entity type hierarchy mapping</see> for more information and examples.
        /// </remarks>
        /// <param name="entityTypeBuilder">The builder for the entity type being configured.</param>
        /// <returns>The same builder instance so that multiple calls can be chained.</returns>
        public static EntityTypeBuilder UseTpcMappingStrategy(this EntityTypeBuilder entityTypeBuilder) => throw new NotImplementedException();
        public static EntityTypeBuilder ToTable([NotNullAttribute] this EntityTypeBuilder entityTypeBuilder, [CanBeNullAttribute] string name, [CanBeNullAttribute] string schema) => throw new NotImplementedException();

    }
}