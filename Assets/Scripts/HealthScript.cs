using UnityEngine;
using System.Collections;

public class HealthScript : MonoBehaviour
{

    public int hp = 3;
    public int attack = 0;
    public int defense = 0;
    public int heal = 0;
    public bool isEnemy;
    public int attackEnhanceTurnLeft = 0;
    public int defenseEnhanceTurnLeft = 0;
    public int healEnhanceTurnLeft = 0;


    public bool isMoving = false;
    public bool isFire = false;

    public Animator animator;

    public AudioSource mouseAudio = null;
    public AudioClip mouseFireClip = null;
    public AudioClip mouseDie = null;
    public AudioClip mouseSelect = null;
    public AudioClip mousePong = null;

    public Texture2D blood_red;
    public Texture2D blood_black;

    void Start()
    {
        animator = this.gameObject.GetComponent<Animator>();
        animator.SetBool("isMoving", false);
        animator.SetBool("isFire", false);
        mouseAudio = this.gameObject.GetComponent<AudioSource>();
    }

    void Update()
    {
        animator.speed = this.GetComponent<Rigidbody2D>().velocity.sqrMagnitude;

        if (this.GetComponent<Rigidbody2D>().velocity != Vector2.zero)
            isMoving = true;
        else
            isMoving = false;
        animator.SetBool("isMoving", isMoving);
    }


    public void Attack(Vector2 dir)
    {
        //mouseAudio.PlayOneShot(mouseSelect, 1f);

        Rigidbody2D rb = gameObject.GetComponent<Rigidbody2D>();
        rb.velocity = dir / 100.0f;
        rb.AddForce(dir);
    }

    public void Damage(int damageCount)
    {
        // TODO: 对比攻击力和防御力，计算攻击伤害
        hp -= damageCount - defense;

        if (hp <= 0)
        {
            mouseAudio.PlayOneShot(mouseDie, 1f);

            // Dead!
			gameObject.SetActive(false);
        }
    }

    public void Restore(int p)
    {
        // TODO: 是否要设定hp的最大值？
		int new_hp = hp + p;

		if (new_hp > 3)
			hp = 3;
		else
			hp = new_hp;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        /* 碰到对手
		 */
        //Debug.Log ("OnCollisionEnter2D: " + collision.gameObject);
        mouseAudio.PlayOneShot(mousePong, 0.1f * collision.relativeVelocity.sqrMagnitude > 1f ? 1f : 0.1f * collision.relativeVelocity.sqrMagnitude);

        HealthScript hs = collision.gameObject.GetComponent<HealthScript>();
        if (hs && hs.attack < attack)
        { // && hs.isEnemy != isEnemy) {
          // 攻击力不为0时，对对手和本方都会造成伤害
            hs.Damage(attack - hs.attack);
        }

        // 碰到树桩受伤害
        ItemScript iScript = collision.gameObject.GetComponent<ItemScript>();
        if (iScript && iScript.isObstacle())
            Damage(iScript.damage);
    }

    void OnTriggerEnter2D(Collider2D otherCollider)
    {
        GameObject iObject = otherCollider.gameObject;
        ItemScript iScript = iObject.GetComponent<ItemScript>();
        //Debug.Log ("hit a trigger: " + iScript.type);
        switch (iScript.type)
        {
            case ItemScript.ItemType.Score:
                Destroy(iObject);
                Restore(1);
                break;
            case ItemScript.ItemType.Trap:
                // hp = 0
                Damage(hp);
                break;
            /*
            case ItemScript.ItemType.Obstacle:
                // 受到伤害HP -= 1
                Damage (1);
                break;
            */
            case ItemScript.ItemType.Fire:
                // 接触到火焰，增加攻击力
                attack = 1;
                attackEnhanceTurnLeft = iScript.effectiveTurns;
				animator.SetBool("isFire", true);
                Destroy(iObject);
                break;
            default:
                break;
        }
    }

    void FixedUpdate()
    {
        Rigidbody2D rb = gameObject.GetComponent<Rigidbody2D>();
        Vector2 v = rb.velocity;
        if (Mathf.Abs(v.x) < 0.1 && Mathf.Abs(v.y) < 0.1)
        {
            rb.velocity = Vector2.zero;
        }
    }

    void OnGUI()
    {
        Vector3 worldPosition = new Vector3(transform.position.x - 0.25f, transform.position.y + 0.3f, transform.position.z);
        var camera = Camera.main;
        Vector2 position = camera.WorldToScreenPoint(worldPosition);
        position = new Vector2(position.x, Screen.height - position.y);
        Vector2 bloodSize = new Vector2(5f, 1.5f);

        int blood_width = blood_red.width * hp / 3;
        GUI.DrawTexture(new Rect(position.x - (bloodSize.x / 2), position.y - bloodSize.y, bloodSize.x, bloodSize.y), blood_black);
        GUI.DrawTexture(new Rect(position.x - (bloodSize.x / 2), position.y - bloodSize.y, blood_width, bloodSize.y), blood_red);
    }
}
