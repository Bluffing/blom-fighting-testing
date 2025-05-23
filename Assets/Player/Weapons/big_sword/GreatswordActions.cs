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
// - [ ] dash

public enum GreatswordActionsState
{
    Idle,
    Stomping,
    Swinging,
    SwingingAcceptInput,
    SwingingEnd,
}
public enum GreatswordActionsMode
{
    Stomp,
    Swing,
}

/// <summary>
/// GreatswordActions
/// 
/// Mode:
/// - Stomp
/// - Swing
/// 
/// Buffers:
/// - SwingMovementHorizontalBuffer
/// - SwingMovementVerticalBuffer
/// - SwingActionBuffer
/// - SwingActionChangeMode
/// </summary>
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

    Vector3 attackStartVector = Vector3.zero;
    Vector3 attackEndVector = Vector3.zero;
    float finalAngle = 0f;
    float startSwingAngle = 0f;
    float attackTimer = 0f;
    bool SwingClockwise = true;
    float SwingAngle = 0f;
    float SwingProgress = 0f;
    List<Transform> SwingHit = new();
    public float CurrentMomentum = 0f;
    public float MaxMomentum = 360; // degrees / second
    public float SwingStartMaxMomentum = 180; // degrees / second
    public float SwingMomentumAccelerationSpeed;
    private float SwingPlayerStartingAngle = 0f;
    private float SwingMaxMomentum(float swingProgress) => swingProgress < 45 ? SwingStartMaxMomentum : MaxMomentum;
    // private (float swingAmount, float ratio)[] SwingMomentum = {
    //     (45, 0.25f),
    //     (90, 0.5f),
    // };
    // private float SwingMaxMomentum(float swingProgress)
    // {
    //     for (int i = 0; i < SwingMomentum.Length; i++)
    //         if (swingProgress < SwingMomentum[i].swingAmount)
    //             return SwingMomentum[i].MaxMomentum * MaxMomentum;
    //     return MaxMomentum;
    // }

    // after img
    public GameObject SwingAfterImageEffect;

    // buffers
    public KeyCode SwingMovementHorizontalBuffer = KeyCode.None;
    public KeyCode SwingMovementVerticalBuffer = KeyCode.None;
    public KeyCode SwingActionBuffer;
    public bool SwingActionChangeMode = false;

    #endregion Stomp / Swing

    #region Attack

    private Transform EnemiesParent;
    public CustomRectangleCollider MaxHitCollider;
    public CustomRectangleCollider MinHitCollider;

    #endregion Attack

    #region Properties

    public float SwordDistFromPlayer = 0.5f;
    public float SwordLength = 0.5f;
    float SwordEnd() => SwordDistFromPlayer + SwordLength;

    private Vector3 lastPos;
    private Vector3 moveVelo = new Vector2(0, 1);
    private float PlayerAngle = 0;
    private int CurrentSwordPos = 0;

    public Dictionary<KeyCode, int> KeyPressToAngle = new()
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
    void ChangeDebugColor(Color c)
    {
        for (int i = 0; i < 6; i++)
            DebugAxisList[i].transform.GetChild(0).GetComponent<SpriteRenderer>().color = c;
    }
    void ResetDebugColor()
    {
        for (int i = 0; i < 6; i++)
            DebugAxisList[i].UnFocus();
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
            case GreatswordActionsState.SwingingAcceptInput:
                {
                    if (Input.GetKeyDown(KeyCode.W))
                        SwingMovementVerticalBuffer = KeyCode.W;
                    if (Input.GetKeyDown(KeyCode.S))
                        SwingMovementVerticalBuffer = KeyCode.S;
                    if (Input.GetKeyDown(KeyCode.D))
                        SwingMovementHorizontalBuffer = KeyCode.D;
                    if (Input.GetKeyDown(KeyCode.A))
                        SwingMovementHorizontalBuffer = KeyCode.A;
                }
                break;
            case GreatswordActionsState.SwingingEnd:
                {
                    if (SwingMovementVerticalBuffer != KeyCode.None)
                    {
                        if (SwingMovementVerticalBuffer == KeyCode.W)
                            move += new Vector3(0, 1, 0);
                        else if (SwingMovementVerticalBuffer == KeyCode.S)
                            move += new Vector3(0, -1, 0);
                    }

                    if (SwingMovementHorizontalBuffer != KeyCode.None)
                    {
                        if (SwingMovementHorizontalBuffer == KeyCode.D)
                            move += new Vector3(1, 0, 0);
                        else if (SwingMovementHorizontalBuffer == KeyCode.A)
                            move += new Vector3(-1, 0, 0);
                    }

                    var prevPos = Parent.position;
                    if (move != Vector3.zero)
                    {
                        if (SwingActionBuffer == KeyCode.None)
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

        PlayerAngle = playerActions.arrow.eulerAngles.z;
    }

    public void ActionUpdate(bool debugMovement = false)
    {
        switch (CurrentState)
        {
            case GreatswordActionsState.Idle:
                {
                    void Action(int angle)
                    {
                        if (CurrentMode == GreatswordActionsMode.Stomp)
                            StompSword(angle);
                        else
                            SwingSword(angle);
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
            case GreatswordActionsState.SwingingAcceptInput:
                {
                    if (Input.GetKeyDown(KeyCode.J))
                        SwingActionChangeMode = !SwingActionChangeMode;

                    if (Input.GetKeyDown(KeyCode.U))
                        SwingActionBuffer = KeyCode.U;
                    else if (Input.GetKeyDown(KeyCode.H))
                        SwingActionBuffer = KeyCode.H;
                    else if (Input.GetKeyDown(KeyCode.N))
                        SwingActionBuffer = KeyCode.N;
                    else if (Input.GetKeyDown(KeyCode.Comma))
                        SwingActionBuffer = KeyCode.Comma;
                    else if (Input.GetKeyDown(KeyCode.L))
                        SwingActionBuffer = KeyCode.L;
                    else if (Input.GetKeyDown(KeyCode.I))
                        SwingActionBuffer = KeyCode.I;

                    SwingSwordUpdate();
                    break;
                }
            case GreatswordActionsState.SwingingEnd:
                {
                    if (SwingActionChangeMode)
                        CurrentMode =
                            CurrentMode == GreatswordActionsMode.Stomp ?
                                GreatswordActionsMode.Swing :
                                GreatswordActionsMode.Stomp;

                    if (SwingActionBuffer != KeyCode.None)
                    {
                        if (CurrentMode == GreatswordActionsMode.Stomp)
                            StompSword(KeyPressToAngle[SwingActionBuffer]);
                        else
                            SwingSword(KeyPressToAngle[SwingActionBuffer]);
                        SwingActionBuffer = KeyCode.None;
                    }
                    else
                    {
                        CurrentMomentum = 0f;
                        CurrentState = GreatswordActionsState.Idle;
                    }

                    ResetSwingBuffers();
                    break;
                }
        }

        if (debugMovement) { }
    }

    #endregion IWeaponActions

    #region MoveGreatsword

    private void UpdateSwordPos(float angle)
    {
        var rot = Quaternion.Euler(new Vector3(0, 0, angle));
        var pos = Parent.position +
                    new Vector3(SwordDistFromPlayer * Mathf.Cos(DegToRad(angle + 90)),
                                SwordDistFromPlayer * Mathf.Sin(DegToRad(angle + 90)),
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
        var fullCircumference = 2 * Mathf.PI * SwordEnd();
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
                                new Vector3(SwordLength * Mathf.Cos(DegToRad(transform.rotation.eulerAngles.z + 90)),
                                            SwordLength * Mathf.Sin(DegToRad(transform.rotation.eulerAngles.z + 90)));

        // drag sword or go towards sword
        var signedAngle = Vector2.SignedAngle(Parent.position - prevSwordPointPos, move);
        if (signedAngle < 90 && signedAngle > -90)
            UpdateGreatswordDrag(move, prevSwordPointPos);
        else
            UpdateGreatswordTowards(move, prevSwordPointPos);

        // draw drag line
        var newSwordPointPos = transform.position +
                                new Vector3(SwordLength * Mathf.Cos(DegToRad(transform.rotation.eulerAngles.z + 90)),
                                            SwordLength * Mathf.Sin(DegToRad(transform.rotation.eulerAngles.z + 90)));
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
                                new Vector3(SwordLength * Mathf.Cos(DegToRad(transform.rotation.eulerAngles.z + 90)),
                                            SwordLength * Mathf.Sin(DegToRad(transform.rotation.eulerAngles.z + 90)));
        var swordAngle = Vector2.SignedAngle(moveVelo, newSwordPointPos - Parent.position) + 360;
        CurrentSwordPos = (int)(swordAngle / 60) % 6;
        DebugAxisList[CurrentSwordPos].Focus();
    }

    void StompSword(int angle)
    {
        CurrentState = GreatswordActionsState.Stomping;

        finalAngle = Vector2.SignedAngle(Vector2.up, moveVelo) + 360 + angle;

        attackStartVector = transform.position +
                                new Vector3(SwordLength * Mathf.Cos(DegToRad(transform.rotation.eulerAngles.z + 90)),
                                            SwordLength * Mathf.Sin(DegToRad(transform.rotation.eulerAngles.z + 90)));
        attackEndVector = Parent.position +
                            new Vector3(SwordEnd() * Mathf.Cos(DegToRad(finalAngle + 90)),
                                        SwordEnd() * Mathf.Sin(DegToRad(finalAngle + 90)));

        if (Vector2.Distance(attackStartVector, attackEndVector) < 0.1f)
        {
            CurrentState = GreatswordActionsState.Idle;
            return;
        }

        var maxDist = 2 * SwordEnd();
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

        var newPos = Parent.position + (newPoint - Parent.position).normalized * SwordDistFromPlayer;
        var rot = Quaternion.AngleAxis(Vector3.Angle(Vector3.up, newPoint - Parent.position), Vector3.Cross(Vector3.up, newPoint - Parent.position));
        transform.SetPositionAndRotation(newPos, rot);

        if (stompProgress == 1f)
            EndSwordStomp();

        // UpdateSwordPos(newSwordAngle);
        UpdateAxis();
    }
    void EndSwordStomp()
    {
        UpdateSwordPos(finalAngle);

        var maxDist = 2 * SwordEnd();
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

        SwingPlayerStartingAngle = PlayerAngle;
        finalAngle = PlayerAngle + angle;
        if (finalAngle < 0)
            finalAngle += 360;
        else if (finalAngle > 360)
            finalAngle -= 360;

        attackStartVector = transform.position +
                                new Vector3(SwordLength * Mathf.Cos(DegToRad(transform.rotation.eulerAngles.z + 90)),
                                            SwordLength * Mathf.Sin(DegToRad(transform.rotation.eulerAngles.z + 90)));
        attackEndVector = Parent.position +
                            new Vector3(SwordEnd() * Mathf.Cos(DegToRad(finalAngle + 90)),
                                        SwordEnd() * Mathf.Sin(DegToRad(finalAngle + 90)));
        if (Vector2.Distance(attackStartVector, attackEndVector) < 0.1f)
        {
            CurrentState = GreatswordActionsState.Idle;
            CurrentMomentum = 0f;
            return;
        }

        // mid point
        {
            startSwingAngle = transform.rotation.eulerAngles.z - PlayerAngle;
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

        SwingHit.Clear();
        SwingProgress = 0f;

        gizmosPoints[1] = attackStartVector;
        gizmosPoints[2] = attackEndVector;
    }
    void SwingSwordUpdate()
    {
        if (CurrentMomentum < 0 != SwingClockwise)
            CurrentMomentum += Time.deltaTime * 4 * MaxMomentum * (SwingClockwise ? -1 : 1);
        else if (Mathf.Abs(CurrentMomentum) < SwingMaxMomentum(SwingProgress * SwingAngle))
        {
            // rotate player towards sword
            if (CurrentMomentum == 0f)
            {
                var a = (PlayerAngle - transform.rotation.eulerAngles.z + 360) % 360;
                if (a > 45 && a < 315)
                {
                    var MoveAmount = Time.deltaTime * (a < 180 ? -720 : 720);
                    playerActions.arrow.rotation = Quaternion.Euler(new Vector3(0, 0, playerActions.arrow.rotation.eulerAngles.z + MoveAmount));
                    return;
                }
            }

            // momentum : degrees / second
            CurrentMomentum += Time.deltaTime * SwingMomentumAccelerationSpeed * (SwingClockwise ? -1 : 1);
            CurrentMomentum = Mathf.Clamp(CurrentMomentum, -MaxMomentum, MaxMomentum);
        }

        var frameMomentum = CurrentMomentum * Time.deltaTime;
        SwingProgress += Mathf.Abs(frameMomentum / SwingAngle) * (frameMomentum < 0 != SwingClockwise ? -1 : 1);

        var newAngle = transform.rotation.eulerAngles.z + frameMomentum; // (SwingClockwise ? -frameMomentum : frameMomentum);

        if (SwingProgress >= 1f)
        {
            SwingProgress = 1f;
            newAngle = finalAngle;
        }

        // 0 - up - 0
        var swingUpAngle = Mathf.Sin(SwingProgress * Mathf.PI) * 30;
        var swingUpAngleX = -Mathf.Cos(DegToRad(newAngle)) * swingUpAngle;
        var swingUpAngleY = -Mathf.Sin(DegToRad(newAngle)) * swingUpAngle;

        // debug line
        {
            var endPoint = Parent.position +
                            new Vector3(SwordEnd() * Mathf.Cos(DegToRad(transform.eulerAngles.z + 90)),
                                        SwordEnd() * Mathf.Sin(DegToRad(transform.eulerAngles.z + 90)));
            var newPoint = Parent.position +
                            new Vector3(SwordEnd() * Mathf.Cos(DegToRad(newAngle + 90)),
                                        SwordEnd() * Mathf.Sin(DegToRad(newAngle + 90)));

            Debug.DrawLine(endPoint, newPoint, Color.red, 1f);
        }

        // after image
        if (Mathf.Abs(CurrentMomentum) > MaxMomentum / 4)
        {
            var afterImage = Instantiate(SwingAfterImageEffect, transform.position + new Vector3(0, 0, 0.1f), Quaternion.identity, Parent);
            // afterImage.transform.localScale = HelperMethods.FullScale(transform);
            afterImage.transform.rotation = transform.rotation;
            Destroy(afterImage, 0.2f);
        }

        var rot = Quaternion.Euler(new Vector3(swingUpAngleX, swingUpAngleY, newAngle));
        var pos = Parent.position +
                    new Vector3(SwordDistFromPlayer * Mathf.Cos(DegToRad(newAngle + 90)),
                                SwordDistFromPlayer * Mathf.Sin(DegToRad(newAngle + 90)),
                                -0.1f);
        transform.SetPositionAndRotation(pos, rot);

        SwingAttackCollide();

        UpdateAxis();
        // rotate player with sword
        if (SwingProgress < 0.5f)
            playerActions.arrow.rotation = Quaternion.Euler(new Vector3(0, 0, newAngle + (SwingClockwise ? -45 : 45)));
        else if (SwingProgress == 1f)
        {
            playerActions.arrow.rotation = Quaternion.Euler(new Vector3(0, 0, SwingPlayerStartingAngle));
            CurrentState = GreatswordActionsState.SwingingEnd;
            ResetDebugColor();
        }
        else
        {
            var relativeAngle = (PlayerAngle - transform.rotation.eulerAngles.z + 360) % 360;
            if (relativeAngle > 45 && relativeAngle < 315)
                playerActions.arrow.rotation = Quaternion.Euler(new Vector3(0, 0, SwingClockwise ? 45 : -45));

            CurrentState = GreatswordActionsState.SwingingAcceptInput;
            ChangeDebugColor(new(1, 0, 0, 0.5f));
        }
    }
    void SwingAttackCollide()
    {
        var dmgRatio = Mathf.Abs(CurrentMomentum) / MaxMomentum;
        if (dmgRatio < 0.25f) return;

        var dmg = dmgRatio * SWORD_SWING_DAMAGE;

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
                enemyInfo.Knockback(enemy.position - Parent.position, 2f * dmgRatio, 0.1f);
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
                enemyInfo.Knockback(enemy.position - Parent.position, 1.2f * dmgRatio, 0.1f);
                dpsTracker.AddDmg(dmg);
            }
        }
    }
    void ResetSwingBuffers()
    {
        SwingMovementHorizontalBuffer = KeyCode.None;
        SwingMovementVerticalBuffer = KeyCode.None;
        SwingActionBuffer = KeyCode.None;
        SwingActionChangeMode = false;
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
        // Gizmos.DrawSphere(Parent.position, SwordEnd());
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