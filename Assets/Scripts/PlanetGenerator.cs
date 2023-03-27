using System.Collections;
using System.Collections.Generic;
using System.Linq;

using Unity.Mathematics;

using UnityEditor;

using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[ExecuteInEditMode]
public class PlanetGenerator : MonoBehaviour {
    private List<Vector3> m_Vertices = new List<Vector3>();
    private List<Vector3> m_Normals = new List<Vector3>();
    private List<Vector2> m_UV = new List<Vector2>();
    private List<int> m_Indices = new List<int>();

    private MeshFilter m_MeshFilter;

    public int Level = 0;
    public int Size = 1;
    public long Seed = 42;
    public Transform ContentRoot;
    public GameObject HandlePrefab;
    public bool ShowGizmos = false;
    public List<Vector3> Vertices { get { return m_Vertices; } }

    private void OnEnable() {
        m_MeshFilter = GetComponent<MeshFilter>();
        if (m_MeshFilter.sharedMesh == null) {
            var mesh = new Mesh();
            mesh.name = "Planet";
            m_MeshFilter.sharedMesh = mesh;

        } else {
            m_Vertices = new List<Vector3>(m_MeshFilter.sharedMesh.vertices);
            m_Normals = new List<Vector3>(m_MeshFilter.sharedMesh.normals);
            m_UV = new List<Vector2>(m_MeshFilter.sharedMesh.uv);
            m_Indices = new List<int>(m_MeshFilter.sharedMesh.triangles);
        }
    }

    public void Rebuild() {

        if (Level < 0)
            Level = 0;

        var w = (2 << Level) + 1;

        // We are accessing these with indices to we can  just resize these
        m_Vertices.Resize(w * w);
        m_UV.Resize(w * w);

        // Normals need to start out at 0, so we cannot just resize
        m_Normals = new List<Vector3>();
        m_Normals.Resize(w * w, new Vector3(0, 0, 0));

        m_Indices.Clear();
        

        ClearHandles();

        float arrayScale = 2.0f / (2 << Level);

        /**
         * In the unfolded image, the points go bottom up. So the "grid" 
         * in the project description goes like this:
         * 
         * 6 7 8                     0 1 2
         * 3 4 5  and not like this  3 4 5
         * 0 1 2                     6 7 8
         */
        for (int z = 0; z < w; z++) {
            for (int x = 0; x < w; x++) {
                float xx = x * arrayScale - 1.0f;
                float zz = z * arrayScale - 1.0f;
                float yy = 1 - Mathf.Abs(xx) - Mathf.Abs(zz);
                float t = Mathf.Min(0, yy);

                Vector3 v = new Vector3(xx + Mathf.Sign(xx) * t, yy, zz + Mathf.Sign(zz) * t).normalized;

                float n = 0, s = 1;
                for (int i = 0; i < Level; s *= 2, i++) {
                    n += OpenSimplex2S.Noise3_ImproveXZ(Seed, s * v.x, s * v.y, s * v.z) / s;
                }
#if true
                m_Vertices[z * w + x] = v * (n * 0.147f + 1.0f) * Size;
#else
                m_Vertices[z * w + x] = v * Size;
#endif
                m_UV[z * w + x] = new Vector2(xx * 0.5f + 0.5f, zz * 0.5f + 0.5f);


                var go = GameObject.Instantiate(HandlePrefab, ContentRoot);
                var data = go.GetComponent<OctahedralExtraData>();

                data.X = x;
                data.Z = z;
                data.ArrayIndex = z * w + x;
                data.ShowGizmos = this.ShowGizmos;
                go.transform.position = m_Vertices[z * w + x];
            }
        }

        for (int j = 0; j < w - 1; j++) {
            for (int i = 0; i < w - 1; i++) {
                /**
                 * If only one of the directions is < w/2 we are on
                 * the quadrant where the quads go like this:
                 *  +-+
                 *  |/|
                 *  +-+
                 *  i.e top left quadrant and bottom right quadrant
                 *  in the project description image.
                 */
                if ((j < w / 2) ^ (i < w / 2)) {
                    // The one I am at
                    m_Indices.Add(j * w + i);
                    // The one above
                    m_Indices.Add(j * w + i + w);
                    // The one above and next
                    m_Indices.Add(j * w + i + w + 1);

                    m_Indices.Add(j * w + i);
                    m_Indices.Add(j * w + i + w + 1);
                    m_Indices.Add(j * w + i + 1);
                }
                /**
                 * If both or neither direction is < w/2 we are on the
                 * other two quadrants, where triangles go like this:
                 * 
                 * +-+
                 * |\|
                 * +-+
                 * 
                 * i.e. top right and bottom left in the project description 
                 * image.
                 */
                else {
                    m_Indices.Add(j * w + i);
                    m_Indices.Add(j * w + i + w);
                    m_Indices.Add(j * w + i + 1);

                    m_Indices.Add(j * w + i + 1);
                    m_Indices.Add(j * w + i + w);
                    m_Indices.Add(j * w + i + w + 1);
                }
            }
        }

        for (int triangle0 = 0; triangle0 < m_Indices.Count; triangle0 += 3) {
            int[] idx = new int[3];
            Vector3 v0, v1, v2;

            idx[0] = m_Indices[triangle0 + 0];
            idx[1] = m_Indices[triangle0 + 1];
            idx[2] = m_Indices[triangle0 + 2];

            v0 = m_Vertices[idx[0]];
            v1 = m_Vertices[idx[1]];
            v2 = m_Vertices[idx[2]];

            Vector3 faceNorm = Vector3.Cross(v1 - v0, v2 - v0);

            m_Normals[idx[0]] += faceNorm;
            m_Normals[idx[1]] += faceNorm;
            m_Normals[idx[2]] += faceNorm;

            /**
             *   20 21 22 23 24
             *   15 16 17 18 19
             *   10 11 12 13 14
             * ^  5  6  7  8  9 
             * z  0  1  2  3  4
             *     x>
             */
#if true
            for (int l = 0; l < 3; l++) {
                int x = idx[l] % w;
                int z = idx[l] / w;

                // We are on the left or right edge of the grid but
                // not in the middle (indices 2 and 22 above) of the
                // X axis, so we have to reflect. So index 1 would reflect
                // to index 3 here.
                if ((z == 0 || z == w - 1) && x != w - x - 1)
                    m_Normals[z * w + w - x - 1] += faceNorm;

                // We are on the top or bottom edge of the grid but
                // not in the middle (indices 10 and 14 above) of the
                // Z axis, so we have to reflect. Index 1 would reflect
                // to index 21 here.
                if ((x == 0 || x == w - 1) && z != w - z - 1)
                    m_Normals[(w - z - 1) * w + x] += faceNorm;

                // We are on any of the corners (indices 0, 5, 20, 24 above)
                if ((x == 0 || x == w - 1) && (z == 0 || z == w - 1))
                    m_Normals[(w - z - 1) * w + w - x - 1] += faceNorm;
            }
#endif
        }

        for (int i = 0; i < m_Normals.Count; i++) {
            m_Normals[i].Normalize();
        }

        var mesh = new Mesh();
        mesh.name = "Planet";
        mesh.vertices = m_Vertices.ToArray();
        mesh.normals = m_Normals.ToArray();
        mesh.uv = m_UV.ToArray();
        mesh.triangles = m_Indices.ToArray();

        m_MeshFilter.sharedMesh = mesh;
        Debug.Log("Done rebuilding");
    }

    public void ToggleGizmos() {
        ShowGizmos = !ShowGizmos;

        var data = ContentRoot.GetComponentsInChildren<OctahedralExtraData>();

        foreach (var d in data) {
            d.ShowGizmos = ShowGizmos;
        }

        Debug.Log($"Toggled {data.Length} objects");
    }

    /**
     * Clears all the handles that might have already been added to ContentRoot 
     * from a previous build
     */
    private void ClearHandles() {
        if (ContentRoot != null) {
            while (ContentRoot.childCount > 0) {
                GameObject.DestroyImmediate(ContentRoot.GetChild(0).gameObject);
            }
        }
    }
}
