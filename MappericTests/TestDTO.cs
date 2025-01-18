using System.Reflection;

namespace MappericTests;

public class TestDTO
{
    private Type _testType;

    [SetUp]
    public void Setup()
    {
        _testType = typeof(DummyClassDto);
    }

    [Test]
    public void TestId()
    {
        var prop = _testType.GetProperty("IdVal", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        Assert.That(prop, Is.Not.Null);
    }
    
    [Test]
    public void TestIdType()
    {
        var prop = _testType.GetProperty("IdVal", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        Type propertyType = prop.PropertyType;
        Assert.That(propertyType, Is.EqualTo(typeof(string)));
    }
    
    [Test]
    public void TestIdOldType()
    {
        var prop = _testType.GetProperty("IdVal", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        Type propertyType = prop.PropertyType;
        Assert.That(propertyType, Is.Not.EqualTo(typeof(int)));
    }
    
    [Test]
    public void TestName()
    {
        var prop = _testType.GetProperty("NameVal", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        Assert.That(prop, Is.Not.Null);
    }
}