using UnityEngine;

public class pipe : MonoBehaviour
{
    public float pipeSpeed = 5f;
    public Rigidbody2D rb;
    public float despawnX = -40;
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    void Update()
    {
        rb.linearVelocity = new Vector2(pipeSpeed*-1,0);
        if (transform.position.x < despawnX)
        {
            Destroy(gameObject);
        }
    }
    private void OnEnable()
    {
        GA.resetPipes += selfDestruct;
    }
    private void OnDisable()
    {
        GA.resetPipes -= selfDestruct;
    }
    public void selfDestruct()
    {
        Destroy(gameObject);
    }
}
