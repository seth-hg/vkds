using UnityEngine;
using System.Collections;

public class ItemScript : MonoBehaviour {

	public enum ItemType {
		Score = 0,
		Trap,
		Obstacle,
		Fire,
	}
	public ItemType type = ItemType.Score;
	public int effectiveTurns = 1;				// 有效回合数
	public int availableTurnsLeft = 3;			// 停留在场上的回合数
	public int damage;							// 碰撞时收到的伤害
	public bool isStatic;						// 是否静态道具

	public bool isObstacle() {
		return type == ItemType.Obstacle;
	}

	// 两个道具相撞，目前应该不会发生
	void OnTriggerEnter2D(Collider2D otherCollider) {
		if (isStatic)
			return;
		Debug.Log ("item overlapped");
		Destroy (gameObject);
	}

	/*
	void OnTriggerEnter2D(Collider2D otherCollider)
	{
		HealthScript hs;
		switch (type) {
		case ItemType.Score:
			Destroy (gameObject);
			hs = otherCollider.gameObject.GetComponent<HealthScript>();
			if (hs != null)
				hs.Restore(1);
			break;
		case ItemType.Trap:
			// FIXME: 怎么处理HP？
			Destroy (otherCollider.gameObject);
			break;
		case ItemType.Obstacle:
			//
			hs = otherCollider.gameObject.GetComponent<HealthScript> ();
			hs.Damage (1);
			break;
		case ItemType.Fire:
			// 接触到火焰，增加攻击力
			hs = otherCollider.gameObject.GetComponent<HealthScript> ();
			hs.attack = 1;
			hs.attackEnhanceTurnLeft = effectiveTurns;
			Destroy(gameObject);
			break;
		default:
			break;
		}
	}
	*/
}
