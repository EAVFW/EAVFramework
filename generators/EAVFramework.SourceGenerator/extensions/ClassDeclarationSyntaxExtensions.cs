﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;



namespace EAVFramework.Generators
{
    public static class ClassDeclarationSyntaxExtensions
    {
        public const string NESTED_CLASS_DELIMITER = "+";
        public const string NAMESPACE_CLASS_DELIMITER = ".";

        public static string GetFullName(this ClassDeclarationSyntax source)
        {
            Contract.Requires(null != source);

            var items = new List<string>();
            var parent = source.Parent;
            while (parent.IsKind(SyntaxKind.ClassDeclaration))
            {
                var parentClass = parent as ClassDeclarationSyntax;
                Contract.Assert(null != parentClass);
                items.Add(parentClass.Identifier.Text);

                parent = parent.Parent;
            }

            var nameSpace = parent as NamespaceDeclarationSyntax;
            Contract.Assert(null != nameSpace);
            var sb = new StringBuilder().Append(nameSpace.Name).Append(NAMESPACE_CLASS_DELIMITER);
            items.Reverse();
            items.ForEach(i => { sb.Append(i).Append(NESTED_CLASS_DELIMITER); });
            sb.Append(source.Identifier.Text);

            var result = sb.ToString();
            return result;
        }

        public static string GetFullName(this ITypeSymbol source)
        {
            if (source==null)
                return null;

            var items = new List<string>() { source.Name };
            var parent = source.ContainingNamespace;
            while (parent !=null && !string.IsNullOrEmpty(parent.Name))
            {
                items.Add(parent.Name);
                parent = parent.ContainingNamespace;
            }


            var sb = new StringBuilder();
            items.Reverse();
            items.ForEach(i => { sb.Append(i).Append(NAMESPACE_CLASS_DELIMITER); });
          
            var result = sb.ToString(0,sb.Length-1);
            return result;
        }
    }
}
