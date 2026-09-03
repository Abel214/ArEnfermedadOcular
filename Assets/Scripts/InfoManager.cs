using UnityEngine;
using DG.Tweening;

public class InfoManager : MonoBehaviour
{
    public GameObject panelInfo;

    void Start()
    {
        if (panelInfo != null)
            panelInfo.SetActive(false);
    }

    public void AbrirInfo()
    {
        panelInfo.SetActive(true);
        panelInfo.transform.localScale = Vector3.zero;
        panelInfo.transform
            .DOScale(Vector3.one, 0.3f)
            .SetEase(Ease.OutBack);
    }

    public void CerrarInfo()
    {
        panelInfo.transform
            .DOScale(Vector3.zero, 0.2f)
            .SetEase(Ease.InBack)
            .OnComplete(() => panelInfo.SetActive(false));
    }
}