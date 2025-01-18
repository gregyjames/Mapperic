using AutoMapper;

namespace MappericTests;

public class TestMapper
{
    private IMapper mapper;
    private DummyClass testObj;
    private DummyClassDto mappedObj;

    [SetUp]
    public void Setup()
    {
        var config = new MapperConfiguration(cfg => {
            cfg.AddProfile<Mapperic.Profiles.DtoMappingProfile>(); // DTO will be generated in the DTOGen.Profiles Namespace
        });
        mapper = config.CreateMapper();
        testObj = new DummyClass()
        {
            Id = 1,
            Name = "Adam",
            Test = 2
        };
        mappedObj = mapper.Map<DummyClassDto>(testObj);
    }

    [Test]
    public void TestId()
    {
        Assert.That(mappedObj.IdVal, Is.EqualTo("1"));
    }
    
    [Test]
    public void TestIdEqual()
    {
        Assert.That(testObj.Id.ToString(), Is.EqualTo(mappedObj.IdVal));
    }
    
    [Test]
    public void TestName()
    {
        Assert.That(mappedObj.NameVal, Is.EqualTo("Adam"));
    }
    
    [Test]
    public void TestNameEqual()
    {
        Assert.That(testObj.Name, Is.EqualTo(mappedObj.NameVal));
    }
    
    [Test]
    public void TestConversion()
    {
        Assert.That(mappedObj.TestVal, Is.EqualTo(4));
    }
    
    [Test]
    public void TestMapperConfiguration()
    {
        mapper.ConfigurationProvider.AssertConfigurationIsValid();
    }
    
}