
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

public class GA : MonoBehaviour
{
    [Header("core Values")]
    public int inputLen = 5;
    public int hiddenLen = 8;
    public int outputLen = 1;
    public float mutationRate = 8;
    public float mutationStrength = 0.1f;
    public int num = 50;
    [SerializeField] private GameObject[] dots;
    [SerializeField] private NN[] dotNN;
    [SerializeField] private float[] fitness;
    public float startMin=-1;
    public float startMax=1;
    public GameObject ai;
    public Vector2 spawnPos = new Vector2(-8.5f,1.1f);
    public bool allDead = false;
    [Header("event stuff")]
    public string filler = "i have no use";
    public static event UnityAction resetPipes;
    [Header("cool stuff")]
    //public float timeScale;
    public float scoreM = 10;
    public int scoreP = 2;
    public float timeSM = 0.1f;
    public float timeARHM = 0.5f;
    public Canvas canvas;
    public TMP_Text text;
    public TMP_Text fitnessIndicator;
    public TMP_Text averageFitnessShower;
    public TMP_Text highestFitnessShower;
    public TMP_Text maxFitnessShower; // max refers to best of last gen
    public float bestFitness;//best refers to best of all time
    public float avgFitness;
    public float bestAvgFitness;
    public float maxFitness;
    public Slider speedSlider;
    public TMP_Text speedShower;
    public int gen = 1;
    public float penaltyTreshholdY = 10f;
    public float penaltyY=5000;
    public float movementYM = 1;
    public float pipeProximityM = 3;
    public float scoreBoostTreshhold = 1;
    public float scoreBoost = 100;//to prevent hovering by giving a boost when score >1 as the odds that its greater than one are only around 20% (est.)
    //ns--------------------------------------
    public List<List<float>> behaviourArchive=new List<List<float>>();
    public int maxArchiveSize = 10;
    public int evaluatedDistances = 5;
    public float noveltyM=1;
    [Header("saving")]
    public string fileName = "bestBrain0";
    public bool saveNext = false;
    public bool loadNext = false;
    public string loadName = "bestBrain0";
    public TMP_Text pathText;
    [Header("mutation")]
    public int bestElitismCount = 4;
    public int elitismCount=8;
    public int randCount = 10;
    public int maxGen = 8000;
    public int genCountFitnessNoIncrease = 0;//how mny gens the fitness hasnt changed;
    public int reshuffleCount=50;//when to reshuffle the population by addin a lot of random dots
    public int reshuffleDotNum = 30;//how much to reshuffle 
    public int crossoverCount = 65;
    [Header("stuff")]
    public Slider cameraSlider;
    public static System.Action<float, float,float,float, int> OnGenerationFinished;
    public static void OnresetPipes()
    {
        resetPipes?.Invoke();
    }
    void Start()
    {
        fitness = new float[num];
        dots=new GameObject[num];
        dotNN=new NN[num];
        RandomizeVals();
    }
    public void RandomizeVals()
    {
        for(int i = 0; i < num; i++)
        {
   
            dots[i] = Instantiate(ai, spawnPos, Quaternion.Euler(new Vector3(0, 0, 0)));
            dotNN[i] = dots[i].GetComponent<NN>();
             //input layer basic initialization
            dotNN[i].inputWeights = new float[inputLen][];
            for(int j=0;j<inputLen; j++)
            {
                dotNN[i].inputWeights[j] = new float[hiddenLen]; //csharp this is..... :(
            }
            //-------------------------------------------------
            //hidden layer basic initialization
            dotNN[i].hiddenWeights = new float[hiddenLen];
            dotNN[i].hiddenBias = new float[hiddenLen];
            //-------------------------------------------------
            //weights from input to hidden
            for (int j = 0; j < inputLen; j++)
            {
                for(int k=0; k < hiddenLen; k++)
                {
                    dotNN[i].inputWeights[j][k]=Random.Range(startMin, startMax); //initializing the weights from input to hidden
                }
            }
            //-------------------------------------------------
            //biases and weights for hidden and output
            for(int j=0; j < hiddenLen; j++)
            {
                dotNN[i].hiddenWeights[j] = Random.Range(startMin, startMax);
                dotNN[i].hiddenBias[j] = Random.Range(startMin, startMax);
            }
            dotNN[i].outputBias = Random.Range(startMin, startMax);
        }
    }
    // Update is called once per frame
    void Update()
    {
        pathText.text = Application.persistentDataPath + "/" + fileName+".json";
        Time.timeScale = speedSlider.value;
        speedShower.text = "x" + Mathf.Round(Time.timeScale*10)/10;
        if (areAllDead())
        {
            nextGen();
        }
        text.text = "" + gen;
        Camera.main.orthographicSize = cameraSlider.value;
        if (Input.GetKeyDown(KeyCode.N))
        {
            nextGen();
        }
        if (Input.GetKeyDown(KeyCode.Alpha0))
        {
            canvas.enabled = !(canvas.enabled);
        }
    }
    public void nextGen()
    {
        gen++;
        //handling mutation
        mutationStrength = Mathf.Lerp(1f, 0.1f, (float)gen / maxGen);
        mutationRate = Mathf.Lerp(25, 5, (float)gen / maxGen);
        //-------------------------------------
        float max = 0;
        int maxi = 0;
        GameObject[] newGen = new GameObject[num];
        NN[] newGenNN = new NN[num];
        float sum = 0;
        List<(int index,float fitness)> scoredBirds=new List<(int index, float fitness)>();
        for(int i=0; i < num; i++)
        {
            fitness[i] = calcFitness(dotNN[i]);
            scoredBirds.Add((i, fitness[i]));
            if (max < fitness[i])
            {
                max = fitness[i];
                maxi=i;
            }
            sum += fitness[i];
        }
        scoredBirds.Sort((a, b) => b.fitness.CompareTo(a.fitness));
        //if needed save Brain
        if (saveNext)
        {
            saveNext = false;
            dotNN[maxi].saveBrain(fileName);

        }
        //show best fitness
        if (max > float.Parse(fitnessIndicator.text))
        {
            bestFitness = Mathf.Round(max * 1000) / 1000;
            fitnessIndicator.text = "" + Mathf.Round(max*1000)/1000;
        }
        //show avg fitness
        averageFitnessShower.text = ""+Mathf.Round((sum / num)*1000)/1000;
        avgFitness= Mathf.Round((sum / num) * 1000) / 1000;
    
        //show highest individual fitness of max gen
        maxFitnessShower.text = "" + Mathf.Round((max) * 1000) / 1000;
        maxFitness = Mathf.Round((max) * 1000) / 1000;
            //show ighest avg fitness
        genCountFitnessNoIncrease++;
        if (sum/num> float.Parse(highestFitnessShower.text))
        {
            genCountFitnessNoIncrease = 0;
            highestFitnessShower.text = "" + Mathf.Round(sum/num * 1000) / 1000;
            bestAvgFitness= Mathf.Round(sum / num * 1000) / 1000;
        }
        //NS---------------------------------------
        for(int i=0;i<num; i++)
        {
            behaviourArchive.Add(dotNN[i].behaviours);
        }
        while(behaviourArchive.Count >= maxArchiveSize)
        {
            behaviourArchive.RemoveAt(0);
        }
        //bestelitism------------------------------------------
        for(int i=0; i < bestElitismCount; i++)
        {
            newGen[i] = copyParent(maxi);
            newGenNN[i] = newGen[i].GetComponent<NN>();
        }
        //-------------------------------------------------
        //elitism------------------------------------
        int elitismIndex = 0;
        for (int i = bestElitismCount; i < bestElitismCount+elitismCount; i++)
        {
            newGen[i] = copyParent(scoredBirds[elitismIndex].index);
            newGenNN[i] = newGen[i].GetComponent<NN>();
            elitismIndex++;
        }

        //---------------------------------------------
        //randism--------------------------------------

        for(int i = elitismCount + bestElitismCount; i < bestElitismCount + elitismCount + randCount; i++)
        {
            newGen[i] = randomBrain();
            newGenNN[i] = newGen[i].GetComponent<NN>();
        }
        //----------------------------------------------
        //F*****G CROSSOVER------------------------------------------------
        for(int i= bestElitismCount + elitismCount + randCount; i< bestElitismCount + elitismCount + randCount+crossoverCount; i++)
        {
            newGen[i] = Instantiate(ai, spawnPos, Quaternion.Euler(new Vector3(0, 0, 0)));
            newGenNN[i] = newGen[i].GetComponent<NN>();
            int parenti1 = findParent(sum);
            NN parentNN1 = dotNN[parenti1];
            int parenti2 = findParent(sum);
            while (parenti2 != parenti1)
            {
                parenti2 = findParent(sum);
            }
            NN parentNN2= dotNN[parenti2];
            //input layer basic initialization
            newGenNN[i].inputWeights = new float[inputLen][];
            for (int j = 0; j < inputLen; j++)
            {
                newGenNN[i].inputWeights[j] = new float[hiddenLen]; //csharp this is..... :(
            }
            //-------------------------------------------------
            //hidden layer basic initialization
            newGenNN[i].hiddenWeights = new float[hiddenLen];
            newGenNN[i].hiddenBias = new float[hiddenLen];
            //-------------------------------------------------
            //mutate
            for (int j = 0; j < inputLen; j++)
            {
                for (int k = 0; k < hiddenLen; k++)
                {
                    newGenNN[i].inputWeights[j][k] = mutate(crossoverCoinFlip(parentNN1.inputWeights[j][k], parentNN2.inputWeights[j][k]));
                }
            }
            for (int j = 0; j < hiddenLen; j++)
            {
                newGenNN[i].hiddenWeights[j] = mutate(crossoverCoinFlip(parentNN1.hiddenWeights[j], parentNN2.hiddenWeights[j]));
                newGenNN[i].hiddenBias[j] = mutate(crossoverCoinFlip(parentNN1.hiddenBias[j], parentNN2.hiddenBias[j]));
            }
            newGenNN[i].outputBias = mutate(crossoverCoinFlip(parentNN1.outputBias,parentNN2.outputBias));
        }
        //---------------------------------------------------------------------------
        for (int i=bestElitismCount+elitismCount+randCount+crossoverCount; i < num; i++)
        {
            newGen[i] = Instantiate(ai, spawnPos, Quaternion.Euler(new Vector3(0, 0, 0)));
            newGenNN[i] = newGen[i].GetComponent<NN>();
            int parenti=findParent(sum);
            NN parentNN = dotNN[parenti];
            //input layer basic initialization
            newGenNN[i].inputWeights = new float[inputLen][];
            for (int j = 0; j < inputLen; j++)
            {
                newGenNN[i].inputWeights[j] = new float[hiddenLen]; //csharp this is..... :(
            }
            //-------------------------------------------------
            //hidden layer basic initialization
            newGenNN[i].hiddenWeights = new float[hiddenLen];
            newGenNN[i].hiddenBias = new float[hiddenLen];
            //-------------------------------------------------
            //load brain stuff
            if (loadNext == true)
            {
                if (i == num - 1)
                {
                    loadNext = false;
                }
                newGenNN[i].loadBrain(loadName);
                continue;
            }
            //mutate and stuff
            for (int j=0; j < inputLen; j++)
            {
                for(int k=0; k < hiddenLen; k++)
                {
                    newGenNN[i].inputWeights[j][k]=mutate(parentNN.inputWeights[j][k]);
                }
            }
            for(int j=0; j < hiddenLen; j++)
            {
                newGenNN[i].hiddenWeights[j] = mutate(parentNN.hiddenWeights[j]);
                newGenNN[i].hiddenBias[j] = mutate(parentNN.hiddenBias[j]);
            }
            newGenNN[i].outputBias=mutate(parentNN.outputBias);

        }
        //reshuffle logic------------------------------------------
        if (genCountFitnessNoIncrease >= reshuffleCount)
        {
            Debug.Log("reshuffle");
            for(int i=num-1; i >= num - reshuffleDotNum; i--)
            {
                newGen[i] = randomBrain();
                newGenNN[i] = newGen[i].GetComponent<NN>();
            }
            genCountFitnessNoIncrease = 0;

        }
        //-----------------------------------------------------------
        for(int i=0; i < num; i++)
        {
            Destroy(dots[i].gameObject);
        }
        GA.resetPipes();
        dots = newGen;
        dotNN = newGenNN;
        OnGenerationFinished?.Invoke(bestFitness, avgFitness,bestAvgFitness,maxFitness, gen);
    }
    public int findParent(float sum)
    {
        float rand = Random.Range(0, sum);
        float cnt = 0;
        int i = 0;
        while (cnt < rand) 
        {
            cnt += fitness[i];
            if (cnt >= rand)
            {
                return i;
            }
            i++;
        }
        return 0;

    }
    public GameObject randomBrain()
    {
        GameObject dot;
        NN dotNN;
        dot= Instantiate(ai, spawnPos, Quaternion.Euler(new Vector3(0, 0, 0)));
        dotNN = dot.GetComponent<NN>();
        //input layer basic initialization
        dotNN.inputWeights = new float[inputLen][];
        for (int j = 0; j < inputLen; j++)
        {
            dotNN.inputWeights[j] = new float[hiddenLen]; //csharp this is..... :(
        }
        //-------------------------------------------------
        //hidden layer basic initialization
        dotNN.hiddenWeights = new float[hiddenLen];
        dotNN.hiddenBias = new float[hiddenLen];
        //-------------------------------------------------
        //weights from input to hidden
        for (int j = 0; j < inputLen; j++)
        {
            for (int k = 0; k < hiddenLen; k++)
            {
                dotNN.inputWeights[j][k] = Random.Range(startMin, startMax); //initializing the weights from input to hidden
            }
        }
        //-------------------------------------------------
        //biases and weights for hidden and output
        for (int j = 0; j < hiddenLen; j++)
        {
            dotNN.hiddenWeights[j] = Random.Range(startMin, startMax);
            dotNN.hiddenBias[j] = Random.Range(startMin, startMax);
        }
        dotNN.outputBias = Random.Range(startMin, startMax);
        return dot;
    }
    public GameObject copyParent(int index)
    {
        GameObject newDot;
        NN newDotNN;
        newDot = Instantiate(ai, spawnPos, transform.rotation);
        newDotNN = newDot.GetComponent<NN>();
        //input layer basic initialization
        newDotNN.inputWeights = new float[inputLen][];
        for (int j = 0; j < inputLen; j++)
        {
            newDotNN.inputWeights[j] = new float[hiddenLen]; //csharp this is..... :(
        }
        //-------------------------------------------------
        //hidden layer basic initialization
        newDotNN.hiddenWeights = new float[hiddenLen];
        newDotNN.hiddenBias = new float[hiddenLen];
        //-------------------------------------------------
        for (int i = 0; i < inputLen; i++)
        {
            for (int j = 0; j < hiddenLen; j++)
            {
                newDotNN.inputWeights[i][j] = dotNN[index].inputWeights[i][j];
            }
        }
        for (int i = 0; i < hiddenLen; i++)
        {
            newDotNN.hiddenWeights[i] = dotNN[index].hiddenWeights[i];
            newDotNN.hiddenBias[i] = dotNN[index].hiddenBias[i];
        }
        newDotNN.outputBias = dotNN[index].outputBias;
        //-------------------------------------------------
        return newDot;
    }
    public bool areAllDead()
    {
        for(int i=0; i < num; i++)
        {
            if (!dotNN[i].dead)
            {
                return false;
            }
        }
        return true;
    }
    public float mutate(float value)
    {
        float rand = Random.Range(0, 100);
        if(rand<mutationRate)
        {
            value += Random.Range(-mutationStrength, mutationStrength);
            value = Mathf.Clamp(value, -1, 1);
        }
        return value;
    }
    float ComputeNoveltyScore(List<float> descriptor)
    {
        if (behaviourArchive.Count == 0)
        {
            return 0;
        }
        List<float> distances = new List<float>();

        foreach (var other in behaviourArchive)
        {
            float dist = 0f;
            int len = Mathf.Min(descriptor.Count, other.Count);
            for (int i = 0; i < len; i++)
            {
                dist += Mathf.Pow(descriptor[i] - other[i], 2);
            }
            dist = Mathf.Sqrt(dist);
            distances.Add(dist);
        }

        distances.Sort();
        return distances.Take(evaluatedDistances).Average();
    }

    public float calcFitness(NN nn)
    { //optimize fitness function
        float ret=Mathf.Pow(nn.score,scoreP)*scoreM+nn.timeAtRightHeight*timeARHM+nn.timeSurvived*timeSM;
        ret += nn.yMovementTotal * movementYM;
        ret += nn.PipeGapProximityScore * pipeProximityM;
        if (nn.score >= scoreBoostTreshhold)
        {
            ret += scoreBoost;
        }
        if (nn.yMovementTotal < penaltyTreshholdY)
        {
            ret -= penaltyY;
        }
        ret += ComputeNoveltyScore(nn.behaviours)*noveltyM;
        return ret;
    }
    public void setSaveToTrue()
    {
        saveNext = true;
    }
    public void setLoadToTrue()
    {
        loadNext = true;
    }
    public float crossoverCoinFlip(float a, float b)
    {
        float rand=Random.Range(0,1);
        if (rand < 0.5)
        {
            return a;
        }
        else
        {
            return b;
        }
    }
}
