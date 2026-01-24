using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemButtonManager : MonoBehaviour
{
    private string itemName;
    private Sprite itemImage;
    private string itemDescription;
    private GameObject item3DModel;
    private ARInteractionManager interactionManager; // ← AR en mayúsculas

    public string ItemName { set => itemName = value; }
    public string ItemDescription { set => itemDescription = value; }
    public Sprite ItemImage { set => itemImage = value; }
    public GameObject Item3DModel { set => item3DModel = value; }

    public void Initialize()
    {
        if (itemName != null)
            transform.GetChild(0).GetComponent<TMP_Text>().text = itemName;

        if (itemImage != null)
            transform.GetChild(1).GetComponent<UnityEngine.UI.Image>().sprite = itemImage;

        if (itemDescription != null)
            transform.GetChild(2).GetComponent<TMP_Text>().text = itemDescription;

        var button = GetComponent<Button>();
        button.onClick.AddListener(GameManager.instance.ArPosition);
        button.onClick.AddListener(Create3DModel);

        interactionManager = FindFirstObjectByType<ARInteractionManager>();
    }

    private void Create3DModel()
    {
        interactionManager.Item3DModel = Instantiate(item3DModel);
    }
}