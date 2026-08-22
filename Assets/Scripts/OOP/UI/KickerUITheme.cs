using TMPro;
using UnityEngine;

public static class KickerUITheme
{
    private static TMP_FontAsset font;
    public static TMP_FontAsset Font => font != null
        ? font
        : font = Resources.Load<TMP_FontAsset>("KickerHUD/KickerFont");

    public static void Apply(TMP_Text text)
    {
        if (text != null && Font != null)
            text.font = Font;
    }
}
