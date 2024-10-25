using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;

public class Piece_Class : MonoBehaviour
{
    //駒の実体
    private GameObject obj;

    //駒id
    private int piece_id;

    /*
    駒のタイプ
    1:falnker
    2:hunter
    3:tank
    4:target
    */
    private int type;

    //駒の盤上の位置
    private (int , int) position;

    //駒が有効か（とられていないか）
    private bool enable = true;
    //駒がみられているか
    private bool visible = false;

    public int Type()
    {
        return type;
    }

    public GameObject Object()
    {
        return obj;
    }

    public int Id()
    {
        return piece_id;
    }

    public (int, int) Position()
    {
        return position;
    }

    public bool Enable()
    {
        return enable;
    }

    public bool Visible()
    {
        return visible;
    }

    public Piece_Class(string name,int id, int piece_type)
    {
        this.obj = GameObject.Find(name).gameObject;
        this.piece_id = id;
        this.type = piece_type;
    }

    public void SetPosition( int x, int y )
    {
        position = (x, y);
    }

    public void SwapEnable()
    {
        if( enable )
        {
            enable = false;
        }
        else
        {
            enable = true;
        }
    }

    public void SwapVisible()
    {
        if( visible )
        {
            visible = false;
        }
        else
        {
            visible = true;
        }
    }
}
