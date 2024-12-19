using UnityEngine;
using UnityEngine.UI;

namespace Readymade.Machinery.Shared {
    /// <summary>
    /// View component for displaying an icon above a gameplay or environment object. 
    /// </summary>
    public class ThoughtDisplay : MonoBehaviour {
        [Tooltip ( "The Image to be used for displaying the icon." )]
        [SerializeField]
        private Image icon;

        [Tooltip ( "The canvas group to use for controlling the visibility of the view." )]
        [SerializeField]
        private CanvasGroup group;

        /// <summary>
        /// Set the visibility of the view while also assigning a new sprite.
        /// </summary>
        /// <param name="isOn">Whether the view should be visible.</param>
        /// <param name="sprite">The sprite to be used in the view. This is mostly useful if <paramref name="isOn"/> is true.</param>
        public void SetVisible ( bool isOn, Sprite sprite ) {
            if ( icon ) {
                icon.sprite = sprite;
            }

            if ( group ) {
                group.interactable = false;
                group.blocksRaycasts = false;
                group.alpha = isOn ? 1.0f : 0f;
            }
        }

        /// <summary>
        /// Set the visibility of the view.
        /// </summary>
        /// <param name="isOn">Whether the view should be visible.</param>
        public void SetVisible ( bool isOn ) {
            if ( group ) {
                group.interactable = false;
                group.blocksRaycasts = false;
                group.alpha = isOn ? 1.0f : 0f;
            }
        }
    }
}