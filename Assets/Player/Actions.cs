using UnityEngine;

public class Actions : MonoBehaviour
{
    [Range(1, 100)]
    public float speed;

    [Range(1, 300)]
    public int throwRadius = 10;
    public bool waveBounce = false;

    public Map map;


    #region throw properrties

    public GameObject testThrowPrefab;
    public MapBotElement testThrowElement = MapBotElement.Fire;

    #endregion

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        MovementUpdate();
        ClickUpdate();
    }

    #region Movement

    void MovementUpdate()
    {
        var move = Vector3.zero;
        if (Input.GetKey(KeyCode.W))
            move += new Vector3(0, 1, 0);
        if (Input.GetKey(KeyCode.S))
            move += new Vector3(0, -1, 0);
        if (Input.GetKey(KeyCode.D))
            move += new Vector3(1, 0, 0);
        if (Input.GetKey(KeyCode.A))
            move += new Vector3(-1, 0, 0);
        transform.position += Vector3.Normalize(move) * speed * Time.deltaTime;
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
