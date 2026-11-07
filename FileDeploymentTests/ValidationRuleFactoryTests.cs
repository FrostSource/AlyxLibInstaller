using FileDeployment.Rules;
using System.Text.Json;

namespace FileDeployment.Tests;

[TestClass()]
public class ValidationRuleFactoryTests
{
    [TestMethod()]
    public void CreateRuleTest()
    {
        var rule = ValidationRuleFactory.CreateRule("hash", "12345");

        Assert.IsInstanceOfType<HashRule>(rule);
        Assert.AreEqual("12345", ((IValidationRuleWithValue)rule).Value);
    }

    [TestMethod()]
    public void CreateRuleWithoutValueTest()
    {
        Assert.IsInstanceOfType<FileExistsRule>(ValidationRuleFactory.CreateRule("FileExists"));
    }

    [TestMethod()]
    public void CreateUnknownRuleThrowsTest()
    {
        Assert.ThrowsException<JsonException>(() => ValidationRuleFactory.CreateRule("NotARule"));
    }

    [TestMethod()]
    public void ValidationRulesAreFoundTest()
    {
        foreach (var alias in new[] { "ContainsText", "FileDoesNotExist", "FileExists", "FileHash", "FileNameDoesNotExist", "Hash" })
            Assert.IsTrue(ValidationRuleFactory._ruleTypeMap.ContainsKey(alias), $"Rule '{alias}' was not found");
    }
}
