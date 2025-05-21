using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

// todo
// - [ ] sword drag
// - [ ] side stomp
// - [ ] shoulder sword (K) / block? (M?)
// - [ ] 2 inputs
// - [ ] perfect next input timing bonus?
// - [ ] momentum

public enum GreatswordActionsState
{
    Idle,
    Stomping,
    Swinging,
}
public enum GreatswordActionsMode
{
    Stomp,
    Swing,
}

public class GreatswordActions : MonoBehaviour, IWeaponActions
{

    #region Info

    public float SwordWeight = 100f;
    public float stompSpeed = 1f;

    public int startingAngle = 0;
    public int SWORD_STOMP_DAMAGE = 200;
    public int SWORD_SWING_DAMAGE = 100;

    #endregion Info

    #region GO

    // debug
    public float DEBUG_FLOAT = 0f;
    public GameObject DebugGO;
    public GameObject DebugAxisPrefab;
    public DebugAxisScript[] DebugAxisList = new DebugAxisScript[6];
    float Axis(float i) => (i * 60 + 30) % 360;

    // player
    private PlayerInfo playerInfo;
    private Actions playerActions;
    private Transform Parent => transform.parent;

    // helper
    private Transform HelperGO;
    private DPS dpsTracker;

    #endregion GO

    #region Stomp / Swing

    public float STOMP_DURATION = 1f;
    public float SWING_DURATION = 1f;

    // float attackStartingAngle = 0f;
    Vector3 attackStartVector = Vector3.zero;
    // Vector3 swingMidVector = Vector3.zero;
    Vector3 attackEndVector = Vector3.zero;
    float attackAngle = 0f;
    float startSwingAngle = 0f;
    float attackTimer = 0f;
    bool SwingClockwise = true;
    public float swingHeight = 0.5f;
    public float SwingAngle;
    List<Transform> SwingHit = new();

    #endregion Stomp / Swing

    #region Attack

    private Transform EnemiesParent;
    public CustomRectangleCollider MaxHitCollider;
    public CustomRectangleCollider MinHitCollider;

    #endregion Attack

    #region Properties

    public float WeaponDistFromPlayer = 0.5f;
    private Vector3 lastPos;
    private Vector3 moveVelo;
    private int CurrentSwordPos = 0;

    public Dictionary<KeyCode, int> KeyPressToAngle = new Dictionary<KeyCode, int>()
    {
        { KeyCode.U, 30 },
        { KeyCode.H, 90 },
        { KeyCode.N, 150 },
        { KeyCode.Comma, 210 },
        { KeyCode.L, 270 },
        { KeyCode.I, 330 },

        // { new KeyCode[] { KeyCode.U, KeyCode.I }, 0 },
        // { new KeyCode[] { KeyCode.I, KeyCode.K }, 0 },
    };

    public GreatswordActionsState CurrentState = GreatswordActionsState.Idle;
    public GreatswordActionsMode CurrentMode = GreatswordActionsMode.Stomp;

    #endregion Properties

    #region Init

    void Start()
    {
        playerInfo = Parent.GetComponent<PlayerInfo>();
        playerActions = Parent.GetComponent<Actions>();

        HelperGO = GameObject.FindGameObjectWithTag("Helper").transform;

        dpsTracker = GameObject
                        .FindGameObjectsWithTag("HUD")
                        .FirstOrDefault(go => go.GetComponent<DPS>() != null)
                        .GetComponent<DPS>();

        EnemiesParent = GameObject
                        .FindGameObjectWithTag("EnemiesParent")
                        .transform;

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

    float DegToRad(float d) => d * Mathf.Deg2Rad;
    float RadToDeg(float r) => r * Mathf.Rad2Deg;
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

        switch (CurrentState)
        {
            case GreatswordActionsState.Idle:
                {
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

                        UpdateGreatsword();
                        moveVelo = Parent.position - prevPos;

                        UpdateAxis();
                    }
                }
                break;
            default:
                break;
        }
    }

    public void ActionUpdate(bool debugMovement = false)
    {
        switch (CurrentState)
        {
            case GreatswordActionsState.Idle:
                {
                    void Action(int angle)
                    {
                        switch (CurrentMode)
                        {
                            case GreatswordActionsMode.Stomp:
                                StompSword(angle);
                                break;
                            case GreatswordActionsMode.Swing:
                                SwingSword(angle);
                                break;
                        }
                    }

                    if (Input.GetKeyDown(KeyCode.J))
                    {
                        CurrentMode =
                            CurrentMode == GreatswordActionsMode.Stomp ?
                                GreatswordActionsMode.Swing :
                                GreatswordActionsMode.Stomp;
                    }

                    if (Input.GetKeyDown(KeyCode.U))
                        Action(KeyPressToAngle[KeyCode.U]);
                    else if (Input.GetKeyDown(KeyCode.H))
                        Action(KeyPressToAngle[KeyCode.H]);
                    else if (Input.GetKeyDown(KeyCode.N))
                        Action(KeyPressToAngle[KeyCode.N]);
                    else if (Input.GetKeyDown(KeyCode.Comma))
                        Action(KeyPressToAngle[KeyCode.Comma]);
                    else if (Input.GetKeyDown(KeyCode.L))
                        Action(KeyPressToAngle[KeyCode.L]);
                    else if (Input.GetKeyDown(KeyCode.I))
                        Action(KeyPressToAngle[KeyCode.I]);
                    break;
                }
            case GreatswordActionsState.Stomping:
                StompSwordUpdate();
                break;
            case GreatswordActionsState.Swinging:
                SwingSwordUpdate();
                break;
        }

        // todo : buffer

        if (debugMovement) { }
    }

    #endregion IWeaponActions

    #region MoveGreatsword

    private void UpdateSwordPos(float angle)
    {
        var rot = Quaternion.Euler(new Vector3(0, 0, angle));
        var pos = Parent.position +
                    new Vector3(WeaponDistFromPlayer * Mathf.Cos(DegToRad(angle + 90)),
                                WeaponDistFromPlayer * Mathf.Sin(DegToRad(angle + 90)),
                                -0.1f);
        transform.SetPositionAndRotation(pos, rot);
    }
    private void UpdateGreatswordDrag(Vector3 move, Vector3 prevSwordPointPos)
    {
        var signedAngleNewPosSwordPoint = Vector2.SignedAngle(Vector2.up, prevSwordPointPos - Parent.position);

        var angleMoveSwordPoint = Vector2.SignedAngle(move, prevSwordPointPos - Parent.position);
        if (angleMoveSwordPoint < -150 || angleMoveSwordPoint > 150)
            return;

        UpdateSwordPos(signedAngleNewPosSwordPoint);

        // todo : drag
    }
    private void UpdateGreatswordTowards(Vector3 move, Vector3 prevSwordPointPos)
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
    private void UpdateGreatsword()
    {
        if (Parent.position == lastPos) return;

        var move = Parent.position - lastPos;
        var prevSwordPointPos = transform.position - move +
                                new Vector3(swordLength * Mathf.Cos(DegToRad(transform.rotation.eulerAngles.z + 90)),
                                            swordLength * Mathf.Sin(DegToRad(transform.rotation.eulerAngles.z + 90)));

        // drag sword or go towards sword
        var signedAngle = Vector2.SignedAngle(Parent.position - prevSwordPointPos, move);
        if (signedAngle < 90 && signedAngle > -90)
            UpdateGreatswordDrag(move, prevSwordPointPos);
        else
            UpdateGreatswordTowards(move, prevSwordPointPos);

        // draw drag line
        var newSwordPointPos = transform.position +
                                new Vector3(swordLength * Mathf.Cos(DegToRad(transform.rotation.eulerAngles.z + 90)),
                                            swordLength * Mathf.Sin(DegToRad(transform.rotation.eulerAngles.z + 90)));
        Debug.DrawLine(prevSwordPointPos, newSwordPointPos, Color.red, 1f);
        lastPos = Parent.position;
    }

    #endregion MoveGreatsword

    #region Stomp

    void UpdateAxis()
    {
        for (int i = 0; i < DebugAxisList.Length; i++)
            DebugAxisList[i].UnFocus();

        var newSwordPointPos = transform.position +
                                new Vector3(swordLength * Mathf.Cos(DegToRad(transform.rotation.eulerAngles.z + 90)),
                                            swordLength * Mathf.Sin(DegToRad(transform.rotation.eulerAngles.z + 90)));
        var swordAngle = Vector2.SignedAngle(moveVelo, newSwordPointPos - Parent.position) + 360;
        CurrentSwordPos = (int)(swordAngle / 60) % 6;
        DebugAxisList[CurrentSwordPos].Focus();
    }

    void StompSword(int angle)
    {
        CurrentState = GreatswordActionsState.Stomping;

        attackAngle = Vector2.SignedAngle(Vector2.up, moveVelo) + 360 + angle;

        attackStartVector = transform.position +
                                new Vector3(swordLength * Mathf.Cos(DegToRad(transform.rotation.eulerAngles.z + 90)),
                                            swordLength * Mathf.Sin(DegToRad(transform.rotation.eulerAngles.z + 90)));
        attackEndVector = Parent.position +
                            new Vector3((swordLength + WeaponDistFromPlayer) * Mathf.Cos(DegToRad(attackAngle + 90)),
                                        (swordLength + WeaponDistFromPlayer) * Mathf.Sin(DegToRad(attackAngle + 90)));

        if (Vector2.Distance(attackStartVector, attackEndVector) < 0.1f)
        {
            CurrentState = GreatswordActionsState.Idle;
            return;
        }

        var maxDist = 2 * (swordLength + WeaponDistFromPlayer);
        var ratio = Vector2.Distance(attackStartVector, attackEndVector) / maxDist;
        ratio = Mathf.Clamp(ratio, 0, 1);
        attackTimer = STOMP_DURATION * ratio;

        // gizmosPoints[1] = attackStartVector;
        // gizmosPoints[2] = attackEndVector;
    }
    void StompSwordUpdate()
    {
        attackTimer -= Time.deltaTime;

        var stompProgress = 1 - attackTimer / STOMP_DURATION;
        if (attackTimer < 0)
        {
            stompProgress = 1f;
            CurrentState = GreatswordActionsState.Idle;
        }
        stompProgress *= stompProgress * stompProgress; // y = x^3

        var p1 = attackStartVector;            // const
        var p2 = attackEndVector;              // const
        var r = Vector2.Distance(p1, p2) / 2; // const
        var midpoint = (p1 + p2) / 2;         // const

        var newPoint = Vector3.Lerp(p1, p2, stompProgress);
        var x = Vector2.Distance(newPoint, midpoint);
        var y = Mathf.Sqrt(r * r - x * x);

        newPoint.z = transform.position.z - y;
        gizmosPoints[0] = newPoint;

        var newPos = Parent.position + (newPoint - Parent.position).normalized * WeaponDistFromPlayer;
        var rot = Quaternion.AngleAxis(Vector3.Angle(Vector3.up, newPoint - Parent.position), Vector3.Cross(Vector3.up, newPoint - Parent.position));
        transform.SetPositionAndRotation(newPos, rot);

        if (stompProgress == 1f)
            EndSwordStomp();

        // UpdateSwordPos(newSwordAngle);
        UpdateAxis();
    }
    void EndSwordStomp()
    {
        UpdateSwordPos(attackAngle);

        var maxDist = 2 * (swordLength + WeaponDistFromPlayer);
        var dmg = Mathf.Clamp((attackEndVector - attackStartVector).magnitude / maxDist, 0, 1);
        dmg *= dmg;
        dmg *= SWORD_STOMP_DAMAGE;

        foreach (Transform enemy in EnemiesParent)
        {
            if (!enemy.TryGetComponent<CustomRectangleCollider>(out var enemyCollider))
            {
                Debug.LogError($"enemy {enemy.name} has no collider");
                continue;
            }

            if (enemyCollider.IsTouching(MaxHitCollider))
            {
                if (!enemy.TryGetComponent<EnemyInfo>(out var enemyInfo))
                {
                    Debug.LogError($"enemy {enemy.name} has no EnemyInfo");
                    continue;
                }
                enemyInfo.TakeDamage(dmg);
                dpsTracker.AddDmg(dmg);
            }
            else if (enemyCollider.IsTouching(MinHitCollider))
            {
                if (!enemy.TryGetComponent<EnemyInfo>(out var enemyInfo))
                {
                    Debug.LogError($"enemy {enemy.name} has no EnemyInfo");
                    continue;
                }
                enemyInfo.TakeDamage(dmg * 0.6f);
                dpsTracker.AddDmg(dmg);
            }
        }
    }

    IEnumerator goBackToIdleIn(float time)
    {
        yield return new WaitForSeconds(time);// Wait for one second
        CurrentState = GreatswordActionsState.Idle;
    }

    #endregion Stomp

    #region Swing

    void SwingSword(int angle)
    {
        CurrentState = GreatswordActionsState.Swinging;

        var playerAngle = Vector2.SignedAngle(Vector2.up, moveVelo);
        attackAngle = playerAngle + angle;
        if (attackAngle < 0)
            attackAngle += 360;
        else if (attackAngle > 360)
            attackAngle -= 360;

        // 0 | 3
        // -- --
        // 1 | 2
        attackStartVector = transform.position +
                                new Vector3(swordLength * Mathf.Cos(DegToRad(transform.rotation.eulerAngles.z + 90)),
                                            swordLength * Mathf.Sin(DegToRad(transform.rotation.eulerAngles.z + 90)));
        attackEndVector = Parent.position +
                            new Vector3((swordLength + WeaponDistFromPlayer) * Mathf.Cos(DegToRad(attackAngle + 90)),
                                        (swordLength + WeaponDistFromPlayer) * Mathf.Sin(DegToRad(attackAngle + 90)));
        if (Vector2.Distance(attackStartVector, attackEndVector) < 0.1f)
        {
            CurrentState = GreatswordActionsState.Idle;
            return;
        }

        // mid point
        {
            startSwingAngle = transform.rotation.eulerAngles.z - playerAngle;
            if (startSwingAngle < 0)
                startSwingAngle += 360;

            bool startRight = startSwingAngle >= 180;
            bool endRight = angle >= 180;

            if (startRight && !endRight) // don't go behind back
            {
                SwingClockwise = false;
                SwingAngle = 360 - startSwingAngle + angle;
            }
            else if (!(!startRight && endRight) && startSwingAngle < angle)
            {
                SwingClockwise = false;
                SwingAngle = angle - startSwingAngle;
            }
            else
            {
                SwingClockwise = true;
                SwingAngle = startSwingAngle - angle;
                if (!startRight && endRight)
                    SwingAngle += 360;
            }
        }

        var ratio = SwingAngle / 360f;
        ratio = Mathf.Clamp(ratio, 0, 1);
        attackTimer = SWING_DURATION * ratio;

        SwingHit.Clear();

        gizmosPoints[1] = attackStartVector;
        gizmosPoints[2] = attackEndVector;
    }
    void SwingSwordUpdate()
    {
        attackTimer -= Time.deltaTime;

        var swingProgress = 1 - (attackTimer / SWING_DURATION);
        if (attackTimer < 0)
        {
            swingProgress = 1f;
            CurrentState = GreatswordActionsState.Idle;
        }
        swingProgress = Mathf.Pow(swingProgress, 7); // y = x^7
        swingProgress *= SwingClockwise ? -1 : 1;

        var newAngle = startSwingAngle + swingProgress * SwingAngle;

        // 0 - up - 0
        var swingUpAngle = Mathf.Sin(swingProgress * Mathf.PI) * 30 * (SwingClockwise ? -1 : 1);
        var swingUpAngleX = -Mathf.Cos(DegToRad(newAngle)) * swingUpAngle;
        var swingUpAngleY = -Mathf.Sin(DegToRad(newAngle)) * swingUpAngle;

        var rot = Quaternion.Euler(new Vector3(swingUpAngleX, swingUpAngleY, newAngle));
        var pos = Parent.position +
                    new Vector3(WeaponDistFromPlayer * Mathf.Cos(DegToRad(newAngle + 90)),
                                WeaponDistFromPlayer * Mathf.Sin(DegToRad(newAngle + 90)),
                                -0.1f);
        transform.SetPositionAndRotation(pos, rot);

        SwingAttack();

        // UpdateSwordPos(newAngle);
        UpdateAxis();
    }
    void SwingAttack()
    {
        var swingProgress = 1 - (attackTimer / SWING_DURATION);
        if (attackTimer < 0)
        {
            swingProgress = 1f;
            CurrentState = GreatswordActionsState.Idle;
        }
        swingProgress = Mathf.Pow(swingProgress, 7); // y = x^7

        const float MAX_DMG_FROM_SWING = 0.5f; // after 50% of the swing = 100% dmg
        swingProgress = Mathf.Clamp(swingProgress, 0, MAX_DMG_FROM_SWING) * 1 / MAX_DMG_FROM_SWING;

        var dmg = swingProgress * SWORD_STOMP_DAMAGE;

        foreach (Transform enemy in EnemiesParent)
        {
            if (SwingHit.Contains(enemy)) // already hit
                continue;

            if (!enemy.TryGetComponent<CustomRectangleCollider>(out var enemyCollider))
            {
                SwingHit.Add(enemy);
                Debug.LogError($"enemy {enemy.name} has no collider");
                continue;
            }

            if (enemyCollider.IsTouching(MaxHitCollider))
            {
                SwingHit.Add(enemy);
                if (!enemy.TryGetComponent<EnemyInfo>(out var enemyInfo))
                {
                    Debug.LogError($"enemy {enemy.name} has no EnemyInfo");
                    continue;
                }
                enemyInfo.TakeDamage(dmg);
                dpsTracker.AddDmg(dmg);
            }
            else if (enemyCollider.IsTouching(MinHitCollider))
            {
                SwingHit.Add(enemy);
                if (!enemy.TryGetComponent<EnemyInfo>(out var enemyInfo))
                {
                    Debug.LogError($"enemy {enemy.name} has no EnemyInfo");
                    continue;
                }
                enemyInfo.TakeDamage(dmg * 0.6f);
                dpsTracker.AddDmg(dmg);
            }
        }
    }

    #endregion Swing

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

        // var c = Color.white;
        // c.a = 0.5f;
        // Gizmos.color = c;
        // Gizmos.DrawSphere(Parent.position, swordLength + WeaponDistFromPlayer);
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