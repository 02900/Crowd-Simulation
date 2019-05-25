using System.Collections.Generic;
using UnityEngine;
using Hydra.HydraCommon.Utils.Comparers;

namespace PowerLaw
{
    public class Obstacle : MonoBehaviour
    {
        private Mesh mesh;
        [SerializeField] private GameObject prefab;
        List<Vector2> contour = new List<Vector2>();
        ConcaveHull ch;

        public enum TypePolygon { NONE, CONCAVE, CONVEX, BOX_COLLIDER }
        public TypePolygon type;

        void Start()
        {
            if (type != TypePolygon.BOX_COLLIDER)
            {
                mesh = GetComponent<MeshFilter>().sharedMesh;
                Vector3[] vertices = mesh.vertices;
                List<EdgeHelpers.Edge> boundaryPath = EdgeHelpers.GetEdges(mesh.triangles).FindBoundary().SortEdges();
                Vector2 v = new Vector2();

                for (int i = 0; i < boundaryPath.Count; i++)
                {
                    Vector3 offset = transform.position;
                    Vector3 pos = vertices[boundaryPath[i].v1] + offset;
                    pos = ExtensionMethods.RotatePointAroundPivot(pos, transform.position, transform.rotation);
                    v.x = (float)System.Math.Round(pos.x, 1);
                    v.y = (float)System.Math.Round(pos.z, 1);

                    if (!contour.Contains(v))
                        contour.Add(v);
                }

                if (type == TypePolygon.CONCAVE)
                {
                    ch = new ConcaveHull();
                    contour = ch.CalculateConcaveHull(contour, 3);
                    contour.RemoveAt(contour.Count - 1);
                    contour.Reverse();
                }

                else if (type == TypePolygon.CONVEX)
                {
                    Vector2 position = new Vector2(transform.position.x, transform.position.z);
                    ClockwiseComparer c = new ClockwiseComparer(position);
                    contour.Sort(c);
                }
            }

            else if (type == TypePolygon.BOX_COLLIDER)
            {
                BoxCollider[] boxColliders = GetComponentsInChildren<BoxCollider>();
                for (int i = 0; i < boxColliders.Length; i++)
                {
                    float minX = boxColliders[i].transform.position.x -
                                 boxColliders[i].size.x * boxColliders[i].transform.lossyScale.x * 0.5f;
                    float minZ = boxColliders[i].transform.position.z -
                                 boxColliders[i].size.z * boxColliders[i].transform.lossyScale.z * 0.5f;
                    float maxX = boxColliders[i].transform.position.x +
                                 boxColliders[i].size.x * boxColliders[i].transform.lossyScale.x * 0.5f;
                    float maxZ = boxColliders[i].transform.position.z +
                                 boxColliders[i].size.z * boxColliders[i].transform.lossyScale.z * 0.5f;


                    Vector3 pos = new Vector3(maxX, transform.position.y, maxZ);
                    pos = ExtensionMethods.RotatePointAroundPivot(pos, transform.position, transform.rotation);
                    contour.Add(new Vector2(pos.x, pos.z));

                    pos = new Vector3(minX, transform.position.y, maxZ);
                    pos = ExtensionMethods.RotatePointAroundPivot(pos, transform.position, transform.rotation);
                    contour.Add(new Vector2(pos.x, pos.z));

                    pos = new Vector3(minX, transform.position.y, minZ);
                    pos = ExtensionMethods.RotatePointAroundPivot(pos, transform.position, transform.rotation);
                    contour.Add(new Vector2(pos.x, pos.z));

                    pos = new Vector3(maxX, transform.position.y, minZ);
                    pos = ExtensionMethods.RotatePointAroundPivot(pos, transform.position, transform.rotation);
                    contour.Add(new Vector2(pos.x, pos.z));
                }
            }

            foreach (var position2d in contour)
            {
                Vector3 p = new Vector3(position2d.x, 0, position2d.y);
                Instantiate(prefab, p, Quaternion.identity, transform);
            }

            if (type != TypePolygon.NONE)
                for (int i = 0; i < contour.Count - 1; i++)
                    Engine.Instance.AddObstacle(new Vector2(contour[i].x, contour[i].y), new Vector2(contour[i + 1].x, contour[i + 1].y));

            else
                Debug.Log("No has establecido el tipo para el poligono: " + name);
        }

        void OnDrawGizmos()
        {
            //Concave hull
            Gizmos.color = Color.yellow;
            for (int i = 0; i < contour.Count - 1; i++)
            {
                Vector3 left = new Vector3(contour[i].x, 2, contour[i].y);
                Vector3 right = new Vector3(contour[i + 1].x, 2, contour[i + 1].y);
                Gizmos.DrawLine(left, right);
            }
        }
    }
}