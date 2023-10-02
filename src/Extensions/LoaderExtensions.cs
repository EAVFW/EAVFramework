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

            if (member.Expression is MemberExpression propEx)
            {
                var prop = propEx.Member as PropertyInfo;
                var b = (prop).GetValue(entity, null);
                var k = prop.DeclaringType.GetProperty(prop.GetCustomAttribute<ForeignKeyAttribute>().Name).GetValue(entity);
                b ??= await context.Context.FindAsync(prop.PropertyType, k);

                prop.SetValue(entity, b);




                return getter(entity);
            }
            else
            {

                propEx = member;

                var prop = propEx.Member as PropertyInfo;
                var b = (prop).GetValue(entity, null);
                var k = prop.DeclaringType.GetProperty(prop.GetCustomAttribute<ForeignKeyAttribute>().Name).GetValue(entity);
                b ??= await context.Context.FindAsync(prop.PropertyType, k);

                prop.SetValue(entity, b);
                  
                return getter(entity);
            }

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
