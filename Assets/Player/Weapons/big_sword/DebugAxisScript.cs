using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebugAxisScript : MonoBehaviour
{
    public Color focusColor = Color.red;
    public Color unFocusColor = Color.green;
    public SpriteRenderer spriteRenderer;

    public void Focus() =>
        spriteRenderer.color = focusColor;
    public void UnFocus() =>
        spriteRenderer.color = unFocusColor;
}
