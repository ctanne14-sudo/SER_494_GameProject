using UnityEngine;

namespace Roguelike
{
    [CreateAssetMenu(fileName = "RunSequence", menuName = "Roguelike/Run Sequence")]
    public class RunSequence : ScriptableObject
    {
        [System.Serializable]
        public class Floor
        {
            public string floorName;
            public string[] variantScenes;
        }

        public Floor[] floors;
    }
}
