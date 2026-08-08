using System;
using UnityEngine;

public class GoldWallet : MonoBehaviour
{
    private const string BalanceKey = "Meta.Gold";
    public static GoldWallet Instance { get; private set; }

    public int Balance { get; private set; }
    public int RunReward { get; private set; }
    public int LastBankedReward { get; private set; }
    private float m_RunRewardExact;
    public event Action<int> OnBalanceChanged;
    public event Action<int> OnRunRewardChanged;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        Instance = null;
        new GameObject(nameof(GoldWallet)).AddComponent<GoldWallet>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        Balance = PlayerPrefs.GetInt(BalanceKey, 0);
    }

    public void Add(int amount)
    {
        if (amount <= 0) return;
        Balance += amount;
        PlayerPrefs.SetInt(BalanceKey, Balance);
        PlayerPrefs.Save();
        OnBalanceChanged?.Invoke(Balance);
    }

    public void AddRunReward(int amount)
    {
        AddRunReward((float)amount);
    }

    public void AddRunReward(float amount)
    {
        if (amount <= 0) return;
        m_RunRewardExact += amount;
        RunReward = Mathf.FloorToInt(m_RunRewardExact + 0.0001f);
        OnRunRewardChanged?.Invoke(RunReward);
    }

    public int BankRunReward()
    {
        int reward = RunReward;
        LastBankedReward = reward;
        RunReward = 0;
        m_RunRewardExact = 0f;
        if (reward > 0) Add(reward);
        OnRunRewardChanged?.Invoke(RunReward);
        return reward;
    }

    public void ResetRunReward()
    {
        RunReward = 0;
        m_RunRewardExact = 0f;
        OnRunRewardChanged?.Invoke(0);
    }

    public bool TrySpend(int amount)
    {
        if (amount < 0 || Balance < amount) return false;
        Balance -= amount;
        PlayerPrefs.SetInt(BalanceKey, Balance);
        PlayerPrefs.Save();
        OnBalanceChanged?.Invoke(Balance);
        return true;
    }
}
