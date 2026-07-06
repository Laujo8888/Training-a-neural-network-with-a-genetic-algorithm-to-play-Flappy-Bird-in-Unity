using UnityEngine;
using UnityEngine.SceneManagement;

public class sceneLoader : MonoBehaviour
{
    public string gameName = "game";
    public string menuName = "menu";
    public string GANNName = "GANN";
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }
    public void startGame()
    {
        SceneManager.LoadScene(gameName);
    }
    public void menu()
    {
        SceneManager.LoadScene(menuName);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void GANN()
    {
        SceneManager.LoadScene(GANNName);
    }
}
/*
 
 */