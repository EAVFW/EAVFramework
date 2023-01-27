using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;
using Microsoft.OData;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EAVFramework.Infrastructure
{
    
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
                    ODataEnumValue val;
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
                    int value = (int)((ConstantNode)leftNode).Value;
                    ODataEnumValue val;
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
