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
        vertices = _v;
        barycenter = GetBarycenter();
    }

    public List<SuperVertice> vertices;
    public Vector3 barycenter;
    public GameObject primitive;

    public Vector3 GetBarycenter()
    {
        Vector3 r_ = new Vector3();
        r_[0] = (vertices[0].vertice.x + vertices[1].vertice.x + vertices[2].vertice.x) / 3.0f;
        r_[1] = (vertices[0].vertice.y + vertices[1].vertice.y + vertices[2].vertice.y) / 3.0f;
        r_[2] = (vertices[0].vertice.z + vertices[1].vertice.z + vertices[2].vertice.z) / 3.0f;
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

    public void ChangeRangeTo(int _range, int prev_range, PrimitiveType _type, GameObject _obj)
    {
        if (triangles.Count == 0) return;
        int start = unaltered_triangles.Count - _range < 0 ? 0 : unaltered_triangles.Count - _range;
        List<SuperTriangle> tr = unaltered_triangles.GetRange(start, _range - prev_range);


        foreach (var t in tr)
        {
            GameObject prim = GameObject.CreatePrimitive(_type);
            prim.transform.localScale = new Vector3(0.05f, 0.05f, 0.05f);
            prim.transform.parent = _obj.transform;
            //prim.AddComponent<Rigidbody>();
            Vector3 N = Vector3.Cross(t.vertices[1].vertice - t.vertices[0].vertice, t.vertices[2].vertice - t.vertices[1].vertice);
            prim.transform.rotation = Quaternion.LookRotation(N);
            t.primitive = prim;
            primitives.Add(prim);
            prim.transform.localPosition = new Vector3(t.barycenter[0], t.barycenter[1], t.barycenter[2]);
            Collider[] overlap = Physics.OverlapBox(prim.transform.localPosition, prim.transform.localScale / 2);
            int i = 0;
            //while (overlap.Length > 4)
            //{
            //    overlap = Physics.OverlapBox(prim.transform.localPosition, prim.transform.localScale / 2);
            //    //prim.transform.localScale /= 2;
            //    //prim.SetActive(false);
            //    //primitives.Remove(prim);
            //    //GameObject.Destroy(prim);
            //}

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
            Vector3 N = Vector3.Cross(_tr[i].vertices[1].vertice - _tr[i].vertices[0].vertice, _tr[i].vertices[2].vertice - _tr[i].vertices[0].vertice);
            ////if (primitives[i].transform.rotation != Quaternion.LookRotation(N))
            primitives[i].transform.rotation = Quaternion.Lerp(primitives[i].transform.rotation, Quaternion.LookRotation(N), t);
        }
    }

    public void LerpInCircle(GameObject _o, float t)
    {
        for (int i = 0; i < primitives.Count; ++i)
        {
            //Vector3 F, A, C;
            float radius = 5.0f;
            float angle = Mathf.LerpAngle(0.0f, 360.0f, t);
            float sin = Mathf.Sin(angle);
            float cos = Mathf.Cos(angle);
           primitives[i].transform.localPosition = Vector3.Lerp(primitives[i].transform.localPosition, new Vector3(radius * cos, 0, radius * sin), t);
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
                if (v.initIndex == i)
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

        if (triangles.Count == 0) return null;

        foreach (var t in triangles)
            foreach (var v in t.vertices)
                r_.Add(v.initIndex);

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
                r_ = triangles.OrderBy(t => t.barycenter[0]); //x
                break;
            case Axis.Y:
                r_ = triangles.OrderBy(t => t.barycenter[1]); //y
                break;
            case Axis.Z:
                r_ = triangles.OrderBy(t => t.barycenter[2]); //z
                break;
            default:
                r_ = triangles.OrderBy(t => t.barycenter[1]);
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


