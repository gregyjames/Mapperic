using Mapperic.Attributes;

namespace MappericTests;

[GenerateDto]
public class DummyClass
{
    [DtoProperty(TargetName = "IdVal", TargetType = typeof(string), ConversionExpression = "src => src.Id.ToString()")]
    public int Id { get; set; }
    
    [DtoProperty(TargetName = "NameVal")]
    public string Name { get; set; }
    
    [DtoProperty(TargetName = "TestVal", TargetType = typeof(int), ConversionExpression = "src => (src.Test * 2)")]
    public int Test { get; set; }
}