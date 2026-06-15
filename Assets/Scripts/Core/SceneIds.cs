using UnityEngine.SceneManagement;

public static class SceneIds
{
    public const string Menu = "MENU_BOOT";
    public const string Exterior = "LOCATION_01_EXTERIOR_DAY";
    public const string Night = "LOCATION_02_PROTECTED_ALLEYS_NIGHT";
    public const string Final = "LOCATION_03_GATE_FINAL";

    public static bool IsGameplay(string sceneName)
    {
        return sceneName == Exterior || sceneName == Night || sceneName == Final;
    }

    public static bool IsGameplay(Scene scene)
    {
        return IsGameplay(scene.name);
    }

    public static string GetLocalizationKey(string sceneName)
    {
        return sceneName switch
        {
            Exterior => "save.scene.exterior",
            Night => "save.scene.night",
            Final => "save.scene.final",
            _ => null
        };
    }
}
