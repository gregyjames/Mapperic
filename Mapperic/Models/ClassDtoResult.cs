using Microsoft.CodeAnalysis;

namespace Mapperic.Models;

internal sealed record ClassDtoResult(INamedTypeSymbol ClassSymbol, List<MemberInfo> Members);