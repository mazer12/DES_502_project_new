using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using TMPro;

public class NewCharacterController : MonoBehaviourPunCallbacks, IDamagable
{
    [SerializeField] Image HealthBarImage;
    [SerializeField] Image playerTopHealthBarImage;
    [SerializeField] GameObject ui;

    [SerializeField] GameObject cameraHolder;

    [SerializeField] Item[] items;


    //[SerializeField] GameObject deathCanvas;
    //[SerializeField] TextMeshProUGUI txt;

    int itemIndex;
    int previousItemIndex = -1;

    PhotonView PV;
    Rigidbody rb;

    const float maxHealth = 100f;
    float currentHealth = maxHealth;

    PlayerManager playerManager;

    [Header("Base setup")]
    public float walkingSpeed = 7.5f;
    public float runningSpeed = 11.5f;
    public float jumpSpeed = 15.0f;
    private float gravity = 20.0f;
    public float mouseSensitivity;
    private float verticalLookRotation;
    public AudioSource GunSound;
    public AudioSource footstepsSound, sprintSound, damageTakenSound;


    [Header("Animator")]
    public Animator anim;
    public bool isJumpingAnim;
    public bool isGroundedAnim;

    CharacterController characterController;
    Vector3 moveDirection = Vector3.zero;
    float rotationX = 0;

    [HideInInspector]
    public bool canMove = true;

    [SerializeField]
    private float cameraYOffset = 0.4f;
    //private float cameraZOffset = -1.0f;


    void Awake()
    {
        PV = GetComponent<PhotonView>();
        rb = GetComponent<Rigidbody>();

        playerManager = PhotonView.Find((int)PV.InstantiationData[0]).GetComponent<PlayerManager>();

        playerManager.anim = anim;

        //playerManager.deathScreen = deathCanvas;
        //playerManager.txt = txt;
    }

    void Start()
    {
        characterController = GetComponent<CharacterController>();

        // Lock cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (PV.IsMine)
        {
            EquipItem(0);
            
        }
        else
        {

            Destroy(GetComponentInChildren<Camera>().gameObject);
            Destroy(rb);
            Destroy(ui);
        }
    }
    void Look()
    {
        transform.Rotate(Vector3.up * Input.GetAxisRaw("Mouse X") * mouseSensitivity);

        verticalLookRotation += Input.GetAxisRaw("Mouse Y") * mouseSensitivity;
        verticalLookRotation = Mathf.Clamp(verticalLookRotation, -90f, 90f);

        cameraHolder.transform.localEulerAngles = Vector3.left * verticalLookRotation;
    }
    void Update()
    {
        if (!PV.IsMine)
            return;

        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.D))
        {
            if (Input.GetKey(KeyCode.LeftShift))
            {
                footstepsSound.enabled = false;
                sprintSound.enabled = true;
            }
            else
            {
                footstepsSound.enabled = true;
                sprintSound.enabled = false;
            }
        }
        else
        {
            footstepsSound.enabled = false;
            sprintSound.enabled = false;
        }
        for (int i = 0; i < items.Length; i++)
        {
            if (Input.GetKeyDown((i + 1).ToString()))
            {
                EquipItem(i);
                break;
            }
        }

        if (Input.GetAxisRaw("Mouse ScrollWheel") > 0f)
        {
            if (itemIndex >= items.Length - 1)
            {
                EquipItem(0);
            }
            else
            {
                EquipItem(itemIndex + 1);
            }
        }
        else if (Input.GetAxisRaw("Mouse ScrollWheel") < 0f)
        {
            if (itemIndex <= 0)
            {
                EquipItem(items.Length - 1);
            }
            else
            {
                EquipItem(itemIndex - 1);
            }
        }

        if (itemIndex == 0)
        {
            playerManager.anim.SetLayerWeight(1, 1);
            playerManager.anim.SetLayerWeight(2, 0);
        }
        else
        {
            playerManager.anim.SetLayerWeight(1, 0);
            playerManager.anim.SetLayerWeight(2, 1);
        }
        if (Input.GetMouseButtonDown(0))
        {
            items[itemIndex].Use();
            GunSound.Play();
        }
        Look();
        bool isRunning = false;

        // Press Left Shift to run
        isRunning = Input.GetKey(KeyCode.LeftShift);

        // We are grounded, so recalculate move direction based on axis
        Vector3 forward = transform.TransformDirection(Vector3.forward);
        Vector3 right = transform.TransformDirection(Vector3.right);

        float curSpeedX = canMove ? (isRunning ? runningSpeed : walkingSpeed) * Input.GetAxis("Vertical") : 0;
        float curSpeedZ = canMove ? (isRunning ? runningSpeed : walkingSpeed) * Input.GetAxis("Horizontal") : 0;
        float movementDirectionY = moveDirection.y;
        moveDirection = (forward * curSpeedX) + (right * curSpeedZ);

        // Handle Animations
        if (moveDirection != Vector3.zero)
        {
            playerManager.anim.SetBool("isMoving", true);
        }
        else
        {
            playerManager.anim.SetBool("isMoving", false);
        }
        float inputMagnitude = Mathf.Clamp01(moveDirection.magnitude);
        float speed = inputMagnitude * walkingSpeed;
        if (isRunning)
        {
            inputMagnitude = inputMagnitude * 2;
        }
        playerManager.anim.SetFloat("Input Magnitude", inputMagnitude, 0.1f, Time.deltaTime);

        if (characterController.isGrounded)
        {
            playerManager.anim.SetBool("isGrounded", true);
            playerManager.anim.SetBool("isFalling", false);
            playerManager.anim.SetBool("isJumping", false);

        }
        else
        {
            playerManager.anim.SetBool("isGrounded", false);
            playerManager.anim.SetBool("isFalling", true);
            footstepsSound.enabled = false;
            sprintSound.enabled = false;
        }

        if (Input.GetButton("Jump") && canMove && characterController.isGrounded)
        {
            moveDirection.y = jumpSpeed;
            playerManager.anim.SetBool("isJumping", true);
        }
        else
        {
            moveDirection.y = movementDirectionY;
        }

        // Hold V to glide
        if (Input.GetKey(KeyCode.V) && !characterController.isGrounded)
        {
            gravity = 5.0f;
            moveDirection.y -= gravity * Time.deltaTime;
        }
        else if (!characterController.isGrounded)
        {
            gravity = 20.0f;
            moveDirection.y -= gravity * Time.deltaTime;
        }
        else
        {
            gravity = 20.0f;
        }



        // Move the controller
        characterController.Move(moveDirection * Time.deltaTime);

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // Unlock Curser
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        if (Input.GetMouseButton(0))
        {
            // Lock cursor
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
    void EquipItem(int _index)
    {
        if (_index == previousItemIndex)
            return;

        itemIndex = _index;

        items[itemIndex].itemGameObject.SetActive(true);

        if (previousItemIndex != -1)
        {
            items[previousItemIndex].itemGameObject.SetActive(false);
        }

        previousItemIndex = itemIndex;

        if (PV.IsMine)
        {
            Hashtable hash = new Hashtable();
            hash.Add("itemIndex", itemIndex);
            PhotonNetwork.LocalPlayer.SetCustomProperties(hash);
            //Hashtable hash1 = new Hashtable();
            //hash.Add("fillAmount", HealthBarImage.fillAmount);
            //PhotonNetwork.LocalPlayer.SetCustomProperties(hash1);
        }
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        if (changedProps.ContainsKey("itemIndex") && !PV.IsMine && targetPlayer == PV.Owner)
        {
            EquipItem((int)changedProps["itemIndex"]);
            //playerTopHealthBarImage.fillAmount = (float)changedProps["fillAmount"];
        }
    }


    void FixedUpdate()
    {
        if (!PV.IsMine)
            return;

        rb.MovePosition(rb.position + transform.TransformDirection(moveDirection) * Time.fixedDeltaTime);
        
    }

    public void TakeDamage(float damage)
    {
        PV.RPC("RPC_TakeDamage", RpcTarget.All, damage);
    }

    //public void FillAmount(float fillAmount)
    //{
    //    PV.RPC("RPC_fillAmount", RpcTarget.All, fillAmount);
    //}

    [PunRPC]
    public void RPC_TakeDamage(float damage, PhotonMessageInfo info)
    {
        if (!PV.IsMine)
        {
            return;
        }

        currentHealth -= damage;

        //Enemy damage is 20 per hit, this ensures that it does not record damage done by enemy
        if(damage > 20)
        {
            PlayerManager.Find(info.Sender).GetDamage(damage);
        }
        
        damageTakenSound.Play();

        HealthBarImage.fillAmount = currentHealth / maxHealth;
        //playerTopHealthBarImage.fillAmount = currentHealth / maxHealth;


        if (currentHealth <= 0)
        {
            Die();
        }

    }

    //[PunRPC]

    //public void RPC_fillAmount(float fillAmount)
    //{
    //    //fillAmount = currentHealth / maxHealth;
    //    playerTopHealthBarImage.fillAmount = currentHealth / maxHealth;
    //}

    void Die()
    {
        playerManager.Die();
        // Unlock Curser
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;     
    }
}
