using KMA.Gameplay.UI;
using NUnit.Framework;

/// Scenes these tests load must not open the first-run guide: it freezes Time.timeScale, so
/// WaitForSeconds would never return. Guide tests call MinigameGuideHost directly instead.
[SetUpFixture]
public sealed class GuideAutoOpenOffForPlayModeTests
{
    [OneTimeSetUp]
    public void DisableGuideAutoOpen() => MinigameGuideHost.AutoOpenEnabled = false;

    [OneTimeTearDown]
    public void RestoreGuideAutoOpen() => MinigameGuideHost.AutoOpenEnabled = true;
}
