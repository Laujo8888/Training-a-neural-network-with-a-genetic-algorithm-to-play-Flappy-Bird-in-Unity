using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

public class pipeSpawner : MonoBehaviour
{
    public GameObject pipe;
    public float minCooldown=0.5f;
    public float maxCooldown=4f;
    public float spawnX=40;
    public float highY=4;
    public float lowY=-4;
    public float leastDifference=1;
    public bool hardCode = false;
    public int hardCnt = 0;
    [SerializeField] private float timer=0;
    [SerializeField] private float cooldown=0;
    [SerializeField] private float lastPos=0;
    
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= cooldown&&hardCode==false)
        {

            cooldown = Random.Range(minCooldown, maxCooldown);
            timer = 0;
            float spawnHeight=Random.Range(lowY,highY);
            while (Mathf.Abs(spawnHeight - lastPos) < leastDifference)
            {
                spawnHeight = Random.Range(lowY, highY);
            }
            lastPos = spawnHeight;
            Instantiate(pipe,new Vector2(spawnX,spawnHeight),Quaternion.Euler(new Vector3(0,0,0)));
        }else if (hardCode == true && timer >= cooldown)
        {
            float spawnHeight = 0;
            cooldown = 2;
            timer = 0;
            if (hardCnt == 0)
            {
                spawnHeight = -2;
                hardCnt++;
            }
            else
            {
                spawnHeight = 2;
                hardCnt = 0;
            }
            Instantiate(pipe, new Vector2(spawnX, spawnHeight), Quaternion.Euler(new Vector3(0, 0, 0)));

        }
    }
    private void OnEnable()
    {
        GA.resetPipes += resetCool;
    }
    private void OnDisable()
    {
        GA.resetPipes -= resetCool;
    }
    public void resetCool()
    {
        cooldown = 0;
    }
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 2);
    }
}
