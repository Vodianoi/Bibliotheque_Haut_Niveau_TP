using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.UI;
using System;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class Deshabillage : MonoBehaviour
{
    private List<SuperVertice> superVertices_; //Vertices list (same for both meshes)

    /// <summary>
    /// Deconstructed MeshFilter
    /// </summary> 
    private MeshFilter in_mesh_f;           //*
    /// <summary>
    /// Deconstructed MeshRenderer
    /// </summary> 
    private MeshRenderer in_mesh_r;         // Deconstructed mesh
    /// <summary>
    /// Deconstructed Triangles List
    /// </summary>   
    private TrianglesList in_triangles_;    //*
    GameObject in_;

    /// <summary>
    /// Reconstructed MeshFilter
    /// </summary> 
    private MeshFilter out_mesh_f;          //*
    /// <summary>
    /// Reconstructed MeshRenderer
    /// </summary> 
    private MeshRenderer out_mesh_r;        // Reconstructed mesh
    /// <summary>
    /// Reconstructed Triangles List
    /// </summary>  
    private TrianglesList out_triangles_;   //*
    GameObject out_;


    private TrianglesList CurrentTrianglesList_;

    private List<int> intervals_;
    private int nbFrameTotal_;

    //Number of remaining vertices to unwrap
    private int nbTriangle_dec;
    //Number of vertices
    private int nbTriangle;
    Text txt;

    public Color PrimColor;

    Vector3 Size;

    [Range(1.0f, 100f)]
    private float speed = 50.0f;
    private float m_Speed = 50.0f;

    private float timer = 0.0f;
    public float time = 0;


    int k = 0;
    int nbTrianglesToDestroy = 0;
    int prev_nbTrianglesToDestroy = 0;

    private bool PrimCreated;
    private bool sorted;
    private bool computed;

    bool Original = true;

    /// <summary>
    /// Deconstruction axis.
    /// </summary>
    public TrianglesList.Axis axis;

    /// <summary>
    /// Debug triangles order in editor
    /// </summary>
    private List<float> v3Triangles;

    public delegate void OnSpeedChangeDelegate(float newSpeed);
    public event OnSpeedChangeDelegate OnSpeedChange;

    public delegate void OnStateChangeDelegate(State newState);
    public event OnStateChangeDelegate OnStateChange;


    private State state = State.Idle;
    private State m_State = State.Idle;

    private float StartTime;

    public float TimeLerp;

    private float timePerTriangle;
    private float frameSize;

    public bool Overlap;


    public PrimitiveType primitiveType;
    
    public enum State
    {
        Idle,
        Wrapping,
        RecreatingTriangles,
        Changing,
        Moving,
        Sliding,
        Computing,
        LinearWrapping
    }

    #region MonoBehaviour Methods
    void Start()
    {
        Clear();
        txt = FindObjectOfType<Text>();
        OnSpeedChange += SpeedChangeHandler; //Associate event "OnSpeedChange" to "SpeedChangeHandler" method
        OnStateChange += StateChangeHandler; //Associate event "OnStateChange" to "StateChangeHandler" method
        Size = new Vector3(
            Mathf.Abs(in_triangles_.triangles[0].barycenter[0] - in_triangles_.triangles[in_triangles_.triangles.Count - 1].barycenter[0]),
            Mathf.Abs(in_triangles_.triangles[0].barycenter[1] - in_triangles_.triangles[in_triangles_.triangles.Count - 1].barycenter[1]),
            Mathf.Abs(in_triangles_.triangles[0].barycenter[2] - in_triangles_.triangles[in_triangles_.triangles.Count - 1].barycenter[2])
        );

        //CurrentTrianglesList_ = in_triangles_;
        PrepareTransform();
    }


    private void Update()
    {

        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("Sorted");
            PrepareTransform();
        }

        if (Input.GetKeyDown(KeyCode.U))
        {
            //Debug.Log("OBO");
            UnwrapOBO();
        }
        if (Input.GetKey(KeyCode.KeypadEnter))
        {
            Debug.Log("Unwrapping...");
            //Unwrap();
            nbTrianglesToDestroy = 0;
            state = State.Wrapping;
            timer = 0;
            k = 0;
        }
        if (Input.GetKeyDown(KeyCode.C))
        {
            Debug.Log("Changing...");
            //in_triangles_.ChangeRangeTo()
            //gameObject.GetComponent<Collider>().enabled = false;
            state = State.Changing;
            nbTrianglesToDestroy = 0;
            timer = 0;
            k = 0;
        }
        if (Input.GetKeyDown(KeyCode.L))
        {
            Debug.Log("Lerping...");
            if (Original)
            {
                if (in_.transform.GetChild(0).GetComponent<Rigidbody>())
                    in_triangles_.SwitchRigidbody();
            }
            else
            {
                if (out_.transform.GetChild(0).GetComponent<Rigidbody>())
                    out_triangles_.SwitchRigidbody();
            }
            state = State.Moving;
            StartTime = Time.fixedTime;
            nbTrianglesToDestroy = 0;
            timer = 0;
            k = 0;
        }
        if (Input.GetKeyDown(KeyCode.G))
        {
            Debug.Log("Switch Gravity (Rigidbody)");
            if (Original)
                in_triangles_.SwitchRigidbody();
            else
                out_triangles_.SwitchRigidbody();
        }
        if (Input.GetKeyDown(KeyCode.R))
        {
            Debug.Log("Reforming...");
            state = State.RecreatingTriangles;
            nbTrianglesToDestroy = 0;
            timer = 0;
            k = 0;
        }
        if (Input.GetKeyDown(KeyCode.O))
        {
            Debug.Log("Sliding...");
            state = State.Sliding;
            nbTrianglesToDestroy = 0;
            StartTime = Time.fixedTime;
            timer = 0;
            k = 0;
        }
        if (Input.GetKeyDown(KeyCode.K))
        {
            Debug.Log("Linear Unwrapping...");
            state = State.LinearWrapping;
            nbTrianglesToDestroy = 0;
            timer = 0;
            k = 0;
        }
        if (Input.GetKeyDown(KeyCode.Alpha0))
        {
            Debug.Log("Switch gameobjects.");
            SwitchTrianglesList();
        }

    }


    void FixedUpdate()
    {


        //int delTriangles = nbTriangle_dec - (nbTriangle - nbTriangle_dec);

        //// if ((lastNbDelTriangle - delTriangles) != 0 && (lastNbDelTriangle - delTriangles) != nbTriangle)
        //txt.text = "Triangles supprimés: " + (lastNbDelTriangle - delTriangles);
        //lastNbDelTriangle = delTriangles;

        switch (state)
        {
            case State.Changing:
                ChangeRepeat();
                SaveToMesh();
                break;


            case State.Computing:
                ComputeIntervals();
                break;


            case State.LinearWrapping:
                LinearUnwrapRepeat();
                SaveToMesh();
                break;


            case State.Moving:
                {
                    float timeSinceStarted = Time.fixedTime - StartTime;
                    float percentageComplete = timeSinceStarted / TimeLerp;

                    if (Original)
                        in_triangles_.LerpAllTo(in_triangles_.unaltered_triangles, percentageComplete);
                    else
                        out_triangles_.LerpAllTo(out_triangles_.unaltered_triangles, percentageComplete);

                    if (percentageComplete >= 1.0f)
                    {
                        Debug.Log("Stop moving");
                        state = State.Idle;
                    }
                    break;
                }


            case State.RecreatingTriangles:
                if (Original ? in_triangles_.primitives.Count == 0 : out_triangles_.primitives.Count == 0)
                    state = State.Idle;
                else
                {
                    ReformRepeat();
                    SaveToMesh();
                }
                break;


            case State.Sliding:
                {
                    float timeSinceStarted = Time.fixedTime - StartTime;
                    float percentageComplete = timeSinceStarted / TimeLerp;
                    if (Original)
                        in_triangles_.LerpInCircle(in_, out_, percentageComplete, nbTriangle);
                    else
                        out_triangles_.LerpInCircle(out_, in_, percentageComplete, nbTriangle);

                    if (percentageComplete >= 1.0f)
                    {
                        SwitchTrianglesList();
                        Debug.Log("Stop sliding");

                    }
                    break;
                }


            case State.Wrapping:
                UnwrapRepeat();
                SaveToMesh();
                break;
        }

        if (m_Speed != speed && OnSpeedChange != null)
        {
            m_Speed = speed;
            OnSpeedChange(speed);
        }

        if (m_State != state && OnStateChange != null)
        {
            m_State = state;
            OnStateChange(state);
        }
    }
    #endregion

    #region Private Methods

    void UnwrapRepeat()
    {
        while (Time.fixedDeltaTime * k > timer && nbTrianglesToDestroy < nbTriangle)
        {
            timer += timePerTriangle;
            nbTrianglesToDestroy++;
        }
        //out_triangles_ = new TrianglesList();
        //Debug.Log(timer);

        if (Original ? in_triangles_.triangles.Count != 0 : out_triangles_.triangles.Count != 0)
        {
            List<SuperTriangle> lst = Original ? in_triangles_.PopRange(nbTrianglesToDestroy) : out_triangles_.PopRange(nbTrianglesToDestroy);
            if (Original)
                out_triangles_.triangles = lst;
            else
                in_triangles_.triangles = lst;

        }
        else
        {
            Debug.Log("Stop wrapping");
            SwitchTrianglesList();
        }

        k++;


    }

    /// <summary>
    /// Unwrap with linear speed (compute with time variable"
    /// </summary>
    void LinearUnwrapRepeat()
    {
        if (!computed)
        {
            ComputeIntervals();
            Debug.Log("not computed yet");
            return;
        }
        while (Time.fixedDeltaTime * k > timer && nbTrianglesToDestroy < nbTriangle)
        {
            timer += frameSize;
            nbTrianglesToDestroy++;
        }

        //Debug.Log("frame: " + nbTrianglesToDestroy);
        List<SuperTriangle> lst = Original ? in_triangles_.PopRange(nbTrianglesToDestroy) : out_triangles_.PopRange(nbTrianglesToDestroy);
        //out_triangles_ = new TrianglesList();
        if (Original)
            out_triangles_.triangles = lst;
        else
            in_triangles_.triangles = lst;
        //Debug.Log(timer);
        k++;
        if (nbTrianglesToDestroy >= nbTriangle)
        {
            Debug.Log("Stop linear wrapping");
            SwitchTrianglesList();
        }
    }

    /// <summary>
    /// Compute intervals for linear unwrapping
    /// </summary>
    void ComputeIntervals()
    {
        while (Time.fixedDeltaTime * k > timer && nbTrianglesToDestroy < nbTriangle)
        {
            timer += timePerTriangle;
            nbTrianglesToDestroy++;
        }
        k++;
        nbFrameTotal_++;
        if (nbTrianglesToDestroy >= nbTriangle)
        {
            Debug.Log("Stop compute intervals");
            switch (axis)
            {
                case TrianglesList.Axis.X:
                    frameSize = Size.x / nbFrameTotal_;
                    break;
                case TrianglesList.Axis.Y:
                    frameSize = Size.y / nbFrameTotal_;
                    break;
                case TrianglesList.Axis.Z:
                    frameSize = Size.z / nbFrameTotal_;
                    break;
            }
            state = State.Idle;
            computed = true;
        }
    }

    /// <summary>
    /// Switch objects
    /// </summary>
    void SwitchTrianglesList()
    {

        PrimCreated = false;

        Original = !Original;
        in_triangles_.primitives = new List<GameObject>();
        out_triangles_.primitives = new List<GameObject>();
        if (Original)
        {
            nbTriangle = in_triangles_.unaltered_triangles.Count;
            foreach (var prim in GameObject.FindGameObjectsWithTag("Primitives"))
            {
                prim.transform.SetParent(in_.transform);
                in_triangles_.primitives.Add(prim);
            }
        }
        else
        {
            nbTriangle = out_triangles_.unaltered_triangles.Count;
            foreach (var prim in GameObject.FindGameObjectsWithTag("Primitives"))
            {
                prim.transform.SetParent(out_.transform);
                out_triangles_.primitives.Add(prim);
            }
        }

        Debug.Log("InPrimNb: " + in_.transform.childCount + ", outPrimNb: " + out_.transform.childCount);
        computed = false;
        state = State.Computing;

    }

    /// <summary>
    /// Change triangles to primitive
    /// </summary>
    void ChangeRepeat()
    {
        prev_nbTrianglesToDestroy = nbTrianglesToDestroy;
        do
        {
            timer += timePerTriangle;
            nbTrianglesToDestroy++;
        }
        while (Time.fixedDeltaTime * k > timer && nbTrianglesToDestroy < nbTriangle);
        if (PrimCreated)
            if (Original)
                in_triangles_.ReformRange(nbTrianglesToDestroy, prev_nbTrianglesToDestroy, false);
            else
                out_triangles_.ReformRange(nbTrianglesToDestroy, prev_nbTrianglesToDestroy, false);
        else
        {
            if (Original)
                in_triangles_.ChangeRangeTo(nbTrianglesToDestroy, prev_nbTrianglesToDestroy, primitiveType, in_, PrimColor, Overlap);
            else
                out_triangles_.ChangeRangeTo(nbTrianglesToDestroy, prev_nbTrianglesToDestroy, primitiveType, out_, PrimColor, Overlap);
        }
        List<SuperTriangle> lst = Original ? in_triangles_.PopRange(nbTrianglesToDestroy) : out_triangles_.PopRange(nbTrianglesToDestroy);

        k++;
        if (nbTrianglesToDestroy >= nbTriangle)
        {
            Debug.Log("Stop Changing");
            state = State.Idle;
        }
    }

    /// <summary>
    /// Recreate triangles
    /// </summary>
    void ReformRepeat()
    {

        if (Original)
        {
            if (nbTriangle > in_.transform.childCount)
                for (int i = 0; i < nbTriangle - in_.transform.childCount; ++i)
                {
                    GameObject prim = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    //Graphics.DrawMesh(progenitor.GetComponent<MeshFilter>().mesh, t.barycenter, Quaternion.LookRotation(t.normal), material, 0);
                    prim.transform.parent = in_.transform;
                    in_triangles_.primitives.Add(prim);
                    prim.transform.localScale = new Vector3(0.05f, 0.05f, 0.05f);

                    prim.transform.localPosition = in_triangles_.unaltered_triangles[nbTriangle - 1 - i].barycenter;
                    prim.tag = "Primitives";
                }
        }
        else
        {
            if (nbTriangle > out_.transform.childCount)
                for (int i = 0; i < nbTriangle - out_.transform.childCount; ++i)
                {
                    GameObject prim = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    //Graphics.DrawMesh(progenitor.GetComponent<MeshFilter>().mesh, t.barycenter, Quaternion.LookRotation(t.normal), material, 0);
                    prim.transform.parent = out_.transform;
                    out_triangles_.primitives.Add(prim);
                    prim.transform.localScale = new Vector3(0.05f, 0.05f, 0.05f);
                    prim.transform.localPosition = out_triangles_.unaltered_triangles[nbTriangle - 1 - i].barycenter;
                    prim.tag = "Primitives";
                }
        }

        prev_nbTrianglesToDestroy = nbTrianglesToDestroy;
        do
        {
            timer += timePerTriangle;
            nbTrianglesToDestroy++;
        }
        while (Time.fixedDeltaTime * k > timer && nbTrianglesToDestroy < nbTriangle);

        if (Original)
            in_triangles_.ReformRange(nbTrianglesToDestroy, prev_nbTrianglesToDestroy);
        else
            out_triangles_.ReformRange(nbTrianglesToDestroy, prev_nbTrianglesToDestroy);
        //List<SuperTriangle> lst = in_triangles_.PopRange(nbTrianglesToDestroy);

        k++;
        if (nbTrianglesToDestroy >= nbTriangle)
        {
            state = State.Idle;
            PrimCreated = true;
        }
    }

    /// <summary>
    /// Invoked when speed variable change (not used anymore)
    /// </summary>
    /// <param name="newSpeed"></param>
    private void SpeedChangeHandler(float newSpeed)
    {
        if (state == State.Wrapping)
        {
            //Debug.Log("Cancel");
            //CancelInvoke("DestroyAfter");
            //Unwrap();
            //StopCoroutine(coroutine);
            //coroutine = StartCoroutine(UnwrapRepeat(speed / 100.0f));

        }
    }


    private void StateChangeHandler(State newState)
    {
        GameObject.Find("State_txt").GetComponent<Text>().text = newState.ToString();
    }

    /// <summary>
    /// Reset
    /// </summary>
    private void Clear()
    {
        in_mesh_f = GetComponent<MeshFilter>();
        in_mesh_r = GetComponent<MeshRenderer>();
        in_ = gameObject;

        out_ = new GameObject();
        out_mesh_f = out_.GetComponent<MeshFilter>() ? out_.GetComponent<MeshFilter>() : out_.AddComponent<MeshFilter>();
        out_mesh_r = out_.GetComponent<MeshRenderer>() ? out_.GetComponent<MeshRenderer>() : out_.AddComponent<MeshRenderer>();
        out_.name = "Reconstruted_" + in_mesh_f.name;
        out_.transform.position = in_.transform.position + new Vector3(3, 0, 0);
        out_.transform.localScale = in_.transform.localScale;

        out_triangles_ = new TrianglesList();

        superVertices_ = new List<SuperVertice>();
        for (int i = 0; i < in_mesh_f.mesh.vertices.Length; i++)
            superVertices_.Add(new SuperVertice(i, in_mesh_f.mesh.vertices[i]));

        out_mesh_f.mesh.vertices = in_mesh_f.mesh.vertices;
        out_mesh_r.material = in_mesh_r.material;

        in_triangles_ = new TrianglesList(in_mesh_f.mesh.triangles, ref superVertices_);
        out_triangles_.unaltered_triangles = in_triangles_.triangles;
        nbTriangle = in_triangles_.triangles.Count;
        nbTriangle_dec = nbTriangle;
        v3Triangles = in_triangles_.ToFloatHeightList();
        intervals_ = new List<int>();
        //computed = false;
    }

    /// <summary>
    /// 
    /// </summary>
    private void SaveToMesh()
    {
        //if (out_triangles_.ToIntList() != null)
        //if (Original)
        //{
        out_mesh_f.mesh.triangles = out_triangles_.ToIntList();
        in_mesh_f.mesh.triangles = in_triangles_.ToIntList();
        nbTriangle_dec = in_triangles_.triangles.Count;
        nbTriangle = in_triangles_.unaltered_triangles.Count;
        //}
        //else
        //{
        //    out_mesh_f.mesh.triangles = in_triangles_.ToIntList();
        //    in_mesh_f.mesh.triangles = out_triangles_.ToIntList();
        //    nbTriangle_dec = out_triangles_.triangles.Count;
        //    nbTriangle = out_triangles_.unaltered_triangles.Count;
        //}



        //if (isWrapping) return;

        //out_mesh_f.mesh.RecalculateBounds();
        out_mesh_f.mesh.RecalculateNormals();
        //out_mesh_f.mesh.RecalculateTangents();

        //in_mesh_f.mesh.RecalculateBounds();
        in_mesh_f.mesh.RecalculateNormals();
        //in_mesh_f.mesh.RecalculateTangents();
    }
    #endregion

    #region Public Methods
    /// <summary>
    /// 
    /// </summary>
    public void PrepareTransform()
    {
        List<SuperTriangle> lstTr = Original ? in_triangles_.SortTriangles(axis) : out_triangles_.SortTriangles(axis);
        if (Original)
        {
            in_triangles_.triangles = lstTr;
            in_triangles_.unaltered_triangles = lstTr;
            out_triangles_.unaltered_triangles = lstTr;
        }
        else
        {
            out_triangles_.triangles = lstTr;
            out_triangles_.unaltered_triangles = lstTr;
            in_triangles_.unaltered_triangles = lstTr;
        }
        SaveToMesh(); //Save infos to mesh
        //out_CurrentTrianglesList_.unaltered_triangles = in_CurrentTrianglesList_.triangles;
        sorted = true;

        timePerTriangle = time / nbTriangle;
        Debug.Log("Pas: " + timePerTriangle);

        state = State.Computing;
        timer = 0;
    }

    /// <summary>
    /// Unwrap triangles one-by-one
    /// </summary>
    public void UnwrapOBO()
    {
        if (!sorted) PrepareTransform();
        SuperTriangle t = Original ? in_triangles_.Pop() : out_triangles_.Pop();
        if (t == null) return;
        //Debug.DrawLine(t.vertices[0].vertice, t.vertices[1].vertice, Color.red, 2.0f);
        //Debug.DrawLine(t.vertices[1].vertice, t.vertices[2].vertice, Color.red, 2.0f);
        //Debug.DrawLine(t.vertices[2].vertice, t.vertices[0].vertice, Color.red, 2.0f);


        SaveToMesh();
    }
    /// <summary>
    /// 
    /// </summary>
    public void Unwrap()
    {
        if (!sorted) PrepareTransform();

        state = State.Wrapping;
        InvokeRepeating("Deconstruct", (float)(nbTriangle_dec / nbTriangle), (100.1f - speed) / 1000.0f);
    }

    /// <summary>
    /// 
    /// </summary>
    void Deconstruct()
    {

        SuperTriangle t = Original ? in_triangles_.Pop() : out_triangles_.Pop();
        if (t != null)
        {
            //Debug.DrawLine(t.vertices[0].vertice, t.vertices[1].vertice, Color.red, 2.0f);
            //Debug.DrawLine(t.vertices[1].vertice, t.vertices[2].vertice, Color.red, 2.0f);
            //Debug.DrawLine(t.vertices[2].vertice, t.vertices[0].vertice, Color.red, 2.0f);
            if (Original)
                out_triangles_.triangles.Add(t);
            else
                in_triangles_.triangles.Add(t);

            SaveToMesh();
        }
        else
            state = State.Idle;

    }
    #endregion
}
