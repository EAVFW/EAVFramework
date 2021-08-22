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
    }
    public class EntityTypeBuilder
    {
        public void Property(string name) => throw new NotImplementedException();

        public virtual KeyBuilder HasKey([NotNullAttribute] params string[] propertyNames) => throw new NotImplementedException();
        public virtual KeyBuilder HasAlternateKey([NotNullAttribute] params string[] propertyNames) => throw new NotImplementedException();


    }
    public static class RelationalEntityTypeBuilderExtensions
    {
        public static EntityTypeBuilder ToTable([NotNullAttribute] this EntityTypeBuilder entityTypeBuilder, [CanBeNullAttribute] string name, [CanBeNullAttribute] string schema) => throw new NotImplementedException();

    }
}