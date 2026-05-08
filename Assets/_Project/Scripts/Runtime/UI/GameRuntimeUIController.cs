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

    [ContextMenu("Preview Monthly Settlement Popup")]
    private void ContextPreviewMonthlySettlementPopup()
    {
        PreviewMonthlySettlementPopupForEditMode();
        MarkSceneDirty();
    }

    [ContextMenu("Close Popup Previews")]
    private void ContextClosePopupPreviews()
    {
        CloseAllRuntimePopupPreviewsForEditMode();
        MarkSceneDirty();
    }

    [ContextMenu("Start Install Tutorial Debug")]
    private void ContextStartInstallTutorialDebug()
    {
        StartInstallTutorialForDebug();
        MarkSceneDirty();
    }

    [ContextMenu("Reset Install Tutorial Flag")]
    private void ContextResetInstallTutorialFlag()
    {
        ResetInstallTutorial();
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
