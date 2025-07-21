using UnityEditor.Build.Content;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Player : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float smoothTime = 0.05f;

    [Header("Shooting Settings")]
    [SerializeField] private GameObject harpoonPrefab;
    [SerializeField] private Transform firingPoint;
    [SerializeField] private float cooldownTime = 0.5f;

    // Components
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;

    // Player state
    private Vector2 moveDirection;
    private Vector2 currentVelocity;
    private bool canShoot = true;
    private bool isInvulnerable = false;
    private float invulnerabilityTimer = 0f;
    private float invulnerabilityDuration = 2f;
    private bool shoot;

    // Movement limit
    private float screenBoundaryX = 8f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (firingPoint == null)
        {
            firingPoint = transform;
        }
    }

    private void Update()
    {
        // if (!GameManager.Instance.IsRunning()) { return; }
        HandleInput();

        if (isInvulnerable) { HandleInvulnerability(); }

    }

    private void HandleInput()
    {
        // Get Horizontal input
        moveDirection.x = Input.GetAxisRaw("Horizontal");

        //Shot
        if ((Input.GetButtonDown("Fire1") || Input.GetKeyDown(KeyCode.Space)) && canShoot)
        {
            shoot = true;
        }
    }

    private void FixedUpdate()
    {
        // if (!GameManager.Instance.IsRunning()) { return; }

        // Move the player
        Vector2 targetVelocity = new Vector2(moveDirection.x * moveSpeed, 0);
        rb.linearVelocity = Vector2.SmoothDamp(rb.linearVelocity, targetVelocity, ref currentVelocity, smoothTime);

        // Limit the player's movement to the screen boundaries
        LimitPlayerMovement();

        if (shoot)
        {
            ShootHarpoon();
            shoot = false;
        }
    }

    private void LimitPlayerMovement()
    {
        Vector3 position = transform.position;
        position.x = Mathf.Clamp(position.x, -screenBoundaryX, screenBoundaryX);
        transform.position = position;
    }

    private void ShootHarpoon()
    {
        if (!canShoot) { return; }

        Instantiate(harpoonPrefab, firingPoint.position, Quaternion.identity);

        // Start cooldown
        canShoot = false;
        Invoke(nameof(ResetCooldown), cooldownTime);
    }

    // array function when only one instruction
    private void ResetCooldown() => canShoot = true;

    private void HandleInvulnerability()
    {
        invulnerabilityTimer -= Time.deltaTime;

        // Blink effect
        float blinkSpeed = 0.1f;
        spriteRenderer.enabled = Mathf.Sin(Time.time / blinkSpeed) > 0;

        if (invulnerabilityTimer <= 0)
        {
            isInvulnerable = false;
            spriteRenderer.enabled = true;
        }

    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ball") && !isInvulnerable)
        {
            // Apply damage
            TakeHit();
        }
    }

    private void TakeHit()
    {
        if (isInvulnerable) return;

        //Notify GameManager
        // GameManager.Instance.PlayerHit();

        // Start invulnerability
        InitializeInvulnerability();
    }

    private void InitializeInvulnerability()
    {

        isInvulnerable = true;
        invulnerabilityTimer = invulnerabilityDuration;
    }

    public void Reset()
    {
        // Reset position and velocity
        transform.position = new Vector3(0, transform.position.y, 0);
        rb.linearVelocity = Vector2.zero;

        // Start invulnerability
        InitializeInvulnerability();
    }
}
