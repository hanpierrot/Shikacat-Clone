using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DecorPool : MonoBehaviour
{
    [SerializeField] private SpriteRenderer prefab;
    [SerializeField] private Transform poolRoot;
    [SerializeField] private int initialSize = 40;
    
    private readonly Stack<SpriteRenderer> free = new Stack<SpriteRenderer>();

    private void Awake()
    {
        if (poolRoot == null) poolRoot = transform;

        for (int i = 0; i < initialSize; i++)
            free.Push(Create());
    }

    public SpriteRenderer Get(Transform parent)
    {
        SpriteRenderer item = free.Count > 0 ? free.Pop() : Create();
        item.transform.SetParent(parent, false);
        item.gameObject.SetActive(true);
        return item;
    }
    
    public void Release(SpriteRenderer item)
    {
        if (item == null) return;

        item.gameObject.SetActive(false);
        item.transform.SetParent(poolRoot, false);
        free.Push(item);
    }

    private SpriteRenderer Create()
    {
        SpriteRenderer item = Instantiate(prefab, poolRoot);
        item.gameObject.SetActive(false);
        return item;
    }
}
