using AutoMapper;

namespace MappericTester;

internal static class Program
{
    static void Main()
    {
        var config = new MapperConfiguration(cfg => {
            cfg.AddProfile<Mapperic.Profiles.DtoMappingProfile>(); // DTO will be generated in the DTOGen.Profiles Namespace
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