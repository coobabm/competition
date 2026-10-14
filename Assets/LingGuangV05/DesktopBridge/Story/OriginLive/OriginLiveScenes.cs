using System;

namespace LingGuangV05.Desktop.Story
{
    /// <summary>One row of the host's scene table: how to construct the scene. Times, fades and drift come from <see cref="LingGuangV05.XingGuang.XgOriginFilm.Spans"/> (same index).</summary>
    public readonly struct OriginSceneEntry
    {
        public readonly Func<IOriginScene> Make;
        public OriginSceneEntry(Func<IOriginScene> make) { Make = make; }
    }

    /// <summary>
    /// The scenes of the film, in order: index i here is index i in XgOriginFilm.Spans and XgOriginFilm.SceneNames
    /// (0 ColdOpen, 1 Title, 2 Stage1, 3 Stage2, 4 Stage3, 5 Stage4, 6 Stage5, 7 Stage6, 8 Finale).
    /// To add or replace a scene, write a class deriving from <see cref="OriginScene"/> in Scenes/ and change its row.
    /// </summary>
    public static class OriginLiveScenes
    {
        public static readonly OriginSceneEntry[] Table =
        {
            new OriginSceneEntry(() => new OriginColdOpen()),
            new OriginSceneEntry(() => new OriginTitle()),
            new OriginSceneEntry(() => new OriginStage1()),
            new OriginSceneEntry(() => new OriginStage2()),
            new OriginSceneEntry(() => new OriginStage3()),
            new OriginSceneEntry(() => new OriginStage4()),
            new OriginSceneEntry(() => new OriginStage5()),
            new OriginSceneEntry(() => new OriginStage6()),
            new OriginSceneEntry(() => new OriginFinale()),
        };
    }
}
