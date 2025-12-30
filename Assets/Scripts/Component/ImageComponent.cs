using UnityEngine;
namespace Vectorier.Component
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Vectorier/Component/Image Component")]
    public class ImageComponent : MonoBehaviour
    {
        public enum ImageType
        {
            None,
            Static,
            Vanishing,
            Dynamic
        }

        public enum ImageDepth
        {
            Front = 0,
            Middle = 1,
            Back = 2
        }

        [Tooltip("None - Default\nStatic - Static\nVanishing - Image will disappear if under trigger with NoneType event.\nDynamic - Plays the image's animation if it has .plist")]
        public ImageType Type = ImageType.None;

        [Tooltip("Layer order; 0 - Front, 0.5 - Middle, 1 - Back")]
        public ImageDepth depth = ImageDepth.Middle;
    }
}