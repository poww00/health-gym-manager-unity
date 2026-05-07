using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

public sealed class GameplayBgmPlayer : MonoBehaviour
{
    private const string GameSceneName = "TestSandbox";
    private const string AudioAssetPath = "Assets/_Project/Audio/BGM/bgm_gameplay_main_loop_01.wav";
    private const string VolumePrefsKey = "GYM_BACKGROUND_VOLUME";
    private const float DefaultVolume = 0.70f;

    private static GameplayBgmPlayer instance;
    private static AudioSource source;
    private static float backgroundVolume = -1f;

    public static float BackgroundVolume
    {
        get
        {
            EnsureVolumeLoaded();
            return backgroundVolume;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void BootstrapAfterSceneLoad()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
        HandleSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    public static void EnsurePlaying()
    {
        if (SceneManager.GetActiveScene().name != GameSceneName)
        {
            return;
        }

        EnsureInstance();
        instance.StartPlaybackIfNeeded();
    }

    public static void SetBackgroundVolume(float volume)
    {
        EnsureVolumeLoaded();
        backgroundVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat(VolumePrefsKey, backgroundVolume);
        PlayerPrefs.Save();

        if (source != null)
        {
            source.volume = backgroundVolume;
        }
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == GameSceneName)
        {
            EnsurePlaying();
            return;
        }

        if (source != null)
        {
            source.Stop();
        }
    }

    private static void EnsureInstance()
    {
        if (instance != null)
        {
            return;
        }

        GameObject existing = GameObject.Find("RuntimeGameplayBgmPlayer");
        if (existing != null)
        {
            instance = existing.GetComponent<GameplayBgmPlayer>();
        }

        if (instance == null)
        {
            GameObject node = new GameObject("RuntimeGameplayBgmPlayer");
            DontDestroyOnLoad(node);
            instance = node.AddComponent<GameplayBgmPlayer>();
        }

        source = instance.GetComponent<AudioSource>();
        if (source == null)
        {
            source = instance.gameObject.AddComponent<AudioSource>();
        }

        source.playOnAwake = false;
        source.loop = true;
        source.spatialBlend = 0f;
        source.volume = BackgroundVolume;
    }

    private static void EnsureVolumeLoaded()
    {
        if (backgroundVolume >= 0f)
        {
            return;
        }

        backgroundVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(VolumePrefsKey, DefaultVolume));
    }

    private void StartPlaybackIfNeeded()
    {
        if (source == null)
        {
            return;
        }

        source.volume = BackgroundVolume;
        if (source.clip != null)
        {
            if (!source.isPlaying)
            {
                source.Play();
            }

            return;
        }

        AudioClip editorClip = LoadEditorAudioClip();
        if (editorClip != null)
        {
            AssignAndPlay(editorClip);
            return;
        }

        StartCoroutine(LoadAudioClipFromProjectPath());
    }

    private void AssignAndPlay(AudioClip clip)
    {
        if (clip == null || source == null)
        {
            return;
        }

        source.clip = clip;
        source.loop = true;
        source.volume = BackgroundVolume;
        source.Play();
    }

    private IEnumerator LoadAudioClipFromProjectPath()
    {
        string projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
        string fullPath = projectRoot != null
            ? Path.Combine(projectRoot, AudioAssetPath.Replace("/", Path.DirectorySeparatorChar.ToString()))
            : Path.GetFullPath(AudioAssetPath);
        if (!File.Exists(fullPath))
        {
            Debug.LogWarning($"[GameplayBgmPlayer] BGM 파일을 찾지 못했습니다: {AudioAssetPath}");
            yield break;
        }

        using UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip("file:///" + fullPath.Replace("\\", "/"), AudioType.WAV);
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogWarning($"[GameplayBgmPlayer] BGM 로드 실패: {request.error}");
            yield break;
        }

        AssignAndPlay(DownloadHandlerAudioClip.GetContent(request));
    }

    private static AudioClip LoadEditorAudioClip()
    {
#if UNITY_EDITOR
        return AssetDatabase.LoadAssetAtPath<AudioClip>(AudioAssetPath);
#else
        return null;
#endif
    }
}
