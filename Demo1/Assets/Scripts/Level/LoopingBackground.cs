using UnityEngine;

public class LoopingBackground : MonoBehaviour
{
    [Header("背景設定")]
    [SerializeField] private float speed = 5f; // 背景移動速度
    public SpriteRenderer reference;           // 若這張是第二張，指定前一張的 SpriteRenderer
    public Sprite openBG;                      // 「開門」版本的背景圖
    private SpriteRenderer sr;                 // 本身的 SpriteRenderer
    private float startPos;
    private float length;

    [Header("開門動畫需要")]
    private bool useOpenNextLoop = false;      // 下一輪是否要換成開門背景
    private bool hasLooped = false;            // 是否已經完成一次循環（避免誤觸）
    public Animator playerAnimator;            // 主角 Animator，用來切換動畫
    [HideInInspector] public bool isMoving = true; // 控制背景是否移動
    public GameObject doorTrigger;             // 門的 trigger（循環時跟著背景移動）

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        length = sr.bounds.size.x;

        // 若有設定 reference，將此背景緊接上一張
        if (reference != null)
        {
            float refWidth = reference.bounds.size.x;
            transform.position = new Vector3(
                reference.transform.position.x + refWidth,
                reference.transform.position.y,
                reference.transform.position.z
            );
        }

        startPos = transform.position.x;

        // ✅ 預設關閉門的 trigger（開門後才啟用）
        if (doorTrigger != null)
        {
            doorTrigger.SetActive(false);
            Debug.Log("🚪 DoorTrigger 預設關閉");
        }
    }

    void Update()
    {
        if (!isMoving) return; // 若停止移動，就不執行更新

        // 背景持續向左移動
        transform.Translate(Vector2.left * speed * Time.deltaTime);

        // 🔁 當這張圖移動超出一個長度後，重設回起點（形成循環）
        if (transform.position.x <= startPos - length)
        {
            transform.position = new Vector3(startPos, transform.position.y, transform.position.z);
            hasLooped = true; // 標記：剛完成一次循環

            // 🧩 如果上層要求「下一次循環換成開門背景」
            if (useOpenNextLoop)
            {
                sr.sprite = openBG;  // ✅ 換成本圖的「開門」版本
                useOpenNextLoop = false;
                Debug.Log("✅ 背景已切換成開門版本！");

                // ✅ 啟用門的 trigger（加上括號修正作用範圍）
                if (doorTrigger != null)
                {
                    doorTrigger.SetActive(true);
                    Debug.Log("🚪 DoorTrigger 已啟用");
                }
            }
        }
    }

    /// <summary>
    /// SceneController 會呼叫這個方法，要求「下次循環」改用開門背景
    /// </summary>
    public void SwitchToNextBGOpen()
    {
        useOpenNextLoop = true;
        hasLooped = false;
        Debug.Log("📩 已設定：下一次循環將切換成開門背景");
    }

    /// <summary>
    /// 被門的 Trigger 呼叫：主角碰到時背景停下、角色切 Idle
    /// </summary>
    public void OnDoorTriggered()
    {
        isMoving = false;
        Debug.Log("🧍‍♀️ 玩家碰到門，停止背景移動");
        if (playerAnimator != null)
            playerAnimator.SetTrigger("Idle");
    }

    /// <summary>
    /// 若這個物件本身帶有 Trigger Collider，也能自動呼叫停下
    /// </summary>
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (doorTrigger == null || !doorTrigger.activeSelf) return;

        if (other.CompareTag("Player"))
        {
            OnDoorTriggered();
        }
    }
}
