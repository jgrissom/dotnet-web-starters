using System.Runtime.CompilerServices;

namespace Cryptids.Checks;

// The app under test boots for real, so its own logging lands in the middle of
// the check results — the startup banner, and an exception dump whenever a check
// deliberately hits a page that isn't built yet. Nothing here reads the log;
// every check asserts on an HTTP response. So turn it off for the test run.
//
// "Microsoft.AspNetCore" is named explicitly because appsettings.json sets a
// level for that category, and a more specific category rule beats Default no
// matter what Default says.
//
// This runs in the CHECKS assembly, which the student's app never loads, so
// their own `dotnet watch` output is untouched.
internal static class Quiet
{
    [ModuleInitializer]
    internal static void Init()
    {
        Environment.SetEnvironmentVariable("Logging__LogLevel__Default", "None");
        Environment.SetEnvironmentVariable("Logging__LogLevel__Microsoft.AspNetCore", "None");
        Environment.SetEnvironmentVariable("Logging__LogLevel__Microsoft.Hosting.Lifetime", "None");
    }
}
