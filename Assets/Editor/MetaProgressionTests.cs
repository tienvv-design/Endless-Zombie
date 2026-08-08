using NUnit.Framework;
using UnityEngine;

public sealed class MetaProgressionTests
{
    [TestCase(0, 20)]
    [TestCase(1, 24)]
    [TestCase(9, 103)]
    [TestCase(10, 124)]
    public void UpgradeCost_MatchesFddExamples(int purchases, int expected)
    {
        Assert.That(MetaProgression.UpgradeCost(purchases), Is.EqualTo(expected));
    }

    [Test]
    public void HealthUpgrade_UsesNineSmallStepsThenBreakthrough()
    {
        Assert.That(MetaProgression.HealthValue(0), Is.EqualTo(10f).Within(0.001f));
        Assert.That(MetaProgression.HealthValue(9), Is.EqualTo(32.5f).Within(0.001f));
        Assert.That(MetaProgression.HealthValue(10), Is.EqualTo(50f).Within(0.001f));
        Assert.That(MetaProgression.HealthValue(11), Is.EqualTo(62.5f).Within(0.001f));
    }

    [Test]
    public void IncomeUpgrade_UsesNineSmallStepsThenBreakthrough()
    {
        Assert.That(MetaProgression.IncomeValue(0), Is.EqualTo(1f).Within(0.001f));
        Assert.That(MetaProgression.IncomeValue(9), Is.EqualTo(1.45f).Within(0.001f));
        Assert.That(MetaProgression.IncomeValue(10), Is.EqualTo(2f).Within(0.001f));
        Assert.That(MetaProgression.IncomeValue(19), Is.EqualTo(2.9f).Within(0.001f));
        Assert.That(MetaProgression.IncomeValue(20), Is.EqualTo(4f).Within(0.001f));
    }
}
