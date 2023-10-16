﻿
namespace EasilyNET.Core.Domain.SourceGenerator;

/// <summary>
/// 审计后实休源代码生成器
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class AuditedEntitySourceGenerator : ISourceGenerator
{
    /// <inheritdoc />
    public void Initialize(GeneratorInitializationContext context)
    {

        // if (!Debugger.IsAttached)
        // {
        //     Debugger.Launch();
        // }
    }


    /// <inheritdoc />
    public void Execute(GeneratorExecutionContext context)
    {
        var compilation = context.Compilation;
        foreach (var syntaxTree in compilation.SyntaxTrees)
        {

            var root = syntaxTree.GetRoot();
            var semanticModel = context.Compilation.GetSemanticModel(syntaxTree);
            var classDeclarations = root.DescendantNodes().OfType<ClassDeclarationSyntax>();
            foreach (var classDeclaration in classDeclarations)
            {

                //判断是否分部类
                if (!classDeclaration.Modifiers.Any(SyntaxKind.PartialKeyword))
                {

                    continue;
                }
                // // 处理分部类并继承自 Entity`1 的情况
                // if (classDeclaration.BaseList?.Types.OfType<SimpleBaseTypeSyntax>()
                //                     .Any(typeSyntax =>
                //                     {
                //                         if (typeSyntax.Type.Kind() == SyntaxKind.GenericName)
                //                         {
                //                             var genericName = (GenericNameSyntax)typeSyntax.Type;
                //                             return genericName.Identifier.ValueText == "Entity";
                //                         }
                //                         return false;
                //                     }) ==
                //     false)
                // {
                //     continue;
                // }
                var classSymbol = semanticModel.GetDeclaredSymbol(classDeclaration);
                if (classSymbol is null)
                {
                    continue;
                }
                //只处理这接口
                foreach (var interfaceSymbol in classSymbol!.AllInterfaces.Where(i =>
                             i.Name == "IMayHaveCreator" ||
                             i.Name == "IHasCreationTime" ||
                             i.Name == "IHasModifierId" ||
                             i.Name == "IHasModificationTime" ||
                             i.Name == "IHasDeleterId" ||
                             i.Name == "IHasDeletionTime"
                         ))
                {
                    //得到接口属性
                    var propertySymbols = interfaceSymbol.GetMembers().OfType<IPropertySymbol>();
                    foreach (var propertySymbol in propertySymbols)
                    {
                        string ns = classSymbol.ContainingNamespace.ToString();
                        string propertyName = propertySymbol.Name;
                        string propertyType = propertySymbol.Type.ToDisplayString();
                        //string propertyAccessibility = propertySymbol.DeclaredAccessibility.ToString();
                        string source = $@"// <auto-generated/>
                        using EasilyNET.Core.Domains;
                        using System;
                        using System.ComponentModel;
                        namespace {ns};
                        public partial class  {classSymbol.Name}  
                            {{
                               public {propertyType} {propertyName} {{get;}}
                            }}";
                        string extensionTextFormatted = CSharpSyntaxTree.ParseText(source).GetRoot().NormalizeWhitespace().SyntaxTree.GetText().ToString();
                        context.AddSource($"{interfaceSymbol.Name}.{classSymbol.Name}.g.cs", SourceText.From(extensionTextFormatted, Encoding.UTF8));
                        //
                        //
                        // Debug.WriteLine($"Property Name: {propertyName}, Property Type: {propertyType}, Accessibility: {propertyAccessibility}");
                        // Debug.WriteLine(source);
                    }
                }
            }
            
        }
    }

}