using Microsoft.VisualStudio.TestTools.UnitTesting;
using FileDeployment;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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