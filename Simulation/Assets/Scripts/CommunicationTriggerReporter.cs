using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(CommonAIBase))]
public class CommunicationTriggerReporter : MonoBehaviour
{
    private CommonAIBase ai;
    private Collider myCollider;

    private Dictionary<GameObject, float> activeCommunications = new(); // 相手NPC ➝ 開始時間

    void Start()
    {
        ai = GetComponent<CommonAIBase>();
        myCollider = transform.Find("CommunicationArea")?.GetComponent<Collider>();

        if (myCollider == null || !myCollider.isTrigger)
        {
            //Debug.LogError($"{name}: CommunicationArea の Collider が Trigger でないか未設定です。");
        }
    }

    void OnTriggerEnter(Collider other)
    {
        //Debug.Log($"[TriggerEnter] {name} ⇄ {other.name}");

        var otherReporter = other.GetComponentInParent<CommunicationTriggerReporter>();
        if (otherReporter == null || otherReporter == this)
        {
            //Debug.Log($"[TriggerEnter] 無効な相手または自分自身: {other.name}");
            return;
        }

        // ★無条件で開始記録（SmartObject比較なし）
        if (!activeCommunications.ContainsKey(otherReporter.gameObject))
        {
            activeCommunications[otherReporter.gameObject] = Time.time;
            //Debug.Log($"[TriggerEnter] {name} ⇄ {otherReporter.name} コミュニケーション開始（仮）");
        }
    }

    void OnTriggerExit(Collider other)
    {
        //Debug.Log($"[TriggerExit] {name} ⇄ {other.name}");

        var otherReporter = other.GetComponentInParent<CommunicationTriggerReporter>();
        if (otherReporter == null)
        {
            //Debug.Log($"[TriggerExit] 無効な相手: {other.name}");
            return;
        }

        if (activeCommunications.TryGetValue(otherReporter.gameObject, out float startTime))
        {
            float duration = Time.time - startTime;
            //Debug.Log($"[TriggerExit] {name} ⇄ {otherReporter.name} コミュニケーション終了: {duration:F2} 秒");
            ProximityCommunicationTracker.Instance?.ReportCommunication(ai, otherReporter.ai, duration);
            activeCommunications.Remove(otherReporter.gameObject);
        }
    }



    bool IsCommunicatingWith(CommunicationTriggerReporter other)
    {
        if (ai == null || other == null || other.ai == null)
            return false;

        var bbA = BlackboardManager.Instance.GetIndividualBlackboard(ai);
        var bbB = BlackboardManager.Instance.GetIndividualBlackboard(other.ai);

        if (!bbA.TryGet(EBlackboardKey.Character_CurrentSmartObject, out GameObject objA) ||
            !bbB.TryGet(EBlackboardKey.Character_CurrentSmartObject, out GameObject objB) ||
            objA != objB)
            return false;

        return true;
    }
}
