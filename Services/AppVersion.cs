namespace MechanicalCalculatorWeb.Services;

/// <summary>
/// Which build of the site is running.
///
/// The number matters here more than it does on a brochure site: these engines'
/// results change between deploys. The Nov 2026 round alone moved the taper fit's
/// three axial forces, the single bolt's slip safety factor and the ball bearing's
/// static safety for a matched pair. A visitor who saved a PDF last week and re-runs
/// the same inputs today gets different numbers, and share links deliberately re-run
/// the CURRENT engine rather than carrying stored results - so the only way a report
/// can be traced to the arithmetic that produced it is for the build to be on it.
///
/// <see cref="BuildInfo.Stamp"/> is generated at compile time by the StampBuildInfo
/// target; see the comment there for why it is not a hand-maintained version number.
/// </summary>
public static class AppVersion
{
    /// <summary>Date and short commit of the deployed build, or "dev" locally.</summary>
    public static string Stamp => BuildInfo.Stamp;

    /// <summary>False for a local build, which has no commit behind it.</summary>
    public static bool IsDeployed => BuildInfo.Stamp != "dev";

    /// <summary>What the sidebar, the About page and the PDF footer show.</summary>
    public static string Display => IsDeployed ? BuildInfo.Stamp : "dev build";
}
