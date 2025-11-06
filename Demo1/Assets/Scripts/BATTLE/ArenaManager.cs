using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// 競技場波次控制：
/// - 每 waveInterval 秒生成一波
/// - 每 5 波生成 Boss
/// - 每過一波加分，遇到 5 的倍數額外加分
/// - 玩家死亡後結算
///
/// 用法：
/// 1) 場景放一個空物件加上本腳本
/// 2) 設定 spawnPoints / enemyPrefabs / bossPrefab / UI 引用
/// 3)（可選）準備 EnemiesParent、BossesParent 做生成整理
/// 4) 玩家物件掛上 ArenaPlayerAdapter（會自動啟用 arenaMode）
/// </summary>
public class ArenaManager : MonoBehaviour
{
    [Header("Spawn Settings")]
    [Tooltip("敵人出生點（至少 1 個）")]
    public Transform[] spawnPoints;

    [Tooltip("一般敵人預製體清單（至少 1 種）")]
    public GameObject[] enemyPrefabs;

    [Tooltip("Boss 預製體（1 種）")]
    public GameObject bossPrefab;

    [Tooltip("生成的敵人會被放到這（可為空）")]
    public Transform enemiesParent;

    [Tooltip("生成的 Boss 會被放到這（可為空）")]
    public Transform bossesParent;

    [Header("Wave Parameters")]
    [Tooltip("波與波間隔秒數")]
    public float waveInterval = 30f;

    [Tooltip("基礎敵數（第 1 波）")]
    public int baseEnemyCount = 3;

    [Tooltip("每一波增加的敵人數")]
    public int enemyCountPerWave = 2;

    [Tooltip("每第 N 波出現 Boss")]
    public int bossEveryNWaves = 5;

    [Tooltip("是否改為『清怪才進下一波』（覆蓋秒數制）。\n開啟後將無視 waveInterval，直到場上敵人全清才開下一波（Boss 死亡也算清空）。")]
    public bool requireClearToProceed = false;

    [Header("Scoring")]
    [Tooltip("普通波完成（觸發生成時就加分）")]
    public int scorePerWave = 100;

    [Tooltip("Boss 波額外加分（在 Boss 波生成時加）")]
    public int bonusPerBossWave = 500;

    [Header("UI")]
    public Text waveText;
    public Text scoreText;
    public GameObject gameOverPanel;
    public Text finalScoreText;
    public Text bestScoreText; // 可選

    [Header("Optional SFX")]
    public AudioSource sfx;
    public AudioClip waveStartClip;
    public AudioClip bossIncomingClip;
    public AudioClip gameOverClip;

    // Runtime
    private int wave = 0;
    private int score = 0;
    private bool running = false;
    private Coroutine loopCo;

    // 簡單的 Best 記錄（PlayerPrefs）
    private const string BestScoreKey = "Arena_BestScore";

    // 追蹤場上敵人（清怪制用）
    private readonly List<GameObject> aliveEnemies = new List<GameObject>();
    private readonly List<GameObject> aliveBosses  = new List<GameObject>();

    private void Start()
    {
        // 基本檢查
        if ((spawnPoints == null || spawnPoints.Length == 0) ||
            (enemyPrefabs == null || enemyPrefabs.Length == 0) ||
            bossPrefab == null)
        {
            Debug.LogWarning("[ArenaManager] 請設定 spawnPoints / enemyPrefabs / bossPrefab。");
        }

        // UI 初始
        UpdateWaveUI(0);
        UpdateScoreUI();

        // 開始主迴圈
        running = true;
        loopCo = StartCoroutine(MainLoop());
    }

    private IEnumerator MainLoop()
    {
        // 開場緩衝 2 秒
        yield return new WaitForSeconds(2f);

        while (running)
        {
            NextWave();

            if (requireClearToProceed)
            {
                // 等待場上「全部敵人 + Boss」清空
                yield return StartCoroutine(WaitUntilCleared());
                // 小緩衝
                yield return new WaitForSeconds(1f);
            }
            else
            {
                // 時間制
                yield return new WaitForSeconds(Mathf.Max(1f, waveInterval));
            }
        }
    }

    private void NextWave()
    {
        wave++;
        UpdateWaveUI(wave);

        bool isBossWave = (bossEveryNWaves > 0 && wave % bossEveryNWaves == 0);

        // 計分
        score += scorePerWave;
        if (isBossWave) score += bonusPerBossWave;
        UpdateScoreUI();

        // SFX
        if (sfx != null)
        {
            if (isBossWave && bossIncomingClip != null)
                sfx.PlayOneShot(bossIncomingClip);
            else if (waveStartClip != null)
                sfx.PlayOneShot(waveStartClip);
        }

        // 生成
        if (isBossWave)
            SpawnBoss();
        else
            SpawnEnemies(GetEnemyCountForWave(wave));
    }

    private int GetEnemyCountForWave(int currentWave)
    {
        // 簡單線性成長：基礎 + (波數-1)*增量
        return Mathf.Max(1, baseEnemyCount + (currentWave - 1) * enemyCountPerWave);
    }

    private void SpawnEnemies(int count)
    {
        if (enemyPrefabs == null || enemyPrefabs.Length == 0 || spawnPoints == null || spawnPoints.Length == 0)
            return;

        for (int i = 0; i < count; i++)
        {
            Transform point = spawnPoints[Random.Range(0, spawnPoints.Length)];
            GameObject prefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];
            GameObject inst = Instantiate(prefab, point.position, Quaternion.identity);

            if (enemiesParent != null) inst.transform.SetParent(enemiesParent);

            TrackAlive(inst, isBoss:false);

            // （可選）告知敵人「這是第幾波，用於自調難度」
            // 例如：敵人腳本可實作 void OnArenaScale(int wave) 來提升攻擊或血量。
            inst.SendMessage("OnArenaScale", wave, SendMessageOptions.DontRequireReceiver);
        }
    }

    private void SpawnBoss()
    {
        if (bossPrefab == null || spawnPoints == null || spawnPoints.Length == 0)
            return;

        Transform point = spawnPoints[Random.Range(0, spawnPoints.Length)];
        GameObject inst = Instantiate(bossPrefab, point.position, Quaternion.identity);

        if (bossesParent != null) inst.transform.SetParent(bossesParent);

        TrackAlive(inst, isBoss:true);

        inst.SendMessage("OnArenaScale", wave, SendMessageOptions.DontRequireReceiver);
    }

    private void TrackAlive(GameObject go, bool isBoss)
    {
        if (go == null) return;

        if (isBoss)
            aliveBosses.Add(go);
        else
            aliveEnemies.Add(go);

        // 綁定死亡回調：若你的敵人繼承 LivingEntity，可在其 Die() 裡 SendMessage
        // 這裡用一個簡單的 AutoUnregister 幫你監聽 Destroy
        var hook = go.AddComponent<_ArenaAutoUnregister>();
        hook.Init(this, isBoss);
    }

    private void Untrack(GameObject go, bool isBoss)
    {
        if (isBoss) aliveBosses.Remove(go);
        else aliveEnemies.Remove(go);
    }

    private IEnumerator WaitUntilCleared()
    {
        // 清理 null（避免場上 Destroy 後 List 殘留）
        PruneDeadRefs();

        while (aliveEnemies.Count > 0 || aliveBosses.Count > 0)
        {
            PruneDeadRefs();
            yield return null;
        }
    }

    private void PruneDeadRefs()
    {
        aliveEnemies.RemoveAll(e => e == null);
        aliveBosses.RemoveAll(b => b == null);
    }

    private void UpdateWaveUI(int curWave)
    {
        if (waveText != null)
            waveText.text = $"Wave {curWave}";
    }

    private void UpdateScoreUI()
    {
        if (scoreText != null)
            scoreText.text = $"Score: {score}";
    }

    // 玩家死亡由 PlayerController(arenaMode) 呼叫
    public void OnPlayerDeath()
    {
        if (!running) return;

        running = false;
        if (loopCo != null) StopCoroutine(loopCo);

        // 清掉後續生成防止殘留
        StopAllCoroutines();

        // 收尾 UI
        if (sfx != null && gameOverClip != null)
            sfx.PlayOneShot(gameOverClip);

        int best = PlayerPrefs.GetInt(BestScoreKey, 0);
        if (score > best)
        {
            best = score;
            PlayerPrefs.SetInt(BestScoreKey, best);
            PlayerPrefs.Save();
        }

        if (finalScoreText != null) finalScoreText.text = $"Final Score: {score}";
        if (bestScoreText  != null) bestScoreText.text  = $"Best: {best}";
        Time.timeScale = 1f;                 // 防止暫停卡住
        if (gameOverPanel) gameOverPanel.SetActive(true);
    }

    // UI Button
    public void Restart()
    {
        Time.timeScale = 1f;                 // 一律復原
        Scene current = SceneManager.GetActiveScene();
        SceneManager.LoadScene(current.buildIndex);  // 直接重載整場
    }

    // UI Button
    public void ExitToMenu(string menuSceneName)
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }

    // —— 內部類：自動移除已毀物件的追蹤 —— //
    private class _ArenaAutoUnregister : MonoBehaviour
    {
        private ArenaManager mgr;
        private bool isBoss;

        public void Init(ArenaManager m, bool bossFlag)
        {
            mgr = m; isBoss = bossFlag;
        }

        private void OnDestroy()
        {
            if (mgr != null)
                mgr.Untrack(gameObject, isBoss);
        }
    }
}
