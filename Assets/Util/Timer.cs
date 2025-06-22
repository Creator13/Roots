using UnityEngine;

namespace Roots.Util
{
    public struct Timer
    {
        private readonly bool loop;
        private readonly float duration;

        private float currentTime;

        public Timer(float duration, bool loop)
        {
            this.loop = loop;
            this.duration = duration;
            currentTime = 0f;
        }

        public bool CheckTime()
        {
            currentTime += Time.deltaTime;

            if (currentTime >= duration)
            {
                if (loop) currentTime -= duration;;
                return true;
            }
            
            return false;
        }

        public void Reset()
        {
            currentTime = 0;
        }
    }
}