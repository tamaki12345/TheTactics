using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
//using System.Numerics;
using Unity.VisualScripting;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class GameControl : MonoBehaviour
{
    private Piece_Class[] pieces = new Piece_Class[9];
    private int[,] board = new int[15, 15];
    private int[,] walls = new int[28, 2] {
        {3,3}, {3,4}, {3,5}, {3,6}, {3,7}, {3,8}, {3,9}, {3,10}, {3,11},  {2,7},  {4,9}, {5,9}, {6,9}, {7,9},
        {11,3},{11,4},{11,5},{11,6},{11,7},{11,8},{11,9},{11,10},{11,11},  {7,5}, {8, 5}, {9, 5}, {10,5}, {12, 7}
    };
    private int[,] piece_point = new int[9, 3] {
        {4, 0, 5}, {7, 0, 6}, {10, 0, 8}, {3, 1, 1}, {5, 1, 7}, {9, 1, 8}, {11, 1, 2}, {6, 2, 3}, {8, 2, 4}
    };

    private int board_size = 15;
    // private List< List<int> > board = new List< List<int> >();

    private (int, int) clickPoint = (0, 0);
    public Camera mainCamera;
    private int selected;
    private bool shown = false;
    private (int, int) shownPoint = (0, 0);
    private List< (int, int) > installables = new List< (int, int) >();
    private (int, int) tmp = (0, 0);

    void Start()
    {
        Application.targetFrameRate = 60; 
        InitializeBoard();
    }

    void Update()
    {
        if( GetClickPoint() )
        {
            if( installables.Contains( ( clickPoint.Item1 , clickPoint.Item2 ) ) )
            {
                Debug.Log( ( (int)clickPoint.Item1, (int)clickPoint.Item2 ) );
                Debug.Log("<color=red>installables clicked</color>");
                board[ (int)clickPoint.Item1, (int)clickPoint.Item2 ] = board[ (int)shownPoint.Item1, (int)shownPoint.Item2 ];
                board[ (int)shownPoint.Item1, (int)shownPoint.Item2 ] = 0;
            }
            else if( SelectPiece() > 0 && !shown )
            {
                Debug.Log( ( (int)clickPoint.Item1, (int)clickPoint.Item2 ) );
                shown = true;
                selected = SelectPiece();
                shownPoint = clickPoint;

                Piece_Class selected_piece = pieces[ selected - 1 ];

                int id = selected_piece.Id();

                installables = GetInstallableTiles(id);

                for( int i = 0; i < installables.Count; i++) Debug.Log( "<color=green>new installables" + ( installables[i].Item1, installables[i].Item2 ) + "</color>" );

                ShowInstallable(installables);
            }
        }
    }

    //boardを初期化
    void InitializeBoard()
    {  
        //駒の初期位置
        for( int i = 0; i < 9; i++ )
        {
            int x = piece_point[i,0];
            int y = piece_point[i,1];
            int type = piece_point[i,2];

            board[x,y] = type;
        }

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
        if( board[ (int)clickPoint.Item1 ,  (int)clickPoint.Item2 ] > 0 )
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
                if( board[ x + 1 , y ] != -1 )
                {
                    ret.Add( ( x + 1 , y ) );
                }
            }
            if( x + 1 < 15 && y + 1 < 15 )
            {
                if( board[ x + 1 , y + 1 ] != -1 )
                {
                    ret.Add( ( x + 1 , y + 1 ) );
                }
            }
            if( y + 1 < 15)
            {
                if( board[ x , y + 1 ] != -1 )
                {
                    ret.Add( ( x , y + 1 ) );
                }
            }
            if( x - 1 >= 0 && y + 1 < 15 )
            {
                if( board[ x - 1 , y + 1 ] != -1 )
                {
                    ret.Add( ( x - 1 , y + 1 ) );
                }
            }
            if( x - 1 >= 0 )
            {
                if( board[ x - 1 , y ] != -1 )
                {
                    ret.Add( ( x - 1 , y ) );
                }
            }
            if( x - 1 >= 0 && y - 1 >= 0 )
            {
                if( board[ x - 1 , y - 1 ] != -1 )
                {
                    ret.Add( ( x - 1 , y - 1 ) );
                }
            }
            if( y - 1 >= 0 )
            {
                if( board[ x , y - 1 ] != -1 )
                {
                    ret.Add( ( x , y - 1 ) );
                }
            }
            if( x + 1 < 15 && y - 1 >= 0 )
            {
                if( board[ x + 1 , y - 1 ] != -1 )
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
                if( y + 1 < 15 )
                {
                    ret.Add( ( x + 2 , y + 1 ) );
                }
                if( y - 1 >= 0 )
                {
                    ret.Add( ( x + 2 , y - 1 ) );
                }
            }
            if( y + 2 < 15 )
            {
                if( x + 1 < 15 )
                {
                    ret.Add( ( x + 1 , y + 2 ) );
                }
                if( x - 1 >= 0 )
                {
                    ret.Add( ( x - 1 , y + 2 ) );
                }
            }
            if( x - 2 >= 0 )
            {
                if( y + 1 < 15 )
                {
                    ret.Add( ( x - 2 , y + 1 ) );
                }
                if( y - 1 >= 0 )
                {
                    ret.Add( ( x - 2 , y - 1 ) );
                }
            }
            if( y - 2 >= 0 )
            {
                if( x + 1 < 15 )
                {
                    ret.Add( ( x + 1 , y - 2 ) );
                }
                if( x - 1 >= 0 )
                {
                    ret.Add( ( x - 1 , y - 2 ) );
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
                    if( board[ x + i, y ] > -1)
                    {
                        ret.Add( ( x + i, y ) );
                        i++;
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
                    if( board[ x + i, y + j ] > -1)
                    {
                        ret.Add( ( x + i, y + j ) );
                        i++;
                        j++;
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
                    if( board[ x , y + j ] > -1)
                    {
                        ret.Add( ( x , y + j ) );
                        j++;
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
                    if( board[ x - i, y + j ] > -1)
                    {
                        ret.Add( ( x - i, y + j ) );
                        i++;
                        j++;
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
                    if( board[ x - i, y ] > -1)
                    {
                        ret.Add( ( x - i, y ) );
                        i++;
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
                    if( board[ x - i, y - j ] > -1)
                    {
                        ret.Add( ( x - i, y - j ) );
                        i++;
                        j++;
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
                    if( board[ x , y - j ] > -1)
                    {
                        ret.Add( ( x , y - j ) );
                        j++;
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
                    if( board[ x + i, y - j ] > -1)
                    {
                        ret.Add( ( x + i, y - j ) );
                        i++;
                        j++;
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

    void ShowInstallable(List< (int, int) > points)
    {
        GameObject installable = (GameObject)Resources.Load("installable");

        for(int i = 0; i < points.Count; i++)
        {
            GameObject instance = (GameObject)Instantiate(installable);
            Vector3 position = instance.transform.position;
            position.x = ( (float)points[i].Item1 - 7f ) * 2f;
            position.z = ( (float)points[i].Item2 - 7f ) * 2f;
            instance.transform.position = position;
        }
    }
}