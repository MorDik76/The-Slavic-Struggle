using UnityEngine;
using Mirror;
using System.Collections;

public class PlayerSlavic : NetworkBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float jumpForce = 8f;
    public float gravity = -20f;

    [Header("Combat")]
    public int maxHealth = 100;
    public int damage = 25;
    public float attackRange = 2f;
    public float attackCooldown = 0.5f;
    public float knockbackForce = 10f;
    public Transform attackPoint;
    public KeyCode attackKey = KeyCode.Mouse0;

    [Header("Stamina & Block")]
    public float maxStamina = 100f;
    public float staminaRegenRate = 8f;
    public int attackStaminaCost = 15;
    public float blockStaminaDrainRate = 5f;
    public float blockStaminaCost = 10f;
    public float blockDamageMultiplier = 0.5f;
    public KeyCode blockKey = KeyCode.Mouse1;

    [Header("Visual")]
    public Animator animator;
    public CharacterController controller;

    [SyncVar(hook = nameof(OnHealthChanged))]
    public int currentHealth;

    [SyncVar(hook = nameof(OnStaminaChanged))]
    public float currentStamina;

    [SyncVar(hook = nameof(OnBlockingChanged))]
    public bool isBlocking;

    public bool isDead { get; private set; }

    private float lastAttackTime;
    private float lastSentDirection;
    private Vector3 velocity;
    private float serverMoveDirection;
    private Vector3 knockbackVelocity;
    private Renderer cachedRenderer;
    private Color originalColor;
    private bool wasBlockingLocal;

    void Start()
    {
        cachedRenderer = GetComponent<Renderer>();
        if (cachedRenderer != null)
            originalColor = cachedRenderer.material.color;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public override void OnStartServer()
    {
        currentHealth = maxHealth;
        currentStamina = maxStamina;
    }

    void Update()
    {
        if (!isLocalPlayer || isDead) return;

        float move = Input.GetAxisRaw("Horizontal");
        if (Mathf.Abs(move - lastSentDirection) > 0.01f)
        {
            CmdMove(move);
            lastSentDirection = move;
        }

        if (Input.GetKeyDown(attackKey) && Time.time >= lastAttackTime + attackCooldown)
        {
            CmdAttack();
            lastAttackTime = Time.time;
        }

        if (Input.GetButtonDown("Jump"))
        {
            CmdJump();
        }

        bool wantsBlock = Input.GetKey(blockKey);
        if (wantsBlock != wasBlockingLocal)
        {
            wasBlockingLocal = wantsBlock;
            CmdSetBlocking(wantsBlock);
        }
    }

    void FixedUpdate()
    {
        if (!isServer || isDead || controller == null) return;

        if (isBlocking)
        {
            currentStamina = Mathf.Max(0, currentStamina - blockStaminaDrainRate * Time.fixedDeltaTime);
            if (currentStamina <= 0)
                isBlocking = false;
        }
        else
        {
            currentStamina = Mathf.Min(maxStamina, currentStamina + staminaRegenRate * Time.fixedDeltaTime);
        }

        float speedMultiplier = isBlocking ? 0.5f : 1f;
        Vector3 move = new Vector3(serverMoveDirection * moveSpeed * speedMultiplier, 0, 0);
        controller.Move(move * Time.fixedDeltaTime);

        if (knockbackVelocity.sqrMagnitude > 0.01f)
        {
            controller.Move(knockbackVelocity * Time.fixedDeltaTime);
            knockbackVelocity = Vector3.Lerp(knockbackVelocity, Vector3.zero, Time.fixedDeltaTime * 10f);
        }

        if (controller.isGrounded && velocity.y < 0)
            velocity.y = -2f;

        velocity.y += gravity * Time.fixedDeltaTime;
        controller.Move(velocity * Time.fixedDeltaTime);
    }

    [Command]
    void CmdMove(float direction)
    {
        if (isDead) return;
        serverMoveDirection = direction;
        RpcSetSpeed(Mathf.Abs(direction));
    }

    [ClientRpc]
    void RpcSetSpeed(float speed)
    {
        if (animator)
            animator.SetFloat("Speed", speed);
    }

    [Command]
    void CmdJump()
    {
        if (isDead || !controller.isGrounded) return;
        velocity.y = Mathf.Sqrt(jumpForce * -2f * gravity);
    }

    [Command]
    void CmdAttack()
    {
        if (isDead || currentStamina < attackStaminaCost) return;
        currentStamina -= attackStaminaCost;
        RpcPlayAttackAnimation();

        Collider[] hits = Physics.OverlapSphere(attackPoint ? attackPoint.position : transform.position, attackRange);
        foreach (Collider hit in hits)
        {
            if (hit.TryGetComponent<PlayerSlavic>(out PlayerSlavic target) && target != this)
            {
                target.TakeDamage(damage, transform.position);
            }
        }
    }

    [ClientRpc]
    void RpcPlayAttackAnimation()
    {
        if (animator)
            animator.SetTrigger("Attack");
    }

    [Server]
    public void TakeDamage(int amount, Vector3 attackerPos)
    {
        if (isDead) return;

        if (isBlocking)
        {
            amount = Mathf.Max(1, Mathf.RoundToInt(amount * blockDamageMultiplier));
            currentStamina = Mathf.Max(0, currentStamina - blockStaminaCost);
        }

        currentHealth = Mathf.Max(0, currentHealth - amount);

        Vector3 dir = (transform.position - attackerPos).normalized;
        knockbackVelocity = new Vector3(dir.x * knockbackForce * 0.5f, 0, 0);

        RpcFlashRed();

        if (currentHealth <= 0)
            Die();
    }

    [Command]
    void CmdSetBlocking(bool blocking)
    {
        if (isDead) return;
        isBlocking = blocking;
    }

    [ClientRpc]
    void RpcFlashRed()
    {
        if (cachedRenderer == null) return;
        StartCoroutine(FlashRoutine());
    }

    IEnumerator FlashRoutine()
    {
        Color restore = isBlocking ? new Color(1f, 0.6f, 0f) : originalColor;
        SetColor(Color.red);
        yield return new WaitForSeconds(0.1f);
        SetColor(restore);
    }

    void SetColor(Color color)
    {
        if (cachedRenderer.material.HasProperty("_BaseColor"))
            cachedRenderer.material.SetColor("_BaseColor", color);
        else
            cachedRenderer.material.color = color;
    }

    void OnStaminaChanged(float oldVal, float newVal) { }

    void OnBlockingChanged(bool oldVal, bool newVal)
    {
        SetColor(newVal ? new Color(1f, 0.6f, 0f) : originalColor);
    }

    [Server]
    void Die()
    {
        isDead = true;
        RpcDie();
        Invoke(nameof(EndGame), 2.5f);
    }

    [ClientRpc]
    void RpcDie()
    {
        isDead = true;
        if (animator)
            animator.SetTrigger("Die");
    }

    [Server]
    void EndGame()
    {
        if (NetworkManagerSlavic.Instance != null)
            NetworkManagerSlavic.Instance.ServerChangeScene(NetworkManagerSlavic.Instance.offlineScene);
    }

    void OnHealthChanged(int oldVal, int newVal)
    {
        if (animator)
            animator.SetInteger("Health", newVal);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint ? attackPoint.position : transform.position, attackRange);
    }
}
