using UnityEngine;

public interface VectorierComponent 
{
    private void TagGameObject(string tag)
    {
        if (this is MonoBehaviour monoBehaviour)
        {
            monoBehaviour.gameObject.tag = "tag";
        }
    }
}
