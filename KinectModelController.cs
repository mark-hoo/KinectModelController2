using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Kinect = Windows.Kinect;

public class KinectModelController : MonoBehaviour {

    // public Material BoneMaterial;
    public GameObject BodySourceManager;

    public GameObject RootObject;

    public GameObject SpineBase;
    public GameObject SpineMid;
    public GameObject Neck;
    public GameObject Head;
    public GameObject ShoulderLeft;
    public GameObject ElbowLeft;
    public GameObject WristLeft;
    public GameObject HandLeft;
    public GameObject ShoulderRight;
    public GameObject ElbowRight;
    public GameObject WristRight;
    public GameObject HandRight;
    public GameObject HipLeft;
    public GameObject KneeLeft;
    public GameObject AnkleLeft;
    public GameObject FootLeft;
    public GameObject HipRight;
    public GameObject KneeRight;
    public GameObject AnkleRight;
    public GameObject FootRight;
    public GameObject SpineShoulder;
    public GameObject HandTipLeft;
    public GameObject ThumbLeft;
    public GameObject HandTipRight;
    public GameObject ThumbRight;

    private Dictionary<ulong, GameObject> _Bodies = new Dictionary<ulong, GameObject>();
    private BodySourceManager _BodyManager;

    private string info = "Waiting for Kinect";

    private Dictionary<Kinect.JointType, Kinect.JointType> _BoneMap = new Dictionary<Kinect.JointType, Kinect.JointType>()
    {
        { Kinect.JointType.FootLeft, Kinect.JointType.AnkleLeft },
        { Kinect.JointType.AnkleLeft, Kinect.JointType.KneeLeft },
        { Kinect.JointType.KneeLeft, Kinect.JointType.HipLeft },
        { Kinect.JointType.HipLeft, Kinect.JointType.SpineBase },
        
        { Kinect.JointType.FootRight, Kinect.JointType.AnkleRight },
        { Kinect.JointType.AnkleRight, Kinect.JointType.KneeRight },
        { Kinect.JointType.KneeRight, Kinect.JointType.HipRight },
        { Kinect.JointType.HipRight, Kinect.JointType.SpineBase },
        
        { Kinect.JointType.HandTipLeft, Kinect.JointType.HandLeft },
        { Kinect.JointType.ThumbLeft, Kinect.JointType.HandLeft },
        { Kinect.JointType.HandLeft, Kinect.JointType.WristLeft },
        { Kinect.JointType.WristLeft, Kinect.JointType.ElbowLeft },
        { Kinect.JointType.ElbowLeft, Kinect.JointType.ShoulderLeft },
        { Kinect.JointType.ShoulderLeft, Kinect.JointType.SpineShoulder },
        
        { Kinect.JointType.HandTipRight, Kinect.JointType.HandRight },
        { Kinect.JointType.ThumbRight, Kinect.JointType.HandRight },
        { Kinect.JointType.HandRight, Kinect.JointType.WristRight },
        { Kinect.JointType.WristRight, Kinect.JointType.ElbowRight },
        { Kinect.JointType.ElbowRight, Kinect.JointType.ShoulderRight },
        { Kinect.JointType.ShoulderRight, Kinect.JointType.SpineShoulder },
        
        { Kinect.JointType.SpineBase, Kinect.JointType.SpineMid },
        { Kinect.JointType.SpineMid, Kinect.JointType.SpineShoulder },
        { Kinect.JointType.SpineShoulder, Kinect.JointType.Neck },
        { Kinect.JointType.Neck, Kinect.JointType.Head },
    };

    /// <summary>
    /// 一括操作用の配列
    /// </summary>
    private GameObject[] _bones;

    /// <summary>
    /// キャリブレーション位置
    /// </summary>
    private Vector3[] _calibrationPoint;

    /// <summary>
    /// モデルのデフォルト位置
    /// </summary>
    private Vector3[] _modelDefaultPoint;

    private bool isCalibrated = false;

	// Use this for initialization
	void Start () {
        _bones = new GameObject[(int)Kinect.JointType.ThumbRight + 1] {
            SpineBase,
            SpineMid,
            Neck,
            Head,
            ShoulderLeft,
            ElbowLeft,
            WristLeft,
            HandLeft,
            ShoulderRight,
            ElbowRight,
            WristRight,
            HandRight,
            HipLeft,
            KneeLeft,
            AnkleLeft,
            FootLeft,
            HipRight,
            KneeRight,
            AnkleRight,
            FootRight,
            SpineShoulder,
            HandTipLeft,
            ThumbLeft,
            HandTipRight,
            ThumbRight         
        };

        _calibrationPoint = new Vector3[(int)Kinect.JointType.ThumbRight + 1];
        _modelDefaultPoint = new Vector3[(int)Kinect.JointType.ThumbRight + 1];

        // Kinectから得られる情報は絶対座標
        // モデルの座標は(localPositionの場合)親オブジェクトとの相対座標
        // モデルの親を統一し、位置情報を確保しておく
        for (Kinect.JointType jt = Kinect.JointType.SpineBase; jt <= Kinect.JointType.ThumbRight; jt++)
        {
            //var fromString = _bones[(int)jt].transform.localPosition.ToString();
            //Debug.Log(fromString + " -> " + _bones[(int)jt].transform.localPosition.ToString());

            _bones[(int)jt].transform.parent = RootObject.transform;
            _modelDefaultPoint[(int)jt] = _bones[(int)jt].transform.localPosition;
        }
	}
	
	void Update () {

        if (BodySourceManager == null)
        {
            return;
        }

        _BodyManager = BodySourceManager.GetComponent<BodySourceManager>();
        if (_BodyManager == null)
        {
            return;
        }

        Kinect.Body[] data = _BodyManager.GetData();
        if (data == null)
        {
            return;
        }

        List<ulong> trackedIds = new List<ulong>();
        foreach (var body in data)
        {
            if (body == null)
            {
                continue;
            }

            if (body.IsTracked)
            {
                trackedIds.Add(body.TrackingId);
            }
        }

        foreach (var body in data)
        {
            if (body == null)
            {
                continue;
            }

            if (body.IsTracked)
            {
                //if (!_Bodies.ContainsKey(body.TrackingId))
                //{
                //    _Bodies[body.TrackingId] = CreateBodyObject(body.TrackingId);
                //}

                // Status
                if (!isCalibrated)
                {
                    // Hand Statusを確認
                    if (body.HandLeftState == Kinect.HandState.Open || body.HandRightState == Kinect.HandState.Open)
                    {
                        // Calibration開始
                        for (Kinect.JointType jt = Kinect.JointType.SpineBase; jt <= Kinect.JointType.ThumbRight; jt++)
                        {
                            _calibrationPoint[(int)jt] = GetVector3FromJoint(body.Joints[jt]);
                        }
                        isCalibrated = true;
                        info = "Calibrated!";
                    }
                }
                else
                {
                    RefreshBodyObject(body);
                }

            }
        }


	}

    private GameObject CreateBodyObject(ulong id)
    {
        GameObject body = new GameObject("Body:" + id);

        for (Kinect.JointType jt = Kinect.JointType.SpineBase; jt <= Kinect.JointType.ThumbRight; jt++)
        {
            GameObject jointObj = GameObject.CreatePrimitive(PrimitiveType.Cube);

            LineRenderer lr = jointObj.AddComponent<LineRenderer>();
            lr.SetVertexCount(2);
          //  lr.material = BoneMaterial;
            lr.SetWidth(0.05f, 0.05f);
            jointObj.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);
            jointObj.name = jt.ToString();
            jointObj.transform.parent = RootObject.transform;
        }

        return body;
    }

    void OnGUI() {
        GUI.Label(new Rect(5, 5, 500, 500), info, new GUIStyle()
        {
            fontSize = 25,
            normal = new GUIStyleState() { textColor = Color.white }
        });
    }


    private void RefreshBodyObject(Kinect.Body body)
    {
        info = "";
        
        // 各関節に関して調べる
        for (Kinect.JointType jt = Kinect.JointType.SpineBase; jt <= Kinect.JointType.ThumbRight; jt++)
        {
            // infoに現在情報を追加
            info += jt.ToString() + " : " + _bones[(int)jt].transform.localPosition.ToString() + "\n";

            Kinect.Joint sourceJoint = body.Joints[jt];
            // 動いた分(現在の場所 - キャリブレーションポイント) + モデルのデフォルト位置を適用
            _bones[(int)jt].transform.localPosition = (GetVector3FromJoint(sourceJoint) - _calibrationPoint[(int)jt]) + _modelDefaultPoint[(int)jt];
        }
    }

    private static Vector3 GetVector3FromJoint(Kinect.Joint joint)
    {
        return new Vector3(joint.Position.X , joint.Position.Y,- joint.Position.Z);
    }

}
