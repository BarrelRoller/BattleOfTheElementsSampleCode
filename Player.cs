using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Player : MonoBehaviour 
{
	//Enums
	public enum State
	{
		Moving,
		Casting,
		Channeling,
		MoveAndChannel,
		Stunned,
		Traversal,
		Dead
	}
	
	protected enum Element
	{
		Fire,
		Earth,
		Wind, 
		Water,
		Arcane
	}
	
	//constants
	const int c_numberOfAbilities = 4;
	const int c_lmNumber = 0;
	const int c_rmNumber = 1;
	const int c_qNumber = 2;
	const int c_eNumber = 3;
	const float c_distFromGround = 1.5f;
	
	
	//public
	public GameObject m_model;
	public GameObject m_firePoint;
	public BaseAbility m_leftMouseAbility;
	public BaseAbility m_rightMouseAbility;
	public BaseAbility m_qAbility;
	public BaseAbility m_eAbility;
	public bool m_cardinal = true;
	
	//protected
	protected List<BaseAbility> m_lmaList = new List<BaseAbility>();
	protected List<BaseAbility> m_rmaList = new List<BaseAbility>();
	protected List<BaseAbility> m_qaList = new List<BaseAbility>();
	protected List<BaseAbility> m_eaList = new List<BaseAbility>();
	
	protected float m_maxHealth;
	protected float m_speed;
	protected float m_armor;
	protected float m_magicResist;
	
	//private
	public State m_copyState = State.Moving;
	State m_state = State.Moving;
	Element m_element;
	GameFlowManager m_gameFlowManager;
	CharacterController m_characterController;
	BaseAbility m_activeAbility;
	AbilityIcons m_abilityIcons;
	Vector3 m_targetLocation;
	Vector3 m_startPos;
	public float m_health;
	float m_castLength;
	float m_castTimer;
	float m_channelLength;
	float m_channelTimer;
	float m_stunLength;
	float m_stunTimer;
		//modifiers
	float m_healthMod;
	float m_hmTimer;
	float m_speedMod;
	float m_smTimer;
	float m_armorMod;
	float m_amTimer;
	float m_magicResMod;
	float m_mrmTimer;
		//traversal
	float m_lerpTimer;
	float m_lerpLength;
	Vector3 m_lerpStartPos;
	Vector3 m_lerpEndPos;
		//cooldowns and timers for abilities
	List<float> m_timers = new List<float>();
	List<bool> m_onCooldown = new List<bool>();
	int m_activeAbilityNumber; 
	
	bool m_queryInput = true;
	bool m_deadAnim;
	const float c_minimumDamage = 5.0f;
	
	public Texture m_healthBar;
	public Texture m_alliedBar;
	public Texture m_enemyBar;
	public Texture m_greyBar;
	public GameObject m_hpText;
	public GameObject m_usernameText;
	GameFlowManager m_GFM;
	public string m_playerName;
	
	void OnGUI()
	{
		
		GUI.DrawTexture(new Rect(Screen.width/2 - 200,Screen.height - 110, 400, 20), m_greyBar);
		GUI.DrawTexture(new Rect(Screen.width/2 - 200,Screen.height - 110, m_health * 2, 20), m_healthBar);	
		
		
		Vector3 position = m_characterController.transform.position;
	 	position = Camera.main.WorldToScreenPoint(position);
		Rect playerHpBar = new Rect(position.x - 50, Screen.height - position.y - 100 , m_health /2, 10);
		
		GUI.DrawTexture(new Rect(position.x - 50, Screen.height - position.y - 100 ,100, 10), m_greyBar);
		GUI.DrawTexture(playerHpBar, m_healthBar);
		
		
	}
	
	protected virtual void Start ()
	{

		GameObject t_go = (GameObject)GameObject.Instantiate(m_usernameText);
		
		m_gameFlowManager = GameObject.FindGameObjectWithTag("GameFlowManager").GetComponent<GameFlowManager>();
		m_characterController = gameObject.GetComponent<CharacterController>();
		m_gameFlowManager.AddToTeamList(this, tag);
		m_abilityIcons = GameObject.Find("AbilityIcons").GetComponent<AbilityIcons>();
		m_startPos = transform.position;
		
		m_health = m_maxHealth;
		m_castTimer = 0.0f;
		m_channelTimer = 0.0f;
		m_stunTimer = 0.0f;
		m_deadAnim = false;
			//mods
		m_healthMod = 0.0f;
		m_speedMod = 0.0f;
		m_armorMod = 0.0f;
		m_magicResMod = 0.0f;
		m_hmTimer = 0.0f;
		m_smTimer = 0.0f;
		m_amTimer = 0.0f;
		m_mrmTimer = 0.0f;
			//CDs
		for(int i = 0; i < c_numberOfAbilities; i++)
		{
			m_timers.Add(0.0f);
			m_onCooldown.Add(false);
		}	
		
		m_GFM = GameObject.Find("GameFlowManager").GetComponent<GameFlowManager>();
		
		if(m_GFM.GetMyPlayer().CompareTag(transform.tag))
			m_healthBar = m_alliedBar;
		else
			m_healthBar = m_enemyBar;
		
	}
	
	protected virtual void Update () 
	{
		m_copyState = m_state;
		
		if(m_queryInput)
		{
			HandleAllCooldowns();
			
			//HandleDistFromGround();
				
			HealthCheck();
			
			switch (m_state)
			{
			case State.Moving:
				
				HandleModifiers();
				
				HandleMovement();
				
				LookAtMouse();
				
				HandleAbilities();
				
				HealthCheck();
				
				break;
			case State.Casting:
				
				HandleCast();
				
				//CancelCheck();
				
				LookAtMouse();
				
				break;
			case State.Channeling:
				
				HandleChannel();
				
				LookAtMouse();
				
				// CancelCheck();
				
				break;
//			case State.MoveAndChannel:
//				
//				HandleMovement();
//				
//				HandleChannel();
//				
//				break;
			case State.Stunned:
				
				HandleStun();
				
				break;
			case State.Traversal:
				
				HandleTraversal();
				
				break;
			case State.Dead:
				
				HandleDeath();
				
				break;
			}
		}
	}
	
	//private functions
	void HandleMovement()
	{
		Vector3 t_dir = Vector3.zero;
		
		if(m_cardinal)
		{
			if(Input.GetKey(KeyCode.W)) 
				t_dir += Vector3.forward;
			
			if(Input.GetKey(KeyCode.A)) 
				t_dir += Vector3.left;
			
			if(Input.GetKey(KeyCode.S)) 
				t_dir += Vector3.back;
			
			if(Input.GetKey(KeyCode.D)) 
				t_dir += Vector3.right;
			
			if(Input.GetKey(KeyCode.Space))
			{
				//for testing stuff
				//SetHealthMod(-.05f, 2f);
				//SetMod(-.05f, 2f, "Health");
				//SetSpeedMod(-9f, 2f);
				SetStun(3f);
			}
		}
		else
		{
			if(Input.GetKey(KeyCode.W)) 
				t_dir += transform.forward;
			
			if(Input.GetKey(KeyCode.A)) 
				t_dir += -transform.right;
			
			if(Input.GetKey(KeyCode.S)) 
				t_dir += -transform.forward;
			
			if(Input.GetKey(KeyCode.D)) 
				t_dir += transform.right;
		}
		
		t_dir.Normalize();
		
		//transform.position += t_dir * (m_speed + m_speedMod) * Time.deltaTime;
		//Debug.Log(t_dir * (m_speed + m_speedMod));
		
		//calculation
		
		if(t_dir == Vector3.zero)
		{
			PlayAnimation("Idle");
		}
		else
		{
			float t_angle = Vector3.Dot(transform.forward, t_dir);
			if(t_angle >= 0) //forward
			{
				PlayAnimation("Forward");
	//			if(Vector3.Dot(transform.right, t_dir) >= 0) //right
	//			{
	//				if(t_angle > 1/Mathf.Sqrt(2))
	//					PlayAnimation("forward");
	//				else
	//					PlayAnimation("right");
	//			}
	//			else //left
	//			{
	//				if(t_angle > 1/Mathf.Sqrt(2))
	//					PlayAnimation("forward");
	//				else
	//					PlayAnimation("left");
	//			}
			}
			else //backwards
			{
				PlayAnimation("Back");
	//			if(Vector3.Dot(transform.right, t_dir) >= 0) //right
	//			{
	//				if(t_angle > -1/Mathf.Sqrt(2))
	//					PlayAnimation("right");
	//				else
	//					PlayAnimation("back");
	//			}
	//			else //left
	//			{
	//				if(t_angle > -1/Mathf.Sqrt(2))
	//					PlayAnimation("left");
	//				else
	//					PlayAnimation("back");
	//			}
			}
		}
		
//		if(t_dir != Vector3.zero)
//			PlayAnimation("forward");
		//else idle
			
		m_characterController.SimpleMove(t_dir * (m_speed + m_speedMod));
	}
	
	void LookAtMouse()
	{
		Vector3 t_lookDir;
		RaycastHit t_hit;
		
		Ray t_ray = Camera.main.ScreenPointToRay(Input.mousePosition);
		
		//players and all above ground get own layer that raycast will ignore
		
		Physics.Raycast(t_ray, out t_hit);
		//Physics.Raycast(t_ray, out t_hit, 1000.0f, LayerMask.NameToLayer("TestGround"));
		//m_mouseLocation = t_hit.point;
		t_lookDir = t_hit.point - transform.position;
		t_lookDir.Normalize();
		t_lookDir.y = 0.0f;
		
		if(t_lookDir != Vector3.zero)
			transform.rotation = Quaternion.LookRotation(t_lookDir);
	}
	
	void HandleAbilities()
	{
		//left mouse ability
		if(Input.GetKeyDown(KeyCode.Mouse0))
			if(!m_onCooldown[c_lmNumber])
				HandleActivation(m_lmaList, c_lmNumber);
		
		//right mouse ability
		if(Input.GetKeyDown(KeyCode.Mouse1))
			if(!m_onCooldown[c_rmNumber])
				HandleActivation(m_rmaList, c_rmNumber);
		
		//q ability
		if(Input.GetKeyDown(KeyCode.Q))
			if(!m_onCooldown[c_qNumber])
				HandleActivation(m_qaList, c_qNumber);
		
		//e ability
		if(Input.GetKeyDown(KeyCode.E))
			if(!m_onCooldown[c_eNumber])
				HandleActivation(m_eaList, c_eNumber);
	}
	
	void HandleAllCooldowns()
	{
		if(m_onCooldown[c_lmNumber])
			HandleCooldown(c_lmNumber, m_lmaList[0].GetCooldown());
		
		if(m_onCooldown[c_rmNumber])
			HandleCooldown(c_rmNumber, m_rmaList[0].GetCooldown());
		
		if(m_onCooldown[c_qNumber])
			HandleCooldown(c_qNumber, m_qaList[0].GetCooldown());
		
		if(m_onCooldown[c_eNumber])
			HandleCooldown(c_eNumber, m_eaList[0].GetCooldown());
	}
	
	void HandleCooldown(int i_abilityNumber, float i_cooldown)
	{
		m_timers[i_abilityNumber] += Time.deltaTime;
		
		if(m_timers[i_abilityNumber] >= i_cooldown)
		{
			m_timers[i_abilityNumber] = 0.0f;
			m_onCooldown[i_abilityNumber] = false;
		}
	}
	
	void HandleActivation(List<BaseAbility> i_aList, int i_abilityNumber)
	{
		m_castLength = i_aList[0].GetCastTime();
		m_channelLength = i_aList[0].GetChannelTime();
		
		foreach(BaseAbility ab in i_aList)
		{
			if(!ab.IsActive())
			{
				//m_targetLocation = Abilityf.GetNearestPointOnGround(transform.position);
				m_activeAbility = ab;
				m_activeAbilityNumber = i_abilityNumber;
				m_state = State.Casting;
				break;
			}
		}
	}

	void HandleCast()
	{
		//Debug.Log("Casting");
		m_castTimer += Time.deltaTime;
		
		PlayAnim("Cast");
		
		if(m_castTimer >= m_castLength)
		{
			m_castTimer = 0.0f;
			if(m_channelLength > 0)
				m_state = State.Channeling;
			else
				m_state = State.Moving;
			
			m_onCooldown[m_activeAbilityNumber] = true;
			
			HandleAbility();
			
			HandleRadialTiles();
		}
	}
	
	void HandleAbility()
	{
		string t_state = m_activeAbility.GetState();
		
		if(t_state == "Instant")
			m_activeAbility.ActivateAbility();
		else if(t_state == "Traversal")
			m_activeAbility.ActivateAbility(Abilityf.GetNearestPointOnGround(transform.position));
//		else if(t_state == "DirectPlacement")
//			m_activeAbility.ActivateAbility(m_targetLocation);
		else if(t_state == "Thrown" || t_state == "DirectPlacement")
			m_activeAbility.ActivateAbility(m_firePoint.transform.position, Abilityf.GetNearestPointOnGround(transform.position));
		else if(t_state == "Projectile")
			m_activeAbility.ActivateAbility(m_firePoint.transform.position, transform.forward);
		else
			Debug.Log("Ability State Not Set Correctly");
	}
	
	void HandleChannel()
	{
		Debug.Log("Channeling");
		m_channelTimer += Time.deltaTime;
		
		if(m_channelTimer >= m_channelLength)
		{
			m_channelTimer = 0.0f;
			m_state = State.Moving;
		}
	}
	
	void HandleTraversal()
	{
		m_lerpTimer += Time.deltaTime;
		transform.position = Vector3.Lerp(m_lerpStartPos, m_lerpEndPos, m_lerpTimer/m_lerpLength);
		
		if(m_lerpTimer > m_lerpLength)
		{
			m_lerpTimer = 0.0f;
			if(m_stunTimer > 0)
				m_state = State.Stunned;
			else
				m_state = State.Moving;
		}
	}
	
	void HandleDistFromGround()
	{
		/*
		//stay set distance from ground
		RaycastHit t_hit;
		if(Physics.Raycast(transform.position, Vector3.down, out t_hit, LayerMask.NameToLayer("GroundLayer")))
		{
			Vector3 t_pos = transform.position;
			t_pos.y = t_hit.point.y + c_distFromGround;
			
			transform.position = t_pos;
		}
		*/
	}
	
	[RPC]
	public void NetworkHandleDeath(NetworkViewID i_id)
	{
		if(networkView.viewID == i_id)
		{
			m_health = 0.0f;
			m_state = State.Dead;
			Respawn();
		}
	}
	
	void HandleDeath()
	{
		//I died. RPC -> I Died.
		//Check If we have lives.
		//GameFlower RPC -> Reduce Team Lives
		//RPC -> Respawn;
		
		if(m_gameFlowManager != null)
		{
			if(m_gameFlowManager.GetNumLives(tag) > 1)
			{
				m_gameFlowManager.ReduceLife(tag);
				networkView.RPC("NetworkHandleDeath", RPCMode.All, networkView.viewID);
			}
			else if(!m_deadAnim)
			{
				m_deadAnim = true;
				PlayAnim("Dead");
			}
		}
		else
		{
			Debug.Log("No GFM ");
			Respawn();
		}
	}
	
	void HandleRadialTiles()
	{
		foreach(BaseAbility ability in m_lmaList)
		{
			if(m_activeAbility == ability)
			{
				m_abilityIcons.m_iconList[0].GetComponent<RadialTile>().invokeCD(m_activeAbility.GetCooldown());
				return;
			}
		}
		
		foreach(BaseAbility ability in m_rmaList)
		{
			if(m_activeAbility == ability)
			{
				m_abilityIcons.m_iconList[1].GetComponent<RadialTile>().invokeCD(m_activeAbility.GetCooldown());
				return;
			}
		}
		
		foreach(BaseAbility ability in m_qaList)
		{
			if(m_activeAbility == ability)
			{
				m_abilityIcons.m_iconList[2].GetComponent<RadialTile>().invokeCD(m_activeAbility.GetCooldown());
				return;
			}
		}
		
		foreach(BaseAbility ability in m_eaList)
		{
			if(m_activeAbility == ability)
			{
				m_abilityIcons.m_iconList[3].GetComponent<RadialTile>().invokeCD(m_activeAbility.GetCooldown());
				return;
			}
		}
	}
	
	void HandleModifiers()
	{
		float t_dt = Time.deltaTime;
		
		//health mod
		if(m_hmTimer > 0)
		{
			m_hmTimer -= t_dt;
			m_health += m_healthMod;
		}
		
		//speed mod
		if(m_smTimer > 0)
			m_smTimer -= t_dt;
		else
			m_speedMod = 0.0f;
		
		//armor mod
		if(m_amTimer > 0)
			m_amTimer -= t_dt;
		else
			m_armorMod = 0.0f;
		
		//magic resist mod
		if(m_mrmTimer > 0)
			m_mrmTimer -= t_dt;
		else
			m_magicResMod = 0.0f;
	}
	
	void HandleStun()
	{
		PlayAnim("Stun");
		m_stunTimer += Time.deltaTime;
		
		if(m_stunTimer >= m_stunLength)
		{
			m_stunTimer = 0.0f;
			m_state = State.Moving;
		}
	}
	
	void HealthCheck()
	{
		if(m_health <= 0.0f)
		{
			m_health = 0.0f;
			m_state = State.Dead;
		}
	}
	
	void CancelCheck()
	{
		bool t_check = false;
		
		if(Input.GetKeyDown(KeyCode.W))
			t_check = true;
		if(Input.GetKeyDown(KeyCode.A))
			t_check = true;
		if(Input.GetKeyDown(KeyCode.S))
			t_check = true;
		if(Input.GetKeyDown(KeyCode.D))
			t_check = true;
		
		if(t_check)
		{
			m_castTimer = 0.0f;
			m_channelTimer = 0.0f;
			if(m_activeAbility.IsActive())
				m_activeAbility.Deactivate();
			m_state = State.Moving;
		}
	}
	
	void PlayAnim(string i_animName)
	{
		if(m_model.animation)
		{
//			if(i_animName == "forward" || i_animName == "back") //or other base animations
//			{
//				if(!m_model.animation.isPlaying)
//					m_model.animation.CrossFade(i_animName);
//					//m_model.animation.Play(i_animName);
//			}
//			else
				if(!m_model.animation.IsPlaying(i_animName))
					m_model.animation.CrossFade(i_animName);
					//m_model.animation.Play(i_animName);
		}
	}
	
	void Respawn() 
	{
		transform.position = m_startPos;
		Reset();
	}
	
	void Reset()
	{
		m_state = State.Moving;
		m_health = m_maxHealth;
		m_castTimer = 0.0f;
		m_channelTimer = 0.0f;
		m_stunTimer = 0.0f;
		m_healthMod = 0.0f;
		m_speedMod = 0.0f;
		m_armorMod = 0.0f;
		m_magicResMod = 0.0f;
		m_hmTimer = 0.0f;
		m_smTimer = 0.0f;
		m_amTimer = 0.0f;
		m_mrmTimer = 0.0f;
		
		for(int i = 0; i < c_numberOfAbilities; i++)
		{
			m_timers[i] = 0.0f;
			m_onCooldown[i] = false;
		}
	}
	
	//public functions
	public bool IsAlive()
	{
		if(m_state == State.Dead)
			return false;
		
		return true;
	}
	
	public float GetHealth()
	{
		return m_health;
	}
	
	public float GetMaxHealth()
	{
		return m_maxHealth;
	}
	
	public float GetCastLength()
	{
		return m_castLength;
	}
	
	public float GetCastTimer()
	{
		return m_castTimer;
	}
	
	public float GetChannelLength()
	{
		return m_channelLength;
	}
	
	public float GetChannelTimer()
	{
		return m_channelTimer;
	}
	
	public bool IsCasting()
	{
		if(m_state == State.Casting)
			return true;
		return false;
	}
	
	public bool IsChanneling()
	{
		if(m_state == State.Channeling)
			return true;
		return false;
	}
	
	public void ReduceHealth(float i_dmg, string i_type)
	{
		if(i_type == "Physical")
		{
			if(i_dmg - m_armor > c_minimumDamage)
				m_health -= (i_dmg - (m_armor + m_armorMod));
			else
				m_health -= c_minimumDamage;
		}
		else if(i_type == "Magical")
		{
			if(i_dmg - m_magicResist > c_minimumDamage)
				m_health -= (i_dmg - (m_magicResist + m_magicResMod));
			else
				m_health -= c_minimumDamage;
		}
		else
			m_health -= i_dmg;
		
		if(m_health < 0)
			m_health = 0;
	}
	
	public void SetHealthMod(float i_healthMod, float i_duration)
	{
		m_healthMod = i_healthMod;
		m_hmTimer = i_duration;
	}
	
	public void SetSpeedMod(float i_speedMod, float i_duration)
	{
		m_speedMod = i_speedMod;
		m_smTimer = i_duration;
	}
	
	public void SetArmorMod(float i_armorMod, float i_duration)
	{
		m_armorMod = i_armorMod;
		m_amTimer = i_duration;
	}
	
	public void SetMagicResistMod(float i_magicResMod, float i_duration)
	{
		m_magicResMod = i_magicResMod;
		m_mrmTimer = i_duration;
	}
	
	public void Stun(float i_stunTime)
	{
		m_stunLength = i_stunTime;
		m_stunTimer = 0.0f;
		m_state = State.Stunned;
	}
	
	public void ActivateTraversal(Vector3 i_endPos, float i_lerpLength)
	{
		m_lerpStartPos = transform.position;
		m_lerpEndPos = i_endPos;
		m_lerpLength = i_lerpLength;
		m_lerpTimer = 0.0f;
		m_state = State.Traversal;
	}
	
	public void Death()
	{
		m_state = State.Dead;
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
	
	public void SetUserInput(bool i_queryinput)
	{
		m_queryInput = i_queryinput;
	}
	
	[RPC]
	void MakeMeAbility(string i_ability, string i_team, NetworkViewID i_id)
	{
		BaseAbility t_ability;
		GameObject t_o;
		
		if(i_ability == "Left")
		{
			t_ability = (BaseAbility)Instantiate(m_leftMouseAbility, m_leftMouseAbility.GetInactivePos(), Quaternion.identity);
			t_ability.tag = i_team + "Ability";
			t_ability.GetComponent<NetworkView>().viewID = i_id;
			
			if(t_ability.networkView.isMine)
			{
				t_ability.SetPlayerRef(this);
				m_lmaList.Add(t_ability);
			}
		}
		
		else if(i_ability == "Right")
		{
			t_ability = (BaseAbility)Instantiate(m_rightMouseAbility, m_rightMouseAbility.GetInactivePos(), Quaternion.identity);
			t_ability.tag = i_team + "Ability";
			t_ability.GetComponent<NetworkView>().viewID = i_id;
			
			if(t_ability.networkView.isMine)
			{
				t_ability.SetPlayerRef(this);
				m_rmaList.Add(t_ability);
			}
		}
		
		else if(i_ability == "Q")
		{
			t_ability = (BaseAbility)Instantiate(m_qAbility, m_qAbility.GetInactivePos(), Quaternion.identity);
			t_ability.tag = i_team + "Ability";
			t_ability.GetComponent<NetworkView>().viewID = i_id;
			
			if(t_ability.networkView.isMine)
			{
				t_ability.SetPlayerRef(this);
				m_qaList.Add(t_ability);
			}
		}
		
		else if(i_ability == "E")
		{
			t_ability = (BaseAbility)Instantiate(m_eAbility, m_eAbility.GetInactivePos(), Quaternion.identity);
			t_ability.tag = i_team + "Ability";
			t_ability.GetComponent<NetworkView>().viewID = i_id;
			
			if(t_ability.networkView.isMine)
			{
				t_ability.SetPlayerRef(this);
				m_eaList.Add(t_ability);
			}
		}
	}
	
	public void PlayAnimation(string i_animName)
	{
		networkView.RPC("NetworkPlayAnimation", RPCMode.All, i_animName, networkView.viewID);
	}
		
	[RPC]
	public void NetworkPlayAnimation(string i_animName, NetworkViewID i_netID)
	{
		if(networkView.viewID == i_netID)
			PlayAnim(i_animName);
	}
	
	protected void SetAnimSpeed(string i_animName, float i_animSpeed)
	{
		if(m_model.animation)
		{
			m_model.animation[i_animName].speed = i_animSpeed;
		}
	}
	
	public void TakeDamage(string i_type, float i_dmg)
	{
		networkView.RPC("NetworkTakeDamage", RPCMode.All, i_type, i_dmg, networkView.viewID);
	}
	
	[RPC]
	public void NetworkTakeDamage(string i_type, float i_dmg, NetworkViewID i_netID)
	{
		if(networkView.viewID == i_netID)
			ReduceHealth(i_dmg, i_type);
	}
	
	public void SetStun(float i_stunTime)
	{
		networkView.RPC("NetworkSetStun", RPCMode.All, i_stunTime, networkView.viewID);
	}
	
	[RPC]
	public void NetworkSetStun(float i_stunTime, NetworkViewID i_netID)
	{
		if(networkView.viewID == i_netID)
			Stun(i_stunTime);
	}
	
	public void SetMod(float i_mod, float i_duration, string i_type)
	{
		//NetworkSetMod(i_mod, i_duration, i_type);
		networkView.RPC("NetworkSetMod", RPCMode.All, i_mod, i_duration, i_type, networkView.viewID);
	}
	
	[RPC]
	public void NetworkSetMod(float i_mod, float i_duration, string i_type, NetworkViewID i_netID)
	{
		if(networkView.viewID == i_netID)
		{
			if(i_type == "Speed")
			{
				SetSpeedMod(i_mod, i_duration);
			}
			else if(i_type == "Health")
			{
				SetHealthMod(i_mod, i_duration);
			}
			//also need armor and mr?
		}
	}
}