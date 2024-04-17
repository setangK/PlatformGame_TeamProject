using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System.IO;

public class PlayerProfileManager : MonoBehaviour
{
    public ItemProperty[] playerItems = new ItemProperty[7];
    public string savePath;
    public UnityEvent<ItemStat> updatePlayerInvenAct;

    PlayerController player;
    ItemProperty[] saveItems = new ItemProperty[7];
    ItemProperty[] loadItems = new ItemProperty[7];


    // load, load가 할 일은 저장된 플레이어의 현재 체력과 인벤토리 정보를 읽어와 플레이어에 반영한다. 반영할 때 이전에 썼던 Calculate를 써서 스텟을 적용한다.
    private void Awake()
    {
        if (SceneChanger.instance != null)
        {
            SceneChanger.instance.savePlayerProfileAct.AddListener(SavePlayerInvenProfile);
        }
        LoadPlayerInvenProfile();
    }

    // Start is called before the first frame update
    void Start()
    {

    }

    public void SavePlayerInvenProfile() // save : save가 할 일은 현재 플레이어의 체력과, 플레이어의 인벤토리 정보를 저장한다.
    {
        player = FindObjectOfType<PlayerController>();
        float _curhp = player.GetCurHP(); // 현재 체력을 가져옴
        for (int i = 0; i < 7; i++)
        {
            if (playerItems[i] != null)
            {
                saveItems[i] = playerItems[i];
            }
        }

        PlayerInventoryProfile playerProfile = new PlayerInventoryProfile(_curhp, saveItems);

        // 데이터를 JSON 형식으로 직렬화
        string json = JsonUtility.ToJson(playerProfile);
        // JSON 파일로 저장
        File.WriteAllText(savePath, json);
    }

    void LoadPlayerInvenProfile()
    {
        player = FindObjectOfType<PlayerController>();
        // JSON 파일로부터 데이터 읽기
        if (File.Exists(savePath))
        {
            string json = File.ReadAllText(savePath);

            // JSON을 데이터 구조로 역직렬화
            PlayerInventoryProfile playerProfile = JsonUtility.FromJson<PlayerInventoryProfile>(json);

            // 캐릭터 위치 설정
            if (player != null) player.Initialize(playerProfile.GetPlayerCurHP());
            for(int i = 0; i < 7; i++)
            {
                if (playerProfile.GetItemProperty(i) != null)
                {
                    loadItems[i] = playerProfile.GetItemProperty(i);
                    updatePlayerInvenAct?.Invoke(loadItems[i].GetItemStat());
                }
            }

            Debug.Log("Character position loaded from " + savePath);
        }
        else
        {
            Debug.LogWarning("No saved character position found at " + savePath);
        }
    }

    [System.Serializable]
    public class PlayerInventoryProfile
    {
        float playerCurHp;
        ItemProperty[] savedInven = new ItemProperty[7];

        public PlayerInventoryProfile(float _playerCurHp, ItemProperty[] _savedInven)
        {
            this.playerCurHp = _playerCurHp;
            this.savedInven = _savedInven;
        }

        public float GetPlayerCurHP()
        {
            return this.playerCurHp;
        }

        public ItemProperty GetItemProperty(int idx)
        {
            return this.savedInven[idx];
        }
    }

}
