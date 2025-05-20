
using UnityEditorInternal;
using UnityEngine;

public enum EnemyDummyState
{
    Idle,
    DmgFlashing,
}

public class EnemyDummy : MonoBehaviour, IEnemy
{
    public EnemyDummyState State = EnemyDummyState.Idle;

    #region Properties

    SpriteRenderer SpriteRenderer;
    public Color colorBase;
    public Color colorFlash;
    private float switchColorTime = 0;

    #endregion Properties

    #region Helpers

    Transform HelperGO;
    PopDamage popDamage;

    #endregion Helpers

    public void Start()
    {
        HelperGO = GameObject.FindGameObjectWithTag("Helper").transform;
        popDamage = HelperGO.GetComponent<PopDamage>();

        SpriteRenderer = GetComponent<SpriteRenderer>();
    }

    #region CustomUpdate
    public void CustomUpdate()
    {
        switch (State)
        {
            case EnemyDummyState.DmgFlashing:
                Flash();
                break;
            case EnemyDummyState.Idle:
            default:
                break;
        }
    }
    #endregion CustomUpdate

    #region TakeDamage
    public void TakeDamage(float damage)
    {
        popDamage.ShowDamage(transform, damage.ToString("0.##"), textcolor: Color.red, time: 0.7f);

        SpriteRenderer.color = colorFlash;

        State = EnemyDummyState.DmgFlashing;
        switchColorTime = 0.2f;
    }
    public void Flash()
    {
        switchColorTime -= Time.deltaTime;
        if (switchColorTime < 0)
        {
            SpriteRenderer.color = colorBase;
            State = EnemyDummyState.Idle;
        }
    }
    #endregion TakeDamage
}