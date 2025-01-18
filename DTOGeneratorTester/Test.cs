using DTOGenerator.Attributes;

namespace DTOGeneratorTester;

[GenerateDto]
[AddDtoProperty(TargetName = "AgeInMonths", TargetType = typeof(int), ConversionExpression = "src => ((DateTime.Today.Year * 12 + DateTime.Today.Month) - (src.BirthDay.Year * 12 + src.BirthDay.Month))")]
[AddDtoProperty(TargetName = "AgeInYears", TargetType = typeof(int), ConversionExpression = "src => ((DateTime.Today.Year) - (src.BirthDay.Year))")]
public class Test
{
    [DtoProperty(TargetName = "HiddenName", TargetType = typeof(string), ConversionExpression = "src => string.Join(' ', src.Name.Split(' ', StringSplitOptions.None).Select(w => w.Length > 0 ? w[0] + new string('*', w.Length - 1) : \"\"))")]
    public string Name { get; set; }
    [DtoProperty]
    public DateOnly BirthDay { get; set; }
    
}