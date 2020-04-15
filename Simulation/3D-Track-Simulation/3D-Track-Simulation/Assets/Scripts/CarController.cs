using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarController : MonoBehaviour
{
    // Object components
    public Rigidbody rb;
    public BoxCollider boxCollider;
    private NeuralNetwork myNN;

    private float m_horizontalInput;
    private float m_verticalInput;
    private float m_steeringAngle;

    public WheelCollider frontDriverW, frontPassengerW;
    public WheelCollider rearDriverW, rearPassengerW;
    public Transform frontDriverT, frontPassengerT;
    public Transform rearDriverT, rearPassengerT;

    public float maxSteerAngle;
    public float motorForce;
    public LayerMask raycastMask; //Mask for the sensors
    private float[] input = new float[3]; //input to the neural network

    public float probingDistance;

    private bool carStarted = false;
    public int fitness;
    public bool hitWall = false;
    private int lastFitness;

    void Awake()
    {
        raycastMask = LayerMask.GetMask("Barrier");
        fitness = 0;
        lastFitness = fitness;
    }

    private void Start()
    {
        rb = this.GetComponent<Rigidbody>();
        boxCollider = GetComponent<BoxCollider>();
        IgnoreContactWithOtherCars();
    }

    private void FixedUpdate()
    {
        if (myNN != null && carStarted && hitWall == false)
        {
            GetInput();
            Steer();
            Accelerate();
            UpdateWheelPoses();
            rb.velocity = Vector3.ClampMagnitude(rb.velocity, 10);
        }
    }

    #region Functions that manage car movement
    private void GetInput()
    {
        GetInputFromProximitySensors();

        m_horizontalInput = 0;
        m_verticalInput = 0;

        float output = myNN.FeedForward(input);
        //if (Input.GetKey(KeyCode.W))
        //{
        //    m_verticalInput = 1;
        //}
        //if (Input.GetKey(KeyCode.A))
        //{
        //    m_horizontalInput = -1;
        //}
        //if (Input.GetKey(KeyCode.D))
        //{
        //    m_horizontalInput = 1;
        //}
        //if (Input.GetKey(KeyCode.D))
        //{
        //    m_verticalInput = -1;
        //}

        if (output == 0)
        {
            m_verticalInput = 1;
        }
        else if (output == 1)
        {
            m_horizontalInput = -1;
        }
        else if (output == 2)
        {
            m_horizontalInput = 1;
        }
        else
        {
            m_verticalInput = -1;
        }
    }

    private void Steer()
    {
        m_steeringAngle = maxSteerAngle * m_horizontalInput;
        frontDriverW.steerAngle = m_steeringAngle;
        frontPassengerW.steerAngle = m_steeringAngle;
    }

    private void Accelerate()
    {
        frontDriverW.motorTorque = m_verticalInput * motorForce;
        frontPassengerW.motorTorque = m_verticalInput * motorForce;
    }

    private void UpdateWheelPoses()
    {
        UpdateWheelPose(frontDriverW, frontDriverT);
        UpdateWheelPose(frontPassengerW, frontPassengerT);
        UpdateWheelPose(rearDriverW, rearDriverT);
        UpdateWheelPose(rearPassengerW, rearPassengerT);
    }

    private void UpdateWheelPose(WheelCollider collider, Transform transform)
    {
        Vector3 pos = transform.position;
        Quaternion quat = transform.rotation;

        collider.GetWorldPose(out pos, out quat);

        transform.position = pos;
        transform.rotation = quat;
    }
    #endregion

    #region Functions that manage collisions
    void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.gameObject.tag == "Barrier")
        {
            hitWall = true;
            frontDriverW.motorTorque = 0;
            frontPassengerW.motorTorque = 0;
        }
    }

    void OnTriggerEnter(Collider collision)
    {
        int nextCheckpoint = (fitness % 118 ) + 1;
        if (collision.gameObject.name == ("CheckPoint (" + nextCheckpoint + ")"))
        {
            fitness += 1; 
        }
    }

    void GetInputFromProximitySensors()
    {
        Vector3 forwardSensor = transform.forward;
        Vector3 northeastSensor = transform.forward + transform.right;
        Vector3 northwestSensor = transform.forward - transform.right;
        Vector3[] proximitySensors = new Vector3[] { forwardSensor, northeastSensor, northwestSensor };

        Vector3 offset = new Vector3(0, 1, 0);
        float distance;
        for (int i = 0; i < proximitySensors.Length; i++)
        {
            RaycastHit hit;
            if (Physics.Raycast(transform.position + offset, proximitySensors[i], out hit, probingDistance, raycastMask))
            {
                distance = probingDistance - hit.distance;
                Debug.DrawLine(transform.position + offset, transform.position + (proximitySensors[i] * probingDistance), Color.red);
            }
            else
            {
                distance = 0;
                Debug.DrawLine(transform.position + offset, transform.position + (proximitySensors[i] * probingDistance), Color.green);
            }

            input[i] = distance;
        }
    }

    void IgnoreContactWithOtherCars()
    {
        Object[] allWheels = GameObject.FindObjectsOfType(typeof(WheelCollider));
        Object[] allBoxColliders = GameObject.FindObjectsOfType(typeof(BoxCollider));

        // Make wheels ignore all other wheels and box colliders in the system
        for (int i = 0; i < allWheels.Length; i++)
        {
            // Ignore all other wheels
            for (int j = 0; j < allWheels.Length; j++)
            {
                Physics.IgnoreCollision(allWheels[i] as WheelCollider, allWheels[j] as WheelCollider);
            }

            // Ignore all other box colliders
            for (int j = 0; j < allBoxColliders.Length; j++)
            {
                Physics.IgnoreCollision(allWheels[i] as WheelCollider, allBoxColliders[j] as BoxCollider);
            }
        }

        // Make all box colliders ignore all other wheels or box colliders in the system
        for (int i = 0; i < allBoxColliders.Length; i++)
        {
            // Ignore all other wheels
            for (int j = 0; j < allWheels.Length; j++)
            {
                Physics.IgnoreCollision(allBoxColliders[i] as BoxCollider, allWheels[j] as WheelCollider);
            }

            // Ignore all other box colliders
            for (int j = 0; j < allBoxColliders.Length; j++)
            {
                BoxCollider myBox = allBoxColliders[i] as BoxCollider;
                BoxCollider otherBox = allBoxColliders[j] as BoxCollider;
                if (myBox.gameObject.tag == "CheckPoint" || otherBox.gameObject.tag == "CheckPoint")
                {
                    continue;
                }

                Physics.IgnoreCollision(allBoxColliders[i] as BoxCollider, allBoxColliders[j] as BoxCollider);
            }
        }
    }

    public void CheckProgression()
    {
        // Kill objects that are not progressing
        if (lastFitness >= fitness)
        {
            //Debug.Log("Last Fitness " + lastFitness + " Current Fitness " + fitness);
            hitWall = true;
        }
        else
        {
            // Set lastFitness to current Fitness
            lastFitness = fitness;
        }
    }
    #endregion

    #region Manage car state
    public void StartCar()
    {
        carStarted = true;
        // 3 seconds to progress
        InvokeRepeating("CheckProgression", 10.0f, 10.0f);
    }

    public void StopCar()
    {
        carStarted = false;
    }

    public void SetNeuralNetwork(NeuralNetwork nn)
    {
        myNN = nn;
        StartCar();
    }
    #endregion
}
