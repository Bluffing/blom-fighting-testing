using System;
using System.Collections;
using UnityEngine;

public enum EnemyTestState
{
    Idle,
    // Searching,
    Attacking,
    StartExploding,
    Exploding,
    Dead,
    DmgFlashing,
}

[RequireComponent(typeof(EnemyInfo))]
public class EnemyTest : MonoBehaviour, IEnemy
{
    GameObject Player;
    private float Speed;
    public EnemyTestState State = EnemyTestState.Idle;
    SpriteRenderer SpriteRenderer;
    EnemyInfo enemyInfo;

    Transform HelperGO;
    PopDamage popDamage;

    public Sprite explosionSpriteStart;
    public Sprite explosionSpriteEnd;

    public Color colorBase = Color.red;
    public Color colorFlash = Color.white;
    public Color colorDeathFlash = Color.black;
    public Color colorDamageStun = Color.white;

    public float ExplosionDistance;
    public float ExplosionDelayTime = 2f;
    public float ExplosionEndTime = 1f;

    private float explosionTimer;
    private float switchColorTime = 0;

    // Start is called before the first frame update
    void Start()
    {
        enemyInfo = GetComponent<EnemyInfo>();
        Speed = enemyInfo.Speed;
        SpriteRenderer = GetComponent<SpriteRenderer>();

        HelperGO = GameObject.FindGameObjectWithTag("Helper").transform;
        popDamage = HelperGO.GetComponent<PopDamage>();

        Player = GameObject.FindGameObjectWithTag("Player");
        if (Player == null)
        {
            Debug.LogError("Player not found in the scene.");
            throw new Exception("Player not found in the scene.");
        }
    }


    #region CustomUpdate
    public void CustomUpdate()
    {
        if (KnockbackTime > 0)
        {
            transform.position += (Vector3)KnockbackVelo * Time.deltaTime;
            KnockbackTime -= Time.deltaTime;
        }

        switch (State)
        {
            case EnemyTestState.Idle:
                State = EnemyTestState.Attacking;
                break;
            case EnemyTestState.Attacking:
                Attack();
                break;
            case EnemyTestState.StartExploding:
                StartExploding();
                break;
            case EnemyTestState.Exploding:
                Explode();
                break;
            case EnemyTestState.DmgFlashing:
                Flash();
                break;
            case EnemyTestState.Dead:
            default:
                Kill();
                break;
        }
    }

    void Attack()
    {
        transform.position = Vector2.MoveTowards(transform.position, Player.transform.position, Speed * Time.deltaTime);
        if (Vector2.Distance(transform.position, Player.transform.position) < ExplosionDistance)
            State = EnemyTestState.StartExploding;
        explosionTimer = ExplosionDelayTime;
    }
    void StartExploding()
    {
        explosionTimer -= Time.deltaTime;
        if (explosionTimer < 0)
        {
            State = EnemyTestState.Exploding;
            explosionTimer = ExplosionEndTime;
            switchColorTime = 0;

            SpriteRenderer.color = colorFlash;
            SpriteRenderer.sprite = explosionSpriteStart;
            return;
        }

        switchColorTime -= Time.deltaTime;
        if (switchColorTime < 0)
        {
            SpriteRenderer.color = SpriteRenderer.color == colorFlash ? colorBase : colorFlash;
            switchColorTime = explosionTimer / 5;
        }
    }
    void Explode()
    {
        if (explosionTimer == ExplosionEndTime)
        {
            var playerCollider = Player.GetComponent<CustomRectangleCollider>();
            var selfCollider = GetComponent<CustomRectangleCollider>();
            if (playerCollider.IsTouching(selfCollider))
            {
                var popDamage = GameObject.FindGameObjectWithTag("Helper").GetComponent<PopDamage>();
                popDamage.ShowDamage(Player.transform, "100", Color.red);
            }
        }

        explosionTimer -= Time.deltaTime;

        if (explosionTimer > 0.6f)
        {
            switchColorTime -= Time.deltaTime;
            if (switchColorTime < 0)
            {
                SpriteRenderer.color = SpriteRenderer.color == colorFlash ? colorDeathFlash : colorFlash;
                switchColorTime = 0.07f;
            }
        }
        else if (explosionTimer > 0)
        {
            SpriteRenderer.color = colorFlash;
            SpriteRenderer.sprite = explosionSpriteEnd;
        }
        else
            State = EnemyTestState.Dead;
    }
    void Kill()
    {
        Destroy(gameObject);
    }
    #endregion CustomUpdate

    #region TakeDamage
    public void TakeDamage(float damage)
    {
        popDamage.ShowDamage(transform, damage.ToString("0.##"), textcolor: Color.red, time: 0.3f);

        SpriteRenderer.color = colorBase;

        State = EnemyTestState.DmgFlashing;
        switchColorTime = 0.3f;

        enemyInfo.HP -= damage;
    }
    public void Flash()
    {
        switchColorTime -= Time.deltaTime;
        if (switchColorTime < 0)
        {
            SpriteRenderer.color = colorBase;

            if (enemyInfo.HP > 0)
                State = EnemyTestState.Attacking;
            else
                State = EnemyTestState.Dead;
        }
    }
    #endregion TakeDamage

    #region Knockback
    public Vector2 KnockbackVelo;
    public float KnockbackTime;
    public void Knockback(Vector2 direction, float force, float time = 0.2f)
    {
        KnockbackVelo = direction.normalized * force;
        KnockbackTime = time;
    }
    #endregion Knockback
}
