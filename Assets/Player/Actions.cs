using UnityEngine;

public class Actions : MonoBehaviour
{
    [Range(1, 300)]
    public int throwRadius = 10;
    public bool waveBounce = false;

    // todo remove
    public Map map;

    public Transform debugAxis;
    public Transform arrow;
    public bool debugMovement = false;

    #region Weapon

    public WeaponType currentWeapon = WeaponType.None;
    public MonoBehaviour weaponActions;
    private IWeaponActions WeaponActions;

    #endregion Weapon


    #region Throw Properties

    public GameObject testThrowPrefab;
    public MapBotElement testThrowElement = MapBotElement.Fire;

    #endregion Throw Properties

    // Start is called before the first frame update
    void Start()
    {
        if (weaponActions is not IWeaponActions)
            throw new System.Exception($"WeaponActions is not of type IWeaponActions: {weaponActions.GetType()}");
        WeaponActions = (IWeaponActions)weaponActions;
    }

    // Update is called once per frame
    void Update()
    {
        MovementUpdate();
        ActionUpdate();

        if (map is not null)
            ClickUpdate();
    }

    #region Updates

    void MovementUpdate()
    {
        switch (currentWeapon)
        {
            case WeaponType.None:
                break;
            case WeaponType.Greatsword:
                WeaponActions.MovementUpdate(debugMovement);
                break;
            default:
                break;
        }

        // var move = Vector3.zero;
        // if (move != Vector3.zero)
        // {
        //     debugAxis.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(move.y, move.x) * Mathf.Rad2Deg - 90);
        //     arrow.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(move.y, move.x) * Mathf.Rad2Deg - 90);
        //     transform.position += move.normalized * speed * Time.deltaTime;
        // }
    }

    void ActionUpdate()
    {
        switch (currentWeapon)
        {
            case WeaponType.None:
                break;
            case WeaponType.Greatsword:
                WeaponActions.ActionUpdate(debugMovement);
                break;
            default:
                break;
        }
    }

    #endregion

    #region Click

    void ClickUpdate()
    {
        Vector2? cursorPosition = null;
        if (Input.GetKeyDown(KeyCode.Mouse0))
        {
            if (cursorPosition is null)
                cursorPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);

            // Debug.Log($"clicked at {cursorPosition}");
            // map.AddRectangleFromWorld(MapElement.Fire, worldPosition.Value, 10, 10);

            // testThrow
            GameObject blom = Instantiate(testThrowPrefab, transform.position, Quaternion.identity);
            Blom blomScript = blom.GetComponent<Blom>();

            // blomScript.speed = 40;
            blomScript.rotateSpeed = 360 * 3;
            blomScript.endPoint = cursorPosition.Value;

            blomScript.eventHandler += (sender, e) =>
            {
                // TODO check if using math and a timer (ticks or wtv ull implement for delayed actions), instead of this event, is better
                // Debug.Log("blom event");
                map.AddCircleFromWorld(testThrowElement, cursorPosition.Value, throwRadius);
            };
        }

        if (Input.GetKeyUp(KeyCode.Mouse1))
        {
            if (cursorPosition is null)
                cursorPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            map.WhatsHere(cursorPosition.Value);
        }
        if (Input.GetKeyUp(KeyCode.P))
        {
            if (cursorPosition is null)
                cursorPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);

            var waveEffect = new MapTickEffectWave()
            {
                mapTickEffect = new MapTickEffectConditionalSet()
                {
                    ifCondition = (element) => element == MapBotElement.Water,
                    mapTickEffectIfTrue = new MapTickEffectSet()
                    {
                        mapElement = MapBotElement.PoisonWater,
                        combine = false,
                    },
                    mapElement = MapBotElement.PoisonWater,
                    combine = false,
                },
                tickDelay = 1,
                radius = 1,

                propagating = true,
                propagatingElem = MapBotElement.PoisonWater,

                passThrough = new MapBotElement[] { MapBotElement.Water, MapBotElement.PoisonWater },

                bounce = waveBounce,
            };

            // tickeffect.mapTickEffectIfFalse = tickeffect;
            // map.StartWaveFromWorld(tickeffect, cursorPosition.Value, true, MapBotElement.PoisonWater, new MapBotElement[] { MapBotElement.Water, MapBotElement.PoisonWater });

            map.StartWaveFromWorld(waveEffect, cursorPosition.Value);
        }
    }

    #endregion
}

public enum WeaponType
{
    None = 0,
    Greatsword = 1,
}