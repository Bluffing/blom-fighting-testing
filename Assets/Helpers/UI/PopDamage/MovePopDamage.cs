using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MovePopDamage : MonoBehaviour
{
    public List<Vector3> path;
    public float shownLength = 1f;
    private float timeLeft;

    bool started = false;

    void Start() { }

    private Vector3 NBezier(float t)
    {
        List<Vector3> temp = new(path);
        int n = path.Count();
        for (int i = 1; i < n; i++)
            for (int j = 0; j < (n - i); j++)
                temp[j] = temp[j] * (1 - t) + temp[j + 1] * t;

        return temp[0];
    }

    public void ShowDamange()
    {
        path = path.Select(p => transform.position + p).ToList();
        timeLeft = shownLength;

        started = true;
    }

    void Update()
    {
        if (!started)
            return;

        timeLeft -= Time.deltaTime;

        if (timeLeft <= 0)
        {
            Destroy(gameObject);
            return;
        }

        transform.position = NBezier(1 - timeLeft / shownLength);
    }
}
