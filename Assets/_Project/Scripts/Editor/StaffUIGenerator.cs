using UnityEditor;
using UnityEngine;

public class StaffUIGenerator : EditorWindow
{
    [MenuItem("_Project/UI/Refresh Staff UI")]
    public static void Generate()
    {
        Debug.LogWarning("[StaffUIGenerator] Old generator flow is disabled. Staff UI is being rebuilt in-scene.");
    }
}
