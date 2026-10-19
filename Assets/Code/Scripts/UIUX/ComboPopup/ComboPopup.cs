using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ComboPopup : MonoBehaviour
{
    [Serializable]
    public struct Tier
    {
        public int minCombo;
        public GameObject effect;
    }

    [SerializeField] private Camera worldCamera;
    [SerializeField] private RectTransform effectContainer;
    [SerializeField] private Tier[] tiers;

    public event Action<int, Vector3> Shown;
    
    private bool warnedNoCanvas;

    private sealed class Effect
    {
        private readonly ParticleSystem[] systems;
        public readonly GameObject Root;
    
        public Effect(GameObject root)
        {
            Root = root;
            systems = root.GetComponentsInChildren<ParticleSystem>(true);
        }

        public bool IsPlaying
        {
            get
            {
                foreach (ParticleSystem s in systems)
                    if (s.IsAlive(false)) return true;
                return false;
            }
        }

        public void Play(Vector3 canvasWorldPoint)
        {
            Root.transform.position = canvasWorldPoint;
            Root.SetActive(true);

            foreach (ParticleSystem s in systems)
            {
                s.Clear(false);
                s.Play(false);
            }
        }

        public void Stop()
        {
            foreach (ParticleSystem s in systems)
                s.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    private List<Effect>[] effects;
    
    public bool PrewarmStep()
    {
        EnsureEffects();

        for (int i = 0; i < effects.Length; i++)
        {
            List<Effect> list = effects[i];
            if (list.Count == 0 || list.Count >= 2) continue;

            GameObject source = list[0].Root;
            GameObject clone = Instantiate(source, source.transform.parent);
            list.Add(new Effect(clone));
            return true;
        }

        return false;
    }
    
    private void EnsureEffects()
    {
        if (effects != null) return;
        
        effects = new List<Effect>[tiers.Length];
        for (int i = 0; i < tiers.Length; i++)
        {
            effects[i] = new List<Effect>();
            GameObject go = tiers[i].effect;
            if (go == null) continue;

            if (effectContainer != null && go.GetComponentInParent<Canvas>(true) == null)
                go.transform.SetParent(effectContainer, false);

            effects[i].Add(new Effect(go));
        }
    }

    public bool TryShow(int combo, Vector3 position)
    {
        EnsureEffects();

        int tierIndex = FindTier(combo);
        if (tierIndex < 0 || effects[tierIndex].Count == 0) return false;

        Effect effect = GetFreeEffect(tierIndex);
        effect.Play(WorldToCanvasPoint(position, effect));
        Shown?.Invoke(tierIndex, position);
        return true;
    }
    
    private Vector3 WorldToCanvasPoint(Vector3 worldPosition, Effect effect)
    {
        Camera gameCamera = worldCamera != null ? worldCamera : Camera.main;
        Canvas canvas = effect.Root.GetComponentInParent<Canvas>(true);
        Canvas rootCanvas = canvas != null ? canvas.rootCanvas : null;
        
        RectTransform container = effect.Root.transform.parent as RectTransform;
        if (container == null && rootCanvas != null) container = rootCanvas.transform as RectTransform;

        if (gameCamera == null || container == null)
        {
            if (!warnedNoCanvas)
            {
                warnedNoCanvas = true;
                Transform parent = effect.Root.transform.parent;
                Debug.LogWarning($"ComboPopup: camera={(gameCamera != null)}, container={(container != null)}, canvas={(canvas != null)}, " +
                                 $"effect='{effect.Root.name}', activeInHierarchy={effect.Root.activeInHierarchy}, parent='{(parent != null ? parent.name : "none")}', " +
                                 $"effectContainer={(effectContainer != null ? effectContainer.name : "null")}.");
            }
            return worldPosition;
        }

        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(gameCamera, worldPosition);
        Camera uiCamera = rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay ? rootCanvas.worldCamera : null;

        RectTransformUtility.ScreenPointToWorldPointInRectangle(container, screenPoint, uiCamera, out Vector3 point);
        return point;
    }
    
    public void Hide()
    {
        EnsureEffects();

        foreach (List<Effect> list in effects)
        foreach (Effect effect in list)
            effect.Stop();
    }
    
    private Effect GetFreeEffect(int tierIndex)
    {
        List<Effect> list = effects[tierIndex];

        foreach (Effect effect in list)
            if (!effect.IsPlaying) return effect;
        
        GameObject source = list[0].Root;
        GameObject clone = Instantiate(source, source.transform.parent);
        var cloned = new Effect(clone);
        list.Add(cloned);
        return cloned;
    }
    
    private int FindTier(int combo)
    {
        int found = -1;
        for (int i = 0; i < tiers.Length; i++)
            if (combo >= tiers[i].minCombo) found = i;
        return found;
    }
}
