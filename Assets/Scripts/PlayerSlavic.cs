using UnityEngine;
using Mirror;

[RequireComponent(typeof(NetworkTransform))]
public class PlayerSlavic : NetworkBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float jumpForce = 8f;

    [Header("Combat")]
    public int maxHealth = 100;
    public int damage = 25;
    public float attackRange = 2f;
    public float attackCooldown = 0.5f;
    public Transform attackPoint;
    public KeyCode attackKey = KeyCode.Space;

    [Header("Visual")]
    public Animator animator;
    public Rigidbody rb;

    [SyncVar(hook = nameof(OnHealthChanged))]
    public int currentHealth;

    public bool isDead { get; private set; }

    private float lastAttackTime;
    private float lastSentDirection;

    public override void OnStartServer()
    {
        currentHealth = maxHealth;
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
    }

    [Command]
    void CmdMove(float direction)
    {
        if (isDead) return;
        Vector3 vel = rb.velocity;
        vel.x = direction * moveSpeed;
        rb.velocity = vel;
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
        if (isDead) return;
        if (Mathf.Abs(rb.velocity.y) < 0.01f)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }
    }

    [Command]
    void CmdAttack()
    {
        if (isDead) return;
        RpcPlayAttackAnimation();

        Collider[] hits = Physics.OverlapSphere(attackPoint ? attackPoint.position : transform.position, attackRange);
        foreach (Collider hit in hits)
        {
            if (hit.TryGetComponent<PlayerSlavic>(out PlayerSlavic target) && target != this)
            {
                target.TakeDamage(damage);
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
    public void TakeDamage(int amount)
    {
        if (isDead) return;
        currentHealth = Mathf.Max(0, currentHealth - amount);
        if (currentHealth <= 0)
            Die();
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
