using UnityEngine;

public class EnemyInfo : MonoBehaviour
{
    public float Speed;
    public float HP = 100f;

    public MonoBehaviour enemyScript;
    public IEnemy EnemyScript;

    public void Start()
    {
        if (enemyScript != null && enemyScript is not IEnemy)
            throw new System.Exception($"EnemyScript is not of type IEnemy: {enemyScript.GetType()}");
        EnemyScript = (IEnemy)enemyScript;
    }

    public void TakeDamage(float damage) =>
        EnemyScript?.TakeDamage(damage);

    void Update() =>
        EnemyScript?.CustomUpdate();
}