using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DecorSpritePool", menuName = "Shikacat/Decor Sprite Pool")]
public class DecorSpritePool : ScriptableObject
{
    [SerializeField] private Sprite[] sprites;
    
    public int Count => sprites != null ? sprites.Length : 0;
    public Sprite Get(int index) => sprites[index];
}
