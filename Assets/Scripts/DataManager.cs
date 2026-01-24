using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DataManager : MonoBehaviour
{
    [SerializeField] private List<Item> items = new List<Item>();
    [SerializeField] private GameObject buttonContainer;
    [SerializeField] private ItemButtonManager itemButtonManager;

    void Start()
    {
        GameManager.instance.OnItemsMenu += CreateButton;
    }

    public void CreateButton()
    {
        foreach (var item in items)
        {
            ItemButtonManager itemButton;
            itemButton = Instantiate(itemButtonManager, buttonContainer.transform);

            // Asignar las propiedades
            itemButton.ItemName = item.itemName;
            itemButton.ItemDescription = item.itemDescription;
            itemButton.ItemImage = item.itemImage;
            itemButton.Item3DModel = item.item3DModel;
            itemButton.name = item.itemName;

            // ✅ AGREGAR ESTA LÍNEA - Inicializar después de asignar todo
            itemButton.Initialize();
        }

        GameManager.instance.OnItemsMenu -= CreateButton;
    }
}