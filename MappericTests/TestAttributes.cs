using System.Reflection;
using Mapperic.Attributes;

namespace MappericTests;

public class TestAttributes
{
    private Type _dummyType;

    [SetUp]
    public void Setup()
    {
        _dummyType = typeof(DummyClass);
    }

    [Test]
    public void TestId()
    {
        var idProperty = _dummyType.GetProperty("Id");
        bool nameHasAttribute = idProperty.IsDefined(typeof(DtoPropertyAttribute), inherit: false);
        Assert.That(nameHasAttribute, Is.True);
    }
    
    [Test]
    public void TestName()
    {
        var nameProperty = _dummyType.GetProperty("Name");
        bool nameHasAttribute = nameProperty.IsDefined(typeof(DtoPropertyAttribute), inherit: false);
        Assert.That(nameHasAttribute, Is.True);
    }
}