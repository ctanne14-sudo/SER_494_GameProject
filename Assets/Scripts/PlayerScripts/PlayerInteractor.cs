using UnityEngine;

public class PlayerInteractor : MonoBehaviour
{
    [SerializeField]
    private float interactRadius = 2f;
    [SerializeField]
    private LayerMask interactionMask;
    
    private ObjectInteraction prevObj;
    private readonly Collider[] hits = new Collider[16];

    void Update()
    {
        Vector3 origin = transform.position;
        ObjectInteraction closet = null;
        float distance = Mathf.Infinity;

        int n = Physics.OverlapSphereNonAlloc(origin, interactRadius, hits, interactionMask);

        for (int i = 0; i < n; i++)
        {
            var o = hits[i].GetComponentInParent<ObjectInteraction>();
            float d = Vector3.Distance(hits[i].ClosestPoint(origin), origin);
            if (d < distance) distance = d; closet = o;
        }

        if (closet != prevObj)
        {
            if (prevObj != null) prevObj.ShowUI(false);                
            prevObj = closet;
        }

        if (prevObj == null) return;  
        
        prevObj.ShowUI(true);

        if (Input.GetKeyDown(KeyCode.E)) prevObj.action(this);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactRadius);
    }
}
