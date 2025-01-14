using System;
using System.Collections;
using UnityEngine;

namespace Roots.Util
{
    public static class CoroutineHelper
    {
        public static IEnumerator ExecuteDelayed(float duration, Action function)
        {
            yield return new WaitForSeconds(duration);

            function();
        }
        
        public static IEnumerator ExecuteNextFrame(Action function)
        {
            yield return null;

            function();
        }
    }
}
