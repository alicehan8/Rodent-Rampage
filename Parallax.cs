using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// controlls the parallax for all background layers 
/// </summary>
public class Parallax : MonoBehaviour
{
    [SerializeField] float depth; //assigned depending on the layer of the background 

    private SpriteRenderer sr;
    bool isGenerated;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>(); //sprite renderer of gameobject
    }

    // Update is called once per frame
    void Update()
    {
        //While player is alive, the background should be moving 
        if (GameManager.isAlive)
        {
            //calculates the velocity of each layer of the background to create
            //perception of depth
            float realVel = GameManager.baseSpeed / depth;
            Vector2 pos = transform.position;

            //changes the position of the background using the velocity and time passed
            pos.x -= realVel * Time.deltaTime;

            //Checks when to instantiate the background so it is continuous 
            if(pos.x <= -10 && !isGenerated)
                //This is the hardcoded value of the edge of the background almost
                //being at the end of the camera
            {
                GameObject bg = Instantiate(gameObject, transform.parent);
                bg.transform.position = new Vector2(pos.x + sr.bounds.size.x, pos.y);
                isGenerated = true;
            }

            //Checks when to destroy gameobject once offscreen
            if(pos.x < -45)
                //This is the hardcoded value of the background being past the cameraview
            {
                Destroy(gameObject);
            }

            //updates the position of the background 
            transform.position = pos;
        }
    }
}
