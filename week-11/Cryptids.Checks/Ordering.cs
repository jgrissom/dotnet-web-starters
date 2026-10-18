using Xunit.Abstractions;
using Xunit.Sdk;

// Without this the runner reports checks in whatever order they finished — 1, 5,
// 3, 2, 4, 6 — so the list stops reading as the sequence of tasks the lab asks
// you to work through. Check1..Check6 then sorts into task order on its own.
[assembly: TestCaseOrderer("Cryptids.Checks.ByCheckNumber", "Cryptids.Checks")]

namespace Cryptids.Checks;

public class ByCheckNumber : ITestCaseOrderer
{
    public IEnumerable<TTestCase> OrderTestCases<TTestCase>(IEnumerable<TTestCase> cases)
        where TTestCase : ITestCase
        => cases.OrderBy(c => c.TestMethod.Method.Name, StringComparer.Ordinal);
}
