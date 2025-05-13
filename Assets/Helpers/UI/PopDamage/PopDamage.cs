using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class PopDamage : MonoBehaviour
{
    public GameObject popPrefab;
    public List<Vector3> path;

    public void ShowDamage(Transform obj, string txt, Color? textcolor = null)
    {
        var go = Instantiate(popPrefab, obj);
        var textMesh = go.GetComponent<TextMesh>();
        var MovePopDamage = go.GetComponent<MovePopDamage>();

        MovePopDamage.path = path;
        MovePopDamage.shownLength = 0.3f;
        if (textcolor != null)
        {
            textMesh.text = txt;
            textMesh.color = (Color)textcolor;
        }

        MovePopDamage.ShowDamange();
    }
}

[CustomEditor(typeof(PopDamage))]
public class PopDamageEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        PopDamage script = (PopDamage)target;
        if (GUILayout.Button("Show Damage"))
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null)
                script.ShowDamage(p.transform, "100");
        }
    }
}