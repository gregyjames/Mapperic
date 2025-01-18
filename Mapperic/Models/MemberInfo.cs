using Microsoft.CodeAnalysis;

namespace Mapperic.Models;

internal sealed record MemberInfo(
    string OriginalName,
    string OriginalType,
    Accessibility Accessibility,
    string FinalName,
    string FinalType,
    string? ConversionExpression,
    bool IsExtraProperty
);