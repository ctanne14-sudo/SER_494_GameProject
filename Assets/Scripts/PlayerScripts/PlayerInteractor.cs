using UnityEngine;

public class PlayerInteractor : MonoBehaviour
{
    [SerializeField] private float interactRadius = 2f;
    [SerializeField] private LayerMask interactionMask;
    public Inventory inventory;
    public HealthController hc;
    private Action prevObj;
    private readonly Collider[] hits = new Collider[16];

    

    void Update()
    {
        Vector3 origin = transform.position;
        Action closet = null;
        float distance = Mathf.Infinity;
        bool closestPriority = false;

        int n = Physics.OverlapSphereNonAlloc(origin, interactRadius, hits, interactionMask);

        for (int i = 0; i < n; i++)
        {
            var a = hits[i].GetComponentInParent<Action>();
            if (a == null) continue;

            float d = Vector3.Distance(hits[i].ClosestPoint(origin), origin);

            bool prioritze = hits[i].CompareTag("InventoryItem") || hits[i].CompareTag("ConsumableItem");

            bool better = (prioritze && !closestPriority) || (prioritze == closestPriority && d < distance);
            if (better) {
                distance = d; 
                closet = a;
                closestPriority = prioritze;
            }
        }

        if (closet != prevObj)
        {
            if (prevObj != null) prevObj.showUI(false);                
            prevObj = closet;
        }

        if (prevObj == null) return;  
        
        prevObj.showUI(true);

        if (Input.GetKeyDown(KeyCode.E)) prevObj.action(this);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactRadius);
    }
}
