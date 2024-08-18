using EAVFramework;
using System;
using System.Reflection;
using System.Linq;
using EAVFramework.Shared;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class GenericTypeExtensions
    {
        public static TImplementation GetDynamicService<TContext,TImplementation>(this IServiceProvider serviceProvider,Type t) where TContext : DynamicContext
        {
             
                var ctx = serviceProvider.GetRequiredService<TContext>();

                var types = ctx.Manager.ModelDefinition.EntityDTOs.Values;

                var ttt = t.GetGenericArguments().Select(ta =>
                {
                    var constraints = ta.GetGenericParameterConstraints();
                    if (constraints.Any(tta => tta == typeof(DynamicContext)))
                    {
                        return typeof(TContext);
                    }

                    if (constraints.Any(tta => tta == typeof(IConvertible)))
                    {
                        //enums
                        var constraintMapping = t.GetCustomAttributes<ConstraintMappingAttribute>().SingleOrDefault(c => c.ConstraintName == ta.Name)
                        ?? throw new InvalidOperationException($"No ConstraintMappingAttribute set for the Contraints {ta.Name} on {t.Name}");

                      
                        var entirtyType = types.SingleOrDefault(t =>
                        t.GetCustomAttribute<EntityDTOAttribute>() is EntityDTOAttribute &&
                        t.GetCustomAttribute<EntityAttribute>() is EntityAttribute attr && attr.EntityKey == constraintMapping.EntityKey);

                        var propertyType = entirtyType.GetProperties().Single(c =>
                            c.GetCustomAttribute<EntityFieldAttribute>() is EntityFieldAttribute field && field.AttributeKey == constraintMapping.AttributeKey);

                        return Nullable.GetUnderlyingType(propertyType.PropertyType) ?? propertyType.PropertyType;


                    }

                    var @interface = constraints.FirstOrDefault(c => c.IsInterface && !string.IsNullOrEmpty(c.GetCustomAttribute<EntityInterfaceAttribute>()?.EntityKey));
                    if (@interface != null)
                    {

                        if (@interface.IsGenericType)
                        {
                            return types.Where(t => t.GetCustomAttribute<EntityDTOAttribute>() != null &&
                           t.GetInterfaces().Any(c => c.IsGenericType && c.GetGenericTypeDefinition() == @interface.GetGenericTypeDefinition())).Single();
                        }


                        return types.Where(t => t.GetCustomAttribute<EntityDTOAttribute>() != null &&
                                t.GetInterfaces().Any(c => c == @interface)).Single();
                    }

                    throw new InvalidOperationException($"Cant find constraint for {ta.Name} on {t.Name}");

                }).ToArray();
                var targetType= t.MakeGenericType(ttt);

                return (TImplementation)ActivatorUtilities.CreateInstance(serviceProvider, targetType);



            
           
        }

        public static Type ResolveGenericArguments<TContext,TModel>(this Type t) where TContext : DynamicContext
        {
            var ttt = t.GetGenericArguments().Select(ta =>
            {
                var constraints = ta.GetGenericParameterConstraints();
                if (constraints.Any(tta => tta == typeof(DynamicContext)))
                {
                    return typeof(TContext);
                }

                if(constraints.Any(tta => tta == typeof(IConvertible)))
                {
                    //enums
                    var constraintMapping = t.GetCustomAttributes<ConstraintMappingAttribute>().SingleOrDefault(c => c.ConstraintName == ta.Name)
                    ?? throw new InvalidOperationException($"No ConstraintMappingAttribute set for the Contraints {ta.Name} on {t.Name}");



                    var entirtyType = typeof(TModel).Assembly.GetTypes().SingleOrDefault(t => 
                    t.GetCustomAttribute<EntityDTOAttribute>() is EntityDTOAttribute &&
                    t.GetCustomAttribute<EntityAttribute>() is EntityAttribute attr && attr.EntityKey == constraintMapping.EntityKey);

                    var propertyType = entirtyType.GetProperties().Single(c =>
                        c.GetCustomAttribute<EntityFieldAttribute>() is EntityFieldAttribute field && field.AttributeKey == constraintMapping.AttributeKey);

                    return Nullable.GetUnderlyingType(propertyType.PropertyType) ?? propertyType.PropertyType;


                }

                var @interface = constraints.FirstOrDefault(c => c.IsInterface && !string.IsNullOrEmpty(c.GetCustomAttribute<EntityInterfaceAttribute>()?.EntityKey));
                if (@interface != null)
                {

                    if (@interface.IsGenericType)
                    {
                        return typeof(TModel).Assembly.GetTypes().Where(t => t.GetCustomAttribute<EntityDTOAttribute>() != null &&
                       t.GetInterfaces().Any(c => c.IsGenericType && c.GetGenericTypeDefinition() == @interface.GetGenericTypeDefinition())).Single();
                    }


                    return typeof(TModel).Assembly.GetTypes().Where(t => t.GetCustomAttribute<EntityDTOAttribute>() != null &&
                            t.GetInterfaces().Any(c => c == @interface)).Single();
                }

                throw new InvalidOperationException($"Cant find constraint for {ta.Name} on {t.Name}" );

            }).ToArray();
            return t.MakeGenericType(ttt);
        }
    }
}
