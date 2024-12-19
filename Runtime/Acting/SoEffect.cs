using UnityEngine;

namespace Readymade.Machinery.Acting
{
    [CreateAssetMenu(
        menuName = nameof(Readymade) + "/" + nameof(Machinery) + "/" + nameof(Acting) + "/" + nameof(SoEffect),
        fileName = "New " + nameof(SoEffect), order = 0)]
    public class SoEffect : ScriptableObject
    {
        [SerializeField] private SoProp unlockedBy;

        [TextArea(2, 30)] [SerializeField] private string description;

        public SoProp UnlockedBy => unlockedBy;

        public string Description => description;

        public void InvokeFor(IActor actor)
        {
        }
    }
}