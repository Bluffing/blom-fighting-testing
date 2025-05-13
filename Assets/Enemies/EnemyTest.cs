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
    Dead
}

[RequireComponent(typeof(EnemyInfo))]
public class EnemyTest : MonoBehaviour
{
    GameObject Player;
    private float Speed;
    public EnemyTestState State = EnemyTestState.Idle;
    SpriteRenderer SpriteRenderer;

    public Sprite explosionSpriteStart;
    public Sprite explosionSpriteEnd;

    public float explosionTime = 3f;
    private float switchColorTime = 0;

    // Start is called before the first frame update
    void Start()
    {
        Speed = GetComponent<EnemyInfo>().Speed;
        SpriteRenderer = GetComponent<SpriteRenderer>();

        Player = GameObject.FindGameObjectWithTag("Player");
        if (Player == null)
        {
            Debug.LogError("Player not found in the scene.");
            throw new Exception("Player not found in the scene.");
        }
    }

    // Update is called once per frame
    void Update()
    {
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
            case EnemyTestState.Dead:
            default:
                Kill();
                break;
        }
    }

    void Attack()
    {
        transform.position = Vector2.MoveTowards(transform.position, Player.transform.position, Speed * Time.deltaTime);
        if (Vector2.Distance(transform.position, Player.transform.position) < 0.6f)
            State = EnemyTestState.StartExploding;
    }
    void StartExploding()
    {
        explosionTime -= Time.deltaTime;
        if (explosionTime < 0)
        {
            State = EnemyTestState.Exploding;
            explosionTime = 1f;
            switchColorTime = 0;

            SpriteRenderer.color = Color.white;
            SpriteRenderer.sprite = explosionSpriteStart;
            return;
        }

        switchColorTime -= Time.deltaTime;
        if (switchColorTime < 0)
        {
            SpriteRenderer.color = SpriteRenderer.color == Color.white ? Color.red : Color.white;
            switchColorTime = explosionTime / 5;
        }
    }
    void Explode()
    {
        explosionTime -= Time.deltaTime;

        if (explosionTime > 0.6f)
        {
            switchColorTime -= Time.deltaTime;
            if (switchColorTime < 0)
            {
                SpriteRenderer.color = SpriteRenderer.color == Color.white ? Color.black : Color.white;
                switchColorTime = 0.07f;
            }
        }
        else if (explosionTime > 0)
        {
            SpriteRenderer.color = Color.white;
            SpriteRenderer.sprite = explosionSpriteEnd;
        }
        else
            State = EnemyTestState.Dead;
    }
    void Kill()
    {
        var playerCollider = Player.GetComponent<Collider2D>();
        if (playerCollider.IsTouching(GetComponent<Collider2D>()))
        {
            var popDamage = GameObject.FindGameObjectWithTag("Helper").GetComponent<PopDamage>();
            popDamage.ShowDamage(Player.transform, "100", Color.red);
        }

        Destroy(gameObject);
    }
}
