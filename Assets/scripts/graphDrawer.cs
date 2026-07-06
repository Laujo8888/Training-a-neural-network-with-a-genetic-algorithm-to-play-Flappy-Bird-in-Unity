using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class FitnessGraph : MonoBehaviour
{
    public LineRenderer bestLine;
    public LineRenderer avgLine;
    public LineRenderer bestAvgLine;
    public LineRenderer maxLine;

    public float graphWidth = 20f;
    public float graphHeight = 8f;
    public Vector2 bestOffset;
    public Vector2 avgOffset;
    public Vector2 bestAvgOffset;
    public Vector2 maxOffset;
    public int maxGenerationsVisible = 200;
    public float GlobalMaxFitness = 0;

    List<float> bestFitness = new List<float>();
    List<float> avgFitness = new List<float>();
    List<float> bestAvgFitness= new List<float>();
    List<float> maxFitness = new List<float>();

    void OnEnable()
    {
        GA.OnGenerationFinished += RecordGeneration;
    }

    void OnDisable()
    {
        GA.OnGenerationFinished -= RecordGeneration;
    }

    void RecordGeneration(float best, float avg,float bestAvg, float maxfitness,int gen)
    {
        bestFitness.Add(best);
        avgFitness.Add(avg);
        bestAvgFitness.Add(bestAvg);
        maxFitness.Add(maxfitness);

        DrawGraph();
    }

    void DrawGraph()
    {
        DrawLine(bestLine, bestFitness,bestOffset);
        DrawLine(avgLine, avgFitness,avgOffset);
        DrawLine(bestAvgLine, bestAvgFitness,bestAvgOffset);
        DrawLine(maxLine, maxFitness,maxOffset);  
    }
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            maxLine.enabled = !(maxLine.enabled);
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            avgLine.enabled = !(avgLine.enabled);
        }
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            bestLine.enabled = !(bestLine.enabled);
        }
        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            bestAvgLine.enabled = !(bestAvgLine.enabled);
        }
    }
    void DrawLine(LineRenderer line, List<float> data, Vector2 offset)
    {
        if (data.Count < 2)
            return;

        int count = Mathf.Min(maxGenerationsVisible, data.Count);
        int start = data.Count - count;

        line.positionCount = count;
        float localMaxFitness=GetMax(data,start);
        GlobalMaxFitness = GlobalMaxFitness<localMaxFitness?localMaxFitness:GlobalMaxFitness;

        for (int i = 0; i < count; i++)
        {
            float fitness = data[start + i];

            // sqrt scaling PERFECT for n? fitness
            float normalized = Mathf.Sqrt(fitness) / Mathf.Sqrt(GlobalMaxFitness);

            //float x = (float)i / maxGenerationsVisible * graphWidth;
            float x = (float)i / (count - 1) * graphWidth;
            float y = normalized * graphHeight;

            line.SetPosition(i, new Vector3(x+offset.x, y+offset.y, 0));
        }
    }

    float GetMax(List<float> data, int start)
    {
        float max = float.MinValue;

        for (int i = start; i < data.Count; i++)
            if (data[i] > max)
                max = data[i];

        return max;
    }
}