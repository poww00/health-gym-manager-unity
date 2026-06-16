using UnityEditor;
using UnityEngine;

public static class ExerciseBikeAssetReimporter
{
    private const string PedalPath = "Assets/_Project/Resources/GeneratedRuntimeUI/objects/exercise_bike_pedal.png";
    private const string SessionKey = "ExerciseBikePedalReimported_20260617_0425_scaled080";

    [InitializeOnLoadMethod]
    private static void AutoReimportAfterScriptReload()
    {
        if (SessionState.GetBool(SessionKey, false))
        {
            return;
        }

        SessionState.SetBool(SessionKey, true);
        EditorApplication.delayCall += ReimportExerciseBikePedal;
    }

    [MenuItem("Tools/GymGame/Art/Reimport Exercise Bike Pedal")]
    public static void ReimportExerciseBikePedal()
    {
        AssetDatabase.ImportAsset(PedalPath, ImportAssetOptions.ForceUpdate);
        AssetDatabase.Refresh();
        Debug.Log($"Reimported {PedalPath}");
    }
}
