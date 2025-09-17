using Hanzzz.MeshDemolisher;
using UnityEngine;

public class MeshBreaking : MonoBehaviour
{
    MeshDemolisher meshDemolisher = new MeshDemolisher();

    meshDemolisher.Demolish(targetGameObject, breakPoints, interiorMaterial);
 // targetGameObject is of type GameObject. targetGameObject is assumed to
 // have a MeshFilter and a MeshRenderer attached.
 // breakPoints is of type List<Transform>. The world position of every
 // Transform in breakPoints is used to break the targetGameObject.
 // interiorMaterial is of type Material. interiorMaterial fills the new
 // faces created in the demolishing process
}
