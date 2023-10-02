using System;
using System.Linq;

namespace EAVFramework.Extensions
{
    public static class TypeChanger
    {
        public static IQueryable<T> ChangeType<T>(IQueryable data)
        {
            return data.Cast<T>();// as IQueryable<T>;
        }

        public static IQueryable Cast(this IQueryable data, Type type)
        {
            return (IQueryable)typeof(TypeChanger).GetMethod(nameof(TypeChanger.ChangeType)).MakeGenericMethod(type).Invoke(null, new[] { data });
        }
    }
}
