using UnityEngine;
using System.Collections;

public class BaseAbility : MonoBehaviour 
{
	protected enum State
	{
		Instant, //for instant, not moving abilites (ex. melee hits, armor up)
		Traversal, //for traversal abilities, move character from position to endpoint (ex. blink, charge)
		Thrown, //for thrown abilities. point A to point B (ex. Bombs)
		Projectile, //for shot abilities that need a shoot point and direction (ex. arrows, fireballs)
		DirectPlacement //for abilities that are placed at a certain location (ex. aoe circles)
	}
	
	protected enum Element
	{
		Fire,
		Earth,
		Wind, 
		Water,
		Arcane
	}
	
	protected enum Type
	{
		Physical,
		Magical,
		True
	}
	
	//public
	public ParticleSystem m_particle;
	
	//protected
	protected Vector3 m_inactivePos = new Vector3(100, 100, 100);
	protected State m_state;
	protected Element m_element;
	protected Type m_type;
	protected Player m_playerRef;
	protected float m_damage;
	protected float m_cooldown;
	protected float m_lifeSpan;
	protected float m_range;
	protected float m_castTime;
	protected float m_channelTime;
	protected float m_stunTime;
	protected bool m_needsInactPos; //true if it has a visible prefab
	protected bool m_needsPlayerRef;
	
	//private
	float m_timer;
	bool m_active;
	
	protected virtual void Start ()
	{
		m_state = State.Instant;
		m_element = Element.Arcane;
		m_type = Type.True;
		m_damage = 0.0f;
		m_cooldown = 0.0f;
		m_lifeSpan = 0.0f;
		m_range = 0.0f;
		m_castTime = 0.0f;
		m_channelTime = 0.0f;
		m_stunTime = 0.0f;
		m_needsInactPos = false;
		m_needsPlayerRef = false;
		
		m_timer = 0.0f;
		m_active = false;
		//gameObject.SetActive(false);
	}
	
	protected virtual void Update () 
	{
		if(m_active)
		{
			m_timer += Time.deltaTime;
			
			if(m_timer >= m_lifeSpan)
				Deactivate();
		}
	}
	//protected functions
	protected float GetDamage()
	{
		return m_damage;
	}
	
	protected string GetAllyTag()
	{
		if(tag == "Team1Ability")
			return "Team1";
		return "Team2";
	}
	
	protected string GetEnemyTag()
	{
		if(tag == "Team1Ability")
			return "Team2";
		return "Team1";
	}
	
	protected Vector3 RangeCheck(Vector3 i_startPos, Vector3 i_endPos)
	{
		Vector3 t_vect = i_endPos - i_startPos;
		
		if(t_vect.magnitude > m_range)
		{
			t_vect.Normalize();
			t_vect = (i_startPos + t_vect * m_range);
			return t_vect;
		}
		return i_endPos;
	}
	
	protected void BasicElementalPassive(Player i_target)
	{
		switch(m_element)
		{
		case Element.Fire:
			//m_hit.SetHealthMod(.05f, 2f);
			i_target.SetMod(0.05f, 2f, "Health");
			break;
		case Element.Water:
			//m_hit.SetSpeedMod(2.5f, 2f);
			i_target.SetMod(2.5f, 2f, "Speed");
			break;
		case Element.Earth:
			i_target.SetStun(.1f);
			break;
		}
	}
	
	//public functions
	public void ActivateAbility()
	{
		m_active = true;
		
		if(networkView != null)
			networkView.RPC("NetworkActivateParticle", RPCMode.All, networkView.viewID);
		//ActivateParticle();
		//gameObject.SetActive(true);
	}
	
	public virtual void ActivateAbility(Vector3 i_startPos)
	{
		m_active = true;
		//transform.position = i_startPos;
	}
	
	public virtual void ActivateAbility(Vector3 i_startPos, Vector3 i_targetLocationOrDirection)
	{
		//use second parameter as location for throw to spot abilities (ex. Bombs)
		//use as direction for projectiles
	}
	
	public void Deactivate()
	{
		m_active = false;
		m_timer = 0.0f;
		
		if(networkView != null)
			networkView.RPC("NetworkDeactivateParticle", RPCMode.All, networkView.viewID);
		DeactivateParticle();
		//gameObject.SetActive(false);
		
		if(m_needsInactPos)
			transform.position = m_inactivePos;
	}
	
	public void ActivateParticle()
	{
		if(m_particle != null)
			m_particle.Play();
	}
	
	public void DeactivateParticle()
	{
		if(m_particle != null)
			m_particle.Stop();
	}
	
	public Vector3 GetInactivePos()
	{
		return m_inactivePos;
	}
	
	public virtual float GetCooldown()
	{
		return m_cooldown;
	}
	
	public bool IsActive()
	{
		return m_active;
	}
	
	public bool NeedsPlayerRef()
	{
		return m_needsPlayerRef;
	}
	
	public void SetPlayerRef(Player i_playerRef)
	{
		m_playerRef = i_playerRef;
	}
	
	public float GetCastTime()
	{
		return m_castTime;
	}
	
	public float GetChannelTime()
	{
		return m_channelTime;
	}
	
//	public void UsingFireElement()
//	{
//		m_element = Element.Fire;
//	}
//	
//	public void UsingEarthElement()
//	{
//		m_element = Element.Earth;
//	}
//	
//	public void UsingWindElement()
//	{
//		m_element = Element.Wind;
//	}
//	
//	public void UsingWaterElement()
//	{
//		m_element = Element.Water;
//	}
	
	public string GetState()
	{
		return m_state.ToString();
	}
	
	[RPC]
	public void NetworkActivateParticle(NetworkViewID i_id)
	{
		if(networkView.viewID == i_id)
			ActivateParticle();
	}
	
	[RPC]
	public void NetworkDeactivateParticle(NetworkViewID i_id)
	{
		if(networkView.viewID == i_id)
			DeactivateParticle();
	}
}