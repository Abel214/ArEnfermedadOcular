using UnityEngine;
using System.Collections;
using System.Collections.Generic;
[CreateAssetMenu]
public class Item : ScriptableObject
{
    public string itemName;
    public Sprite itemImage;
    public string itemDescription;
    public GameObject item3DModel;

}
