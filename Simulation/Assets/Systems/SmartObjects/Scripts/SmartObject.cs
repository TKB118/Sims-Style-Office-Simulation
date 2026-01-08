using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SmartObject : MonoBehaviour
{
    [SerializeField] protected string _DisplayName;
    [SerializeField] protected Transform _InteractionMarker;
    [SerializeField] protected Transform _LookAtPoint;

    protected List<BaseInteraction> CachedInteractions = null;

    public Vector3 InteractionPoint => _InteractionMarker != null ? _InteractionMarker.position : transform.position;

    public string DisplayName
    {
        get => _DisplayName;
        set => _DisplayName = value;
    }

    private string nameOfBlock;
    public Transform LookAtPoint => _LookAtPoint != null ? _LookAtPoint : transform;

    public List<BaseInteraction> Interactions
    {
        get
        {
            if (CachedInteractions == null)
                CachedInteractions = new List<BaseInteraction>(GetComponents<BaseInteraction>());

            return CachedInteractions;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        SmartObjectManager.Instance.RegisterSmartObject(this);
        SetDisplayName();
    }

    private void OnDestroy()
    {
        if (SmartObjectManager.Instance != null)
        {
            SmartObjectManager.Instance.DeregisterSmartObject(this);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetDisplayName()
    {
        nameOfBlock = gameObject.name;
        
        DisplayName = nameOfBlock;
    }
}
