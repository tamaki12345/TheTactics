using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using Unity.VisualScripting;
using System.Data.Common;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.Rendering;

public class GameControl : MonoBehaviourPunCallbacks
{
    //プレイヤーの駒
    private Piece_Class[] pieces = new Piece_Class[10];
    
    //相手の駒
    private Piece_Class[] enemy_pieces = new Piece_Class[10];
    
    //盤面変数, player:1~9, enemy:11~19, wall:-1
    private int[,] board = new int[15, 15];

    //wall位置
    private int[,] walls = new int[28, 2] {
        {3,3}, {3,4}, {3,5}, {3,6}, {3,7}, {3,8}, {3,9}, {3,10}, {3,11},  {2,7},  {4,9}, {5,9}, {6,9}, {7,9},
        {11,3},{11,4},{11,5},{11,6},{11,7},{11,8},{11,9},{11,10},{11,11},  {7,5}, {8, 5}, {9, 5}, {10,5}, {12, 7}
    };

    //駒初期位置
    private int[,] piece_point = new int[10, 3] {
        {4, 0, 5}, {7, 0, 9}, {10, 0, 6}, {3, 1, 1}, {5, 1, 7}, {9, 1, 8}, {11, 1, 2}, {6, 2, 3}, {7, 3, 10}, {8, 2, 4}
    };

    //mainCamera
    public Camera mainCamera;

    //待機画面
    private GameObject waiting_overlay;
    //終了画面
    private GameObject end_overlay;
    //終了画面
    private GameObject turn_overlay;

    //被選択駒id
    private int selected;

    //設置可能位置表示中か否か
    private bool shown = false;

    //表示中の駒位置
    private (int, int) shownPoint = (0, 0);

    //設置可能位置List
    private List< (int, int) > installables = new List< (int, int) >();

    //移動アニメーション中か
    private bool moving = false;

    //ターン中か
    private bool yourTurn = true;

    //設置可能位置 表示Object List
    private List< GameObject > temporaryObjects = new List< GameObject >();
    
    //通信用PhotonView
    PhotonView view;

    //SE再生用
    private AudioSource audioSource;

    private bool RuleIsOn = false;

    //Start時実行
    void Start()
    {
        waiting_overlay = GameObject.Find("Waiting_Overlay").gameObject;
        waiting_overlay.SetActive(true);

        end_overlay = GameObject.Find("GameEndOverlay").gameObject;
        end_overlay.SetActive(false);

        turn_overlay = GameObject.Find("Turn_Overlay").gameObject;
        turn_overlay.SetActive(false);

        view = PhotonView.Get(this);

        Application.targetFrameRate = 60; 

        InitializeBoard();
        yourTurn = false;

        audioSource = this.GetComponent<AudioSource>();

        // PhotonServerSettingsの設定内容を使ってマスターサーバーへ接続する
        PhotonNetwork.ConnectUsingSettings();
    }

    //クリック位置変数
    private (int, int) clickPoint = (0, 0);
    //毎フレーム実行
    void Update()
    {
        if( GetClickPoint() && !moving && ( yourTurn && !RuleIsOn ) )
        {
            if( installables.Contains( ( clickPoint.Item1 , clickPoint.Item2 ) ) )
            {
                moving = true;

                SendAction(shownPoint, clickPoint);
            }
            else if( SelectPiece() > 0 && !shown && SelectPiece() < 11 )
            {
                shown = true;
                selected = SelectPiece();
                shownPoint = clickPoint;

                Piece_Class selected_piece = pieces[ selected - 1 ];

                int id = selected_piece.Id();

                installables = new List< (int, int) >();
                installables = GetInstallableTiles(id);

                ShowInstallable(installables);
            }
            else if( SelectPiece() > 0 && shown )
            {
                if( shownPoint != clickPoint )
                {
                    DestroyTemporaries();
                    selected = SelectPiece();
                    shownPoint = clickPoint;

                    Piece_Class selected_piece = pieces[ selected - 1 ];

                    int id = selected_piece.Id();

                    installables = new List< (int, int) >();
                    installables = GetInstallableTiles(id);

                    ShowInstallable(installables);
                }
            }
            else if( SelectPiece() < 1 )
            {
                moving = false;
                shown = false;
                installables = new List< (int, int) >();
                DestroyTemporaries();
            }
        }
    }

    //boardを初期化
    void InitializeBoard()
    {  
        //壁の設置
        for( int i = 0; i < 28; i++ )
        {
            int x = walls[i,0];
            int y = walls[i,1];

            board[x,y] = -1;
        }

        //駒の情報の整理
        {
            pieces[0] = new Piece_Class("flanker", 1, 1);
            pieces[1] = new Piece_Class("flanker (1)", 2, 1);
            pieces[2] = new Piece_Class("flanker (2)", 3, 1);
            pieces[3] = new Piece_Class("flanker (3)", 4, 1);
            pieces[4] = new Piece_Class("hunter", 5, 2);
            pieces[5] = new Piece_Class("hunter (1)", 6, 2);
            pieces[6] = new Piece_Class("tank", 7, 3);
            pieces[7] = new Piece_Class("tank (1)", 8, 3);
            pieces[8] = new Piece_Class("target", 9, 4);
            pieces[9] = new Piece_Class("tank (2)", 10, 3);
        }

        //敵駒の情報の整理
        {
            enemy_pieces[0] = new Piece_Class("enemy_flanker", 1, 1);
            enemy_pieces[1] = new Piece_Class("enemy_flanker (1)", 2, 1);
            enemy_pieces[2] = new Piece_Class("enemy_flanker (2)", 3, 1);
            enemy_pieces[3] = new Piece_Class("enemy_flanker (3)", 4, 1);
            enemy_pieces[4] = new Piece_Class("enemy_hunter", 5, 2);
            enemy_pieces[5] = new Piece_Class("enemy_hunter (1)", 6, 2);
            enemy_pieces[6] = new Piece_Class("enemy_tank", 7, 3);
            enemy_pieces[7] = new Piece_Class("enemy_tank (1)", 8, 3);
            enemy_pieces[8] = new Piece_Class("enemy_target", 9, 4);
            enemy_pieces[9] = new Piece_Class("enemy_tank (2)", 10, 3);
        }

        //駒の初期位置
        for( int i = 0; i < 10; i++ )
        {
            int x = piece_point[i,0];
            int y = piece_point[i,1];
            int id = piece_point[i,2];

            board[x,y] = id;
            board[14 - x,14 - y] = 10 + id;

            pieces[id - 1].SetPosition(x, y);
            enemy_pieces[id - 1].SetPosition(14 - x, 14 - y);
            enemy_pieces[id - 1].SwapVisible();

            if(!pieces[i].Enable())
            {
                pieces[i].SwapEnable();
            }
            if(!enemy_pieces[i].Enable())
            {
                enemy_pieces[i].SwapEnable();
            }
        }

        for( int i = 0; i < 9; i++ )
        {
            UpdateBoard( pieces[i].Position() ) ;
        }
    }

    //クリック位置を取得
    bool GetClickPoint()
    {
        if (Input.GetMouseButton(0)) {
            var ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit = new RaycastHit();
            if (Physics.Raycast(ray.origin, ray.direction, out hit, Mathf.Infinity)) {
                clickPoint.Item1 = (int)Math.Round(hit.point.x / 2, MidpointRounding.ToEven) + 7;
                clickPoint.Item2 = (int)Math.Round( (hit.point.z + 0.25f) / 2, MidpointRounding.ToEven ) + 7;

                return true;
            }
        }
        return false;
    }

    //駒を選択
    int SelectPiece()
    {
        //クリック位置に駒があるか?
        if( board[ (int)clickPoint.Item1 ,  (int)clickPoint.Item2 ] > 0 && board[ (int)clickPoint.Item1 ,  (int)clickPoint.Item2 ] < 11 )
        {
            return board[ (int)clickPoint.Item1 ,  (int)clickPoint.Item2 ];
        }
        else
        {
            return -1;
        }
    }

    //駒の移動先をreturn
    List< (int, int) > GetInstallableTiles(int piece_id)
    {
        int x = (int)clickPoint.Item1;
        int y = (int)clickPoint.Item2;
        int type = pieces[ piece_id - 1 ].Type();

        List< (int, int) > ret = new List< (int, int) >();

        if( type == 1 || type == 4 )
        {
            if( x + 1 < 15 )
            {
                if( board[ x + 1 , y ] == 0 || board[ x + 1 , y ] > 10 )
                {
                    ret.Add( ( x + 1 , y ) );
                }
            }
            if( x + 1 < 15 && y + 1 < 15 )
            {
                if( board[ x + 1 , y + 1 ] == 0 || board[ x + 1 , y + 1 ] > 10 )
                {
                    ret.Add( ( x + 1 , y + 1 ) );
                }
            }
            if( y + 1 < 15)
            {
                if( board[ x , y + 1 ] == 0 || board[ x , y + 1 ] > 10 )
                {
                    ret.Add( ( x , y + 1 ) );
                }
            }
            if( x - 1 >= 0 && y + 1 < 15 )
            {
                if( board[ x - 1 , y + 1 ] == 0 || board[ x - 1 , y + 1 ] > 10 )
                {
                    ret.Add( ( x - 1 , y + 1 ) );
                }
            }
            if( x - 1 >= 0 )
            {
                if( board[ x - 1 , y ] == 0 || board[ x - 1 , y ] > 10 )
                {
                    ret.Add( ( x - 1 , y ) );
                }
            }
            if( x - 1 >= 0 && y - 1 >= 0 )
            {
                if( board[ x - 1 , y - 1 ] == 0 || board[ x - 1 , y - 1 ] > 10 )
                {
                    ret.Add( ( x - 1 , y - 1 ) );
                }
            }
            if( y - 1 >= 0 )
            {
                if( board[ x , y - 1 ] == 0 || board[ x , y - 1 ] > 10 )
                {
                    ret.Add( ( x , y - 1 ) );
                }
            }
            if( x + 1 < 15 && y - 1 >= 0 )
            {
                if( board[ x + 1 , y - 1 ] == 0 || board[ x + 1 , y - 1 ] > 10 )
                {
                    ret.Add( ( x + 1 , y - 1 ) );
                }
            }

            return ret;

        }
        else if( type == 2 )
        {
            if( x + 2 < 15 )
            {
                if( y + 1 < 15 && (board[ x + 2 , y + 1 ] == 0 || board[ x + 2 , y + 1 ] > 10 ) )
                {
                    if( board[ x + 1, y ] > -1 && board[ x + 1, y + 1 ] > -1 )
                    {
                        ret.Add( ( x + 2 , y + 1 ) );
                    }
                }
                if( y - 1 >= 0 && ( board[ x + 2 , y - 1 ] == 0 || board[ x + 2 , y - 1 ] > 10 ) )
                {
                    if( board[ x + 1, y ] > -1 && board[ x + 1, y - 1 ] > -1 )
                    {
                        ret.Add( ( x + 2 , y - 1 ) );
                    }
                }
            }
            if( y + 2 < 15 )
            {
                if( x + 1 < 15 && ( board[ x + 1 , y + 2 ] == 0 || board[ x + 1 , y + 2 ] > 10) )
                {
                    if( board[ x , y + 1 ] > -1 && board[ x + 1, y + 1 ] > -1 )
                    {
                        ret.Add( ( x + 1 , y + 2 ) );
                    }
                }
                if( x - 1 >= 0 && ( board[ x - 1 , y + 2 ] == 0 || board[ x - 1 , y + 2 ] > 10) )
                {
                    if( board[ x , y + 1 ] > -1 && board[ x - 1, y + 1 ] > -1 )
                    {
                        ret.Add( ( x - 1 , y + 2 ) );
                    }
                }
            }
            if( x - 2 >= 0 )
            {
                if( y + 1 < 15 && ( board[ x - 2 , y + 1 ] == 0 || board[ x - 2 , y + 1 ] > 10) )
                {
                    if( board[ x - 1, y ] > -1 && board[ x - 1, y + 1 ] > -1 )
                    {
                        ret.Add( ( x - 2 , y + 1 ) );
                    }
                }
                if( y - 1 >= 0 && ( board [ x - 2 , y - 1 ] == 0 || board [ x - 2 , y - 1 ] > 10) )
                {
                    if( board[ x - 1, y ] > -1 && board[ x - 1, y - 1 ] > -1 )
                    {
                        ret.Add( ( x - 2 , y - 1 ) );
                    }
                }
            }
            if( y - 2 >= 0 )
            {
                if( x + 1 < 15 && ( board[ x + 1 , y - 2 ] == 0 || board[ x + 1 , y - 2 ] > 10) )
                {
                    if( board[ x + 1, y - 1 ] > -1 && board[ x , y - 1 ] > -1 )
                    {
                        ret.Add( ( x + 1 , y - 2 ) );
                    }
                }
                if( x - 1 >= 0 && ( board[ x - 1 , y - 2 ] == 0 || board[ x - 1 , y - 2 ] > 10 ) )
                {
                    if( board[ x , y - 1 ] > -1 && board[ x - 1, y - 1 ] > -1 )
                    {
                        ret.Add( ( x - 1 , y - 2 ) );
                    }
                }
            }

            return ret;

        }
        else if( type == 3 )
        {
            int i,j;

            i = 1;
            while(true)
            {
                if( x + i < 15 )
                {
                    if( board[ x + i, y ] == 0 )
                    {
                        ret.Add( ( x + i, y ) );
                        i++;
                    }
                    else if( board[ x + i, y ] > 10 )
                    {
                        ret.Add( ( x + i, y ) );
                        break;
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    break;
                }
            }

            i = 1;
            j = 1;
            while(true)
            {
                if( x + i < 15 && y + j < 15 )
                {
                    if( board[ x + i, y + j ] == 0 )
                    {
                        ret.Add( ( x + i, y + j ) );
                        i++;
                        j++;
                    }
                    else if( board[ x + i, y + j ] > 10 )
                    {
                        ret.Add( ( x + i, y + j ) );
                        break;
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    break;
                }
            }
            
            j = 1;
            while(true)
            {
                if( y + j < 15 )
                {
                    if( board[ x , y + j ] == 0 )
                    {
                        ret.Add( ( x , y + j ) );
                        j++;
                    }
                    else if( board[ x , y + j ] > 10 )
                    {
                        ret.Add( ( x , y + j ) );
                        break;
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    break;
                }
            }

            i = 1;
            j = 1;
            while(true)
            {
                if( x - i >= 0 && y + j < 15 )
                {
                    if( board[ x - i, y + j ]  == 0 )
                    {
                        ret.Add( ( x - i, y + j ) );
                        i++;
                        j++;
                    }
                    else if( board[ x - i, y + j ]  > 10 )
                    {
                        ret.Add( ( x - i, y + j ) );
                        break;
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    break;
                }
            }

            i = 1;
            while(true)
            {
                if( x - i >= 0 )
                {
                    if( board[ x - i, y ] == 0 )
                    {
                        ret.Add( ( x - i, y ) );
                        i++;
                    }
                    else if( board[ x - i, y ] > 10 )
                    {
                        ret.Add( ( x - i, y ) );
                        break;
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    break;
                }
            }

            i = 1;
            j = 1;
            while(true)
            {
                if( x - i >= 0 && y - j >= 0 )
                {
                    if( board[ x - i, y - j ] == 0 )
                    {
                        ret.Add( ( x - i, y - j ) );
                        i++;
                        j++;
                    }
                    else if( board[ x - i, y - j ] > 10 )
                    {
                        ret.Add( ( x - i, y - j ) );
                        break;
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    break;
                }
            }

            j = 1;
            while(true)
            {
                if( y - j >= 0 )
                {
                    if( board[ x , y - j ] == 0 )
                    {
                        ret.Add( ( x , y - j ) );
                        j++;
                    }
                    else if( board[ x , y - j ] > 10 )
                    {
                        ret.Add( ( x , y - j ) );
                        break;
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    break;
                }
            }

            i = 1;
            j = 1;
            while(true)
            {
                if( x + i < 15 && y - j >= 0 )
                {
                    if( board[ x + i, y - j ] == 0 )
                    {
                        ret.Add( ( x + i, y - j ) );
                        i++;
                        j++;
                    }
                    else if( board[ x + i, y - j ] > 10 )
                    {
                        ret.Add( ( x + i, y - j ) );
                        break;
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    break;
                }
            }

            return ret;
        }
        else
        {
            return null;
        }
    }

    //設置可能位置を表示
    void ShowInstallable(List< (int, int) > points)
    {
        GameObject installable = (GameObject)Resources.Load("installable");

        for(int i = 0; i < points.Count; i++)
        {
            GameObject instance = (GameObject)Instantiate(installable);
            temporaryObjects.Add(instance);
            Vector3 position = instance.transform.position;
            position.x = ( (float)points[i].Item1 - 7f ) * 2f;
            position.z = ( (float)points[i].Item2 - 7f ) * 2f;
            instance.transform.position = position;
        }
    }

    //駒を移動
    string SEname;
    void MovePiece( (int, int) original, (int, int) destination )
    {
        GameObject obj;
        SEname = "SetPiece";
        
        int original_id = board[ (int)original.Item1, (int)original.Item2 ];
        int destination_id = board[ (int)destination.Item1, (int)destination.Item2 ];

        if( yourTurn )
        {
            obj = pieces[ original_id - 1 ].Object();
            pieces[original_id-1].SetPosition( (int)destination.Item1, (int)destination.Item2 );

            //敵駒をとったら
            if( destination_id > 10 )
            {
                SEname = "GetPiece";
                enemy_pieces[destination_id - 11].SwapEnable();

                if( enemy_pieces[destination_id-11].Type() == 4 )
                {
                    Win();
                    return;
                }
            }

            waiting_overlay.SetActive(true);
        }
        else
        {
            obj = enemy_pieces[ original_id - 11 ].Object();
            enemy_pieces[original_id-11].SetPosition( (int)destination.Item1, (int)destination.Item2 );

            //駒を取られたら
            if( destination_id > 0 )
            {
                SEname = "GotPiece";
                pieces[destination_id-1].SwapEnable();

                if( pieces[destination_id-1].Type() == 4 )
                {
                    Lose();
                    return;
                }
            }

            waiting_overlay.SetActive(false);
        }

        board[ (int)destination.Item1, (int)destination.Item2 ] = original_id;
        board[ (int)original.Item1, (int)original.Item2 ] = 0;

        Vector3 destination_position = obj.transform.position;

        float downDestination = destination_position.y;

        destination_position.x = ( (float)destination.Item1 - 7f ) * 2f;
        destination_position.y = 5f;
        destination_position.z = ( (float)destination.Item2 - 7f ) * 2f;

        obj.transform.position = destination_position;

        Debug.Log(destination + " : " + board[ (int)destination.Item1, (int)destination.Item2 ]);
        UpdateBoard( destination );
        DestroyTemporaries();

        PlaySE(SEname);
        StartCoroutine( DownObject(obj, downDestination, destination) );
    }

    //駒移動アニメーション
    IEnumerator DownObject(GameObject obj, float downDestination, (int, int) destination )
    {
        yield return new WaitForSeconds(0.001f);
        var tmp = obj.transform.position;
        tmp.y -= 0.1f;
        obj.transform.position = tmp;
        if( tmp.y > downDestination )
        {
            StartCoroutine(DownObject(obj, downDestination, destination));
        }
        else
        {
            moving = false;
            shown = false;
            installables = new List< (int, int) >();
            
            SwapTurn();
        }
    }


    //使用SE
    [SerializeField]
    private AudioClip SetPieceSE;
    [SerializeField]
    private AudioClip GetPieceSE;
    [SerializeField]
    private AudioClip GotPieceSE;
    [SerializeField]
    private AudioClip GameEndSE;
    //SEを鳴らす
    AudioClip SE;
    void PlaySE(string SEname)
    {

        if( SEname == "SetPiece" )
        {
            audioSource.clip = SetPieceSE;
        }
        else if( SEname == "GetPiece" )
        {
            audioSource.clip = GetPieceSE;
        }
        else if( SEname == "GotPiece" )
        {
            audioSource.clip = GotPieceSE;
        }
        else if ( SEname == "GameEnd" )
        {
            audioSource.clip = GameEndSE;
        }

        audioSource.Play();
    }

    //設置可能位置 表示Objectを破壊
    void DestroyTemporaries()
    {
        for(int i = 0; i < temporaryObjects.Count; i++)
        {
            Destroy(temporaryObjects[i]);
        }
        for(int i = 0; i < temporaryObjects.Count; i++)
        {
            temporaryObjects.RemoveAt(i);
        }
    }

    //選択した位置を送信
    void SendAction( (int, int) ex_position, (int, int) new_position )
    {
        view.RPC( nameof(GetAction) , RpcTarget.Others , ex_position.Item1, ex_position.Item2, new_position.Item1, new_position.Item2 );
 
        MovePiece( ex_position, new_position );
    }

    //選択した位置を受信
    [PunRPC]
    void GetAction( int ex_position_x, int ex_position_y, int new_position_x, int new_position_y )
    {
        var modify_ex_pos = ( 14 - ex_position_x, 14 - ex_position_y );
        var modify_new_pos = ( 14 - new_position_x, 14 - new_position_y );

        MovePiece( modify_ex_pos, modify_new_pos );
    }

    //盤面を更新
    void UpdateBoard( (int, int) new_position )
    {
        //自分のターンだったら
        if( yourTurn )
        {
            //ある敵の駒に対して
            for( int k = 0; k < 10; k++ )
            {
                int i = enemy_pieces[k].Position().Item1;
                int j = enemy_pieces[k].Position().Item2;
                var I = (float)( i - 7 ) * 2f;
                var J = (float)( j - 7 ) * 2f;

                if(k==9) Debug.Log( (i,j) );

                //その駒がとられてなくて，見えていない場合に見えるかチェック
                if( enemy_pieces[k].Enable() == true && enemy_pieces[k].Visible() == false )
                {
                    //ある自分の駒から
                    for( int l = 0; l < pieces.Length; l++ )
                    {
                        if( !pieces[l].Enable() )
                        {
                            continue;
                        }
                        var X = (float)(pieces[l].Position().Item1 - 7) * 2f;
                        var Y = (float)(pieces[l].Position().Item2 - 7) * 2f;

                        Vector3 origin = new Vector3( X, 2f, Y );
                        Vector3 destination = new Vector3( I, 2f, J );
                        Vector3 ray = destination - origin;

                        RaycastHit hit;

                        //間に壁がなければ
                        if ( !Physics.Raycast(origin, ray, out hit,ray.magnitude ) )
                        {
                            enemy_pieces[k].SwapVisible();
                            break;
                        }
                    }
                }

                //その駒がとられてなくて，見えていたらまだ見えるかチェック
                else if( enemy_pieces[k].Enable() == true && enemy_pieces[k].Visible() == true )
                {
                    bool visible = false;

                    //ある自分の駒に対して
                    for( int l = 0; l < pieces.Length; l++ )
                    {
                        if( !pieces[l].Enable() )
                        {
                            continue;
                        }
                        var S = (float)(pieces[l].Position().Item1 - 7) * 2f;
                        var T = (float)(pieces[l].Position().Item2 - 7) * 2f;

                        Vector3 origin = new Vector3( S, 2f, T );
                        Vector3 destination = new Vector3( I, 2f, J );
                        Vector3 ray = destination - origin;

                        RaycastHit hit;

                        //間に壁がなければ
                        if ( !Physics.Raycast(origin, ray, out hit, ray.magnitude ) )
                        {
                            visible = true;
                        }
                    }

                    //すべての間に壁があったら
                    if( visible == false )
                    {
                        enemy_pieces[k].SwapVisible();
                    }
                }
            }
        }

        //相手のターンだったら
        else
        {
            //ある敵の駒に対して
            for( int k = 0; k < enemy_pieces.Length; k++ )
            {
                int i = enemy_pieces[k].Position().Item1;
                int j = enemy_pieces[k].Position().Item2;
                var I = (float)( i - 7 ) * 2f;
                var J = (float)( j - 7 ) * 2f;

                if(k==9)
                {
                    Debug.Log( (i,j) + " : " + board[i,j]);
                    Debug.Log( "visible? : " + enemy_pieces[k].Visible() + " , " + "enable? : " + enemy_pieces[k].Enable() );
                }

                //その駒がとられてなくて，見えていない場合に見えるかチェック
                if( enemy_pieces[k].Enable() == true && enemy_pieces[k].Visible() == false )
                {
                    //ある自分の駒から
                    for( int l = 0; l < pieces.Length; l++ )
                    {
                        if( !pieces[l].Enable() )
                        {
                            continue;
                        }
                        var X = (float)(pieces[l].Position().Item1 - 7) * 2f;
                        var Y = (float)(pieces[l].Position().Item2 - 7) * 2f;

                        Vector3 origin = new Vector3( X, 2f, Y );
                        Vector3 destination = new Vector3( I, 2f, J );
                        Vector3 ray = destination - origin;

                        RaycastHit hit;

                        //間に壁がなければ
                        if ( !Physics.Raycast(origin, ray, out hit, ray.magnitude ) )
                        {
                            enemy_pieces[k].SwapVisible();
                            break;
                        }
                    }
                }

                //その駒がとられてなくて，見えていたらまだ見えるかチェック
                else if( enemy_pieces[k].Enable() == true && enemy_pieces[k].Visible() == true )
                {
                    bool visible = false;

                    //ある自分の駒に対して
                    for( int l = 0; l < pieces.Length; l++ )
                    {
                        if( !pieces[l].Enable() )
                        {
                            continue;
                        }
                        var S = (float)(pieces[l].Position().Item1 - 7) * 2f;
                        var T = (float)(pieces[l].Position().Item2 - 7) * 2f;

                        Vector3 origin = new Vector3( S, 2f, T );
                        Vector3 destination = new Vector3( I, 2f, J );
                        Vector3 ray = destination - origin;

                        RaycastHit hit;

                        //間に壁がなければ
                        if ( !Physics.Raycast(origin, ray, out hit, ray.magnitude ) )
                        {
                            visible = true;
                        }
                    }

                    //すべての間に壁があったら
                    if( visible == false )
                    {
                        enemy_pieces[k].SwapVisible();
                    }
                }
            }
        }
    }

    //敵ターンを待機
    void SwapTurn()
    {
        if(yourTurn)
        {
            yourTurn = false;
            Debug.Log("<color=green>ENEMY TURN</color>");
        }
        else
        {
            yourTurn = true;
            Debug.Log("<color=green>YOUR TURN</color>");
        }

        turn_overlay.SetActive(yourTurn);
    }

    //勝利時実行
    private void Win()
    {
        // 効果音
        PlaySE("GameEnd");
        
        yourTurn = false;
        waiting_overlay.SetActive(false);
        turn_overlay.SetActive(false);
        end_overlay.SetActive(true);
    }

    //相手が投了時実行
    [PunRPC]
    private void GiveUpWin( int viewID )
    {
        // 効果音
        PlaySE("GameEnd");

        yourTurn = false;
        waiting_overlay.SetActive(false);
        turn_overlay.SetActive(false);
        end_overlay.SetActive(true);
        GameObject txt = GameObject.Find("EndText").gameObject;
        txt.GetComponent<TextMeshProUGUI>().text = "YOU WIN !!\nEnemy GIve Up";
    }

    //敗北時実行
    private void Lose()
    {
        // 効果音
        GetComponent<AudioSource>().Play();

        yourTurn = false;
        waiting_overlay.SetActive(false);
        turn_overlay.SetActive(false);
        end_overlay.SetActive(true);
        GameObject txt = GameObject.Find("EndText").gameObject;
        txt.GetComponent<TextMeshProUGUI>().text = "YOU LOSE ...";
    }

    //投了
    public void GiveUp()
    {
        view.RPC( nameof(GiveUpWin), RpcTarget.Others, view.ViewID );
        turn_overlay.SetActive(false);
        Lose();
    }

    //部屋を作成
    private void CreateRoom()
    {
        RoomOptions roomOptions = new RoomOptions();
        roomOptions.MaxPlayers = 2;
        PhotonNetwork.CreateRoom(null, roomOptions, null);
    }

    //マッチングを開始
    private void Match()
    {
        Debug.Log("<color=red>Look for Room</color>");
        PhotonNetwork.JoinRandomRoom();
    }

    //部屋がなかったら
    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        Debug.Log("<color=red>Create Room (no server)</color>");
        CreateRoom();
    }

    //部屋に参加したら
    public override void OnJoinedRoom()
    {
        Debug.Log("<color=red>Joined Room, players : " + PhotonNetwork.CountOfPlayersInRooms + "</color>");
        // if( PhotonNetwork.CountOfPlayersInRooms == 1 )
        // {
        //     view.RPC( nameof(StartGame), RpcTarget.Others, view.ViewID );
        // }    
    }

    // マスターサーバーへの接続が成功した時に呼ばれるコールバック
    public override void OnConnectedToMaster() {
        Debug.Log("<color=red>Server Connected</color>");
        Match();
    }

    //プレイヤーが入ってきたら
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log("<color=blue>Other Player Joined Room, players : " + PhotonNetwork.CountOfPlayersInRooms + "</color>");

        //test
        view.RPC( nameof(TestRPC), RpcTarget.All, view.ViewID );

        PhotonNetwork.CurrentRoom.IsOpen = false;

        StartGame( view.ViewID );
    }

    //test
    [PunRPC]
    void TestRPC( int viewID )
    {
        Debug.Log("<color=blue>Connect Test : " + view.ViewID + "</color>");
    }

    //ゲーム開始時
    [PunRPC]
    private void StartGame( int viewID )
    {
        Debug.Log("<color=red>Game Started with your turn!</color>   " + yourTurn);
        waiting_overlay.SetActive(false);
        SwapTurn();
    }

    //タイトル画面へ
    public void BackToTitle()
    {
        SceneManager.LoadScene("TitleScene", LoadSceneMode.Single);
    }

    //リマッチ
    public void Rematch()
    {
        PhotonNetwork.LeaveRoom();
        SceneManager.LoadScene("PlayScene", LoadSceneMode.Single);
    }

    // 属性の設定
    [RuntimeInitializeOnLoadMethod]
    static void OnRuntimeMethodLoad() {
        Debug.Log("After Scene is loaded and game is running");
        // スクリーンサイズの指定
        Screen.SetResolution(1920, 1080, false);
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
            toGame();
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
        if(!RuleIsOn)
        {
            RuleIsOn = true;
            rules.SetActive(true);
            waiting_overlay.SetActive(false);
            page = 1;

            moving = false;
            shown = false;
            installables = new List< (int, int) >();
            DestroyTemporaries();
        }
    }
    
    public void toGame()
    {
        RuleIsOn = false;
        rules.SetActive(false);
        if( !yourTurn )
        {
            waiting_overlay.SetActive(true);
        }
    }
//github:tamaki12345
}