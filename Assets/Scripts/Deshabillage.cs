using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.UI;

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

    private TrianglesList out_CurrentTrianglesList_ { get; set; }

    private TrianglesList in_CurrentTrianglesList_;

    private List<int> intervals_;
    private int nbFrameTotal_;

    //Number of remaining vertices to unwrap
    private int nbTriangle_dec;
    //Number of vertices
    private int nbTriangle;
    private bool isWrapping = false;
    private bool sorted;
    private bool preparing = false;
    Text txt;

    Vector3 Size;

    [Range(1.0f, 100f)]
    private float speed = 50.0f;
    private float m_Speed = 50.0f;

    private float timer = 0.0f;
    public float time = 0;


    int k = 0;
    int nbTrianglesToDestroy = 0;
    int prev_nbTrianglesToDestroy = 0;
    private bool isRecreatingTriangles;
    private bool PrimCreated;

    public bool isMoving { get; private set; }

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

    private float StartTime;

    public float TimeLerp;

    #region MonoBehaviour Methods
    void Start()
    {
        Clear();
        txt = FindObjectOfType<Text>();
        OnSpeedChange += SpeedChangeHandler; //Associate event "OnSpeedChange" to "SpeedChangeHandler" method
        Size = new Vector3(
            Mathf.Abs(in_triangles_.triangles[0].barycenter[0] - in_triangles_.triangles[in_triangles_.triangles.Count - 1].barycenter[0]),
            Mathf.Abs(in_triangles_.triangles[0].barycenter[1] - in_triangles_.triangles[in_triangles_.triangles.Count - 1].barycenter[1]),
            Mathf.Abs(in_triangles_.triangles[0].barycenter[2] - in_triangles_.triangles[in_triangles_.triangles.Count - 1].barycenter[2])
        );

        in_CurrentTrianglesList_ = in_triangles_;
        out_CurrentTrianglesList_ = out_triangles_;
        PrepareTransform();
    }

    private void Update()
    {


    }

    private float timePerTriangle;
    private bool isChanging;
    private bool isSliding;
    private bool isComputing;
    private bool computed;
    private float frameSize;
    private bool isLinearWrapping;

    void FixedUpdate()
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
            //Debug.Log("Unwrapping");
            //Unwrap();
            nbTrianglesToDestroy = 0;
            isWrapping = true;
            isRecreatingTriangles = false;
            isChanging = false;
            isMoving = false;
            isSliding = false;
            isComputing = false;
            isLinearWrapping = false;
            timer = 0;
            k = 0;
        }
        if (Input.GetKeyDown(KeyCode.C))
        {
            //in_triangles_.ChangeRangeTo()
            //gameObject.GetComponent<Collider>().enabled = false;
            isChanging = true;
            isRecreatingTriangles = false;
            isWrapping = false;
            isMoving = false;
            isSliding = false;
            isComputing = false;
            isLinearWrapping = false;
            nbTrianglesToDestroy = 0;
            timer = 0;
            k = 0;
        }
        if (Input.GetKeyDown(KeyCode.L))
        {
            in_CurrentTrianglesList_.SwitchRigidbody();
            isMoving = true;
            isRecreatingTriangles = false;
            isWrapping = false;
            isChanging = false;
            isSliding = false;
            isComputing = false;
            isLinearWrapping = false;
            StartTime = Time.fixedTime;
            nbTrianglesToDestroy = 0;
            timer = 0;
            k = 0;
        }
        if (Input.GetKeyDown(KeyCode.G))
        {
            in_CurrentTrianglesList_.SwitchRigidbody();
        }
        if (Input.GetKeyDown(KeyCode.R))
        {
            isRecreatingTriangles = true;
            isMoving = false;
            isWrapping = false;
            isChanging = false;
            isSliding = false;
            isComputing = false;
            isLinearWrapping = false;
            nbTrianglesToDestroy = 0;
            timer = 0;
            k = 0;
        }
        if (Input.GetKeyDown(KeyCode.O))
        {
            isSliding = true;
            isRecreatingTriangles = false;
            isMoving = false;
            isWrapping = false;
            isChanging = false;
            isComputing = false;
            isLinearWrapping = false;
            nbTrianglesToDestroy = 0;
            timer = 0;
            k = 0;
        }
        if (Input.GetKeyDown(KeyCode.K))
        {
            isLinearWrapping = true;
            isSliding = false;
            isRecreatingTriangles = false;
            isMoving = false;
            isWrapping = false;
            isChanging = false;
            isComputing = false;
            nbTrianglesToDestroy = 0;
            timer = 0;
            k = 0;
        }
        if (Input.GetKeyDown(KeyCode.Alpha0))
        {
            SwitchTrianglesList();
        }

        //int delTriangles = nbTriangle_dec - (nbTriangle - nbTriangle_dec);

        //// if ((lastNbDelTriangle - delTriangles) != 0 && (lastNbDelTriangle - delTriangles) != nbTriangle)
        //txt.text = "Triangles supprimés: " + (lastNbDelTriangle - delTriangles);
        //lastNbDelTriangle = delTriangles;

        if (m_Speed != speed && OnSpeedChange != null)
        {
            m_Speed = speed;
            OnSpeedChange(speed);
        }

        if (preparing)
        {
            timePerTriangle = time / nbTriangle;
            Debug.Log("Pas: " + timePerTriangle);

            preparing = false;
            isComputing = true;
            timer = 0;
        }


        if (isWrapping)
        {
            if (nbTrianglesToDestroy < nbTriangle && in_CurrentTrianglesList_.triangles.Count > 0)
            {
                UnwrapRepeat();
                SaveToMesh();
            }
        }

        if (isChanging)
        {
            if (nbTrianglesToDestroy < nbTriangle)
            {
                ChangeRepeat();
                SaveToMesh();
            }
        }

        if (isMoving)
        {
            float timeSinceStarted = Time.fixedTime - StartTime;
            float percentageComplete = timeSinceStarted / TimeLerp;
            in_CurrentTrianglesList_.LerpAllTo(in_CurrentTrianglesList_.unaltered_triangles, percentageComplete);
            if (percentageComplete >= 1.0f)
            {
                Debug.Log("Stop moving");
                isMoving = false;
            }
        }

        if (isSliding)
        {
            float timeSinceStarted = Time.fixedTime - StartTime;
            float percentageComplete = timeSinceStarted / TimeLerp;
            if (in_CurrentTrianglesList_ == in_triangles_)
                in_CurrentTrianglesList_.LerpInCircle(in_, percentageComplete);
            else
                in_CurrentTrianglesList_.LerpInCircle(out_, percentageComplete);

            if (percentageComplete >= 1.0f)
            {
                Debug.Log("Stop moving");
                isMoving = false;
            }
        }

        if (isRecreatingTriangles)
        {
            if (in_CurrentTrianglesList_.primitives.Count == 0)
                isRecreatingTriangles = false;
            else
            {
                ReformRepeat();
                SaveToMesh();
            }
        }

        if (isComputing)
        {
            ComputeIntervals();
        }

        if (isLinearWrapping)
        {
            LinearUnwrapRepeat();
            SaveToMesh();
        }



    }
    #endregion

    #region Private Methods

    void UnwrapRepeat()
    {
        while (Time.fixedDeltaTime * k > timer && nbTrianglesToDestroy < in_triangles_.unaltered_triangles.Count)
        {
            timer += timePerTriangle;
            nbTrianglesToDestroy++;
        }

        List<SuperTriangle> lst = in_CurrentTrianglesList_.PopRange(nbTrianglesToDestroy);
        //out_triangles_ = new TrianglesList();
        out_CurrentTrianglesList_.triangles = lst;
        //Debug.Log(timer);
        k++;
        if (nbTrianglesToDestroy >= nbTriangle)
        {
            Debug.Log("Stop wrapping");
            SwitchTrianglesList();
            isWrapping = false;
        }
    }

    void LinearUnwrapRepeat()
    {
        if (!computed)
        {
            ComputeIntervals();
            Debug.Log("not computed yet");
            return;
        }
        while (Time.fixedDeltaTime * k > timer && nbTrianglesToDestroy < in_triangles_.unaltered_triangles.Count)
        {
            timer += frameSize;
            nbTrianglesToDestroy++;
        }

        //Debug.Log("frame: " + nbTrianglesToDestroy);
        List<SuperTriangle> lst = in_CurrentTrianglesList_.PopRange(nbTrianglesToDestroy);
        //out_triangles_ = new TrianglesList();
        out_CurrentTrianglesList_.triangles = lst;
        //Debug.Log(timer);
        k++;
        if (nbTrianglesToDestroy >= nbTriangle)
        {
            Debug.Log("Stop linear wrapping");
            SwitchTrianglesList();
            isLinearWrapping = false;
        }
    }

    void ComputeIntervals()
    {
        while (Time.fixedDeltaTime * k > timer && nbTrianglesToDestroy < in_triangles_.unaltered_triangles.Count)
        {
            timer += timePerTriangle;
            nbTrianglesToDestroy++;
        }
        intervals_.Add(nbTrianglesToDestroy);
        k++;
        nbFrameTotal_++;
        if (nbTrianglesToDestroy >= nbTriangle)
        {
            Debug.Log("Stop compute intervals");
            switch(axis){
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
            isComputing = false;
            computed = true;
        }
    }

    void SwitchTrianglesList()
    {
        if (in_CurrentTrianglesList_ == in_triangles_)
        {
            for (int i = 0; i < in_.transform.childCount; ++i)
            {
                in_.transform.GetChild(i).SetParent(out_.transform);
                GameObject.Destroy(in_.transform.GetChild(i).gameObject);

            }
            out_CurrentTrianglesList_ = in_triangles_;
            in_CurrentTrianglesList_ = out_triangles_;

        }
        else if (in_CurrentTrianglesList_ == out_triangles_)
        {
            for (int i = 0; i < out_.transform.childCount; ++i)
            {
                out_.transform.GetChild(i).SetParent(in_.transform);
                GameObject.Destroy(out_.transform.GetChild(i).gameObject);
            }
            out_CurrentTrianglesList_ = out_triangles_;
            in_CurrentTrianglesList_ = in_triangles_;

        }
        //PrimCreated = false;
        //in_CurrentTrianglesList_.primitives = new List<GameObject>();
        //out_CurrentTrianglesList_.primitives = new List<GameObject>();

        for (int i = 0; i < in_CurrentTrianglesList_.triangles.Count; ++i)
        {
            in_CurrentTrianglesList_.triangles[i].barycenter = in_CurrentTrianglesList_.triangles[i].GetBarycenter();
        }

        computed = false;
        isComputing = true;

    }

    void ChangeRepeat()
    {
        prev_nbTrianglesToDestroy = nbTrianglesToDestroy;
        do
        {
            timer += timePerTriangle;
            nbTrianglesToDestroy++;
        }
        while (Time.fixedDeltaTime * k > timer && nbTrianglesToDestroy < in_CurrentTrianglesList_.unaltered_triangles.Count);
        if (PrimCreated)
            in_CurrentTrianglesList_.ReformRange(nbTrianglesToDestroy, prev_nbTrianglesToDestroy, false);
        else
        {
            if (in_CurrentTrianglesList_ == in_triangles_)
                in_CurrentTrianglesList_.ChangeRangeTo(nbTrianglesToDestroy, prev_nbTrianglesToDestroy, PrimitiveType.Cube, in_);
            else
                in_CurrentTrianglesList_.ChangeRangeTo(nbTrianglesToDestroy, prev_nbTrianglesToDestroy, PrimitiveType.Cube, out_);
        }
        List<SuperTriangle> lst = in_CurrentTrianglesList_.PopRange(nbTrianglesToDestroy);

        k++;
        if (nbTrianglesToDestroy >= nbTriangle)
        {
            Debug.Log("Stop Changing");
            isChanging = false;
        }
    }

    void ReformRepeat()
    {
        prev_nbTrianglesToDestroy = nbTrianglesToDestroy;
        do
        {
            timer += timePerTriangle;
            nbTrianglesToDestroy++;
        }
        while (Time.fixedDeltaTime * k > timer && nbTrianglesToDestroy < in_CurrentTrianglesList_.unaltered_triangles.Count);

        in_CurrentTrianglesList_.ReformRange(nbTrianglesToDestroy, prev_nbTrianglesToDestroy);
        //List<SuperTriangle> lst = in_triangles_.PopRange(nbTrianglesToDestroy);

        k++;
        if (nbTrianglesToDestroy >= nbTriangle)
        {
            isRecreatingTriangles = false;
            PrimCreated = true;
        }
    }

    /// <summary>
    /// Invoked when speed variable change
    /// </summary>
    /// <param name="newSpeed"></param>
    private void SpeedChangeHandler(float newSpeed)
    {
        if (isWrapping)
        {
            //Debug.Log("Cancel");
            //CancelInvoke("DestroyAfter");
            //Unwrap();
            //StopCoroutine(coroutine);
            //coroutine = StartCoroutine(UnwrapRepeat(speed / 100.0f));

        }
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
        out_mesh_f.mesh.triangles = out_triangles_.ToIntList();
        in_mesh_f.mesh.triangles = in_triangles_.ToIntList();
        nbTriangle_dec = in_triangles_.triangles.Count;

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
        List<SuperTriangle> lstTr = in_CurrentTrianglesList_.SortTriangles(axis);
        in_CurrentTrianglesList_.triangles = lstTr;
        in_CurrentTrianglesList_.unaltered_triangles = lstTr;

        preparing = true;

        SaveToMesh(); //Save infos to mesh
        out_CurrentTrianglesList_.unaltered_triangles = in_CurrentTrianglesList_.triangles;
        sorted = true;
    }

    /// <summary>
    /// Unwrap triangles one-by-one
    /// </summary>
    public void UnwrapOBO()
    {
        if (!sorted) PrepareTransform();
        SuperTriangle t = in_CurrentTrianglesList_.Pop();
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
        isWrapping = true;
        InvokeRepeating("Deconstruct", (float)(nbTriangle_dec / nbTriangle), (100.1f - speed) / 1000.0f);
    }

    /// <summary>
    /// 
    /// </summary>
    void Deconstruct()
    {
        SuperTriangle t = in_triangles_.Pop();
        if (t != null)
        {
            //Debug.DrawLine(t.vertices[0].vertice, t.vertices[1].vertice, Color.red, 2.0f);
            //Debug.DrawLine(t.vertices[1].vertice, t.vertices[2].vertice, Color.red, 2.0f);
            //Debug.DrawLine(t.vertices[2].vertice, t.vertices[0].vertice, Color.red, 2.0f);
            out_triangles_.triangles.Add(t);
            SaveToMesh();
        }
        else
            isWrapping = false;

    }
    #endregion
}
