using UnityEngine;

public sealed class GameRuntimeUIController : RuntimeGameUIController
{
#if UNITY_EDITOR
    [ContextMenu("Materialize Game UI")]
    private void ContextMaterializeGameUi()
    {
        MaterializeForEditMode();
        MarkSceneDirty();
    }

    [ContextMenu("Preview Operate Panel")]
    private void ContextPreviewOperatePanel()
    {
        PreviewOperatePanelForEditMode();
        MarkSceneDirty();
    }

    [ContextMenu("Preview Install Panel")]
    private void ContextPreviewInstallPanel()
    {
        PreviewInstallPanelForEditMode();
        MarkSceneDirty();
    }

    [ContextMenu("Preview Menu Popup")]
    private void ContextPreviewMenuPopup()
    {
        PreviewMenuPopupForEditMode();
        MarkSceneDirty();
    }

    private void MarkSceneDirty()
    {
        UnityEditor.EditorUtility.SetDirty(this);
        UnityEditor.EditorUtility.SetDirty(gameObject);
        if (gameObject.scene.IsValid())
        {
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
        }
    }
#endif
}
