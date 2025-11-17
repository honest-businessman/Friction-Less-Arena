using Pathfinding;
using UnityEngine;

public class MapObjectPathing : MonoBehaviour
{
    GraphUpdateObject penaltyGuo;

    private void Awake()
    {
        // Example usage: Update graph around this object's bounds
        Bounds bounds = GetComponent<Collider2D>().bounds;
        UpdateGraph(bounds);
    }
    void UpdateGraph(Bounds bounds)
    {
        bounds.size = Vector3.one * AIManager.Instance.MapObjectSize;
        penaltyGuo = new GraphUpdateObject(bounds);
        penaltyGuo.updatePhysics = true;
        penaltyGuo.resetPenaltyOnPhysics = false;
        penaltyGuo.addPenalty = AIManager.Instance.WallErosionPenalty;

        AstarPath.active.UpdateGraphs(penaltyGuo);
    }

    private void OnDestroy()
    {
        if(penaltyGuo != null)
        {
            ResetPenalty(penaltyGuo);
        }
        
    }

    private void ResetPenalty(GraphUpdateObject guo)
    {
        if (penaltyGuo != null) return;
        GraphUpdateObject newGuo = new GraphUpdateObject(guo.bounds); // Must create new GraphUpdateObject, cannot reuse old one.
        newGuo.updatePhysics = true;
        newGuo.resetPenaltyOnPhysics = false;
        newGuo.addPenalty = -(guo.addPenalty); // Reverts penalty amount
        AstarPath.active.UpdateGraphs(newGuo);
    }
}
