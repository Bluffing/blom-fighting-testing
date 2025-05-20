using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class PopDamage : MonoBehaviour
{
    public GameObject popPrefab;
    public List<Vector3> path;

    public Transform DEBUG_TARGET;

    // Prolly better to make this a helper function instead
    public void ShowDamage(Transform obj, string txt, Color? textcolor = null, float time = 0.3f)
    {
        var go = Instantiate(popPrefab, obj);
        var textMesh = go.GetComponent<TextMesh>();
        var MovePopDamage = go.GetComponent<MovePopDamage>();

        MovePopDamage.path = path.Select(p => Vector3.Scale(p, obj.transform.localScale)).ToList();
        MovePopDamage.shownLength = time;
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
            if (script.DEBUG_TARGET != null)
            {
                script.ShowDamage(script.DEBUG_TARGET, "100");
                return;
            }

            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null)
                script.ShowDamage(p.transform, "100");
        }
    }
}