using System.Text;
using Mapperic.Attributes;
using Mapperic.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Mapperic
{
    [Generator]
    public class DtoSourceGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            // 1. Collect all class declarations
            var classDeclarations = context.SyntaxProvider
                .CreateSyntaxProvider(
                    predicate: static (node, _) => node is ClassDeclarationSyntax,
                    transform: static (ctx, _) => (ClassDeclarationSyntax)ctx.Node
                );

            // 2. Combine with the compilation
            var compilationAndClasses = context.CompilationProvider.Combine(classDeclarations.Collect());

            // 3. We'll produce two outputs:
            //    (a) The "per-class" DTO partial classes
            //    (b) A single "AutoMapper profile" for all [GenerateDto] classes

            // 3a. Create a pipeline that transforms each class into a "DTO generation" result
            var classDtoResults = compilationAndClasses
                .SelectMany((tuple, _) =>
                {
                    var (compilation, classes) = tuple;
                    var results = new List<ClassDtoResult>();

                    var generateDtoAttr  = compilation.GetTypeByMetadataName("Mapperic.Attributes.GenerateDtoAttribute");
                    var dtoPropertyAttr  = compilation.GetTypeByMetadataName("Mapperic.Attributes.DtoPropertyAttribute");
                    var addDtoPropertyAttr = compilation.GetTypeByMetadataName("Mapperic.Attributes.AddDtoPropertyAttribute");

                    if (generateDtoAttr is null || dtoPropertyAttr is null || addDtoPropertyAttr is null)
                        return results;

                    foreach (var cls in classes)
                    {
                        var semanticModel = compilation.GetSemanticModel(cls.SyntaxTree);
                        if (semanticModel.GetDeclaredSymbol(cls) is not INamedTypeSymbol classSymbol)
                            continue;

                        // check if [GenerateDto]
                        if (!HasAttribute(classSymbol, generateDtoAttr))
                            continue;

                        // Collect normal [DtoProperty] members
                        var memberInfos = classSymbol
                            .GetMembers()
                            .Where(m => IsFieldOrPropertyWithDtoAttribute(m, dtoPropertyAttr))
                            .Select(m => ExtractMemberInfo(m, dtoPropertyAttr))
                            .Where(mi => mi != null)
                            .Cast<MemberInfo>()
                            .ToList();

                        // Collect [AddDtoProperty] from the class itself
                        var extraProperties = ExtractAddDtoProperties(classSymbol, addDtoPropertyAttr);
                        memberInfos.AddRange(extraProperties);

                        results.Add(new ClassDtoResult(classSymbol, memberInfos));
                    }

                    return results;
                });

            // 3b. Output partial classes
            context.RegisterSourceOutput(classDtoResults, (spc, dtoResult) =>
            {
                var code = GenerateDtoClass(dtoResult.ClassSymbol, dtoResult.Members);
                spc.AddSource($"{dtoResult.ClassSymbol.Name}Dto.g.cs", SourceText.From(code, Encoding.UTF8));
            });

            // 3c. Aggregate all results and produce one mapping profile
            var allResults = classDtoResults.Collect();
            context.RegisterSourceOutput(allResults, (spc, results) =>
            {
                if (!results.Any()) return;
                var profileCode = GenerateMappingProfile(results);
                spc.AddSource("DtoMappingProfile.g.cs", SourceText.From(profileCode, Encoding.UTF8));
            });
        }
        
        #region Helpers
        private static bool HasAttribute(ISymbol symbol, INamedTypeSymbol attrSymbol)
        {
            return symbol.GetAttributes().Any(a => 
                SymbolEqualityComparer.Default.Equals(a.AttributeClass, attrSymbol));
        }

        private static bool IsFieldOrPropertyWithDtoAttribute(ISymbol member, INamedTypeSymbol dtoPropAttr)
        {
            if (member is IFieldSymbol or IPropertySymbol)
            {
                return member.GetAttributes().Any(a =>
                    SymbolEqualityComparer.Default.Equals(a.AttributeClass, dtoPropAttr));
            }
            return false;
        }

        private static MemberInfo? ExtractMemberInfo(ISymbol member, INamedTypeSymbol dtoPropAttrSymbol)
        {
            // Original name & type
            var originalName = member.Name;
            var origType = member switch
            {
                IFieldSymbol f    => f.Type,
                IPropertySymbol p => p.Type,
                _ => null
            };
            if (origType == null) return null;

            var accessibility = member.DeclaredAccessibility;

            // Default final name/type
            var finalName = originalName;
            var finalType = origType.ToString()!;
            string? conversionExpr = null;

            // read attribute named args
            var attrData = member.GetAttributes()
                .First(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, dtoPropAttrSymbol));

            foreach (var namedArg in attrData.NamedArguments)
            {
                switch (namedArg.Key)
                {
                    case nameof(DtoPropertyAttribute.TargetName):
                        if (namedArg.Value.Value is string s && !string.IsNullOrEmpty(s))
                            finalName = s;
                        break;
                    case nameof(DtoPropertyAttribute.TargetType):
                        if (namedArg.Value.Value is ITypeSymbol ts)
                            finalType = ts.ToString()!;
                        break;
                    case nameof(DtoPropertyAttribute.ConversionExpression):
                        if (namedArg.Value.Value is string expr && !string.IsNullOrEmpty(expr))
                            conversionExpr = expr;
                        break;
                }
            }

            return new MemberInfo(
                OriginalName: originalName,
                OriginalType: origType.ToString()!,
                Accessibility: accessibility,
                FinalName: finalName,
                FinalType: finalType,
                ConversionExpression: conversionExpr,
                IsExtraProperty: false
            );
        }

        /// <summary>
        /// Extract [AddDtoProperty] attributes from the class itself, 
        /// returning a list of "pseudo-members" (MemberInfo) that do not 
        /// exist in the original class, but we want in the DTO.
        /// </summary>
        private static List<MemberInfo> ExtractAddDtoProperties(INamedTypeSymbol classSymbol, INamedTypeSymbol addDtoAttrSymbol)
        {
            var results = new List<MemberInfo>();

            // The class can have multiple [AddDtoProperty(...)] attributes
            var attrs = classSymbol.GetAttributes()
                .Where(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, addDtoAttrSymbol))
                .ToList();

            foreach (var attr in attrs)
            {
                var targetName = string.Empty;
                var targetType = "System.Object";
                string? conversionExpr = null;

                // read the named arguments from [AddDtoProperty(...)]
                foreach (var namedArg in attr.NamedArguments)
                {
                    switch (namedArg.Key)
                    {
                        case nameof(AddDtoPropertyAttribute.TargetName):
                            if (namedArg.Value.Value is string s && !string.IsNullOrEmpty(s))
                                targetName = s;
                            break;
                        case nameof(AddDtoPropertyAttribute.TargetType):
                            if (namedArg.Value.Value is ITypeSymbol ts)
                                targetType = ts.ToString()!;
                            break;
                        case nameof(AddDtoPropertyAttribute.ConversionExpression):
                            if (namedArg.Value.Value is string expr && !string.IsNullOrEmpty(expr))
                                conversionExpr = expr;
                            break;
                    }
                }

                // We'll store a synthetic "member" with no original name/type
                // ,but we do need an "OriginalName" if we want to reference it in conversion code. 
                // However, there's no real original field. We'll set OriginalName = targetName 
                // or something to indicate it's purely extra.
                results.Add(new MemberInfo(
                    OriginalName: targetName,
                    OriginalType: "<<<ExtraProperty>>>", 
                    Accessibility: Accessibility.Public,
                    FinalName: targetName,
                    FinalType: targetType,
                    ConversionExpression: conversionExpr,
                    IsExtraProperty: true
                ));
            }

            return results;
        }

        /// <summary>
        /// Generates a partial {ClassName}Dto with both normal [DtoProperty] members 
        /// AND the extra [AddDtoProperty] members.
        /// </summary>
        private static string GenerateDtoClass(INamedTypeSymbol classSymbol, List<MemberInfo> members)
        {
            var sb = new StringBuilder();
            var ns = (classSymbol.ContainingNamespace.IsGlobalNamespace ? "" : classSymbol.ContainingNamespace.ToString())!;

            sb.AppendLine("// <auto-generated />");
            if (!string.IsNullOrEmpty(ns))
            {
                sb.AppendLine($"namespace {ns}");
                sb.AppendLine("{");
            }
            sb.AppendLine($"    public partial class {classSymbol.Name}Dto");
            sb.AppendLine("    {");

            foreach (var m in members)
            {
                sb.AppendLine($"        public {m.FinalType} {m.FinalName} {{ get; set; }}");
            }

            sb.AppendLine("    }");
            if (!string.IsNullOrEmpty(ns))
            {
                sb.AppendLine("}");
            }
            return sb.ToString();
        }

        private static string GenerateMappingProfile(IReadOnlyList<ClassDtoResult> allClasses)
        {
            var sb = new StringBuilder();
            sb.AppendLine("// <auto-generated />");
            sb.AppendLine("using AutoMapper;");
            sb.AppendLine();
            sb.AppendLine("namespace Mapperic.Profiles");
            sb.AppendLine("{");
            sb.AppendLine("    public partial class DtoMappingProfile : Profile");
            sb.AppendLine("    {");
            sb.AppendLine("        public DtoMappingProfile()");
            sb.AppendLine("        {");

            foreach (var cls in allClasses)
            {
                var classFqn = cls.ClassSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                var dtoFqn   = classFqn + "Dto";

                sb.AppendLine($"            CreateMap<{classFqn}, {dtoFqn}>()");

                // For each real or extra property that needs ForMember
                foreach (var m in cls.Members)
                {
                    // If it's an "extra" property, or if there's rename/type difference,
                    // or there's a custom expression, or it's private, let's do .ForMember
                    var isPrivate = (m.Accessibility != Accessibility.Public);
                    var renamed   = m.FinalName != m.OriginalName;
                    var typeDiff  = m.FinalType != m.OriginalType;
                    var hasExpr   = !string.IsNullOrEmpty(m.ConversionExpression);

                    // If it's an extra property with no expression, we might do something default
                    // ,or we can skip it entirely. We'll assume the user might do 
                    if (m.IsExtraProperty)
                    {
                        // if no expression, do we skip? or do we default to e.g. zero/empty string?
                        if (string.IsNullOrEmpty(m.ConversionExpression))
                        {
                            // We'll just skip .ForMember so it stays default (0/null).
                            continue;
                        }
                        // else we do a .ForMember using the expression
                        sb.AppendLine($"                .ForMember(d => d.{m.FinalName}, opt => opt.MapFrom({m.ConversionExpression}))");
                        continue;
                    }

                    // For normal members:
                    if (isPrivate || renamed || typeDiff || hasExpr)
                    {
                        string mapExpression;
                        if (!string.IsNullOrEmpty(m.ConversionExpression))
                        {
                            mapExpression = m.ConversionExpression!; 
                        }
                        else if (typeDiff && m.FinalType == "System.String")
                        {
                            mapExpression = $"s => s.{m.OriginalName}.ToString()";
                        }
                        else
                        {
                            mapExpression = $"s => s.{m.OriginalName}";
                        }

                        sb.AppendLine($"                .ForMember(d => d.{m.FinalName}, opt => opt.MapFrom({mapExpression}))");
                    }
                }

                sb.AppendLine("                ;");
            }

            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("}");
            return sb.ToString();
        }

        #endregion
    }
}
