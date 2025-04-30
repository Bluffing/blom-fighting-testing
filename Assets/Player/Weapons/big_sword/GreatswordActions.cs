using System;
using UnityEditor;
using UnityEngine;

public class GreatswordActions : MonoBehaviour, IWeaponActions
{

    #region Info

    public float SwordWeight = 100f;
    public float swingSpeed = 1f;

    [Range(0, 360)]
    public int startingAngle = 0;

    #endregion Info

    #region GO

    // debug
    public GameObject DebugGO;
    public GameObject DebugAxisPrefab;
    public DebugAxisScript[] DebugAxisList = new DebugAxisScript[6];
    float Axis(float i) => (i * 60 + 30) % 360;

    // player
    private PlayerInfo playerInfo;
    private Actions playerActions;
    private Transform Parent;

    #endregion GO

    #region Properties

    public float WeaponDistFromPlayer = 0.5f;
    private Vector3 lastPos;
    private Vector3 moveVelo;

    #endregion Properties

    #region Init

    void Start()
    {
        Parent = transform.parent;
        playerInfo = Parent.GetComponent<PlayerInfo>();
        playerActions = Parent.GetComponent<Actions>();

        lastPos = Parent.position;
        Init();
    }

    public void Init()
    {
        GenDebug();
        UpdateSwordPos(startingAngle);
    }

    public void GenDebug()
    {
        CleanChildren(DebugGO.transform);

        for (int i = 0; i < 6; i++)
        {
            var go = Instantiate(DebugAxisPrefab, DebugGO.transform);
            go.transform.localRotation = Quaternion.Euler(new Vector3(0, 0, Axis(i)));
            go.name = $"DebugAxis_{i}";
            DebugAxisList[i] = go.GetComponent<DebugAxisScript>();
        }
    }

    #endregion Init

    #region Helpers

    float DegToRad(float d) => d * 0.0174533f;
    float RadToDeg(float r) => r * 57.29578f;
    void CleanChildrenEditor(Transform t)
    {
        while (t.childCount > 0)
            DestroyImmediate(t.GetChild(0).gameObject);
    }
    void CleanChildrenPlaying(Transform t)
    {
        for (int i = t.childCount - 1; i >= 0; i--)
            Destroy(t.GetChild(i).gameObject);
    }
    public void CleanChildren(Transform t)
    {
        // todo : remove
        if (!Application.isPlaying)
            CleanChildrenEditor(t);
        else
            CleanChildrenPlaying(t);
    }

    #endregion Helpers

    #region IWeaponActions

    public void MovementUpdate(bool debugMovement = false)
    {
        var move = Vector3.zero;

        if (debugMovement)
        {
            if (Input.GetKeyDown(KeyCode.W))
                move += new Vector3(0, 1, 0);
            if (Input.GetKeyDown(KeyCode.S))
                move += new Vector3(0, -1, 0);
            if (Input.GetKeyDown(KeyCode.D))
                move += new Vector3(1, 0, 0);
            if (Input.GetKeyDown(KeyCode.A))
                move += new Vector3(-1, 0, 0);
        }
        else
        {
            if (Input.GetKey(KeyCode.W))
                move += new Vector3(0, 1, 0);
            if (Input.GetKey(KeyCode.S))
                move += new Vector3(0, -1, 0);
            if (Input.GetKey(KeyCode.D))
                move += new Vector3(1, 0, 0);
            if (Input.GetKey(KeyCode.A))
                move += new Vector3(-1, 0, 0);
        }

        var prevPos = Parent.position;
        if (move != Vector3.zero)
        {
            Parent.position += playerInfo.Speed * Time.deltaTime * move.normalized;

            playerActions.debugAxis.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(move.y, move.x) * Mathf.Rad2Deg - 90);
            playerActions.arrow.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(move.y, move.x) * Mathf.Rad2Deg - 90);

            UpdateGreatswordV3();
            moveVelo = Parent.position - prevPos;

            UpdateAxis();
        }
    }

    #endregion IWeaponActions

    #region MoveGreatsword

    private void UpdateSwordPos(float angle)
    {
        transform.rotation = Quaternion.Euler(new Vector3(0, 0, angle));
        transform.position = Parent.position +
                    new Vector3(WeaponDistFromPlayer * Mathf.Cos(DegToRad(transform.rotation.eulerAngles.z + 90)),
                                WeaponDistFromPlayer * Mathf.Sin(DegToRad(transform.rotation.eulerAngles.z + 90)));
    }

    private void UpdateGreatswordV3Drag(Vector3 move, Vector3 prevSwordPointPos)
    {
        var signedAngleNewPosSwordPoint = Vector2.SignedAngle(Vector2.up, prevSwordPointPos - Parent.position);

        var angleMoveSwordPoint = Vector2.SignedAngle(move, prevSwordPointPos - Parent.position);
        if (angleMoveSwordPoint < -150 || angleMoveSwordPoint > 150)
            return;

        UpdateSwordPos(signedAngleNewPosSwordPoint);

        // todo : drag
    }
    private void UpdateGreatswordV3Towards(Vector3 move, Vector3 prevSwordPointPos)
    {
        var prevPos = Parent.position - move;

        // move player
        var playerNewMoveDistance = playerInfo.Weight / (SwordWeight + playerInfo.Weight) * move.magnitude;
        Parent.position = prevPos + playerNewMoveDistance * move;

        // move sword
        var swordNewMoveDistance = SwordWeight / (SwordWeight + playerInfo.Weight) * move.magnitude;
        var fullCircumference = 2 * Mathf.PI * (WeaponDistFromPlayer + swordLength);
        var sign = Vector2.SignedAngle(prevSwordPointPos - prevPos, move) > 0 ? -1 : 1;
        var newAngle = swordNewMoveDistance / fullCircumference * 360 * sign +
                       transform.rotation.eulerAngles.z;

        UpdateSwordPos(newAngle);

        // todo : drag
    }
    private void UpdateGreatswordV3()
    {
        if (Parent.position == lastPos) return;

        var move = Parent.position - lastPos;
        var prevSwordPointPos = transform.position - move +
                                new Vector3(swordLength * Mathf.Cos(DegToRad(transform.rotation.eulerAngles.z + 90)),
                                            swordLength * Mathf.Sin(DegToRad(transform.rotation.eulerAngles.z + 90)));

        // drag sword or go towards sword
        var signedAngle = Vector2.SignedAngle(Parent.position - prevSwordPointPos, move);
        if (signedAngle < 90 && signedAngle > -90)
            UpdateGreatswordV3Drag(move, prevSwordPointPos);
        else
            UpdateGreatswordV3Towards(move, prevSwordPointPos);

        // draw drag line
        var newSwordPointPos = transform.position +
                                new Vector3(0.5f * Mathf.Cos(DegToRad(transform.rotation.eulerAngles.z + 90)),
                                            0.5f * Mathf.Sin(DegToRad(transform.rotation.eulerAngles.z + 90)));
        Debug.DrawLine(prevSwordPointPos, newSwordPointPos, Color.red, 1f);
        lastPos = Parent.position;
    }

    #endregion MoveGreatsword

    #region MoveAxis

    void UpdateAxis()
    {
        // currently not usefull but if end point stops being aligned with handle it will be usefull 
        // var newSwordPointPos = transform.position +
        //                         new Vector3(0.5f * Mathf.Cos(DegToRad(transform.rotation.eulerAngles.z + 90)),
        //                                     0.5f * Mathf.Sin(DegToRad(transform.rotation.eulerAngles.z + 90)));

        for (int i = 0; i < DebugAxisList.Length; i++)
            DebugAxisList[i].UnFocus();

        Debug.Log($"bleak : {Vector2.SignedAngle(Vector2.right, moveVelo)}");

        // int swordAxis = (((int)(Vector2.SignedAngle(Vector2.right, moveVelo) - transform.rotation.eulerAngles.z) / 60) + 6) % 6;
        // Debug.Log($"swordAxis : {swordAxis}");
        // DebugAxisList[swordAxis].Focus();
    }

    #endregion MoveAxis

    #region Debug

    public void Test()
    {
        var center = new Vector2(0, 0);
        var r = 10f;
        var starting = new Vector2(0, -10);
        var step = new Vector2(-1, 1);

        // var bleak = MoveAroundCircle(center, r, starting, step);
        // Debug.Log($"bleak : {bleak}");

        var t1 = new Vector2(0, 1);

        Debug.Log($"uno :    {Vector2.SignedAngle(t1, new Vector2(0, 1))}");
        Debug.Log($"dos :    {Vector2.SignedAngle(t1, new Vector2(1, 0))}");
        Debug.Log($"tres :   {Vector2.SignedAngle(t1, new Vector2(-1, 0))}");
        Debug.Log($"cuatro : {Vector2.SignedAngle(t1, new Vector2(0, -1))}");
    }

    const float swordLength = 0.5f;

    Color[] gizmosColors = {
        Color.red,
        Color.green,
        Color.blue,
        Color.yellow,
        Color.cyan,
        Color.magenta
    };
    Vector3[] gizmosPoints = {
        Vector3.zero,
        Vector3.zero,
        Vector3.zero,
        Vector3.zero,
        Vector3.zero,
        Vector3.zero,
    };
    (Vector2 pt1, Vector2 pt2)[] gizmosLines = {
        (Vector2.zero, Vector2.zero),
        (Vector2.zero, Vector2.zero),
        (Vector2.zero, Vector2.zero),
        (Vector2.zero, Vector2.zero),
        (Vector2.zero, Vector2.zero),
        (Vector2.zero, Vector2.zero),
    };
    void OnDrawGizmos()
    {
        for (int i = 0; i < gizmosPoints.Length; i++)
        {
            Gizmos.color = gizmosColors[i % gizmosColors.Length];
            Gizmos.DrawSphere(gizmosPoints[i], 0.02f);
        }
        for (int i = 0; i < gizmosLines.Length; i++)
        {
            Gizmos.color = gizmosColors[i % gizmosColors.Length];
            Gizmos.DrawLine(gizmosLines[i].pt1, gizmosLines[i].pt2);
        }
    }

    #endregion Debug
}

[CustomEditor(typeof(GreatswordActions))]
public class GreatswordActionsEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        GreatswordActions script = (GreatswordActions)target;

        if (GUILayout.Button("init"))
        {
            script.Init();
        }
        if (GUILayout.Button("gen debug"))
        {
            script.GenDebug();
        }
        if (GUILayout.Button("test"))
        {
            script.Test();
        }
    }
}