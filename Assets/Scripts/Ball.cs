using System;
using Unity.VisualScripting;
using UnityEditor.Build.Content;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CircleCollider2D))]
public class Ball : MonoBehaviour
{
    [Header("Param de la balle")]
    [SerializeField] private float baseSpeed = 5f;
    [SerializeField] private float baseSize = 1f;
    [SerializeField] private int basePoints = 100;

    [Header("Param de rebond")]

    [SerializeField] private float baseJumpHeight = 3f;
    [SerializeField] private float sizeJumpMultiplier = 0.5f;
    [SerializeField] private float gravityScale = 1f;
    [SerializeField] private float minHorizontalSpeed = 1f;
    [SerializeField] private float wallCheckDistance = 0.1f;

    // Composants

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;

    // Etat de la balle

    private int size;
    private float currentSpeed;
    private float jumpForce;
    private float stuckCheckTimer = 0f;
    private const float STUCK_CHECK_INTERVAL = 0.2f;
    private const string BALL_LAYER_NAME = "Balls";
    private const int BALL_LAYER = 8;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        // config le layer
        SetupCollisionLayer();
    }

    private void SetupCollisionLayer()
    {
        int ballLayer = LayerMask.NameToLayer(BALL_LAYER_NAME);
        if (ballLayer == -1)// Si non existant
        {
            ballLayer = BALL_LAYER;
            Debug.LogWarning($"Layer '{BALL_LAYER_NAME}' non trouvé. Utilisation du layer {ballLayer}");
        }

        gameObject.layer = ballLayer;
    }

    private void Initialize(int ballSize, Color ballColor)
    {
        size = ballSize;

        // définir taille physique et visuel de la balle
        float scale = baseSize * (size * 0.5f);
        transform.localScale = new Vector3(scale, scale, 1f);

        //config collider
        GetComponent<CircleCollider2D>().radius = 0.5f;

        //Couleur
        spriteRenderer.color = ballColor;

        // vitesse horizontale basé sur la taille (petit balle + rapide)
        currentSpeed = baseSpeed * (1f + (4 - size) * 0.25f);

        // force du saut (grande balle + haut saut)
        jumpForce = baseJumpHeight * (1f + size * sizeJumpMultiplier);

        //config gravité
        rb.gravityScale = gravityScale;

        //direction initiale aléatoire du mvnt horizontale
        SetInitialVelocity();
    }

    private void SetInitialVelocity()
    {
        //dir horizontale aléatoire
        float directionX = UnityEngine.Random.Range(0, 2) * 2 - 1; //1 ou -1

        // appliquer la vitesse horizontale de base
        rb.linearVelocity = new Vector2(directionX * currentSpeed, jumpForce);
    }

    private void SetCustomVelocity(float directionX, float initialVerticalVelocity)
    {
        // s'assure que la direction est normalisé à -1 ou 1
        directionX = Mathf.Sign(directionX);
        if (directionX == 0) directionX = 1;// par défaut aller à droit si X == 0

        //Appliquer la vitesse horizontale
        rb.linearVelocity = new Vector2(directionX * currentSpeed, initialVerticalVelocity);
    }

    private void Split()
    {
        if (size > 1)
        {
            SpawnSplitBalls();

            //ajout score
            int points = basePoints * (5 - size);
            // GameManager.Instance.AddScore(points);
        }
        else
        {
            // GameManager.Instance.AddScore(basePoints * 4);
        }

        Destroy(gameObject);
    }

    private void SpawnSplitBalls()
    {
        // récup la vitesse verticale
        float currentVerticalVelocity = rb.linearVelocity.y;

        // créer deux balles + petites
        for (int i = 0; i < 2; i++)
        {
            //position un peu decalé
            Vector3 spawnPosition = transform.position;
            spawnPosition.x += (i == 0) ? -0.2f : 0.2f;

            GameObject newBall = new GameObject("Ball");//TODO SUPPRIMER
            // utiliser le game manager pour créer une nouvelle balle
            // GameObject newBall = GameManager.Instance.SpawnBall(size - 1, spawnPosition);

            // récupérer le composant ballde la nouvelle balle
            Ball ballComponent = newBall.GetComponent<Ball>();
            if (ballComponent != null)
            {
                // dir horizontale
                float directionX = (i == 0) ? -1f : 1f;

                // vitesse verticale
                float verticalVelocity = currentVerticalVelocity + UnityEngine.Random.Range(-1f, 1f);
                if (verticalVelocity < 0) verticalVelocity = 1f;

                //définir la viesse perso
                ballComponent.SetCustomVelocity(directionX, verticalVelocity);
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Wall") || collision.gameObject.CompareTag("Ceilling"))
        {
            HandleBounce(collision);
        }

        //effets sonores de rebond
        // AudioManager.Instance.PlayBouceSound();
    }

    private void HandleBounce(Collision2D collision)
    {
        // determiner si c'est un sol
        bool isGroundContact = false;
        foreach (ContactPoint2D contact in collision.contacts)
        {
            if (contact.normal.y > 0.5f)
            {
                isGroundContact = true;
                break;
            }
        }

        //si contact sol, appliquer force de saut
        if (isGroundContact)
        {
            // on garde vitesse horizontale mais on applique la force du saut
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        }
        // Sinon contact mur
        else
        {
            // determiner la direction du rebond
            float dirX = rb.linearVelocity.x > 0 ? -1 : 1;
            //appliquer la nouvelle direction
            rb.linearVelocity = new Vector2(dirX * MathF.Max(currentSpeed, minHorizontalSpeed), rb.linearVelocity.y);

        }
    }

    private void FixedUpdate()
    {
        //Verifier si la balle est bloqué
        CheckIfStuck();
        // maintenir une vitesse horizontale minimal
        EnsureMinimumHorizontalSpeed();
        //limite la position de la balle à l'écran
        LimitBallPosition();
    }

    private void CheckIfStuck()
    {
        stuckCheckTimer += Time.fixedDeltaTime;

        if (stuckCheckTimer >= STUCK_CHECK_INTERVAL)
        {
            stuckCheckTimer = 0;

            //vit horizontale trop faible
            if (MathF.Abs(rb.linearVelocity.x) < minHorizontalSpeed)
            {
                // obtenir dimensions de l'écran en unités du monde
                float screenWidth = Camera.main.orthographicSize * Camera.main.aspect;
                float ballRadius = transform.localScale.x * 0.5f;

                // verif si balle est proche d'un mur
                bool nearLeftWall = transform.position.x < -screenWidth + ballRadius + wallCheckDistance;
                bool nearRightWall = transform.position.x > screenWidth - ballRadius - wallCheckDistance;

                if (nearLeftWall || nearRightWall)
                {
                    //donner une impulsion horizontale dans la dir opposé
                    float dirX = nearLeftWall ? 1f : -1f;
                    rb.linearVelocity = new Vector2(dirX * currentSpeed, rb.linearVelocity.y);
                }
            }
        }
    }
    private void EnsureMinimumHorizontalSpeed()
    {
        // s'assurer de la vitese horizontale est toujours au moins égale à minHorizontaleSpeed
        float currentXVelocity = rb.linearVelocity.x;

        if (Mathf.Abs(currentXVelocity) < minHorizontalSpeed)
        {
            //garder le signe mais augmenter la magnitude
            float dirX = Mathf.Sign(currentXVelocity);
            //si la vitesse est nulle choisir une direction aléatoire
            if (dirX == 0f) dirX = UnityEngine.Random.Range(0, 2) * 2 - 1;

            rb.linearVelocity = new Vector2(dirX * currentSpeed, rb.linearVelocity.y);
        }
    }
    private void LimitBallPosition()
    {
        // obtenir dimension de l'écran en unités du monde
        float screenHeight = Camera.main.orthographicSize;
        float screenWidth = screenHeight * Camera.main.aspect;

        // obtenir le rayon de la balle
        float ballRadius = transform.localScale.x * 0.5f;

        Vector2 position = transform.position;
        Vector2 velocity = rb.linearVelocity;

        // appliquer les limites horizontales et verticales
        HandleHorizontaleBoundaries(ref position, ref velocity, screenWidth, ballRadius);
        HandleVerticalBoundaries(ref position, ref velocity, screenHeight, ballRadius);

        // appliquer les changement
        transform.position = position;
        rb.linearVelocity = velocity;
    }

    private void HandleHorizontaleBoundaries(ref Vector2 position, ref Vector2 velocity, float screenWidth, float ballRadius)
    {
        const float OFFSET = 0.01f;//petit decalage pour eviter de reste coincé

        //limites horizontale
        if (position.x < -screenWidth + ballRadius)
        {
            position.x = -screenWidth + ballRadius + OFFSET;
            velocity.x = Mathf.Abs(velocity.x);

            // s'assurer que la vitesse est suffisante après rebond
            if (velocity.x < minHorizontalSpeed) velocity.x = currentSpeed;
        }
        else if (position.x > screenWidth - ballRadius)
        {
            position.x = screenWidth - ballRadius - OFFSET;
            velocity.x = -Mathf.Abs(velocity.x);

            if (Mathf.Abs(velocity.x) < minHorizontalSpeed) velocity.x = -currentSpeed;
        }
    }

    private void HandleVerticalBoundaries(ref Vector2 position, ref Vector2 velocity, float screenHeight, float ballRadius)
    {
        const float OFFSET = 0.01f;

        if (position.y < -screenHeight + ballRadius)
        {
            position.y = -screenHeight + ballRadius + OFFSET;
            velocity.y = jumpForce;
        }
        else if (position.y > screenHeight - ballRadius)
        {
            position.y = screenHeight - ballRadius - OFFSET;
            velocity.y = -Mathf.Abs(velocity.y);
        }
    }
}
