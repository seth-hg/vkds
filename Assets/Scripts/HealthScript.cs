using UnityEngine;
using System.Collections;

public class HealthScript : MonoBehaviour {

	public int hp = 3;
	public int attack = 0;
	public int defense = 0;
	public int heal = 0;
	public bool isEnemy;
	public int attackEnhance = 0;
	public int defenseEnhance = 0;
	public int healEnhance = 0;
	public int attackEnhanceTurnLeft = 0;
	public int defenseEnhanceTurnLeft = 0;
	public int healEnhanceTurnLeft = 0;

	public void Attack (Vector2 dir)
	{
		Rigidbody2D rb = gameObject.GetComponent<Rigidbody2D> ();
		rb.velocity = dir / 100.0f;
		rb.AddForce (dir);
	}

	public void Damage(int damageCount)
	{
		// TODO: 对比攻击力和防御力，计算攻击伤害
		hp -= damageCount - defense;
		
		if (hp <= 0)
		{
			// Dead!
			Destroy(gameObject);
			/*
			if (isEnemy && GameObject.FindGameObjectsWithTag("enemy").Length == 0) {
				// 对手死光光
				Application.LoadLevel("win");
				return;
			}
			if (!isEnemy && GameObject.FindGameObjectsWithTag("player").Length == 0) {
				// 自己死光光
				Application.LoadLevel("lose");
				return;
			}
			*/
		}
	}

	public void Restore (int hp)
	{
		// TODO: 是否要设定hp的最大值？
		hp += hp;
	}

	void OnCollisionEnter2D(Collision2D collision)
	{
		/* 碰到对手
		 */
		//Debug.Log ("OnCollisionEnter2D: " + collision.gameObject);
		HealthScript hs = collision.gameObject.GetComponent<HealthScript> ();
		if (hs && hs.isEnemy != isEnemy) {
			// FIXME: 伤害逻辑，主动攻击者不受伤害，被攻击这受伤害
			if (attack + attackEnhance > 0) {
				hs.Damage (attack + attackEnhance);
			}
		}
	}

	void OnTriggerEnter2D(Collider2D otherCollider) {
		GameObject iObject = otherCollider.gameObject;
		ItemScript iScript = iObject.GetComponent<ItemScript>();
		Debug.Log ("hit a trigger: " + iScript.type);
		switch (iScript.type) {
		case ItemScript.ItemType.Score:
			Destroy (iObject);
			Restore(1);
			break;
		case ItemScript.ItemType.Trap:
			// hp = 0
			Damage(hp);
			break;
		case ItemScript.ItemType.Obstacle:
			// 受到伤害HP -= 1
			Damage (1);
			break;
		case ItemScript.ItemType.Fire:
			// 接触到火焰，增加攻击力
			attackEnhance = 1;
			attackEnhanceTurnLeft = iScript.effectiveTurns;
			Destroy(iObject);
			break;
		default:
			break;
		}
	}

	void FixedUpdate () {
		Rigidbody2D rb = gameObject.GetComponent<Rigidbody2D> ();
		Vector2 v = rb.velocity;
		if (Mathf.Abs(v.x) < 0.1 && Mathf.Abs(v.y) < 0.1) {
			rb.velocity = Vector2.zero;
		}
	}
}
