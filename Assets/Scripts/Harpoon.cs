using UnityEngine;

public class Harpoon : MonoBehaviour
{
    [SerializeField] private float speed = 10f;
    [SerializeField] private float maxDistance = 10f;
    [SerializeField] private float width = 0.33f;

    private Vector2 startPosition;

    private const float INITIAL_HEIGHT = 0.1f;

    void Start()
    {
        startPosition = transform.position;
        transform.localScale = new Vector3(width, INITIAL_HEIGHT, 1f);
    }

    void Update()
    {
        ExtendHarpoon();
    }

    private void ExtendHarpoon()
    {
        // extend harpoon
        float newY = transform.localScale.y + speed * Time.deltaTime;
        transform.localScale = new Vector3(width, newY, 1f);

        // adjust position for extend in Y
        transform.position = startPosition + Vector2.up * (newY / 2f);

        // verify harpoon is max height
        if (transform.localScale.y >= maxDistance || transform.position.y >= Camera.main.orthographicSize)
        {
            Destroy(gameObject);
        }
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Ball"))
        {
            Ball ball = collision.GetComponent<Ball>();
            if (ball != null)
            {
                ball.Split();
            }

            Destroy(gameObject);
        }
        else if (collision.CompareTag("Ceilling"))
        {
            Destroy(gameObject);
        }
    }


}
