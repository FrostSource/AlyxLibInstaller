namespace FileDeployment.Tests;

[TestClass()]
public class ValidationRuleFactoryTests
{
    [TestMethod()]
    public void CreateRuleTest()
    {
        Assert.Fail();
    }

    [TestMethod()]
    public void ValidationRulesAreFoundTest()
    {
        Console.WriteLine(ValidationRuleFactory._ruleTypeMap.Count);
        foreach (var rule in ValidationRuleFactory._ruleTypeMap)
        {
            Console.WriteLine($"{rule.Key} => {rule.Value}");
        }
    }
}