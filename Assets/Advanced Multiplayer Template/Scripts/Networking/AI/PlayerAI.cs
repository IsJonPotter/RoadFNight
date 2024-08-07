using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using VehicleEnterExit;
using StarterAssets;
using UnityEngine.AI;
using UnityEngine.Serialization;

[RequireComponent(typeof(UnityEngine.AI.NavMeshAgent))]
public class PlayerAI : NetworkBehaviour
{
    public UnityEngine.AI.NavMeshAgent agent;
    public Transform target;

    public ManageGenerate _manageGenerate;
    
    private VehicleSync _targetedVehicle;
    
    [SyncVar] public bool isStopped = true;

    [SyncVar] public bool isSetAsAi = false;

    [SyncVar] public bool HasHandsUp = false;

    [SyncVar] public bool isfearful = false;

    [SyncVar] public bool isfearfulWalking = false;

    [SerializeField] private Animator _aiAnimator;

    bool _travellingToTarget = false;

    [SerializeField] private PlayerInteraction playerInteraction;
    [SerializeField] private NetworkTransform networkTransform;
    [SerializeField] private ThirdPersonController thirdPersonController;
    [SerializeField] private ManageTPController manageTpController;
    [SerializeField] private Health health;
    
    delegate void DestinationReached();
    DestinationReached Event_DestinationReached;


    private void Start()
    {
        //agent = GetComponentInChildren<UnityEngine.AI.NavMeshAgent>();
       
        //_playerInteraction = GetComponent<PlayerInteraction>();
        //_networkTransform = GetComponent<NetworkTransform>();
        //_aiAnimator = GetComponent<Animator>();
        _manageGenerate = FindObjectOfType<ManageGenerate>();
        
        agent.updateRotation = true;
        agent.updatePosition = true;
    }

    public void SetAsBot()
    {
        thirdPersonController.enabled = false;
        manageTpController.enabled = false;
        
        Debug.Log(manageTpController.enabled + "ManageTP");
        Debug.Log(thirdPersonController.enabled + "Third person Controller");
        
        _aiAnimator.SetFloat("MotionSpeed", 1);
        Walk();

        isSetAsAi = true;
        RpcSetAsBot(true);
        ToggleClassServer();
        
        networkTransform.clientAuthority = false;
    }

    [ClientRpc]
    void ToggleClassServer()
    {
        thirdPersonController.enabled = false;
        manageTpController.enabled = false;
    }
    
    [ClientRpc]
    void RpcSetAsBot(bool status)
    {
        isSetAsAi = status;
    }

    private void Update()
    {
       if (!_manageGenerate.navMeshCreated)
       {
           return;
       }

       if (isSetAsAi)
       {
          CheckForPlayerWeapon();
          CheckIfShot();
       }

       MoveAI();
       AIDestinationReached();
       AiRunAway();
       AiDeath();
    }

    private void MoveAI()
    {
        if (!HasHandsUp && !isStopped && target != null)
            agent.SetDestination(target.position);
    }

    private void AIDestinationReached()
    {
        if (!HasHandsUp && _travellingToTarget)
        {
            if (Vector3.Distance(transform.position, target.position) < 0.3f)
            {
                _travellingToTarget = false;
                Event_DestinationReached?.Invoke();
            }
        }
    }

    private void AiDeath()
    {
        if (isSetAsAi && health.isDeath)
        {
            _aiAnimator.ResetTrigger("FearfulRunning");
            _aiAnimator.ResetTrigger("FearfulWalk");
            _aiAnimator.ResetTrigger("Walk");
            _aiAnimator.ResetTrigger("Run");
            _aiAnimator.SetTrigger("Idle");
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
            Stop();
            _aiAnimator.SetLayerWeight(2, 0);
        }
    }
    
    private void AiRunAway() 
    {
        if (HasHandsUp && _aiAnimator.GetCurrentAnimatorStateInfo(2).IsName("FearfulRunning"))
        {
            _aiAnimator.SetLayerWeight(2, 1);
            if (!_aiAnimator.GetCurrentAnimatorStateInfo(2).IsName("HandsUp"))
                _aiAnimator.Play("HandsUp");
        }
        
        if (_aiAnimator.GetCurrentAnimatorStateInfo(2).IsName("FearfulRunning") && !_aiAnimator.GetBool("Run")) // this is causing a null reference error for some reason
        {
            Run();
        }
      
    }
    
    private void CheckForPlayerWeapon()
    {
           var colliders = Physics.OverlapSphere(transform.position, 5f, 1 << 6);
           foreach (var collider in colliders)
           {
               if (collider != null && collider.tag == "Player" && collider.GetComponent<Player>() != null)
               {
                   if (!playerInteraction.inVehicle && !isfearfulWalking && !isfearful && !HasHandsUp)
                   {
                       var manageTPController = collider.GetComponent<ManageTPController>();
                       
                       foreach (Transform wps in manageTPController.AllFoundWeapons)
                       {
                           if (wps.gameObject.activeInHierarchy && !isfearfulWalking)
                           {
                               isfearfulWalking = true;
                               StartCoroutine(EndFearfulWalking());
                               FearfulWalk();
                           }
                       }
                   }

                   if (collider.GetComponent<ManageTPController>().aimValue == 1 && !isfearful)
                   {
                       float dist = Vector3.Distance(transform.position, collider.transform.position);
                       if (dist < 5)
                       {
                           if (!HasHandsUp)
                           {
                               HasHandsUp = true;
                               if (!playerInteraction.inVehicle)
                                   isfearful = true;
                               StartCoroutine(EndHandsUpCoroutine());

                               if (_aiAnimator == null)
                               {
                                   Debug.LogError("_aiAnimator is null");
                                   return;
                               }
                               _aiAnimator.SetLayerWeight(2, 1);
                               HandsUp();
                           }
                           if (!playerInteraction.inVehicle)
                           {
                               var towardsPlayer = collider.transform.position - transform.position;

                               transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(towardsPlayer), Time.deltaTime * 1);

                               transform.position += transform.forward * 1 * Time.deltaTime;
                           }
                       }
                   }
               }
           }
    }

    private void CheckIfShot()
    {
        var ProjectileColliders = Physics.OverlapSphere(transform.position, 20f, 1 << 8);
        foreach (var pCollider in ProjectileColliders)
        {
            if (pCollider.CompareTag("Bullet"))
            {
                if (pCollider.GetComponent<NetworkBullet>() != null)
                {
                    if (!HasHandsUp && !isfearful)
                    {
                        if (!playerInteraction.inVehicle)
                        {
                            isfearful = true;
                            StartCoroutine(EndFearfulness());
                            if (_aiAnimator == null)
                            {
                                Debug.LogError("_aiAnimator is null");
                                return;
                            }
                            _aiAnimator.SetLayerWeight(2, 1);
                            Run();
                        }
                        else
                        {
                            isfearful = true;
                            StartCoroutine(EndFearfulness());
                            Event_DestinationReached += OnReachedDefaultTarget;

                            if (_targetedVehicle == null)
                            {
                                Debug.LogError("_targetedVehicle is null");
                                return;
                            }

                            _targetedVehicle.RequestExiting(playerInteraction, false);
                            SetNavmeshTarget(GameObject.FindGameObjectWithTag("BOTWAYPOINT").transform);
                            _aiAnimator.SetLayerWeight(2, 0);
                            RunOutOfVehicle();
                        }
                    }
                }
            }
        }
    }
    
    IEnumerator EndFastDriving()
    {
        yield return new WaitForSeconds(20f);

        _targetedVehicle.GetComponent<CarAI>().ignoreObstacles = false;
        isfearful = false;
        _targetedVehicle.GetComponent<CarAI>().desiredSpeed = 12f;
        StopCoroutine(EndFastDriving());
    }

    IEnumerator EndHandsUpCoroutine()
    {
        yield return new WaitForSeconds(3.14f);

        HasHandsUp = false;
        EndHandsUp();
        StopCoroutine(EndHandsUpCoroutine());
    }

    IEnumerator EndFearfulness()
    {
        yield return new WaitForSeconds(30);

        isfearful = false;
        StopCoroutine(EndFearfulness());
    }

    IEnumerator EndFearfulWalking()
    {
        yield return new WaitForSeconds(5);

        isfearfulWalking = false;
        StopCoroutine(EndFearfulWalking());
    }

    public void HandsUp()
    {
        _aiAnimator.ResetTrigger("FearfulRunning");
        _aiAnimator.ResetTrigger("FearfulWalk");
        _aiAnimator.ResetTrigger("Walk");
        _aiAnimator.ResetTrigger("Run");
        _aiAnimator.SetTrigger("Idle");
        agent.isStopped = true;
        agent.velocity = Vector3.zero;
        Stop();
        _aiAnimator.SetLayerWeight(2, 1);
        if (!_aiAnimator.GetCurrentAnimatorStateInfo(2).IsName("HandsUp"))
            _aiAnimator.Play("HandsUp");
    }

    void EndHandsUp()
    {
        _aiAnimator.ResetTrigger("HandsUp");
        if (!playerInteraction.inVehicle)
        {
            _aiAnimator.Play("FearfulRunning");
            StartCoroutine(EndFearfulness());
            Run();
        }
        else
        {
            isfearful = true;
            StartCoroutine(EndFearfulness());
            Event_DestinationReached += OnReachedDefaultTarget;
            _targetedVehicle.RequestExiting(playerInteraction, false);
            SetNavmeshTarget(GameObject.FindGameObjectWithTag("BOTWAYPOINT").transform);
            _aiAnimator.SetLayerWeight(2, 0);
            RunOutOfVehicle();

            /*int mode = Random.Range(134, 523);
            if (mode > 250)
            {
                isfearful = true;
                StartCoroutine(EndFearfulness());
                animator.SetTrigger("FearfulRunning");
                GetComponent<Animator>().SetLayerWeight(2, 1);
                Event_DestinationReached += OnReachedDefaultTarget;
                _targetedVehicle.RequestExiting(_playerInteraction, false);
                SetNavmeshTarget(GameObject.FindGameObjectWithTag("BOTWAYPOINT").transform);
                Run();
            }
            else if (mode < 250)
            {
                isfearful = true;
                StartCoroutine(EndFastDriving());
                _targetedVehicle.GetComponent<CarAI>().desiredSpeed = 100f;
                _targetedVehicle.GetComponent<CarAI>().ignoreObstacles = true;
            }
            mode = 0;*/
        }
    }

    public void Stop()
    {
        isStopped = true;
        _aiAnimator.SetTrigger("Idle");
        _aiAnimator.ResetTrigger("Walk");
        _aiAnimator.ResetTrigger("FearfulWalk");
        _aiAnimator.ResetTrigger("Run");

        _aiAnimator.SetFloat("Speed", 0);
        _aiAnimator.SetBool("Grounded", true);
    }

    public void Run()
    {
        isStopped = false;
        if (agent.isStopped == true)
            agent.isStopped = false;
        _aiAnimator.SetTrigger("Idle");
        _aiAnimator.ResetTrigger("FearfulWalk");
        _aiAnimator.ResetTrigger("Walk");
        _aiAnimator.SetTrigger("Run");
        _aiAnimator.SetLayerWeight(2, 1);
        _aiAnimator.Play("FearfulRunning");

        _aiAnimator.SetFloat("Speed", 6);
        _aiAnimator.SetBool("Grounded", true);

        agent.speed = 6;
    }

    public void RunOutOfVehicle()
    {
        isStopped = false;
        if (agent.isStopped == true)
            agent.isStopped = false;
        _aiAnimator.SetTrigger("Idle");
        _aiAnimator.ResetTrigger("FearfulWalk");
        _aiAnimator.ResetTrigger("Walk");
        _aiAnimator.SetTrigger("Run");
        StartCoroutine(RunOutOfVehicleCoroutine());

        _aiAnimator.SetFloat("Speed", 6);
        _aiAnimator.SetBool("Grounded", true);

        agent.speed = 6;
    }

    IEnumerator RunOutOfVehicleCoroutine()
    {
        yield return new WaitForSeconds(3f);

        _aiAnimator.SetLayerWeight(2, 1);
        _aiAnimator.Play("FearfulRunning");
        StopCoroutine(RunOutOfVehicleCoroutine());
    }

    public void RunToCar()
    {
        isStopped = false;
        if (agent.isStopped == true)
            agent.isStopped = false;
        _aiAnimator.SetTrigger("Idle");
        _aiAnimator.ResetTrigger("FearfulWalk");
        _aiAnimator.ResetTrigger("Walk");
        _aiAnimator.SetTrigger("Run");
        _aiAnimator.SetLayerWeight(2, 0);
        _aiAnimator.ResetTrigger("FearfulRunning");

        _aiAnimator.SetFloat("Speed", 6);
        _aiAnimator.SetBool("Grounded", true);

        agent.speed = 6;
    }

    public void Walk()
    {
        isStopped = false;
        if (agent.isStopped == true)
            agent.isStopped = false;
        _aiAnimator.ResetTrigger("FearfulWalk");
        _aiAnimator.ResetTrigger("FearfulRunning");
        
        _aiAnimator.SetLayerWeight(2, 0);
        
        _aiAnimator.SetTrigger("Idle");
        _aiAnimator.ResetTrigger("Run");
        _aiAnimator.SetTrigger("Walk");

        _aiAnimator.SetFloat("Speed", 2);
        _aiAnimator.SetBool("Grounded", true);

        agent.speed = 2;
    }

    public void FearfulWalk()
    {
        isStopped = false;
        if (agent.isStopped == true)
            agent.isStopped = false;
        _aiAnimator.ResetTrigger("FearfulRunning");
        _aiAnimator.SetLayerWeight(2, 1);
        _aiAnimator.SetTrigger("Idle");
        _aiAnimator.ResetTrigger("Run");
        _aiAnimator.SetTrigger("Walk");
        _aiAnimator.SetTrigger("FearfulWalk");

        _aiAnimator.SetFloat("Speed", 2);
        _aiAnimator.SetBool("Grounded", true);

        agent.speed = 2;
    }

    public void SetWaypoint(Transform waypoint, string movement)
    {
        SetNavmeshTarget(waypoint);
        if (movement == "Running")
            Run();
        else if (movement == "Walking" & !isfearful & !isfearfulWalking)
            Walk();
    }

    #region behaviours
    public void GetInTheVehicle(VehicleSync vehicle)
    {
        SetNavmeshTarget(vehicle._seats[0].EnterPoint);

        _targetedVehicle = vehicle;

        Event_DestinationReached += OnReachedDesiredVehicle;

        RunToCar();
    }

    #endregion
    void OnReachedDesiredVehicle()
    {
        Event_DestinationReached -= OnReachedDesiredVehicle;
        _targetedVehicle.RequestEntering(0, playerInteraction, false, false);
    }


    //what happens when ai got kicked out of car
    public void ServerEvent_GotKickedOutOfCar()
    {
        Event_DestinationReached += OnReachedDefaultTarget;
        SetNavmeshTarget(GameObject.FindGameObjectWithTag("BOTWAYPOINT").transform);
        RunOutOfVehicle();
    }
    void OnReachedDefaultTarget()
    {
        Event_DestinationReached -= OnReachedDefaultTarget;
        Stop();
    }

    void SetNavmeshTarget(Transform destination)
    {
        agent.SetDestination(destination.position);
        target = destination;
        _travellingToTarget = true;
    }
}

