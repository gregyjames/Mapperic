using Microsoft.CodeAnalysis;

namespace DTOGenerator.Models;

internal sealed record MemberInfo(
    string OriginalName,
    string OriginalType,
    Accessibility Accessibility,
    string FinalName,
    string FinalType,
    string? ConversionExpression,
    bool IsExtraProperty
);