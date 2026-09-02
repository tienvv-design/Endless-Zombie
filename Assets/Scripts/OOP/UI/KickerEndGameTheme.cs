using System.Collections.Generic;
using UnityEngine;

public static class KickerEndGameTheme
{
    private static Dictionary<string, Sprite> winSprites;
    private static Dictionary<string, Sprite> uiSprites;

    public static Sprite Win(string name) => Get(ref winSprites, "KickerHUD/endgame_win_atlas", name);
    public static Sprite UI(string name) => Get(ref uiSprites, "KickerHUD/endgame_ui_atlas", name);
    public static Sprite LoseImage => Resources.Load<Sprite>("KickerHUD/endgame_lose");
    public static Sprite Flag => Resources.Load<Sprite>("KickerHUD/endgame_flag");
    public static Sprite Flare => Resources.Load<Sprite>("KickerHUD/endgame_flare");
    public static Sprite LosegameHeader => Resources.Load<Sprite>("KickerHUD/losegame_header");
    public static Sprite LosegameCollected => Resources.Load<Sprite>("KickerHUD/losegame_collected");
    public static Sprite LosegameBar => Resources.Load<Sprite>("KickerHUD/losegame_bar");
    public static Sprite LosegameBarFill => Resources.Load<Sprite>("KickerHUD/losegame_bar_fill");
    public static Sprite LosegameHome => Resources.Load<Sprite>("KickerHUD/losegame_home");
    public static Sprite LosegameMarker => Resources.Load<Sprite>("KickerHUD/losegame_marker");
    public static Sprite LosegameBoss => Resources.Load<Sprite>("KickerHUD/losegame_boss");
    public static Sprite LosegameZombiePanel => Resources.Load<Sprite>("KickerHUD/losegame_zombie_panel");
    public static Sprite Gold => Resources.Load<Sprite>("KickerHUD/gold");

    private static Sprite Get(ref Dictionary<string, Sprite> cache, string path, string name)
    {
        if (cache == null)
        {
            cache = new Dictionary<string, Sprite>();
            foreach (Sprite sprite in Resources.LoadAll<Sprite>(path))
                cache[sprite.name] = sprite;
        }
        return cache.TryGetValue(name, out Sprite result) ? result : null;
    }
}
