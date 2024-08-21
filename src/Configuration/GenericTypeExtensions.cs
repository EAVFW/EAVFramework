using EAVFramework;
using System;
using System.Reflection;
using System.Linq;
using EAVFramework.Shared;
using System.Collections.Generic;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class GenericTypeExtensions
    {
        public static object GetDynamicService<TContext>(this IServiceProvider serviceProvider,Type t) where TContext : DynamicContext
        {

            var ctx = serviceProvider.GetRequiredService<TContext>();

            var targetType = ResoveType<TContext>(t,ctx);

            return ActivatorUtilities.CreateInstance(serviceProvider, targetType);





        }

        private static Type ResoveType<TContext>(Type t, TContext ctx) where TContext : DynamicContext
        {
           

            var types = ctx.Manager.ModelDefinition.EntityDTOs.Values;

            var ttt = t.GetGenericArguments().Select(genericTypeArgument =>
            {
                return ResolveGenericArgument(t, ctx, genericTypeArgument);

            }).ToArray();
            var targetType = t.MakeGenericType(ttt);
            return targetType;
        }

        private static Type ResolveGenericArgument<TContext>(Type t, TContext ctx, Type genericTypeArgument) where TContext : DynamicContext
        {
            /**
             * genericTypeArguments is the TSomething of class Test<TSomething>
             * 
             * If Constraint is DynamicContext, return TContext
           
             */

            var types = ctx.Manager.ModelDefinition.EntityDTOs.Values;

            var constraints = genericTypeArgument.GetGenericParameterConstraints();
            if (constraints.Any(tta => tta == typeof(DynamicContext)))
            {
                return typeof(TContext);
            }

            /**
             * If Constraint is IConvertible, 
             *  return the type of the property that has the EntityFieldAttribute with the AttributeKey that matches the ConstraintMappingAttribute
             */
            if (constraints.Any(tta => tta == typeof(IConvertible)))
            {
                //enums
                var constraintMapping = t.GetCustomAttributes<ConstraintMappingAttribute>().SingleOrDefault(c => c.ConstraintName == genericTypeArgument.Name);
                // ?? throw new InvalidOperationException($"No ConstraintMappingAttribute set for the Contraints {genericTypeArguments.Name} on {t.Name}");

                if (constraintMapping == null)
                {
                    var otherConstraint = t.GetGenericArguments()
                        .Where(gta => gta.GetGenericParameterConstraints().Any(gpc => gpc.IsInterface && gpc.GetGenericArguments().Any(ga => ga.Name == genericTypeArgument.Name)))
                        .FirstOrDefault();

                    if (otherConstraint != null)
                    {
                        var targetInterface = otherConstraint.GetGenericParameterConstraints().FirstOrDefault(c => c.IsInterface);
                        constraintMapping = targetInterface
                            .GetCustomAttributes<ConstraintMappingAttribute>().SingleOrDefault(c => c.ConstraintName == genericTypeArgument.Name);
                        constraintMapping.EntityKey ??= targetInterface.GetCustomAttribute<EntityInterfaceAttribute>()?.EntityKey;
                    }


                }

                if (constraintMapping == null)
                    throw new InvalidOperationException($"No ConstraintMappingAttribute set for the Contraints {genericTypeArgument.Name} on {t.Name}");


                var entirtyType = types.SingleOrDefault(t =>
                t.GetCustomAttribute<EntityDTOAttribute>() is EntityDTOAttribute &&
                t.GetCustomAttribute<EntityAttribute>() is EntityAttribute attr && attr.EntityKey == constraintMapping.EntityKey)
                ?? throw new InvalidOperationException($"No ConstraintMappingAttribute set for the Contraints {genericTypeArgument.Name} on {t.Name}"); ;

                var propertyType = entirtyType.GetProperties().SingleOrDefault(c =>
                    c.GetCustomAttribute<EntityFieldAttribute>() is EntityFieldAttribute field && field.AttributeKey == constraintMapping.AttributeKey)
                ?? throw new InvalidOperationException($"No ConstraintMappingAttribute set for the Contraints {genericTypeArgument.Name} on {t.Name}");

                return Nullable.GetUnderlyingType(propertyType.PropertyType) ?? propertyType.PropertyType;


            }

            /**
             * If the consraint has a interface with EntityInterfaceAttribute we can use that to find the type.
             */

            var @interface = constraints.FirstOrDefault(c => c.IsInterface && !string.IsNullOrEmpty(c.GetCustomAttribute<EntityInterfaceAttribute>()?.EntityKey));
            if (@interface != null)
            {

                if (@interface.IsGenericType)
                {
                    return types.Where(t => t.GetCustomAttribute<EntityDTOAttribute>() != null &&
                   t.GetInterfaces().Any(c => c.IsGenericType && c.GetGenericTypeDefinition() == @interface.GetGenericTypeDefinition()))
                    .SingleOrDefault() ?? throw new InvalidOperationException($"No type that has interface {@interface}"); ;
                }

                var constraintMappinng = t.GetCustomAttributes<ConstraintMappingAttribute>().FirstOrDefault(cm => cm.ConstraintName == genericTypeArgument.Name);
                if (constraintMappinng?.EntityKey != null)
                {
                    var targetdto = types.Where(t => t.GetCustomAttribute<EntityAttribute>()?.EntityKey == constraintMappinng.EntityKey).FirstOrDefault();
                    if (targetdto != null)
                        return targetdto;
                }

                var result = types.Where(t => t.GetCustomAttribute<EntityDTOAttribute>() != null &&
                        t.GetInterfaces().Any(c => c == @interface)).ToArray();
                if (result.Length > 1)
                {
                    result= result.Where(r => !result.Any(rr => rr == r.BaseType)).ToArray();
                }
                return result.SingleOrDefault() ?? throw new InvalidOperationException($"No type that has interface {@interface}"); ;
            }

            /**
             * If we cant find the constraint,it could be a type from the DI
             */
            if (constraints.Skip(1).Any())
            {
                throw new InvalidOperationException($"Cant find constraint for {genericTypeArgument.Name} on {t.Name}");
            }

            var targetServiceType = constraints.First();

            return ResoveType(targetServiceType.IsGenericType ? targetServiceType.GetGenericTypeDefinition() : targetServiceType, ctx);
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
