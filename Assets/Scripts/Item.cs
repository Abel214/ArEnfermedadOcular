using UnityEngine;
using System.Collections;
using System.Collections.Generic;
[CreateAssetMenu(fileName = "NewEyeDisease", menuName = "Medical AR/Eye Disease")]
public class Item : ScriptableObject
{
    [Header("Información General")]
    public string itemName;
    public Sprite itemImage;

    [TextArea(3, 5)]
    public string itemDescription;

    [Header("Modelos 3D")]
    public GameObject item3DModel; // Modelo principal (externo o interno)

    [Header("Información Médica (para IA)")]
    [TextArea(5, 10)]
    public string medicalContext = @"
    Información detallada sobre esta enfermedad ocular.
    Incluye: síntomas, causas, tratamiento, partes del ojo afectadas.
    ";

    [Header("Tipo de Vista")]
    public bool isAnatomyView = false; // ¿Es vista interna?

}
