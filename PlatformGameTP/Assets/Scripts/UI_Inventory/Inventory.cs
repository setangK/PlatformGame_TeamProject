using InventorySystem;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using UnityEngine;
using UnityEngine.UI;

public class Inventory :ItemProperty
{

    public Transform MyInventory;
    [SerializeField] Transform MyOptions;

    public GameObject MyExplanation;
    public bool checkInventory;
    public bool checkOptions;
    public bool CheckItemBox;
    public AudioSource myInventory;
    public AudioClip startInventory;
    public AudioClip closeInventory;
    public AudioClip startOption;
    public AudioClip closeOption;

    // Start is called before the first frame update
    void Start()
    {
        CheckItemBox = false;
        if (MyInventory != null)
        {
            checkInventory = false;
            MyInventory.gameObject.SetActive(false);
 
        }
        if (MyOptions != null)
        {
            checkOptions = false;
            MyOptions.gameObject.SetActive(false);
        }

        if (MyExplanation != null)
        {
           
            MyExplanation.gameObject.SetActive(false);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.I))
        {
            if (checkInventory == false && checkOptions == false && CheckItemBox == false)
            {
                PopUp(MyInventory);
                checkInventory= true;
                if (myInventory != null)
                {
                    myInventory.clip = startInventory;
                    myInventory.PlayOneShot(startInventory);
                }
                if (myInventory.isPlaying) Debug.Log("인벤토리 열기");
            }
            else
            {
                PopDown(MyInventory);
                checkInventory= false;
                MyExplanation.gameObject.SetActive(false);
                if (myInventory != null)
                {
                    myInventory.clip = closeInventory;
                    myInventory.PlayOneShot(closeInventory);
                }
                if (myInventory.isPlaying) Debug.Log("인벤토리 닫기");
            }
        }

        if(Input.GetKeyDown(KeyCode.Escape))
        {
            if (checkOptions == false && checkInventory == false && CheckItemBox == false)
            {
                PopUp(MyOptions);
                checkOptions= true;
                if (myInventory != null)
                {
                    myInventory.clip = startOption;
                    myInventory.PlayOneShot(startOption);
                }
                if (myInventory.isPlaying) Debug.Log("옵션 열기");
            }
            else
            {
                PopDown(MyOptions);
                checkOptions= false;
                if (myInventory != null)
                {
                    myInventory.clip = closeOption;
                    myInventory.PlayOneShot(closeOption);
                }
                if (myInventory.isPlaying) Debug.Log("인벤토리 닫기");
            }
        }

    }

    public void PopUp(Transform popup)
    {
        popup.gameObject.SetActive(true);
    }
    public void PopDown(Transform popup)
    {
        popup.gameObject.SetActive(false);
    }


}

