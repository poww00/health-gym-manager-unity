using UnityEngine;

public class TitleMenuManager : MonoBehaviour
{
    [Header("Scene Settings")]
    [SerializeField] private string gameSceneName = "TestSandbox";

    public string GameSceneName => gameSceneName;

    private void OnGUI()
    {
        // Retired in favor of the runtime-built title UI.
    }
}
