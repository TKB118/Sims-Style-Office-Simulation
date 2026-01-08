using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;


public class SmartObjectManager : MonoBehaviour
{
    public static SmartObjectManager Instance { get; private set; }

    private static List<SmartObjectManager> activeManagers = new List<SmartObjectManager>();

    public List<SmartObject> RegisteredObjects { get; private set; } = new List<SmartObject>();

    private void Awake()
    {
        activeManagers.Add(this);
        Instance = this; // 最後に追加されたものをInstanceにする（これが「現在のレイアウト」になる）
    }

    private void OnDestroy()
    {
        activeManagers.Remove(this);
        if (Instance == this)
        {
            Instance = activeManagers.LastOrDefault(); // 他があれば切り替え
        }
    }

    public void RegisterSmartObject(SmartObject obj)
    {
        if (!RegisteredObjects.Contains(obj))
        {
            RegisteredObjects.Add(obj);
        }
    }

    public void DeregisterSmartObject(SmartObject obj)
    {
        if (RegisteredObjects.Contains(obj))
        {
            RegisteredObjects.Remove(obj);
        }
    }
}

