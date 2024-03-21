using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class PlayerController : BattleSystem
{

    [SerializeField][Header("플레이어 이동 속도")] float moveSpeed = 4.0f;
    [SerializeField][Header("플레이어 회전 속도")] float rotSpeed = 1.0f;
    [SerializeField][Header("플레이어 점프 세기")] float jumpForce = 2.0f;
    [SerializeField][Header("플레이어 스펠")] Transform spellObject; // 스펠 어짜피 1개 들고 다니니깐 이거 배열이 아니라 그냥 한개로 수정해야함
    [SerializeField]
    [Header("플레이어 2D 이동 방식 토글")] bool controll2D = true;
    [SerializeField] Vector2 rotYRange = new Vector2(0.0f, 180.0f);
    

    public GameObject orgFireball;
    public LayerMask groundMask;
    public Transform leftAttackPoint;
    public Transform rightAttackPoint;
    public UnityEvent<int> switchTrackedOffset;
    public UnityEvent changeCamera2D;
    public UnityEvent changeCameraTopView;
    public UnityEvent<bool> changeCamera3D;
    public bool isSpellReady = false;
    public bool is3d = true;

    float curRotY;
    float ap;
    bool isGround;
    float attackDeltaTime = 0.0f;
    float teleportDeltaTime = 0.0f;

    Rigidbody rigid;
    Fireball fireBall;
    Coroutine attackDelay;
    Coroutine teleportDelay;
    Coroutine rotating;
    // Start is called before the first frame update
    private void Awake()
    {
        Initialize();
    }
    void Start()
    {
        curRotY = transform.localRotation.eulerAngles.y;
        rigid = this.GetComponent<Rigidbody>();

    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (isAlive())
        {
            if (controll2D) // 앞뒤 2방향으로 움직이고 점프하는 코드
            {
                IsGround();
                TryJump();
                Rotate();
                Move(); // 회전과 동시에 움직이기
                        //if (canMove) Move(); // 회전이 끝나면 움직이기
            }
            else // 앞뒤, 양옆 4방향으로 움직이는 코드, 점프는 안만듬
            {
                Rotate3D();
                Move3D();
            }
        }
    }
    #region ControllChange
    public void SwitchControllType2D(bool _type)
    {
        controll2D = _type;
        if (controll2D)
        {
            Constraints2D();
            // 2D 카메라 시점 변경 함수 호출
            changeCamera2D?.Invoke();
        }
        else
        {
            Constraints3D();
            // 3D 카메라 시점 변경 함수 호출
            if(is3d)changeCamera3D?.Invoke(is3d);
            else changeCameraTopView?.Invoke();
        }
    }

    void Constraints2D()
    {
        rigid.constraints = RigidbodyConstraints.None;
        rigid.constraints = RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationZ;
    }

    void Constraints3D()
    {
        rigid.constraints = RigidbodyConstraints.None;
        rigid.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationZ;
    }
    #endregion

    #region MoveOn2D
    void Move()
    {
        Constraints2D();
        float x = Input.GetAxis("Horizontal");
        Vector3 deltaPos = transform.forward * x * Time.deltaTime * moveSpeed;

        if (!Mathf.Approximately(x, 0.0f) && Input.GetKeyDown(KeyCode.LeftShift) && Mathf.Approximately(teleportDeltaTime, 0.0f)) // 텔레포트, 추후 쿨타임 추가 예정
        {
            teleportDelay = StartCoroutine(CoolingTelePort());
            deltaPos += deltaPos.normalized * 1.5f;
            if (Physics.Raycast(new Ray(transform.position + new Vector3(0.0f, 0.5f, 0.0f), transform.forward), out RaycastHit hit,
                1.5f, groundMask))
            {
                Debug.Log("벽에 막힘");
                deltaPos = deltaPos.normalized * hit.distance;
            }
        }

        transform.Translate(deltaPos); // 앞뒤 이동.
        transform.position = new Vector3(0, transform.position.y, transform.position.z);
        myAnim.SetFloat("SpeedX", Mathf.Abs(x));

    }

    void Rotate()
    {
        
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) // 오른쪽으로 회전, +
        {
            curRotY = 0.0f; // 곧바로 방향 전환
            switchTrackedOffset?.Invoke(1); // 
        }
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) // 왼쪽으로 회전, -
        {
            curRotY = 180.0f; // 곧바로 방향 전환
            switchTrackedOffset?.Invoke(-1);
        }
        curRotY = Mathf.Clamp(curRotY, rotYRange.x, rotYRange.y); // rotYRange안에 값으로 제한됨
        transform.localRotation = Quaternion.Euler(0, curRotY, 0);

    }

    void IsGround()
    {

        isGround = Physics.Raycast(transform.position + new Vector3(0.0f, 1.0f, 0.0f), Vector3.down, 1.1f, groundMask);
        Debug.DrawRay(transform.position + new Vector3(0, 1, 0), Vector3.down, Color.blue);

        myAnim.SetBool("IsGround", isGround);
        if (isGround)
        {
            Debug.Log("hit");
        }
    }

    void TryJump()
    {
        /*if (isGround && Input.GetKey(KeyCode.Space))
        {
          jumpCharge += Time.deltaTime;
        }

        if (isGround && Input.GetKeyUp(KeyCode.Space))
        {
            Jump();
            myAnim.SetTrigger("Jumping");
        }
        UnityEngine.Debug.Log(isGround);*/
        if (isGround && Input.GetKeyDown(KeyCode.Space))
        {
            Jump();
            myAnim.SetTrigger("Jumping");
        }
    }

    void Jump()
    {

        rigid.AddForce(transform.up * jumpForce, ForceMode.Impulse);
    }
    #endregion

    #region MoveOn3D
    void Move3D()
    {
        Constraints3D();
        float x = Input.GetAxis("Vertical");
        float y = Input.GetAxis("Horizontal");
        Vector3 deltaXPos = Vector3.forward * x * Time.deltaTime * moveSpeed;
        Vector3 deltaYPos = Vector3.right * y * Time.deltaTime * moveSpeed;

        if ((!Mathf.Approximately(x, 0.0f) || !Mathf.Approximately(y, 0.0f)) &&
            Input.GetKeyDown(KeyCode.LeftShift) && Mathf.Approximately(teleportDeltaTime, 0.0f)) // 텔레포트, 추후 쿨타임 추가 예정
        {
            teleportDelay = StartCoroutine(CoolingTelePort());
            deltaXPos += deltaXPos.normalized * 1.5f;
            deltaYPos += deltaYPos.normalized * 1.5f;
            if (Physics.Raycast(new Ray(transform.position + new Vector3(0.0f, 0.5f, 0.0f), transform.forward), out RaycastHit hit,
                1.5f, groundMask))
            {
                Debug.Log("벽에 막힘");
                deltaXPos = deltaXPos.normalized * hit.distance;
                deltaYPos = deltaYPos.normalized * hit.distance;
            }
        }

        transform.Translate(deltaXPos + deltaYPos, Space.World); // 앞뒤 이동.
        myAnim.SetFloat("SpeedX", Mathf.Abs(x));
        myAnim.SetFloat("SpeedY", Mathf.Abs(y));
    }

    void Rotate3D()
    { // 월드 기준으로 회전한다.
        /*
        #region 노가다 형식의 8방향 회전
        Vector3 dir = Vector3.zero;
        // 아래 임시 회전 코드, 나중에 싹다 갈아 엎을거임
        if(Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) // 월드 상 앞을 본다
        {
            dir = Vector3.forward;
        }

        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))// 월드 상 뒤를 본다
        {
            dir = Vector3.back;
        }

        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) // 월드 상 오른쪽을 본다
        {
            dir = Vector3.right;
        }
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) // 월드 상 왼쪽을 본다
        {
            dir = Vector3.left;
        }

        if((Input.GetKey(KeyCode.W) && Input.GetKey(KeyCode.A)) ||
            (Input.GetKey(KeyCode.UpArrow) && Input.GetKey(KeyCode.LeftArrow))) // 월드상 좌측 앞을 본다
        {
            dir = Vector3.forward + Vector3.left;
        }

        if ((Input.GetKey(KeyCode.W) && Input.GetKey(KeyCode.D)) ||
            (Input.GetKey(KeyCode.UpArrow) && Input.GetKey(KeyCode.RightArrow))) // 월드상 우측 앞을 본다
        {
            dir = Vector3.forward + Vector3.right;
        }

        if ((Input.GetKey(KeyCode.S) && Input.GetKey(KeyCode.A)) ||
            (Input.GetKey(KeyCode.DownArrow) && Input.GetKey(KeyCode.LeftArrow))) // 월드상 좌측 뒤를 본다
        {
            dir = Vector3.back + Vector3.left;
        }

        if ((Input.GetKey(KeyCode.S) && Input.GetKey(KeyCode.D)) ||
            (Input.GetKey(KeyCode.DownArrow) && Input.GetKey(KeyCode.RightArrow))) // 월드상 좌측 뒤를 본다
        {
            dir = Vector3.back + Vector3.right;
        }



        if (rotating != null)
        {
            StopCoroutine(rotating);
            rotating = null;
        }
        else
        {
            rotating = StartCoroutine(Rotating(dir));
        }
        #endregion
        */
        float x = Input.GetAxis("Vertical");
        float y = Input.GetAxis("Horizontal");

        Vector3 rotDir = new Vector3(y, 0, x);
        if(!Mathf.Approximately(x,0.0f) || !Mathf.Approximately(y, 0.0f))
        transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(rotDir), Time.deltaTime * rotSpeed);
    }

    IEnumerator Rotating(Vector3 dir)
    {
        float angle = Vector3.Angle(transform.forward, dir);
        float rotDir = 1.0f;
        if (Vector3.Dot(transform.right, dir) < 0.0f)
        {
            rotDir = -1.0f;
        }

        while (!Mathf.Approximately(angle, 0.0f))
        {
            float delta = rotSpeed * Time.deltaTime;
            if (delta > angle)
            {
                delta = angle;
            }
            angle -= delta;
            transform.Rotate(Vector3.up * rotDir * delta);
            yield return null;
        }
    }
    #endregion

    // 이 아래부턴 나중에 스크립트 분리할 수도 있음
    protected void Attack() // 공격 함수, 정면을 정확히 바라볼때만 공격 가능
    {
        if (Mathf.Approximately(attackDeltaTime, 0.0f)) // 쿨타임 계산, 마음에 안드는데 일단 후순위
        {
            if (attackDelay != null)
            {
                StopCoroutine(attackDelay);
            }
            //attackDelay = StartCoroutine(CoolingTime(battleStat.AttackDelay, attackDeltaTime));
            attackDelay = StartCoroutine(CoolingAttack());
        }
        else
        {
            return;
        }
        if (controll2D)
        {
            float angle_F = Vector3.Angle(transform.forward, Vector3.forward); // -> 방향을 볼때 0, 반대는 180
            if (Mathf.Approximately(angle_F, 0.0f) || Mathf.Approximately(angle_F, 180.0f))
                myAnim.SetTrigger("Attack");
        }
        else
        {
            myAnim.SetTrigger("Attack");
        }
    }

    public new void OnAttack()
    {
        ap = GetAp();
        // 애니메이션 이벤트
        // 파이어볼(?)이 생성되어 앞으로 발사되는 함수
        GameObject obj = Instantiate(orgFireball, rightAttackPoint);
        obj.transform.SetParent(null);
        obj.GetComponent<Fireball>().SetFireBallAP(ap); // 파이어볼 공격력 설정
        obj.GetComponent<Fireball>().SetAttackRange(GetAttackRange()); // 파이어볼 공격 사거리 결정
        obj.GetComponent<Fireball>().SetProjectileSpeed(GetProjectileSpeed()); // 파이어볼 투사체 속도 결정
    }

    // 아래로 중복코드, 수정 필요함
    IEnumerator CoolingAttack()
    {

        while (!Mathf.Approximately(battleStat.AttackDelay, attackDeltaTime))
        {
            attackDeltaTime += 1f;
            yield return new WaitForSeconds(1f);
        }
        attackDeltaTime = 0f;
    }

    IEnumerator CoolingTelePort()
    {

        while (!Mathf.Approximately(3.0f, teleportDeltaTime))
        {
            teleportDeltaTime += 1f;
            yield return new WaitForSeconds(1f);
        }
        teleportDeltaTime = 0f;
    }


    public void ReadyToUseSpell(bool isReady)
    {
        isSpellReady = isReady;
        myAnim.SetBool("IsSpellReady", isReady);
    }

    public void UsingSpell(Vector3 spellPoint) // 여기서 스펠을 사용한다.
    {
        if (spellObject != null)
        {
            myAnim.SetTrigger("UseSpell");
            if (spellObject.gameObject.tag == "AttackSpell")Instantiate(spellObject, new Vector3(0, spellPoint.y + 0.1f, spellPoint.z), Quaternion.identity);
            else Instantiate(spellObject,this.transform);
            spellObject = null;

        }
        
    }

    public void ResetSpellTrigger()
    {
        myAnim.ResetTrigger("UseSpell");
    }

    public Transform GetCurrentSpell()
    {
        return this.spellObject;
    }

    public void HealBuff()
    {

    }

    public void SpeedBuff()
    {

    }
    public void UpdatePlayerStat(ItemStat _itemStat)
    {
        if (_itemStat.ItemType == ITEMTYPE.NONE) return;
        switch (_itemStat.ItemType)
        {
            case ITEMTYPE.WEAPON:
                if (_itemStat.Ap != 0) this.battleStat.AP = _itemStat.Ap; // 공격력 증가
                break;
            case ITEMTYPE.ARMOR:
                if(!Mathf.Approximately(_itemStat.PlusHeart,0.0f)) this.battleStat.MaxHp = _itemStat.PlusHeart; // 최대 체력 증가
                break;
            case ITEMTYPE.ACCE:
                if (!Mathf.Approximately(_itemStat.PlusSpeed, 0.0f)) this.battleStat.MoveSpeed = _itemStat.PlusSpeed; // 이속 증가
                break;
            case ITEMTYPE.PASSIVE:
                break;
            case ITEMTYPE.CURSEDACCE:
                break;
            case ITEMTYPE.SPELL:
                break;
            default:
                break;
        }
        Initialize();
    }

}
