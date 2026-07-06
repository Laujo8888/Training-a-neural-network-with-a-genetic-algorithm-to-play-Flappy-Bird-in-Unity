using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.SocialPlatforms.Impl;

public class pc : MonoBehaviour
{
    public Rigidbody2D rb;
    public float maxFallspeed=-10f;
    public float flapStrength = 5f;
    public GameObject deathExplo;//polish
    [SerializeField] private int score=0;
    public TMP_Text text;
    private float lastY;
    public float overAllY=0;
    void Start()
    {
        lastY = transform.position.y;
        rb=GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    void Update()
    {
        overAllY += Mathf.Abs(lastY - transform.position.y);
        lastY=transform.position.y;
        if (rb.linearVelocity.y < maxFallspeed)
        {
            rb.linearVelocity=new Vector2(rb.linearVelocity.x,maxFallspeed); //so it never falls to fast
        }
        if (Input.GetButtonDown("Fire1"))
        {
            rb.linearVelocity =new Vector2(rb.linearVelocity.x, flapStrength); //flap
        }
        text.SetText(""+score); //to turn it into a string cuz nothing + a number equals just the num
    }
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.tag == "borders")
        {
            return;//to prevent it from bursting when it hits the ceiling or other borders
        }
        Vector3 spawnPos = transform.position;
        Instantiate(deathExplo,spawnPos,Quaternion.Euler(new Vector3(0,0,0)));
        transform.position = new Vector3(0, -500, transform.position.z);
        Invoke("reload", 1f); //so that you have time to look at the polish
    }
    public void reload()
    {
        Debug.Log(overAllY);
       SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        score++;
    }
}
