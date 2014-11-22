using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GameFlowManager : MonoBehaviour 
{
	enum GameType
	{
		CaptureTheFlag,
		DeathMatch,
		Elimination
	}
	
	GameType m_gameType = GameType.DeathMatch;
	
	public List<Player> m_teamOneList = new List<Player>();
	public List<Player> m_teamTwoList = new List<Player>();
	
	public int m_teamOneLives = 3;
	public int m_teamTwoLives = 3;
	
	Player m_myPlayer;
	
	bool m_teamOneLose = false;
	bool m_teamTwoLose = false;
	
	void Start () 
	{
		
	}
	
	void Update () 
	{
		if(m_teamOneLives <= 0)
			m_teamOneLose = true;
		
		else if(m_teamTwoLives <= 0)
			m_teamTwoLose = true;
	}
	
	void OnGUI()
	{
		if(m_teamOneLose)
		{
			GUI.Label(new Rect(Screen.width * 0.5f - 50, Screen.height * 0.5f - 10, 100, 20), "Team 2 Wins");
		}
		
		else if(m_teamTwoLose)
		{
			GUI.Label(new Rect(Screen.width * 0.5f - 50, Screen.height * 0.5f - 10, 100, 20), "Team 1 Wins");
		}
	}
		
	[RPC]
	public void NetworkReduceLife(string i_teamName)
	{
		if(i_teamName == "Team1")
			m_teamOneLives --;
		else
			m_teamTwoLives --;
	}
	
	public void ReduceLife(string i_teamName)
	{
		networkView.RPC("NetworkReduceLife", RPCMode.All, i_teamName);
	}
	
	public int GetNumLives(string i_teamName)
	{
		if(i_teamName == "Team1")
			return m_teamOneLives;
		
		return m_teamTwoLives;
	}
	
	public void AddToTeamList(Player i_class, string i_teamName)
	{
		if(i_class.networkView.isMine)
			m_myPlayer = i_class;
		
		if(i_teamName == "Team1")
			m_teamOneList.Add(i_class);
		else
			m_teamTwoList.Add(i_class);
	}
	
	public string GetGameType()
	{
		return m_gameType.ToString();
	}
	
	public Player GetMyPlayer()
	{
		return m_myPlayer;
	}
	
	public Player GetPlayerWithNetID(NetworkViewID i_id)
	{
		for(int i = 0; i < m_teamOneList.Count; i++)
		{
			if(m_teamOneList[i].networkView.viewID == i_id)
				return m_teamOneList[i];
		}
		
		for(int i = 0; i < m_teamTwoList.Count; i++)
		{
			if(m_teamTwoList[i].networkView.viewID == i_id)
				return m_teamTwoList[i];
		}
		
		return null;
	}
}
