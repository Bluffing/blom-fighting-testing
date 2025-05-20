using UnityEngine;

public enum MultiGunActionStates
{
    Idle,
    Move,
    // Attack,
    // Reload,
    // Dodge,
}

public class MultiGun : MonoBehaviour, IWeaponActions
{
    #region Properties

    public Sprite Rifle;
    public Sprite Sniper;
    // public Transform SpriteObject;

    #endregion Properties

    #region Helpers
    #endregion Helpers

    #region Constants

    public float WeaponDistFromPlayer = 0.5f;

    #endregion Constants

    #region Trackers

    private Transform Parent => transform.parent;
    private PlayerInfo PlayerInfo;
    public MultiGunActionStates CurrentState = MultiGunActionStates.Idle;
    private Actions PlayerActions;
    public Transform Firepoint;

    #endregion Trackers

    #region Unity Methods

    void Start()
    {
        PlayerInfo = GetComponentInParent<PlayerInfo>();
        PlayerActions = GetComponentInParent<Actions>();
    }

    void Update()
    {

    }

    #endregion Unity Methods

    #region Helper Methods

    private float DegToRad(float angle) =>
        angle * Mathf.Deg2Rad;

    private void UpdatePos(float angle) =>
        transform.position = Parent.position +
                    new Vector3(2 * WeaponDistFromPlayer * Mathf.Cos(DegToRad(angle + 90)),
                                2 * WeaponDistFromPlayer * Mathf.Sin(DegToRad(angle + 90)),
                                -0.1f);

    #endregion Helper Methods


    #region IWeaponActions

    public void MovementUpdate(bool debugMovement = false)
    {
        var move = Vector3.zero;

        switch (CurrentState)
        {
            case MultiGunActionStates.Idle:
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

                    if (move != Vector3.zero)
                    {
                        PlayerActions.arrow.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(move.y, move.x) * Mathf.Rad2Deg - 90);
                        Parent.position += PlayerInfo.Speed * Time.deltaTime * move.normalized;
                    }

                    UpdateGun();
                }
                break;
            default:
                break;
        }
    }

    public void ActionUpdate(bool debugMovement)
    {
        switch (CurrentState)
        {
            case MultiGunActionStates.Idle:
                {
                    if (Input.GetMouseButtonDown(0))
                    {
                        Shoot();
                    }
                }
                break;
            case MultiGunActionStates.Move:
                {
                    if (Input.GetMouseButtonUp(0))
                    {
                        // PlayerActions.arrow.gameObject.SetActive(true);
                        CurrentState = MultiGunActionStates.Idle;
                    }
                }
                break;
            default:
                break;
        }
    }

    #endregion IWeaponActions

    private void UpdateGun()
    {
        var mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        var mouseVector = mousePosition - Parent.position;

        var angle = Vector2.SignedAngle(Vector2.up, mouseVector);
        if (angle < 0)
            angle += 360;
        Parent.rotation = Quaternion.Euler(0, 0, angle);

        // 0 | 3
        // --+--
        // 1 | 2
        // int quadrant = (int)(angle / 90);
        // quadrant = 3;
        // float minAngle = quadrant * 90 + 30;
        // float maxAngle = quadrant * 90 + 60;

        mouseVector = mousePosition - transform.position;
        angle = Vector2.SignedAngle(Vector2.up, mouseVector);
        if (angle < 0)
            angle += 360;
        transform.rotation = Quaternion.Euler(0, 0, angle);
        // float heldAngle = Mathf.Clamp(angle, minAngle, maxAngle);

        // UpdatePos(heldAngle);
    }

    private void Shoot()
    {
        var mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        var mouseVector = mousePosition - Parent.position;
    }
}
