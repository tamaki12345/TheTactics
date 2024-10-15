using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

    public Piece_Class(string name,int id, int piece_type)
    {
        this.obj = GameObject.Find(name).gameObject;
        this.piece_id = id;
        this.type = piece_type;
    }
}
