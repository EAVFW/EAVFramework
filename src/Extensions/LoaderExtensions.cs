using EAVFramework.Endpoints;
using EAVFramework.Plugins;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace EAVFramework.Extensions
{
    public static class LoaderExtensions
    {
        public static async ValueTask<TProperty> LoadIfMissingAsync<TContext, TEntity, TProperty>(this EAVDBContext<TContext> context, TEntity entity, Expression<Func<TEntity, TProperty>> selector)
            where TContext : DynamicContext
            where TEntity : DynamicEntity
        {
            var member = selector.Body as MemberExpression;
            var getter = selector.Compile();

            return await ResolveProp(context, entity, getter, member.Member as PropertyInfo);

        }

        private static async ValueTask<TProperty> ResolveProp<TContext, TEntity, TProperty>(EAVDBContext<TContext> context, TEntity entity, Func<TEntity, TProperty> getter, PropertyInfo prop)
            where TContext : DynamicContext
            where TEntity : DynamicEntity
        {
            if (prop.DeclaringType.IsInterface)
            {
                prop = entity.GetType().GetProperty(prop.Name);
            }

            var b = (prop).GetValue(entity, null);
            var k = prop.DeclaringType.GetProperty(prop.GetCustomAttribute<ForeignKeyAttribute>().Name).GetValue(entity);
            b ??= await context.Context.FindAsync(prop.PropertyType, k);

            prop.SetValue(entity, b);




            return getter(entity);
        }

        public static ValueTask<TProperty> LoadIfMissingAsync<TContext, TEntity, TProperty>(this PluginContext<TContext, TEntity> context, Expression<Func<TEntity, TProperty>> selector)
            where TContext : DynamicContext
            where TEntity : DynamicEntity
        {

            return context.DB.LoadIfMissingAsync<TContext, TEntity, TProperty>(context.Input, selector);
        }
        public static ValueTask<TProperty> LoadIfMissingAsync<TContext, TInputentity,TEntity, TProperty>(this PluginContext<TContext, TInputentity> context, TEntity entity, Expression<Func<TEntity, TProperty>> selector)
           where TContext : DynamicContext
           where TInputentity : DynamicEntity
           where TEntity : DynamicEntity
        {

            return context.DB.LoadIfMissingAsync<TContext, TEntity, TProperty>(entity, selector);
        }

        public static ValueTask<TProperty> LoadAsync<TContext, TEntity, TProperty>(this PluginContext<TContext, TEntity> context, Expression<Func<TEntity, TProperty>> selector)
           where TContext : DynamicContext
           where TEntity : DynamicEntity
        {

            return context.DB.LoadIfMissingAsync<TContext, TEntity, TProperty>(context.Input, selector);
        }


    }
}
