using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;
using Microsoft.OData;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.OData.Query.Expressions;
using System.Linq.Expressions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.OData;
using Microsoft.Extensions.Options;
using System.Reflection.Metadata;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
using Microsoft.AspNetCore.OData.Query;

namespace EAVFramework.Infrastructure
{

    public class CustomFilterBinder : FilterBinder
    {
        public override Expression? BindFilter(FilterClause filterClause, QueryBinderContext context)
        {
            if (filterClause == null || context == null)
            {
                return null;
            }

            if (filterClause.Expression is SingleValueFunctionCallNode functionCallNode &&
                functionCallNode.Name == "totalseconds")
            {
                return BindCustomMethodExpressionOrNull(functionCallNode, context);
            }

            return base.BindFilter(filterClause, context);
        }

        internal static Expression ExtractValueFromNullableExpression(Expression source)
        {
            return Nullable.GetUnderlyingType(source.Type) != null ? Expression.Property(source, "Value") : source;
        }

        private static IEnumerable<Expression> ExtractValueFromNullableArguments(IEnumerable<Expression> arguments)
        {
            return arguments.Select(arg => ExtractValueFromNullableExpression(arg));
        }

        internal Expression MakeFunctionCall(MemberInfo member, params Expression[] arguments)
        {
            
            IEnumerable<Expression> functionCallArguments = arguments;
            

            // if the argument is of type Nullable<T>, then translate the argument to Nullable<T>.Value as none
            // of the canonical functions have overloads for Nullable<> arguments.
            functionCallArguments = ExtractValueFromNullableArguments(functionCallArguments);

            Expression functionCall;
            if (member.MemberType == MemberTypes.Method)
            {
                MethodInfo method = member as MethodInfo;
                if (method.IsStatic)
                {
                    functionCall = Expression.Call(null, method, functionCallArguments);
                }
                else
                {
                    functionCall = Expression.Call(functionCallArguments.First(), method, functionCallArguments.Skip(1));
                }
            }
            else
            {
                // property
                functionCall = Expression.Property(functionCallArguments.First(), member as PropertyInfo);
            }

            return CreateFunctionCallWithNullPropagation(functionCall, arguments);
        }
        internal Expression CreateFunctionCallWithNullPropagation(Expression functionCall, Expression[] arguments)
        {
            //if (QuerySettings.HandleNullPropagation == HandleNullPropagationOption.True)
            //{
            //    Expression test = CheckIfArgumentsAreNull(arguments);

            //    if (test == FalseConstant)
            //    {
            //        // none of the arguments are/can be null.
            //        // so no need to do any null propagation
            //        return functionCall;
            //    }
            //    else
            //    {
            //        // if one of the arguments is null, result is null (not defined)
            //        return
            //            Expression.Condition(
            //            test: test,
            //            ifTrue: Expression.Constant(null, ToNullable(functionCall.Type)),
            //            ifFalse: ToNullable(functionCall));
            //    }
            //}
            //else
            {
                return functionCall;
            }
        }


        //https://github.com/dotnet/efcore/blob/main/src/EFCore.SqlServer/Query/Internal/SqlServerMemberTranslatorProvider.cs
        protected override Expression? BindCustomMethodExpressionOrNull(SingleValueFunctionCallNode node, QueryBinderContext context)
        {
           
            if (node.Name.Equals("DateDiffSecond", StringComparison.OrdinalIgnoreCase))
            {
                if (node.Parameters.Count() != 2)
                {
                    throw new InvalidOperationException("totalseconds function requires exactly one parameter");
                }
                var args = BindArguments(node.Parameters, context);
                var startDate = args[0];
                var endDate = args[1];

                var method = typeof(SqlServerDbFunctionsExtensions).GetMethod("DateDiffSecond", new Type[] { typeof(DbFunctions), typeof(DateTimeOffset), typeof(DateTimeOffset) });
               
                    
                //    var efType = typeof(EF).GetProperty("Functions")!.PropertyType;
              //  var efFunctions = Expression.Property(null, typeof(EF), "Functions");

                return MakeFunctionCall(method, [Expression.Constant(EF.Functions), .. args]);

               
            }

            return base.BindCustomMethodExpressionOrNull(node, context);
        }

        private static Expression? BindFilter(Type elementType, SingleValueFunctionCallNode node, QueryBinderContext context)
        {
            var parameterExpression = Expression.Parameter(elementType, "record");
            var property = node.Parameters.FirstOrDefault() as SingleValuePropertyAccessNode;
            var memberExpression = Expression.Property(parameterExpression, property?.Property?.Name ?? "");

            var equalExpressions = new List<Expression>();
            foreach (var constantNode in node.Parameters.Skip(1))
            {
                if (constantNode is ConstantNode constant)
                {
                    var constantExpression = Expression.Constant(constant.Value, typeof(string));
                    var equalExpression = Expression.Equal(memberExpression, constantExpression);
                    equalExpressions.Add(equalExpression);
                }
            }

            if (equalExpressions.Any())
            {
                Expression orExpression = equalExpressions.Aggregate(Expression.OrElse);
                return Expression.Lambda(orExpression, parameterExpression);
            }

            return null;
        }
    }

    public static class EAVOdataServices
    {
        public static IServiceCollection AddEAVOdata<TContext>(this IServiceCollection services)
            where TContext : DynamicContext
        {
           

            CustomUriFunctions.AddCustomUriFunction("DateDiffSecond", new FunctionSignatureWithReturnType(
                EdmCoreModel.Instance.GetDecimal(false), EdmCoreModel.Instance.GetDate(true), EdmCoreModel.Instance.GetDateTimeOffset(true)
                ));

           
            CustomUriFunctions.AddCustomUriFunction("DateDiffSecond", new FunctionSignatureWithReturnType(
                EdmCoreModel.Instance.GetDecimal(false), EdmCoreModel.Instance.GetDateTimeOffset(true), EdmCoreModel.Instance.GetDate(true)
                ));

            CustomUriFunctions.AddCustomUriFunction("DateDiffSecond", new FunctionSignatureWithReturnType(
                EdmCoreModel.Instance.GetDecimal(false), EdmCoreModel.Instance.GetDate(true), EdmCoreModel.Instance.GetDate(true)
                ));
            CustomUriFunctions.AddCustomUriFunction("DateDiffSecond", new FunctionSignatureWithReturnType(
              EdmCoreModel.Instance.GetDecimal(false), EdmCoreModel.Instance.GetDateTimeOffset(true), EdmCoreModel.Instance.GetDateTimeOffset(true)
              ));

            services.AddScoped(sp =>
            {
                var o = new ODataOptions();
                o.AddRouteComponents("/api/", sp.GetRequiredService<TContext>().EnsureModelCreated().Model,
                    services => {
                        services.AddSingleton<ODataUriResolver>(sp => new EAVODataUriResolver { EnableCaseInsensitive = true }); 
                        services.AddSingleton<IFilterBinder, CustomFilterBinder>();

                    }
                    );
                return Options.Create(o);
            });
            return services;
        }
    }
    public class EAVODataUriResolver : ODataUriResolver
    {
        /// <summary>
        /// Parse string or integer to enum value
        /// </summary>
        /// <param name="enumType">edm enum type</param>
        /// <param name="value">input string value</param>
        /// <param name="enumValue">output edm enum value</param>
        /// <returns>true if parse succeeds, false if fails</returns>
        private static bool TryParseEnum(IEdmEnumType enumType, string value, out ODataEnumValue enumValue)
        {
            long parsedValue;
            bool success = enumType.TryParseEnum(value, true, out parsedValue);
            enumValue = null;
            if (success)
            {
                enumValue = new ODataEnumValue(parsedValue.ToString(CultureInfo.InvariantCulture),
                    enumType.FullTypeName());
            }

            return success;
        }


        public override void PromoteBinaryOperandTypes(BinaryOperatorKind binaryOperatorKind,
            ref SingleValueNode leftNode, ref SingleValueNode rightNode, out IEdmTypeReference typeReference)
        {
            typeReference = null;

            if (leftNode.TypeReference != null && rightNode.TypeReference != null)
            {
                if ((leftNode.TypeReference.IsEnum()) && (rightNode.TypeReference.IsString()) &&
                    rightNode is ConstantNode)
                {
                    string text = ((ConstantNode)rightNode).Value as string;
                    ODataEnumValue val;
                    IEdmTypeReference typeRef = leftNode.TypeReference;

                    if (TryParseEnum(typeRef.Definition as IEdmEnumType, text, out val))
                    {
                        rightNode = new ConstantNode(val, text, typeRef);
                        return;
                    }
                }
                else if ((rightNode.TypeReference.IsEnum()) && (leftNode.TypeReference.IsString()) &&
                         leftNode is ConstantNode)
                {
                    string text = ((ConstantNode)leftNode).Value as string;
                    ODataEnumValue val;
                    IEdmTypeReference typeRef = rightNode.TypeReference;
                    if (TryParseEnum(typeRef.Definition as IEdmEnumType, text, out val))
                    {
                        leftNode = new ConstantNode(val, text, typeRef);
                        return;
                    }
                }
                else if ((leftNode.TypeReference.IsEnum()) &&
                         (rightNode.TypeReference.Definition is IEdmPrimitiveType typeType &&
                          typeType.PrimitiveKind == EdmPrimitiveTypeKind.Int32) && rightNode is ConstantNode)
                {
                    int value = (int)((ConstantNode)rightNode).Value;
                     
                    IEdmTypeReference typeRef = leftNode.TypeReference;
                    var enumType = typeRef.Definition as IEdmEnumType;


                    rightNode = new ConstantNode(
                        new ODataEnumValue(value.ToString(CultureInfo.InvariantCulture), enumType.FullTypeName()),
                        enumType.Members.FirstOrDefault(k => k.Value.Value == value).Name, typeRef);
                    return;
                }
                else if ((rightNode.TypeReference.IsEnum()) &&
                         (leftNode.TypeReference.Definition is IEdmPrimitiveType lefttype &&
                          lefttype.PrimitiveKind == EdmPrimitiveTypeKind.Int32) && leftNode is ConstantNode)
                {
                    int value = (int) ((ConstantNode) leftNode).Value;
                   
                    IEdmTypeReference typeRef = rightNode.TypeReference;
                    var enumType = typeRef.Definition as IEdmEnumType;

                    leftNode = new ConstantNode(
                        new ODataEnumValue(value.ToString(CultureInfo.InvariantCulture), enumType.FullTypeName()),
                        enumType.Members.FirstOrDefault(k => k.Value.Value == value).Name, typeRef);
                    return;
                }
            }

            // fallback
            base.PromoteBinaryOperandTypes(binaryOperatorKind, ref leftNode, ref rightNode, out typeReference);
        }
    }
}
