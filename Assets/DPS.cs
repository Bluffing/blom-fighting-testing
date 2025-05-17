using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class DPS : MonoBehaviour
{
    private Text text;
    public bool showDPS = true;
    public bool showDPSWarning = true;
    public float dpsWindowSec = 5f;

    private float totalDmg = 0;

    public void Start()
    {
        if (!TryGetComponent(out text))
            Debug.LogError("Text component not found");
    }

    void Update()
    {
        if (showDPS)
            text.text = (totalDmg / dpsWindowSec).ToString("0.##") + " dmg/s";
    }

    public void AddDmg(float dmg) =>
        StartCoroutine(DmgCoroutine(dmg));
    private IEnumerator DmgCoroutine(float dmg)
    {
        totalDmg += dmg;
        yield return new WaitForSeconds(dpsWindowSec);
        totalDmg -= dmg;
    }
}