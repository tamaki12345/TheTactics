using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Title : MonoBehaviour
{
    public void PlayScene()
    {
        SceneManager.LoadScene("PlayScene", LoadSceneMode.Single);
    }

    [SerializeField]
    private GameObject title;
    [SerializeField]
    private GameObject rules;
    [SerializeField]
    private GameObject rule1;
    [SerializeField]
    private GameObject rule2;
    [SerializeField]
    private GameObject rule3;

    private int page = 1;

    public void nextPage()
    {
        if( page == 1 )
        {
            rule1.SetActive(false);
            rule2.SetActive(true);
            page += 1;
        }
        else if( page == 2 )
        {
            rule2.SetActive(false);
            rule3.SetActive(true);
            page += 1;
        }
        else if( page == 3 )
        {
            rule1.SetActive(true);
            rule2.SetActive(false);
            rule3.SetActive(false);
            rules.SetActive(false);
            title.SetActive(true);
            page = 1;
        }
    }
    
    public void backPage()
    {
        if( page == 1 )
        {
            rule1.SetActive(true);
            rule2.SetActive(false);
            rule3.SetActive(false);
            rules.SetActive(false);
            title.SetActive(true);
            page = 1;
        }
        else if( page == 2 )
        {
            rule2.SetActive(false);
            rule1.SetActive(true);
            page -= 1;
        }
        else if( page == 3 )
        {
            rule2.SetActive(true);
            rule3.SetActive(false);
            page -= 1;
        }
    }

    public void toRules()
    {
        rules.SetActive(true);
        title.SetActive(false);
        page = 1;
    }
    
    public void toTitle()
    {
        rules.SetActive(false);
        title.SetActive(true);
    }
}
