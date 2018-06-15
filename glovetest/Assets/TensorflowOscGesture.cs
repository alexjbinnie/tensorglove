using System.Collections;
using System.Net;
using HI5;
using Rug.Osc;
using UnityEngine;

/// <summary>
/// Class for communicating with the tensorglove Osc server.
/// </summary>
public class TensorflowOscGesture : MonoBehaviour
{
    //ip address the tensorglove server is using.
    [SerializeField] private string ipAddress = "127.0.0.1";

    // port to transmit to server.
    [SerializeField] private int sendPort = 54321;

    // port to receive from server.
    [SerializeField] private int recvPort = 54322;

    private OscListener listener;

    private OscSender sender;

    // how long to wait before transmitting positions to tensorflow server.
    [Range(0f, 10f)] [SerializeField] private float waitTime = 5f;

    [SerializeField] private HI5_TransformInstance leftHand;
    [SerializeField] private HI5_TransformInstance rightHand;

    // glove material, for color.
    [SerializeField] private Renderer leftGloveRenderer;
    [SerializeField] private Renderer rightGloveRenderer;

    // colors to assign to each gesture.
    [SerializeField] private Color[] gestureColors;

    private bool runningGestures = false;

    [SerializeField] private GameObject leftLaser;
    [SerializeField] private GameObject rightLaser;

    private int leftHandPrediction = 0;
    private int rightHandPrediction = 0;

    // Use this for initialization
    void Start()
    {
        listener = new OscListener(IPAddress.Parse(ipAddress), recvPort);
        listener.Connect();
        listener.Attach("/prediction", OnPrediction);


        sender = new OscSender(IPAddress.Parse(ipAddress), sendPort);
        sender.Connect();
    }

    private void OnPrediction(OscMessage message)
    {
        Debug.Log("Prediction Received " + message);
        if ((string)message[0] == "RIGHT")
        {
            rightHandPrediction = (int)message[1];
        }
        if ((string)message[0] == "LEFT")
        {
            leftHandPrediction = (int)message[1];
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (runningGestures == false)
        {
            runningGestures = true;
            StartCoroutine(GestureDetection());
        }

        rightGloveRenderer.material.color = gestureColors[rightHandPrediction];
        leftGloveRenderer.material.color = gestureColors[leftHandPrediction];
        // pointing class.
        if (rightHandPrediction == 3)
        {
            rightLaser.gameObject.SetActive(true);
        }
        else
        {
            rightLaser.gameObject.SetActive(false);
        }
        if (leftHandPrediction == 3)
        {
            leftLaser.gameObject.SetActive(true);
        }
        else
        {
            leftLaser.gameObject.SetActive(false);
        }
    }

    IEnumerator GestureDetection()
    {
        while (runningGestures)
        {
            SendCurrentPositions(rightHand);
            yield return new WaitForSeconds(waitTime);
            //SendCurrentPositions(leftHand);
            //yield return new WaitForSeconds(waitTime);
        }
    }

    private void SendCurrentPositions(HI5_TransformInstance hand)
    {
        // send the quaternions of each finger. 
        object[] featureValues = new object[hand.HandBones.Length * 4 + 1];
        int boneIndex = 0;
        featureValues[0] = hand.HandType.ToString();

        if (hand.HandType.ToString() == "RIGHT")
        {
            foreach (var t in hand.HandBones)
            {
                for (int i = 0; i < 4; i++)
                {
                    featureValues[boneIndex * 4 + i + 1] = (t.localRotation[i]);
                }

                boneIndex++;
            }
        }
        //Reflection code for left hand, not quite working!
        /*
        else if (hand.HandType.ToString() == "LEFT")
        {
            // finger index to base
            int[] hand_thumb_indexes = { 0, 1, 2, 3, 4 };
            int[] hand_index_indexes = { 0, 1, 5, 6, 7, 8 };
            int[] hand_middle_indexes = { 0, 1, 9, 10, 11, 12 };
            int[] hand_ring_indexes = { 0, 1, 13, 14, 15, 16 };
            int[] hand_pinky_indexes = { 0, 1, 17, 18, 19, 20 };

            int[][] matrix = { hand_thumb_indexes , hand_index_indexes , hand_middle_indexes, hand_ring_indexes, hand_pinky_indexes };

            foreach (var indexes in matrix)
            {
                foreach (var i in indexes)
                {
                    // Initially the local rotation
                    var rootRot = hand.HandBones[i].localRotation;
                    // Move up until it is relative to the root
                    for (int j = i; j >= 0; j--)
                    {
                        rootRot = rootRot * hand.HandBones[j].localRotation;
                    }
                    // Flip the rotation
                    var flippedRootRot = Flip(rootRot);
                    // Transform back to a location rotation
                    for (int j = 0; j <= i; j++)
                    {
                        flippedRootRot = flippedRootRot * Quaternion.Inverse(hand.HandBones[j].localRotation);
                    }
                    // Set the local rotation
                    for (int k = 0; k < 4; k++)
                    {
                        featureValues[boneIndex * 4 + k + 1] = (flippedRootRot[k]);
                    }
                    boneIndex++;
                }
            }
            
        }
        */
        //send the quaternion.
        Debug.Log("Hand Type: " + hand.HandType);
        OscMessage message = new OscMessage("/predict", featureValues);
        sender.Send(message);
    }

    private Quaternion Flip(Quaternion quat)
    {
        return new Quaternion(quat.x, -quat.y, -quat.z, quat.w);
    }
}