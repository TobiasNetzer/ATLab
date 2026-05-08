using System;
using ATLab.Models;

namespace ATLab.Helpers;

public static class TestStepRuntimeInitializer
{
    public static void InitializeRuntimeValues(TestStep step)
    {
        if (!string.IsNullOrWhiteSpace(step.NominalValueExpression))
        {
            if (!step.NominalValueExpression.Contains("{"))
            {
                step.NominalValue = ResolveLiteral(step.NominalValueExpression, step.Unit);
            }
        }

        if (!string.IsNullOrWhiteSpace(step.LowerLimitExpression))
        {
            if (!step.LowerLimitExpression.Contains("{"))
            {
                step.LowerLimit = ResolveLiteral(step.LowerLimitExpression, step.Unit);
            }
        }

        if (!string.IsNullOrWhiteSpace(step.UpperLimitExpression))
        {
            if (!step.UpperLimitExpression.Contains("{"))
            {
                step.UpperLimit = ResolveLiteral(step.UpperLimitExpression, step.Unit);
            }
        }

        if (!string.IsNullOrWhiteSpace(step.DelayExpression))
        {
            if (!step.DelayExpression.Contains("{"))
            {
                var seconds = ResolveLiteral(step.DelayExpression, "s");
                step.Delay = (int)Math.Round(seconds * 1000);
            }
        }
        
    }

    private static double ResolveLiteral(string literal, string? unit)
    {
        if (UnitParser.TryParse(literal, out var result, unit))
            return result;

        return 0;
    }
    
}