using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour

{
    public event Action OnMainMenu;
    public event Action OnItemsMenu;
    public event Action OnArPosition;
    public event Action OnIAMenu;
    public static GameManager instance;


    private void Awake()
    {
        if(instance != null && instance != this)  
        {
            Destroy(gameObject);
        }
        else
        {
            instance = this;
        }

    }
    void Start()
    {
        MainMenu();

    }
    public void MainMenu()
    {
        //? consta de que existe una instancia suscrito al evento
        OnMainMenu?.Invoke();
        Debug.Log("Main Menu Activated");
        Debug.Log("Items Menu Activated");
        Debug.Log("AI Menu Activated");

    }


    public void ItemsMenu()
    {
        OnItemsMenu?.Invoke();
        Debug.Log("Items Menu Activate");
    }
    public void ArPosition()
    {
        OnArPosition?.Invoke();
        Debug.Log("Ar position Activated");
    }
    public void CloseApp()
    {
        Application.Quit();
    }
    public void AIMenu()
    {
        OnIAMenu?.Invoke();
        Debug.Log("AI Menu Activate");
    }


}
