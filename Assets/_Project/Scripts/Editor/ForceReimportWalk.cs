using UnityEditor;

public static class ForceReimportWalk {
    [InitializeOnLoadMethod]
    public static void DoReimport() {
        // Only run once by checking session state
        if (!SessionState.GetBool("HeadReimported3", false)) {
            SessionState.SetBool("HeadReimported3", true);
            AssetDatabase.ImportAsset("Assets/_Project/Resources/GeneratedRuntimeUI/characters/customer/head/male_chubby/head_male_chubby_3dir_32x48_3x1.png", ImportAssetOptions.ForceUpdate);
            UnityEngine.Debug.Log("Forced reimport of head!");
        }
    }
}
