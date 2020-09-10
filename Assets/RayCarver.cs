using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RayCarver : MonoBehaviour
{
    LineRenderer line;
    void Start(){
        line = GetComponent<LineRenderer>();
    }
    void Update()
    {
        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        if(Input.GetMouseButton(0)||Input.GetMouseButton(1)){
            RaycastHit2D hit = Physics2D.Raycast(mousePos,Vector2.down);
            if(hit){
                line.enabled = true;
                line.SetPositions(new Vector3[]{mousePos,hit.point});
                ClumpGenerator clump = hit.collider.GetComponent<ClumpGenerator>();
                if(clump!=null){
                    if(Input.GetMouseButton(0)){
                        clump.Circle(hit.point-(hit.normal*1.5f),4f,0.125f);
                    }else{
                        clump.Circle(hit.point,-4f,0.125f);
                    }
                }
            }
        }
        else
        {
            line.enabled = false;
        }
    }
}
