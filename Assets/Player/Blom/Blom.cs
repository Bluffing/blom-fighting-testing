using System;
using System.Linq;
using UnityEngine;

public class Blom : MonoBehaviour
{
    public float arcMagnitude = 1f;
    public Vector2 startPoint;
    public Vector2 endPoint;
    public float speed;
    public float rotateSpeed;
    public EventHandler eventHandler;

    public Transform shadow;
    private LineRenderer lineRenderer;
    float startingTime;
    float endingTime;
    Vector2[] curvepoints;

    void Start()
    {
        startPoint = transform.position;
        startingTime = Time.time;
        endingTime = startingTime + Vector2.Distance(startPoint, endPoint) / speed;

        var middle = Vector2.Lerp(startPoint, endPoint, .5f);
        float arcDirection = Vector2.SignedAngle(Vector2.up, endPoint - startPoint); // > 0 ? 1 : -1;
        var apex = middle + arcMagnitude * Vector2.Perpendicular(endPoint - startPoint) * -arcDirection;

        // Debug.Log($"length: {Vector2.Perpendicular(endPoint - startPoint).magnitude}");

        curvepoints = new Vector2[] {
            startPoint,
            apex,
            endPoint
        };

        // lineRenderer = GetComponent<LineRenderer>();
        // lineRenderer.startWidth = 0.1f;
        // lineRenderer.endWidth = 0.1f;
        // lineRenderer.positionCount = curvepoints.Length;
        // lineRenderer.SetPositions(new Vector3[] {
        //     startPoint,
        //     apex,
        //     endPoint
        // });
    }

    void Update()
    {
        float p = (Time.time - startingTime) / (endingTime - startingTime);

        var ptsCopy = (Vector2[])curvepoints.Clone();
        for (int iteration = 1; iteration < ptsCopy.Length; iteration++)
            for (int i = 0; i < ptsCopy.Length - iteration; i++)
                ptsCopy[i] = Vector2.Lerp(ptsCopy[i], ptsCopy[i + 1], p);

        var pt = ptsCopy[0];

        shadow.position = Vector2.Lerp(startPoint, endPoint, p);

        float newScale = Mathf.Clamp(1 / Vector2.Distance(pt, shadow.position), 0, 0.9f);
        shadow.localScale = new Vector3(newScale, newScale, 1);
        // Debug.Log($"newScale: {newScale}");

        shadow.Rotate(Vector3.forward, Time.deltaTime * rotateSpeed);

        transform.position = pt;
        transform.Rotate(Vector3.forward, Time.deltaTime * rotateSpeed);

        if (Vector2.Distance(transform.position, endPoint) < 0.1f)
        {
            eventHandler?.Invoke(this, EventArgs.Empty);
            Destroy(shadow.gameObject);
            Destroy(gameObject);
        }
    }
}
