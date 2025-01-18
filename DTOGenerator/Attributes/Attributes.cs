namespace DTOGenerator.Attributes;

/// <summary>
/// Mark a field or property with this attribute to include it in the generated DTO.
/// Optionally specify a different DTO property name and type,
/// plus an AutoMapper conversion expression.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class DtoPropertyAttribute: Attribute
{
    /// <summary>
    /// If specified, the generated DTO property will have this name instead of the original.
    /// </summary>
    public string? TargetName { get; set; }
    
    /// <summary>
    /// If specified, the generated DTO property will have this .NET type instead of the original.
    /// Example usage: [DtoProperty(TargetType = typeof(string))].
    /// </summary>
    public Type? TargetType { get; set; }
    
    /// <summary>
    /// If specified, the generated AutoMapper mapping will use this code to map from
    /// the source property to the DTO property. For example:
    /// <c>ConversionExpression = src => src.BirthDate.ToString(\"yyyy-MM-dd\")</c>.
    /// </summary>
    public string? ConversionExpression { get; set; }
}
    
/// <summary>
/// Mark a class with this attribute to generate a corresponding DTO.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class GenerateDtoAttribute: Attribute
{
}

/// <summary>
/// Place on a class (along with [GenerateDto]) to add an extra property to the generated DTO.
/// For instance, you can define TargetName, TargetType, ConversionExpression to inject a 
/// brand-new DTO property that doesn't directly map to an existing member.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class AddDtoPropertyAttribute : Attribute
{
    /// <summary>
    /// The name of the new property in the DTO.
    /// </summary>
    public string TargetName { get; set; } = "";

    /// <summary>
    /// The .NET type of the new DTO property. 
    /// E.g. typeof(string), typeof(int), etc.
    /// </summary>
    public Type? TargetType { get; set; }

    /// <summary>
    /// Optional AutoMapper expression used in .ForMember(...). 
    /// For example: "src => 123" or "src => src.ComputeValue()".
    /// </summary>
    public string? ConversionExpression { get; set; }
}