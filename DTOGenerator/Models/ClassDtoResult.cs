using Microsoft.CodeAnalysis;

namespace DTOGenerator.Models;

internal sealed record ClassDtoResult(INamedTypeSymbol ClassSymbol, List<MemberInfo> Members);