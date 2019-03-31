using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SuperTriangleEMILIEN
{
    public Vector3[] tmp_triplet;
    public int[] tmp_index;
    public Vector3 trianglePos;

    public SuperTriangleEMILIEN(Vector3[] _tmp_triplet, int[] _tmp_index, Vector3 _trianglePos)
    {
        this.tmp_triplet = _tmp_triplet;
        this.tmp_index = _tmp_index;
        this.trianglePos = _trianglePos;
    }
    public float getAxis(string axis)
    {
        if (axis == "x") return trianglePos.x;
        else if (axis == "y") return trianglePos.y;
        else if (axis == "z") return trianglePos.z;
        else return 0;
    }
    public int getIndex0()
    {
        return tmp_index[0];
    }
    public int getIndex1()
    {
        return tmp_index[1];
    }
    public int getIndex2()
    {
        return tmp_index[2];
    }
}


public class deconstruction : MonoBehaviour
{

    private int tj = 0;
    private int ti = 0;

    public static bool isdeconstructing = false;

    private int[] triangles;
    private Vector3[] vertices;
    public List<SuperTriangleEMILIEN> UnsortedTriangles;
    public List<SuperTriangleEMILIEN> SortedTriangles;

    public Mesh mesh;
    public int[] newTriangles;

    //public bool iscoroutinestarted = false;

    //private IEnumerator coroutine;

    int start = 0;
    int count;
    // Use this for initialization
    void Start()
    {
        mesh = transform.GetComponent<MeshFilter>().mesh;

        triangles = mesh.triangles;
        vertices = mesh.vertices;

        UnsortedTriangles = new List<SuperTriangleEMILIEN>();
        SortedTriangles = new List<SuperTriangleEMILIEN>();


        for (int i = 0; i != mesh.triangles.Length / 3; i++)
        {


            Vector3[] tmp_triplet = new Vector3[3];
            int[] tmp_index = new int[3];
            Vector3 trianglePos;

            tmp_index[0] = mesh.triangles[i * 3];
            tmp_index[1] = mesh.triangles[i * 3 + 1];
            tmp_index[2] = mesh.triangles[i * 3 + 2];

            //print("tmp_index " + tmp_index[0] + " " + tmp_index[1] + " " + tmp_index[2]);

            tmp_triplet[0] = vertices[tmp_index[0]];
            tmp_triplet[1] = vertices[tmp_index[1]];
            tmp_triplet[2] = vertices[tmp_index[2]];

            trianglePos = (tmp_triplet[0] + tmp_triplet[1] + tmp_triplet[2])/3.0f;

            SuperTriangleEMILIEN tmp_triangles = new SuperTriangleEMILIEN(tmp_triplet, tmp_index, trianglePos);

            print(i + " indices: " + " " + tmp_triangles.tmp_index[0] + " " + tmp_triangles.tmp_index[1] + " " + tmp_triangles.tmp_index[2] + "pos: " + trianglePos.y);

            UnsortedTriangles.Add(tmp_triangles);
            count = SortedTriangles.Count - 1;

        }
    }

    // Update is called once per frame
    
    private void FixedUpdate()
    {
        string input = Input.inputString;

        switch (input)
        {
            case "x":
                SortedTriangles = UnsortedTriangles.OrderByDescending(y => y.getAxis("x")).ToList();
                //print(SortedTriangles[0].getIndex(0) +" "+ SortedTriangles[0].getIndex(1) +" "+ SortedTriangles[0].getIndex(2));
                //Rewrite();
                isdeconstructing = true;
                //StartCoroutine("Rewrite");
                //Rewrite();

                break;

            case "y":
                SortedTriangles = UnsortedTriangles.OrderByDescending(y => y.getAxis("y")).ToList();
                //Rewrite();
                isdeconstructing = true;
                //StartCoroutine("Rewrite");
                //Rewrite();

                break;
            case "z":
                SortedTriangles = UnsortedTriangles.OrderByDescending(y => y.getAxis("z")).ToList();
                //Rewrite();
                isdeconstructing = true;
                //StartCoroutine("Rewrite");
                //Rewrite();

                break;
            case "s":
                SphereEffect();
                break;
        }

        if(isdeconstructing)
        {
            Rewrite(ref start, count);
        }

    }

    //public GameObject sphere;
    //public Vector3 spherePos;

    private void SphereEffect()
    {
        //mesh = transform.GetComponent<MeshFilter>().mesh;

        for (int i = 0; i < mesh.triangles.Length; i++)
        {
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);

            sphere.transform.parent = gameObject.transform;
            sphere.transform.localScale = new Vector3(0.03f, 0.03f, 0.03f);
            sphere.transform.localPosition = SortedTriangles[i].trianglePos;
            //spherePos = transform.TransformPoint(SortedTriangles[i].trianglePos);
            //Instantiate(sphere, spherePos, sphere.transform.rotation);
        }

        //throw new NotImplementedException();
    }

    private void Rewrite(ref int start, int count)
    {
        List<SuperTriangleEMILIEN> Currenttriangles = SortedTriangles;

        count = SortedTriangles.Count;
        if (isdeconstructing == true)
        {
            //while (i < j)
            //{
            //if (i > 0 && j < SortedTriangles.Count)
            List<SuperTriangleEMILIEN> newtriangles = (start < 0 || (count - start) >= SortedTriangles.Count) ? SortedTriangles.GetRange(0, 0) : SortedTriangles.GetRange(start, count - start);
            //for (int k = 0; k < SortedTriangles.Count - i; k++)
            //{
            //    mesh.triangles[k * 3] = newtriangles[k].getIndex0();
            //    mesh.triangles[k * 3 + 1] = newtriangles[k].getIndex0();
            //    mesh.triangles[k * 3 + 2] = newtriangles[k].getIndex0();
            //}
            mesh.triangles = ToIntList(newtriangles);
            start++;
            Debug.Log(start);
            //}
        }
    }

    private int[] ToIntList(List<SuperTriangleEMILIEN> _list)
    {
        List<int> result = new List<int>();

        foreach (var t in _list)
            foreach (var i in t.tmp_index)
                result.Add(i);

        return result.ToArray();

    }
}

