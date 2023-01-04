using UnityEngine;

namespace Insight
{
    class TickRate : MonoBehaviour 
    {
        public int tickRate = 30;

        void Start() 
        {
            // a check to protect server hardware, whilst not overwriting args.
            if (Application.targetFrameRate <= 0 || Application.targetFrameRate > 120)
            {
                Application.targetFrameRate = tickRate;
            }
        }
    }
}
