using UnityEngine;

public class HY_Scanner : MonoBehaviour
{
   public float scanRage;
   public LayerMask targetLayer;
   public RaycastHit2D[] targets;
   public Transform nearestTarget;

   void FixedUpdate()
   {
      targets = Physics2D.CircleCastAll(transform.position, scanRage, Vector2.zero, 0 , targetLayer);
      nearestTarget = GetNearestTarget();
   }

   Transform GetNearestTarget()
   {
      Transform result = null;
      float diff = 100;

      foreach (RaycastHit2D target in targets)
      {
         Vector3 myPos = transform.position;
         Vector3 targetPos =  target.transform.position;
         float curDiff = Vector3.Distance(myPos, targetPos);

         if (curDiff < diff)
         {
            diff= curDiff;
            result = target.transform;
         }
      }
      return result;
   }
}
