using Unity.Mathematics.Geometry;
using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System;
using NUnit.Framework;
using System.Linq;
using Unity.VisualScripting;

public class NN : MonoBehaviour
{
    //the values all together like weights biases etc will be handled by an external script
    [Header("input")]
    public int inputLen = 5;
    public float[] input; //len=4, 0=birdY, 1=birdLinearVeloY,2=pipeDistance,3=pipeGapheightDelta
    public float[][] inputWeights;
    [Header("hidden")]
    public int hiddenLen = 8;
    public float[] hidden;
    public float[] hiddenWeights;// cuz theres only one output else itd be a 2d array
    public float[] hiddenBias;
    [Header("output")]
    public int outputLen = 1;
    public float output; //didnt do an array cuz its just one output node (flap)
    public float outputBias;
    public float fireNum = 0.5f;
    [Header("detection/important values")]
    public bool disableBirdY = false;
    public bool disableBirdVelo = false;
    public float sight = 9999f;// to detect the pipes
    public float pipeMinDistance = 30f;
    public float pipeMaxDistance = 0;
    public float pipeGapMaxHeight = 4;
    public float pipeGapMinHeight = -4;
    public float birdMinY=-4.36f;
    public float birdMaxY=6.8f;
    public float birdMaxLVY=12;
    public float birdMinLVY=-15; //LVY linearvelo Y
    [Header("action/game stuff")]
    public Rigidbody2D rb;
    public float maxFallspeed=-15;
    public float flapStrength=12;
    public Vector2 disabledPosition=new Vector2(0,999);
    public bool dead = false;
    [Header("fitness")]
    public float score=0;
    public float timeSurvived = 0;
    public float timeAtRightHeight = 0;
    public float yMovementTotal = 0;
    public float lastY = 1.1f;
    public float timeARHArea = 3;
    public float offAreaPenalty = 4;
    public float PipeGapProximityScore = 0;
    public float timeDeceaseZone = 3;
    public float onDeathScoreM = 10;
    public List<float> flapsIntervals = new List<float>();
    public float lastFlapTime=-1;
    public float flapIntervalCount = 20;
    public float rythmKillTreshhold=0.05f;
    public float hoverPenalty = 2;
    public float changeTreshhold = 0.1f;
    public float reactivnessTReward = 0.1f;
    public List<float> inputChanges=new List<float>();
    public List<float> flaps=new List<float>();
    public float[] pastInputs;
    public int checkReactivnessTimeframe = 5;
    public float reactivnessLegitTime = 0.2f;
    public float birdYNoise = 0.02f;
    public float birdVeloYNoise = 0.01f;
    //novelty search
    public List<float> behaviours=new List<float>();
    public float behaviourSampleIntervals = 0;
    public float behaviourSampleTimer = 0;
    public int behaviourSampleCount = 50;
    [Header("polish")]
    public GameObject deathExplo;
    void Start()
    {
        score = 0;
        rb = GetComponent<Rigidbody2D>();
        input = new float[inputLen];
        hidden = new float[hiddenLen];
        rb.simulated = true;
        pastInputs = new float[inputLen];
        pastInputs[0] = 0;
        pastInputs[1] = 0;
        pastInputs[2] = 0;
        pastInputs[3] = 0;
    }

    // actions/actual game logic
    public void flap()
    {
        rb.linearVelocityY = flapStrength;
        float now = Time.time;
        if (lastFlapTime >= 0)
        {
            flapsIntervals.Add(now-lastFlapTime);
            if (flapsIntervals.Count > flapIntervalCount)
            {
                flapsIntervals.RemoveAt(0);
            }
        }
        lastFlapTime = now;
        checkRythm();
        flaps.Add(now);
        if(flaps.Count >= checkReactivnessTimeframe)
        {
            flaps.RemoveAt(0);
        }
    }
    public void checkRythm()
    {
        if (flapsIntervals.Count < flapIntervalCount - 2)
        {
            return;
        }
        float avg = flapsIntervals.Average();
        float variance = flapsIntervals.Sum(i => Mathf.Pow(i - avg, 2) )/flapsIntervals.Count;// basically calculating the avg difference to the avg interval and pow-ing it to make it positive through a sqrt afterwards
        float overAllDifferene = Mathf.Sqrt(variance);
        if( overAllDifferene < rythmKillTreshhold)
        {
            score -= hoverPenalty;
            die();
        }

    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        score+=1f;
    }
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.tag == "borders")
        {
            //timeSurvived--;
            //return;
        }
        //Vector2 y = transform.position;
        //Debug.Log(y);
       // evaluateDeathScore(y);
        die();
    }
    public void die()
    {

        //Instantiate(deathExplo,transform.position,Quaternion.Euler(new Vector3(0,0,0)));
        rb.simulated = false;
        transform.position = disabledPosition;
        dead = true;
    }
    public void evaluateDeathScore(Vector2 y)
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(y, sight);
        float minDis = Vector2.Distance(colliders[0].transform.position, y);
        int mindex = 0;//LOL
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i].gameObject == gameObject || colliders[i].gameObject.tag == "borders" || colliders[i].gameObject.tag == "ground")
            {
                continue;
            }
            if (colliders[i].transform.position.x <y.x) continue;
            if (minDis > Vector2.Distance(colliders[i].transform.position, y))
            {   
                minDis = Vector2.Distance(colliders[i].transform.position, y);
                mindex = i;
            }
        }
        GameObject pipe = getNearestPipe();
        float dis = Mathf.Abs(y.y - pipe.transform.position.y);
        float exp = Mathf.Exp(-(dis * dis)); //so that the smaller the distance the higher the score 
        score += exp*onDeathScoreM;

    }
    void Update()
    {
        if (!dead)
        {
            gatherData();
            think();
            timeSurvived += Time.deltaTime;
            yMovementTotal += Mathf.Abs(lastY - transform.position.y);
            lastY = transform.position.y;
            checkForReactivness();
            collectBehaviourSamples();
            if (rb.linearVelocityY < birdMinLVY)
            {
                rb.linearVelocityY = birdMinLVY;
            }
        }
        
    }
    public void collectBehaviourSamples()
    {
        behaviourSampleTimer += Time.deltaTime;
        if (behaviourSampleTimer >= behaviourSampleIntervals)
        {
            behaviours.Add(transform.position.y);
            if (behaviours.Count >= behaviourSampleCount)
            {
                behaviours.RemoveAt(0);
            }
            behaviourSampleTimer = 0;
        }
    }
    public void checkForReactivness()
    {
        float maxChange = 0;
        for (int i=0; i < inputLen; i++)
        {
            maxChange = Mathf.Abs(pastInputs[i] - input[i]) > maxChange ? Mathf.Abs(pastInputs[i] - input[i]) : maxChange;
        }
        if (maxChange > changeTreshhold)
        {
            inputChanges.Add(Time.time);
            if (inputChanges.Count >= checkReactivnessTimeframe)
            {
                inputChanges.RemoveAt(0);
            }
        }
        for(int i=flaps.Count-1; i >=0; i--)//remove the ones that are to old
        {
            if (Mathf.Abs(flaps[i] - Time.time) > reactivnessLegitTime)
            {
                flaps.RemoveAt(i);
            }
        }
        for (int i = inputChanges.Count-1; i>=0; i--)
        {
            if (Mathf.Abs(inputChanges[i] - Time.time) > reactivnessLegitTime)
            {
                inputChanges.RemoveAt(i);
            }
        }
        if (flaps.Count>0&&inputChanges.Count>0)
        {
            score += reactivnessTReward;
            flaps.Clear();                       //check for reactivness
            inputChanges.Clear();
  //          Debug.Log("reactivness reward");
        }
        for (int i = 0; i < inputLen; i++)
        {
            pastInputs[i]=input[i];
        }
    }
    //brain part
    public void think()
    {
        for(int i=0; i < hiddenLen; i++) //reseting the values so it doesnt accumelate
        {
            hidden[i] = 0;
        }
        output = 0;
        for(int i=0; i < inputLen; i++)
        {
            for(int j=0; j < hiddenLen; j++)
            {
                hidden[j] += input[i] * inputWeights[i][j]; //adding the input to each hidden node
            }
        }
        for(int i=0; i < hiddenLen; i++)
        {
            hidden[i] += hiddenBias[i];
            hidden[i] = sigmoid(hidden[i]);
            output += hidden[i] * hiddenWeights[i];//adding the bias and changing the output
        }
        output += outputBias;
        output = sigmoid(output);//adding bias
        if (output > fireNum)
        {
            flap();
        }
    }
    public void gatherData()
    {//mathf.inverselerp(min,max,x) is basically this formular: (x-min)/(max-min)
        //its used to normalize values above 1 into a range from 0 to 1
        input[0] = Mathf.InverseLerp(birdMinY,birdMaxY,transform.position.y);//bird pos y
        input[1] = Mathf.InverseLerp(birdMinLVY,birdMaxLVY,rb.linearVelocity.y);//bird linear velo y
        GameObject nearestPipe = getNearestPipe();
        input[2] = Mathf.InverseLerp(pipeMinDistance,pipeMaxDistance,Mathf.Abs(nearestPipe.transform.position.x-transform.position.x));
       // Debug.Log("pipedistance: " + Mathf.Abs(nearestPipe.transform.position.x - transform.position.x));
        //Debug.Log("pipePosx: " +nearestPipe.transform.position.x);
        input[3]=Mathf.InverseLerp(pipeGapMinHeight-birdMaxY,pipeGapMaxHeight-birdMinY,nearestPipe.transform.position.y);
        input[0] += UnityEngine.Random.Range(-birdYNoise, birdYNoise);
        input[1] += UnityEngine.Random.Range(-birdVeloYNoise, birdVeloYNoise);
        if(Mathf.Abs(transform.position.y-nearestPipe.transform.position.y)<timeARHArea){
            timeAtRightHeight++;
        }
        else
        {
            timeAtRightHeight-=offAreaPenalty;
        }
        //see if close to pipe gap and change value based on that
        if (Mathf.Abs(transform.position.x - nearestPipe.transform.position.x) < 0.2f)
        {
            float distanceToGap = MathF.Abs(transform.position.y-nearestPipe.transform.position.y);
            float timeDecay = Mathf.Clamp01(timeDeceaseZone - timeSurvived);
            PipeGapProximityScore += Mathf.Exp(-distanceToGap * distanceToGap)*Time.deltaTime*timeDecay;
        }

        if (disableBirdY)
        {
            Debug.Log("its setting");
            input[1] = 0;
        }
        if (disableBirdVelo)
        {
            input[0] = 0;
        }
    
    }
    public GameObject getNearestPipe()
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, sight);
        float minDis = float.MaxValue;
        GameObject pipe = null;
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i].gameObject == gameObject || colliders[i].gameObject.tag == "borders" 
                || colliders[i].gameObject.tag == "ground"
                || colliders[i].transform.position.x<transform.position.x
                || colliders[i].gameObject.tag=="ai")
            {
                continue;
            }
            if (minDis > Mathf.Abs(colliders[i].transform.position.x - transform.position.x))
            {
                //Debug.Log("seeing pipe");
                minDis = Mathf.Abs(colliders[i].transform.position.x - transform.position.x);
                pipe = colliders[i].gameObject;
            }
        }
        return pipe;
    }
    public float sigmoid(float x) // the firing function 
    {
        return 1 / (1 + Mathf.Exp(-x)); //mathf.exp returns e raised to the parameter
    }
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, sight);
        Gizmos.color = Color.red;
        //Gizmos.DrawLine(transform.position, new Vector2(transform.position.x, getNearestPipe().transform.position.y));
    }
    public void saveBrain(string fileName)
    {
        brain newBrain = new brain();
        newBrain.inputWeights0 = inputWeights[0];
        newBrain.inputWeights1 = inputWeights[1];
        newBrain.inputWeights2 = inputWeights[2];
        newBrain.inputWeights3 = inputWeights[3];

        newBrain.hiddenWeights = hiddenWeights;
        newBrain.hiddenBiases = hiddenBias;
        newBrain.outputBias = outputBias;
        string json=JsonUtility.ToJson(newBrain,true);
        string path=Application.persistentDataPath + "/" + fileName+".json";
        File.WriteAllText(path, json);
        Debug.Log("saved to:"+path);
    }
    public void loadBrain(string fileName)
    {
        string path = Application.persistentDataPath + "/" + fileName + ".json";
        if(!File.Exists(path))
        {
            Debug.Log("error 404 on"+path);
            return;
        }
        string json=File.ReadAllText(path);
        brain newBrain = JsonUtility.FromJson<brain>(json);
        inputWeights[0] = newBrain.inputWeights0;
        inputWeights[1] = newBrain.inputWeights1;
        inputWeights[2] = newBrain.inputWeights2;//unitys json stuff cant handle 2d stuff (T-T)
        inputWeights[3] = newBrain.inputWeights3;

        hiddenWeights = newBrain.hiddenWeights;
        hiddenBias = newBrain.hiddenBiases;
        outputBias = newBrain.outputBias;
        Debug.Log("brain loaded from:" + path);
    }
}
