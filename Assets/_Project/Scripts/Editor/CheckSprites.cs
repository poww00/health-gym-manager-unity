using UnityEngine;
using UnityEditor;
using System.IO;

public static class CheckSprites
{
    [MenuItem("Tools/Check Sprites")]
    public static void Check()
    {
        Sprite[] sprites = Resources.LoadAll<Sprite>("GeneratedRuntimeUI/characters/customer/body/male_chubby/body_male_chubby_idle_base_32x48_4x2");
        string result = "Loaded " + sprites.Length + " sprites.\n";
        for (int i = 0; i < sprites.Length; i++)
        {
            result += "Sprite " + i + ": " + sprites[i].name + ", rect=" + sprites[i].rect + "\n";
        }
        File.WriteAllText("sprite_check_output.txt", result);
    }
}
