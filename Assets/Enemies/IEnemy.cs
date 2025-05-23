
using UnityEngine;

public interface IEnemy
{
    void TakeDamage(float damage);
    void CustomUpdate();
    void Knockback(Vector2 direction, float force, float time);
}