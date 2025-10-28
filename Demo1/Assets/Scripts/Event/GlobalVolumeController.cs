using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections;

public class GlobalVolumeController : MonoBehaviour
{
    private Volume volume;
    private Bloom bloom;
    private ChromaticAberration chroma;
    private ColorAdjustments colorAdjustments;
    private PaniniProjection panini;
    private DepthOfField depthOfField;
    private FilmGrain filmGrain;
    private Vignette vignette;

    void Awake()
    {
        volume = GetComponent<Volume>();
        volume.profile.TryGet(out chroma);
        volume.profile.TryGet(out panini);
        volume.profile.TryGet(out colorAdjustments);
        volume.profile.TryGet(out depthOfField);
        volume.profile.TryGet(out filmGrain);
        volume.profile.TryGet(out vignette);



        if (volume != null && volume.profile.TryGet(out bloom))
        {
            Debug.Log("✅ 成功抓到 Bloom 組件");
        }
        else
        {
            Debug.LogWarning("⚠️ 無法取得 Bloom 組件，請確認 Volume Profile 內有加入 Bloom");
        }
    }

    public void SetVignette()
    {
        if (vignette != null)
        {
            vignette.intensity.value = 0.4f;
            Debug.Log("🎯 SetVignette(): Vignette 強度設定為 0.4");
        }
        else
        {
            Debug.LogWarning("⚠️ 尚未抓到 Vignette 組件！");
        }
    }

    // 🌤️ 回到一般場景時重置暗角
    public void ResetVignette()
    {
        if (vignette != null)
        {
            vignette.intensity.value = 0f;
            Debug.Log("🌤️ ResetVignette(): Vignette 強度歸零");
        }
        else
        {
            Debug.LogWarning("⚠️ 尚未抓到 Vignette 組件！");
        }
    }

    public void SetBlur()
    {
        if (depthOfField != null)
        {
            depthOfField.focalLength.value = 128f;
            Debug.Log("🎯 SetBlur(): focalLength 強度設定為 128");
        }
        else
        {
            Debug.LogWarning("⚠️ 尚未抓到 depthOfField 組件！");
        }
    }

    // 🌤️ 回到一般場景時重置暗角
    public void ResetBlur()
    {
        if (depthOfField != null)
        {
            depthOfField.focalLength.value = 0f;
            Debug.Log("🌤️ ResetBlur(): focalLength 強度設定為 128");
        }
        else
        {
            Debug.LogWarning("⚠️ 尚未抓到 depthOfField 組件！");
        }
    }

    // 🌟 閃白特效：Bloom intensity 0 → 10 → 0，總時長約 1 秒
    public void FlashWhite()
    {
        if (bloom == null)
        {
            Debug.LogWarning("⚠️ Bloom 未設置，FlashWhite() 無效");
            return;
        }

        StopAllCoroutines(); // 防止重疊播放
        StartCoroutine(FlashWhiteRoutine());
    }

    public void FlashRed()
    {
        if (!volume.profile.TryGet(out chroma))
        {
            Debug.LogWarning("⚠️ Volume 中沒有 ChromaticAberration 組件，FlashRed 無效");
            return;
        }

        if (!volume.profile.TryGet(out ColorAdjustments colorAdjustments))
        {
            Debug.LogWarning("⚠️ Volume 中沒有 ColorAdjustments 組件，FlashRed 無效");
            return;
        }

        StopAllCoroutines(); // 避免與其他特效重疊
        StartCoroutine(FlashRedRoutine(chroma, colorAdjustments));
    }

    public void ClassRoom_Start()
    {
        if (!volume.profile.TryGet(out chroma))
        {
            Debug.LogWarning("⚠️ Volume 中沒有 ChromaticAberration 組件，ClassRoom_Start 無效");
            return;
        }

        if (!volume.profile.TryGet(out panini))
        {
            Debug.LogWarning("⚠️ Volume 中沒有 PaniniProjection 組件，ClassRoom_Start 無效");
            return;
        }

        StopAllCoroutines(); // 避免與其他特效重疊
        StartCoroutine(ClassRoom_StartRoutine());
    }

    public void Fade_Out()
    {
        if (!volume.profile.TryGet(out chroma))
        {
            Debug.LogWarning("⚠️ Volume 中沒有 ChromaticAberration 組件，Fade_Out 無效");
            return;
        }

        if (!volume.profile.TryGet(out panini))
        {
            Debug.LogWarning("⚠️ Volume 中沒有 PaniniProjection 組件，Fade_Out 無效");
            return;
        }
        if (!volume.profile.TryGet(out depthOfField))
        {
            Debug.LogWarning("⚠️ Volume 中沒有 depthOfField 組件，Fade_Out 無效");
            return;
        }
        if (!volume.profile.TryGet(out filmGrain))
        {
            Debug.LogWarning("⚠️ Volume 中沒有 filmGrain 組件，Fade_Out 無效");
            return;
        }

        StopAllCoroutines(); // 避免與其他特效重疊
        StartCoroutine(Fade_OutRoutine());
    }

public void Fade_In()
    {
        if (!volume.profile.TryGet(out chroma))
        {
            Debug.LogWarning("⚠️ Volume 中沒有 ChromaticAberration 組件，Fade_Out 無效");
            return;
        }

        if (!volume.profile.TryGet(out panini))
        {
            Debug.LogWarning("⚠️ Volume 中沒有 PaniniProjection 組件，Fade_Out 無效");
            return;
        }
        if (!volume.profile.TryGet(out depthOfField))
        {
            Debug.LogWarning("⚠️ Volume 中沒有 depthOfField 組件，Fade_Out 無效");
            return;
        }
        if (!volume.profile.TryGet(out filmGrain))
        {
            Debug.LogWarning("⚠️ Volume 中沒有 filmGrain 組件，Fade_Out 無效");
            return;
        }

        StopAllCoroutines(); // 避免與其他特效重疊
        StartCoroutine(Fade_InRoutine());
    }
    private IEnumerator FlashWhiteRoutine()
    {
        float duration = 0.1f; // 上升時間
        float maxIntensity = 10f;
        float timer = 0f;

        // 上升階段（0 → 10）
        while (timer < duration)
        {
            timer += Time.deltaTime;
            bloom.intensity.value = Mathf.Lerp(0, maxIntensity, timer / duration);
            yield return null;
        }

        // 下降階段（10 → 0）
        timer = 0f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            bloom.intensity.value = Mathf.Lerp(maxIntensity, 0, timer / duration);
            yield return null;
        }

        bloom.intensity.value = 0f;
        Debug.Log("🌟 閃白完成");
    }

    private IEnumerator FlashRedRoutine(ChromaticAberration ca, ColorAdjustments caAdj)
    {
        float durationUp = 1f;   // 前半秒：漸強
        float durationDown = 0.5f; // 後半秒：漸弱
        float maxIntensity = 0.6f;
        float minSaturation = -100f;

        float timer = 0f;

        // 前半秒：強化階段
        while (timer < durationUp)
        {
            timer += Time.deltaTime;
            float t = timer / durationUp;
            ca.intensity.value = Mathf.Lerp(0f, maxIntensity, t);
            caAdj.saturation.value = Mathf.Lerp(minSaturation, 0f, t);
            yield return null;
        }

        // 後半秒：回復階段
        timer = 0f;
        while (timer < durationDown)
        {
            timer += Time.deltaTime;
            float t = timer / durationDown;
            ca.intensity.value = Mathf.Lerp(maxIntensity, 0f, t);
            caAdj.saturation.value = Mathf.Lerp(0f, minSaturation, t);
            yield return null;
        }

        // 結尾：確保回歸初始值
        ca.intensity.value = 0f;
        caAdj.saturation.value = minSaturation;

        Debug.Log("🔴 FlashRed 完成");
    }

    private IEnumerator ClassRoom_StartRoutine()
    {
        // --- 取得初始值 ---
        float chromaStart = chroma.intensity.value;
        float distanceStart = panini.distance.value;
        float cropStart = panini.cropToFit.value;

        // 🔹 第一階段：進入 (0 → 0.5 秒)
        float duration = 0.5f;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float lerp = t / duration;
            chroma.intensity.value = Mathf.Lerp(chromaStart, 1f, lerp);
            panini.distance.value = Mathf.Lerp(distanceStart, 0.7f, lerp);
            panini.cropToFit.value = Mathf.Lerp(cropStart, 0.5f, lerp);
            yield return null;
        }

        // 🔹 循環三次（第二、第三階段交替）
        for (int i = 0; i < 3; i++)
        {
            // 第二階段 (A)：0.5 秒
            t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float lerp = t / duration;
                chroma.intensity.value = Mathf.Lerp(1f, 0.8f, lerp);
                panini.distance.value = Mathf.Lerp(0.7f, 0.9f, lerp);
                panini.cropToFit.value = Mathf.Lerp(0.5f, 0f, lerp);
                yield return null;
            }

            // 第三階段 (B)：0.5 秒
            t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float lerp = t / duration;
                chroma.intensity.value = Mathf.Lerp(0.8f, 1f, lerp);
                panini.distance.value = Mathf.Lerp(0.9f, 0.7f, lerp);
                panini.cropToFit.value = Mathf.Lerp(0f, 0.5f, lerp);
                yield return null;
            }
        }

        // 🔹 最後階段：回到初始值 (3.5s → 4s)
        t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float lerp = t / duration;
            chroma.intensity.value = Mathf.Lerp(1f, 0f, lerp);
            panini.distance.value = Mathf.Lerp(0.7f, 0f, lerp);
            panini.cropToFit.value = Mathf.Lerp(0.5f, 1f, lerp);
            yield return null;
        }

        // --- 收尾：保險設定回原狀 ---
        chroma.intensity.value = chromaStart;
        panini.distance.value = 0;
        panini.cropToFit.value = cropStart;
    }

    private IEnumerator Fade_OutRoutine()
    {
        Debug.Log("GVC play Fade_Out");
        // --- 取得初始值 ---
        float chromaStart = chroma.intensity.value;
        float dofStart = depthOfField.focalLength.value;
        float grainIntensityStart = filmGrain.intensity.value;
        float grainResponseStart = filmGrain.response.value;
        float paniniDistanceStart = panini.distance.value;

        float chromaTarget = 1f;
        float dofTarget = 126f;
        float grainIntensityTarget = 1f;
        float grainResponseTarget = 0f;
        float paniniTarget = 0.5f;

        float chromaDuration = 1f;
        float othersDuration = 2f;

        float t = 0f;

        // 🔹 第一階段：Chroma, DoF, FilmGrain, Panini 一起開始
        while (t < othersDuration)
        {
            t += Time.deltaTime;
            float lerpChroma = Mathf.Clamp01(t / chromaDuration); // 一秒完成
            float lerpOthers = Mathf.Clamp01(t / othersDuration); // 兩秒完成

            // Chromatic Aberration
            chroma.intensity.value = Mathf.Lerp(chromaStart, chromaTarget, lerpChroma);

            // Depth of Field
            depthOfField.focalLength.value = Mathf.Lerp(dofStart, dofTarget, lerpOthers);

            // Film Grain
            filmGrain.intensity.value = Mathf.Lerp(grainIntensityStart, grainIntensityTarget, lerpOthers);
            filmGrain.response.value = Mathf.Lerp(grainResponseStart, grainResponseTarget, lerpOthers);

            // Panini Projection
            panini.distance.value = Mathf.Lerp(paniniDistanceStart, paniniTarget, lerpOthers);

            yield return null;
        }

        // --- 🔁 持續震盪：0.4 ↔ 0.5 ---
        float oscillationTime = 0f;
        float oscillationSpeed = 3f; // 調整震動頻率
        float minDistance = 0.4f;
        float maxDistance = 0.5f;

        while (true) // 持續到手動停止（或切換場景時自動中斷）
        {
            oscillationTime += Time.deltaTime * oscillationSpeed;
            float pingpong = Mathf.PingPong(oscillationTime, 1f); // 0~1之間循環
            panini.distance.value = Mathf.Lerp(minDistance, maxDistance, pingpong);
            yield return null;
        }
    }

    IEnumerator Fade_InRoutine()
    {
        // --- 取得初始值 ---
        float chromaStart = chroma.intensity.value;
        float dofStart = depthOfField.focalLength.value;
        float grainIntensityStart = filmGrain.intensity.value;
        float grainResponseStart = filmGrain.response.value;
        float paniniDistanceStart = panini.distance.value;

        // --- 目標值 ---
        float chromaTarget = 0f;
        float dofTarget = 0f;
        float grainIntensityTarget = 0f;
        float grainResponseTarget = 1f;
        float paniniTarget = 0f;

        // --- 時間設定 ---
        float chromaDuration = 3f; // Chromatic Aberration 花3秒
        float othersDuration = 2f; // 其他花2秒
        float t = 0f;

        while (t < chromaDuration)
        {
            t += Time.deltaTime;
            float lerpChroma = Mathf.Clamp01(t / chromaDuration);
            float lerpOthers = Mathf.Clamp01(t / othersDuration);

            // Chromatic Aberration 從 1 → 0
            chroma.intensity.value = Mathf.Lerp(chromaStart, chromaTarget, lerpChroma);

            // Depth of Field 從 126 → 0
            depthOfField.focalLength.value = Mathf.Lerp(dofStart, dofTarget, lerpOthers);

            // Film Grain 從 (1,0) → (0,1)
            filmGrain.intensity.value = Mathf.Lerp(grainIntensityStart, grainIntensityTarget, lerpOthers);
            filmGrain.response.value = Mathf.Lerp(grainResponseStart, grainResponseTarget, lerpOthers);

            // Panini Projection 從 0 → 0.5
            panini.distance.value = Mathf.Lerp(paniniDistanceStart, paniniTarget, lerpOthers);

            yield return null;
        }

        // --- 確保最後值準確 ---
        chroma.intensity.value = chromaTarget;
        depthOfField.focalLength.value = dofTarget;
        filmGrain.intensity.value = grainIntensityTarget;
        filmGrain.response.value = grainResponseTarget;
        panini.distance.value = paniniTarget;
    }

}
