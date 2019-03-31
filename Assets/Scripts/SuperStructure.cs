using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SuperVertice// les vertices triées par un axe (x ou y ou z)
{
    public SuperVertice(int _i, Vector3 _v)
    {
        initIndex = _i;
        vertice = _v;
    }
    public int initIndex;      // l'indice du vertice  dans le tableau initial de vertices
    public Vector3 vertice;    // la position du vertice

    public new string ToString() => "ID: " + initIndex + ", Vertice: " + vertice.ToString() + "\n";
}

public class SuperTriangle
{
    public SuperTriangle(ref List<SuperVertice> _v)
    {
        if (_v.Count > 3)
            throw new System.Exception("Trop d'indices");
        vertices = new int[3] { _v[0].initIndex, _v[1].initIndex, _v[2].initIndex };
        barycenter = GetBarycenter(ref _v);
        normal = Vector3.Cross(_v[1].vertice - _v[0].vertice, _v[2].vertice - _v[1].vertice);
    }

    public int[] vertices;
    public Vector3 barycenter;
    public Vector3 normal;
    public GameObject primitive;

    public Vector3 GetBarycenter(ref List<SuperVertice> _v)
    {
        Vector3 r_ = new Vector3();
        r_[0] = (_v[0].vertice.x + _v[1].vertice.x + _v[2].vertice.x) / 3.0f;
        r_[1] = (_v[0].vertice.y + _v[1].vertice.y + _v[2].vertice.y) / 3.0f;
        r_[2] = (_v[0].vertice.z + _v[1].vertice.z + _v[2].vertice.z) / 3.0f;
        return r_;
    }

    public new string ToString()
    {
        string r_ = "Vertice: ";
        foreach (var v in vertices)
            r_ += "\t" + v.ToString() + "\n";
        return r_;
    }
}

public class TrianglesList
{
    public enum Axis
    {
        X,
        Y,
        Z
    }

    public List<SuperTriangle> triangles;
    public List<SuperTriangle> unaltered_triangles;

    public List<GameObject> primitives;

    public TrianglesList()
    {
        triangles = new List<SuperTriangle>();
        unaltered_triangles = new List<SuperTriangle>();
        primitives = new List<GameObject>();
    }
    public TrianglesList(int[] _t, ref List<SuperVertice> _sv)
    {
        triangles = new List<SuperTriangle>();
        for (int i = 0; i < _t.Length - 2; i += 3)
        {
            List<SuperVertice> lsv = new List<SuperVertice>
            {
                //new SuperVertice(_t[i], _sv[_t[i]].vertice),
                //new SuperVertice(_t[i + 1],_sv[_t[i + 1]].vertice),
                //new SuperVertice(_t[i + 2], _sv[_t[i + 1]].vertice)
                _sv[_t[i]],
                _sv[_t[i + 1]],
                _sv[_t[i + 2]]
            };

            var v = new SuperTriangle(ref lsv);
            if (!triangles.Contains(v))
                triangles.Add(v);
        }
        unaltered_triangles = triangles;
        primitives = new List<GameObject>();
        //Debug.Log("DoneInit");
    }

    public SuperTriangle Pop()
    {
        if (triangles.Count == 0) return null;
        SuperTriangle r_ = triangles[triangles.Count - 1];
        //triangles.RemoveAt(triangles.Count - 1);
        triangles = triangles.GetRange(0, triangles.Count - 1);
        return r_;
    }

    public List<SuperTriangle> PopRange(int _range)
    {
        if (triangles.Count == 0) return new List<SuperTriangle>();
        int start = unaltered_triangles.Count - _range < 0 ? 0 : unaltered_triangles.Count - _range;

        List<SuperTriangle> r_ = unaltered_triangles.GetRange(start, _range);

        triangles = unaltered_triangles.GetRange(0, unaltered_triangles.Count - _range);
        return r_;

    }


    GameObject progenitor;
    public void ChangeRangeTo(int _range, int prev_range, PrimitiveType _type, GameObject _obj, Color _color, bool _overlap)
    {
        if (triangles.Count == 0) return;
        int start = unaltered_triangles.Count - _range < 0 ? 0 : unaltered_triangles.Count - _range;
        List<SuperTriangle> tr = unaltered_triangles.GetRange(start, _range - prev_range);
        Material material = null;
        if (progenitor == null)
        {
            progenitor = GameObject.CreatePrimitive(_type);
            material = progenitor.GetComponent<MeshRenderer>().material;
            progenitor.GetComponent<MeshRenderer>().material.enableInstancing = true;
            progenitor.transform.localScale = new Vector3(0.05f, 0.05f, 0.05f);
            //prim.AddComponent<Rigidbody>();
            material.color = _color;
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            material.hideFlags = HideFlags.DontSaveInEditor;
        }

        foreach (var t in tr)
        {
            GameObject prim = GameObject.Instantiate(progenitor);
            //Graphics.DrawMesh(progenitor.GetComponent<MeshFilter>().mesh, t.barycenter, Quaternion.LookRotation(t.normal), material, 0);
            prim.transform.parent = _obj.transform;
            Vector3 N = t.normal;
            if (N != Vector3.zero)
                prim.transform.rotation = Quaternion.LookRotation(N);
            t.primitive = prim;
            primitives.Add(prim);
            prim.transform.localPosition = t.barycenter;
            prim.tag = "Primitives";
            Collider[] overlap = Physics.OverlapBox(prim.transform.position, prim.transform.localScale * 2);
            int i = 0;
            if (_overlap)
                if (overlap.Length > 1)
                {
                    //prim.transform.localScale /= 1 + (i++ * .1f);
                    //overlap = Physics.OverlapSphere(prim.transform.position, 2 * prim.transform.localScale.x + (10.0f / 100.0f * prim.transform.localScale.x));
                    //if (i > 1000)
                    //{
                    //    Debug.Log("Break");
                    //    break;
                    //}
                    //prim.SetActive(false);
                    primitives.Remove(prim);
                    GameObject.Destroy(prim);
                }

        }

    }

    public void ReformRange(int _range, int prev_range, bool sens = true)
    {
        int start = primitives.Count - _range < 0 ? 0 : primitives.Count - _range;


        List<GameObject> objs = primitives.GetRange(start, _range - prev_range);
        List<SuperTriangle> tr = unaltered_triangles.GetRange(start, _range - prev_range);
        List<GameObject> toDestroy = new List<GameObject>();

        if (sens)
        {
            foreach (var o in objs)
                o.SetActive(false);

            foreach (var t in tr)
                triangles.Add(t);
        }
        else
        {
            foreach (var o in objs)
                o.SetActive(true);

            triangles = tr;
        }

        //for (int i = 0; i < toDestroy.Count; i++)
        //{
        //    primitives.Remove(toDestroy[i]);
        //    GameObject.Destroy(toDestroy[i]);
        //}

    }

    public void SwitchRigidbody()
    {
        foreach (var o in primitives)
        {

            Rigidbody rigidbody = o.GetComponent<Rigidbody>();
            if (rigidbody)
                GameObject.Destroy(rigidbody);
            else
            {
                o.AddComponent<Rigidbody>();
                //rigidbody.detectCollisions = !o.GetComponent<Rigidbody>().detectCollisions;
                //rigidbody.useGravity = !o.GetComponent<Rigidbody>().useGravity;
            }

        }
    }

    public void LerpAllTo(List<SuperTriangle> _tr, float t)
    {

        for (int i = 0; i < primitives.Count; ++i)
        {
            //if (primitives[i].transform.localPosition != _tr[i].barycenter)
            primitives[i].transform.localPosition = Vector3.Slerp(primitives[i].transform.localPosition, _tr[i].barycenter, t);
            Vector3 N = _tr[i].normal;
            ////if (primitives[i].transform.rotation != Quaternion.LookRotation(N))
            primitives[i].transform.rotation = Quaternion.Lerp(primitives[i].transform.rotation, Quaternion.LookRotation(N), t);
        }
    }

    public void LerpInCircle(GameObject _i, GameObject _o, float t, int nbTriangles)
    {
        int max = GameObject.FindGameObjectsWithTag("Primitives").Length;
        for (int i = 0; i < max; ++i)
        {
            Vector3 SpawnPos = new Vector3(_o.transform.position.x + (Mathf.Cos(i * (2 * Mathf.PI / nbTriangles)) /** _o.GetComponent<Renderer>().bounds.size.x*/),
                /*_o.transform.position.y*/0,
                _o.transform.position.z + Mathf.Sin(i * (2 * Mathf.PI / nbTriangles)) /** _o.GetComponent<Renderer>().bounds.size.x*/);

            primitives[i].transform.position = Vector3.Slerp(primitives[i].transform.position, SpawnPos, t);
        }
    }

    //Not used, too greedy
    public SuperVertice Find(int _i, List<SuperVertice> _sv)
    {
        foreach (var v in _sv)
        {
            if (v.initIndex == _i)
                return v;
        }
        return null;
    }

    public TrianglesList(List<SuperTriangle> _t)
    {
        triangles = _t;
    }


    /// <summary>
    /// Deprecated, too greedy.
    /// </summary>
    /// <param name="i"></param>
    /// <returns> All triangles with indice i. </returns>
    private List<SuperTriangle> GetAllTrianglesWithInd(int i)
    {
        List<SuperTriangle> r = new List<SuperTriangle>();
        foreach (var t in triangles)
        {
            foreach (var v in t.vertices)
            {
                if (v == i)
                {
                    r.Add(t);
                    break;
                }

            }
        }
        return r;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public int[] ToIntList()
    {
        List<int> r_ = new List<int>();

        if (triangles.Count == 0) return new int[0];

        foreach (var t in triangles)
            r_.AddRange(t.vertices);

        return r_.ToArray();
    }

    ///Debug
    public List<float> ToFloatHeightList()
    {
        List<float> r_ = new List<float>();

        foreach (var t in triangles)
            r_.Add(t.barycenter[1]);

        return r_;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    private IOrderedEnumerable<SuperTriangle> SortTrianglesByHeight()
    {
        IOrderedEnumerable<SuperTriangle> r_ = triangles.OrderBy(t => t.barycenter[1]);
        unaltered_triangles = r_.ToList();
        return r_;
    }

    /// <summary>
    /// Sorts triangles according to an axis
    /// </summary>
    /// <param name="_a"></param>
    /// <returns> IOrderedEnumerable<SuperTriangle> (use ToList() orToArray() to convert it. </returns>
    private IOrderedEnumerable<SuperTriangle> SortTrianglesBy(Axis _a)
    {
        IOrderedEnumerable<SuperTriangle> r_ = null;
        switch (_a)
        {
            case Axis.X:
                r_ = triangles.OrderBy(t => t.barycenter.x); //x
                break;
            case Axis.Y:
                r_ = triangles.OrderBy(t => t.barycenter.y); //y
                break;
            case Axis.Z:
                r_ = triangles.OrderBy(t => t.barycenter.z); //z
                break;
            default:
                r_ = triangles.OrderBy(t => t.barycenter.y);
                break;

        }
        unaltered_triangles = r_.ToList();
        return r_;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    private int[] GetSortedTrianglesInd()
    {
        triangles = SortTrianglesByHeight().ToList();
        return ToIntList();
    }

    /// <summary>
    /// Sorts triangles according to an axis
    /// </summary>
    /// <param name="_a">Axis</param>
    /// <returns> List<SuperTriangle> sorted. </returns>
    public List<SuperTriangle> SortTriangles(Axis _a)
    {
        triangles = SortTrianglesBy(_a).ToList();
        return triangles;
    }

    public void Add(ref SuperTriangle _t)
    {
        triangles.Add(_t);
    }

    public new string ToString()
    {
        string r_ = "Triangles: ";
        foreach (var t in triangles)
            r_ += "\t" + t.ToString() + "\n";
        return r_;
    }

}


