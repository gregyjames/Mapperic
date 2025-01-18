# Mapperic  
Mapping Magic! Automatically generate DTO Classes and AutoMapper Configurations using Roslyn Incremental Source Generators.

## How it works
This library utilizes Roslyn Incremental Source Generators to generate Data Transfer Objects (DTOs) for classes marked with the attribute `[GenerateDto]` and properties/fields marked with the attribute `[DtoProperty]`. Then the relationship between the DTO and the original object is automatically saved into profile that can be fed into AutoMapper to create an object mapper.

## Who is this for?
This tool is tailor-made for developers who can’t stand the repetitive task of creating DTOs and writing AutoMapper mappings—but still find themselves stuck doing it on every project.

## Example
Test.cs
```csharp
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
```

Program.cs
```csharp
using AutoMapper;  
  
namespace DTOGeneratorTester;  
  
internal static class Program  
{  
    static void Main()  
    {  
        var config = new MapperConfiguration(cfg => {  
	        cfg.AddProfile<DTOGenerator.Profiles.DtoMappingProfile>(); // DTO will be generated in the DTOGen.Profiles Namespace  
		});  
        var mapper = config.CreateMapper();  
  
        var test = new Test{  
            Name = "Michael Jones",  
            BirthDay = new DateOnly(2020, 12, 31),  
		};  
  
        var testMap = mapper.Map<TestDto>(test);  
        Console.WriteLine($"Original:\t[FirstName: {test.Name}, Birthday: {test.BirthDay.ToString("d")}]");  
        Console.WriteLine($"Mapped:\t\t[FullName: {testMap.HiddenName}, AgeInMonths: {testMap.AgeInMonths}, AgeInYears: {testMap.AgeInYears}]");  
    }  
}
```

.csproj
```
<PackageReference Include="DTOGenerator" Version="x.y.z" PrivateAssets="all" />
```

## Known Limitations
- ConversionExpression does not work on Private Fields/Properties because AutoMapper has no way to access them.
- ConversionExpression is a little annoying to do for complex statements since there is no IntelliSense. I chose to use strings for these because it is a workaround for C# attributes only being able to store compile-time constants.

## Contributions
Feel free to fork this project and submit pull requests to add enhancements or bug fixes.

## License
MIT License

Copyright (c) 2025 Greg James

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.