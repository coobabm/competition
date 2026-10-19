using System;

// Retired 2026-10-19 to preserve the approved release identity. The old body permanently rewrote PlayerSettings
// company/product to the October 8 test identity (RippleLocalPreview / LingGuang-Playability-20261008)
// and saved the assets, which moves every player's save folder and PlayerPrefs registry key.
// The only supported release identity is DefaultCompany / competition; build with IntegratedReleaseBuild.Run.
// The class and both method names stay so an old -executeMethod call fails loudly instead of "method not found".
public static class WindowsReleaseBuild
{
    const string Retired =
        "WindowsReleaseBuild is retired: it would change the save identity (company/product). " +
        "Use IntegratedReleaseBuild.Run, which keeps DefaultCompany/competition. No PlayerSettings or build files were changed.";

    public static void VerifyAndBuild() { throw new InvalidOperationException(Retired); }

    public static void Build() { throw new InvalidOperationException(Retired); }
}
