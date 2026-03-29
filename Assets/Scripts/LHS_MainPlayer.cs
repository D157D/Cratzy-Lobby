using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// ������� �Է°��� ���� �¿�յڷ� �̵��ϰ� �ʹ�.
// jumpŰ�� ������ �ٰ� �ʹ�.
public class LHS_MainPlayer : MonoBehaviour
{
    
    // �̵��ӵ�
    public float speed = 10;
    // ȸ�� �ӵ�
    public float rotateSpeed = 5;
    // ���� �Ŀ�
    public float jumpPower = 5;

    // ī�޶�
    private Camera currentCamera;
    public bool UseCameraRotation = true;

    // ���� ��ƼŬ
    public ParticleSystem dust;

    // ��
    public GameObject bar;

    // �浹 
    public string playerTag;
    public float bounceForce;
    public ParticleSystem bounce;


    // ���� ȿ��
    public AudioSource mysfx;
    public AudioClip jumpfx;
    public AudioClip bouncefx;


    Animator anim;
    Rigidbody rigid;

    // ���� Ȯ��
    bool isJump;
    //bool isGround;
    bool jDown;
    
    bool isDie;

    float hAxis;
    float vAxis;

    Vector3 moveVec;

    // Start is called before the first frame update
    void Awake()
    {
        rigid = GetComponent<Rigidbody>();
        anim = GetComponentInChildren<Animator>();

        // �� Ȱ��ȭ
        bar.SetActive(true);
        bar.transform.position = Camera.main.WorldToScreenPoint(transform.position + new Vector3(0, 3.35f, 0));
    }

    private void Start()
    {
        currentCamera = FindObjectOfType<Camera>();
    }

    private void FixedUpdate()
    {
        FreezeRotation();
        GetInput();
        Move();
        Turn();
        Jump();
        Die();
        Expression();
    }

    void FreezeRotation()
    {
        // �浹 ���� �� ���� ȸ���� ���ϰ� �ʹ�.
        // ������ ���� ���� ���ֱ�
        rigid.angularVelocity = Vector3.zero;
    }

    void GetInput()
    {
        hAxis = Input.GetAxis("Horizontal");
        vAxis = Input.GetAxis("Vertical");
        jDown = Input.GetButton("Jump");
    }

    void Move()
    {
        // ����
        moveVec = new Vector3(hAxis, 0, vAxis).normalized;

        //ī�޶� �������� �����ش�.
        if (UseCameraRotation)
        {
            //ī�޶��� yȸ���� ���ؿ´�.
            Quaternion v3Rotation = Quaternion.Euler(0f, currentCamera.transform.eulerAngles.y, 0f);
            //�̵��� ���͸� ������.
            moveVec = v3Rotation * moveVec;
        }

        // �����δ�
        transform.position += moveVec * speed * Time.deltaTime;

        // Move �ִϸ��̼� true
        anim.SetBool("isMove", moveVec != Vector3.zero);
    }

    void Turn()
    {
        // �ڿ������� ȸ�� = ���ư��� �������� �ٶ󺻴�
        // transform.LookAt(transform.position + moveVec);
        if (hAxis == 0 && vAxis == 0)
            return;
        Quaternion newRotation = Quaternion.LookRotation(moveVec);
        rigid.rotation = Quaternion.Slerp(rigid.rotation, newRotation, rotateSpeed * Time.deltaTime);
    }

    void Jump()
    {
        // jump�ϰ� �մ� ��Ȳ���� Jump���� �ʵ��� ����
        // ������ �ϰ� ���� �ʴٸ�
        if (jDown && !isJump)
        {
            rigid.AddForce(Vector3.up * jumpPower, ForceMode.Impulse);
            isJump = true;

            //anim.SetBool("isJump", true);
            anim.SetTrigger("doJump");
            mysfx.PlayOneShot(jumpfx);
            dust.Play();
        }
    }

    void Die()
    {
        if (isDie)
        {
            anim.SetTrigger("doDie");
            isDie = true;
        }
    }

    // �ٴڿ� ����� �� �ٽ� flase�� �ٲ��ش�. (����)
    // �浹 �� �ڷ� �з����� + ���� / ��ƼŬ 
    private void OnCollisionEnter(Collision collision)
    {
        // �ٴ�
        if (collision.gameObject.tag == "Floor")
        {
           // anim.SetBool("isGround", false);
           //isGround = false;
            anim.SetBool("isJump", false);
            
            isJump = false;
        }

        // ȸ������ 
        else if (collision.gameObject.tag == "Platform")
        {
            anim.SetBool("isJump", false);
            
            isJump = false;
        }

        // �� (�浹)
        else if (collision.collider.tag == "Wall")
        {
            anim.SetTrigger("doDie");
            isDie = false;

            rigid.linearVelocity = new Vector3(0, 0, 0);
            rigid.AddForce(Vector3.back * bounceForce, ForceMode.Impulse);

            mysfx.PlayOneShot(bouncefx);
            bounce.Play();

            bounce.transform.position = transform.position;
        }

    }

    // ����ǥ��
    void Expression()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            anim.SetTrigger("doDance01");
        }

        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            anim.SetTrigger("doDance02");
        }

        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            anim.SetTrigger("doVictory");
        }
    }

}



  



