using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Puts all the functions related to powerups in one place to make things more organized
/// Most are static and can just be called in a static context
/// </summary>
public class PowerUpFunctions :MonoBehaviour
{
    private static GameManager gameManager;
    [Header("Variables involving speed boost powerup")]
    [Range(0f, 100f)]
    public static float SpdBoost = 45f;
    [Range(0f, 5f)]
    public static float SpdBoostDuration = 3f;
    private static float boostStartTime = 0;
    [Header("Variables involving other powerups")]
    private static float magnetBound = 10f; //How far the maganet can reach
    //How much the shield bounces object in each direction
    static float bounceX = 10; 
    static float bounceY = 5;
    //List of powerups that deal with collisions (Power ups that require isInvic to be true)
    private static string[] CollisonPowerUps = new string[] { "Shield", "Beserk" }; 
    private bool gunActive; // Gun lock variable
    

    private void Start()
    {
        //Set up gameManager ref
        gameManager = GetComponent<GameManager>();
        gameManager.enabled = true;
    }
    /// <summary>
    /// Sets isInvinc to true so player won't die when hitting obs (see Player)
    /// </summary>
    public static void CollisonPowerUp()
    {
        GameManager.isInvinc = true;
    }
    /// <summary>
    /// Sets isInvinc to false so player goes back to fainting when hitting obs (see Player)
    /// </summary>
    public static void CollisonPowerDown()
    {
        //Before isInvinc can be set to false check if there are any power ups on that'd keep it at true
        //(Don't want to overwrite invinc state from a later power up thats enabled at same time)
        Dictionary<string, bool> temp = GameManager.powerUps;
        bool tempBool = false; //Bool to flip if any invic power ups are on
        //Looks to see if any invic power ups are on and if so switches tempBool to true
        foreach (string key in CollisonPowerUps)
        {
            if (temp[key])
            {
                tempBool = true;
                break;
            }
        }
        //Sets isInvinc based on results of the for loop
        GameManager.isInvinc = tempBool;
    }
    /// <summary>
    /// Turns on visulation for the shield
    /// </summary>
    public static void EnableShield()
    {
        CollisonPowerUp();
        GameObject.Find("Shield").GetComponent<SpriteRenderer>().enabled = true;
    }
    /// <summary>
    /// Turns of visulazation for shield
    /// </summary>
    public static void DisableShield()
    {
        CollisonPowerDown();
        GameObject.Find("Shield").GetComponent<SpriteRenderer>().enabled = false;
    }
    
    ///<summary>
    /// This method handles the behavior for the berserk powerup. When the player collides with an object,
    /// the object should be destroyed and a cluster of acorns should spawn in its place. A particle effect
    /// should also play to give the player more feedback that the object has been blasted/destroyed 
    /// and indicates to the player that the berserk powerup gives them strength to destroy.
    ///</summary>
    public static void Beserk(GameObject AcornCluster, GameObject ObjectToDestroy, GameObject PartSys)
    {
        AudioManager.instance.PlayOneShot(FMODEvents.instance.BeserkCollision, ObjectToDestroy.transform.position);

        // instantiate a cluster of acorns at the position and rotation of the obstacle being destroyed
        Instantiate(AcornCluster, ObjectToDestroy.transform.position, ObjectToDestroy.transform.rotation);
        // instantiate and play the particle effect
        GameObject instantiatedPartSys = 
            Instantiate(PartSys, GameManager.player.transform.position, GameManager.player.transform.rotation);
        instantiatedPartSys.GetComponent<ParticleSystem>().Play();
        // destroy the obstacle that has been collided with by the player
        Destroy(ObjectToDestroy);
    }
    /// <summary>
    /// Changes the size of the player based on param size, used for beserk
    /// </summary>
    /// <param name="size">Size to switch the player too</param>
    public static void ChangePlayerSize(float size)
    {
        //Calls coroutine to smoothly scale the players size
        GameManager.player.GetComponent<Player>().StartCoroutine("changeSize", size);
    }
    
    /// <summary>
    /// Increases acorn sore by param scoreIncr
    /// </summary>
    /// <param name="scoreIncr">How much to increase the score by</param>
    public static void TwoTimes(int scoreIncr)
    {
        GameManager.CurrScore += scoreIncr;
    }
    /// <summary>
    /// Switches acorn to respective state, used for 2x animation
    /// </summary>
    /// <param name="acornView">AcornView script of acorn to change</param>
    /// <param name="anState">State to change to</param>
    public static void SwitchAcorn2X(AcornView acornView, int anState)
    {
        acornView.GetComponent<AnimationStateController>().SwitchAnState(anState);
    }

    /// <summary>
    /// Finds all acorns in scene and changes them to the proper animation
    /// </summary>
    /// <param name="anState">Animation state to change to</param>
    public static void ChangeAllAcorns(int anState) {
        var acornViews = GameObject.FindObjectsOfType<AcornView>();
        foreach(AcornView eachAcorn in acornViews)
        {
            SwitchAcorn2X(eachAcorn, anState);
        }
    }
    /// <summary>
    /// Moves acorn to player (mag behavior)
    /// </summary>
    /// <param name="transform">What to move</param>
    /// <param name="moveSpd">How fast to move it</param>
    public static bool MagnetMovement(Transform transform, float moveSpd)
    {
        // how fast the acorn should move
        float step = moveSpd * Time.deltaTime;
        //How far away it is
        float distance = transform.position.x - GameManager.player.transform.position.x;
        //If acorn is in range move it towards the player (one step at a time)
        if (Mathf.Abs(distance) <= magnetBound) 
        {
            transform.position = Vector3.MoveTowards(transform.position, GameManager.player.transform.position, step);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Bounces obs that hit the player when shield is enabled
    /// </summary>
    /// <param name="gameObject">What to bounce</param>
    /// <param name="ObRigidBody">Rigidbody of what is being bounced</param>
    /// <param name="animations">Animation states of what is being bounced</param>
    public static void ShieldBounce(GameObject gameObject, Rigidbody2D ObRigidBody, string animations) //SHould probably destroy soonner than later?
    {
        AudioManager.instance.PlayOneShot(FMODEvents.instance.ShieldRepel, gameObject.transform.position);

        Animator temp = gameObject.GetComponent<Animator>();
        //If it has an animation to play when hit by shield play it
        if (animations.Contains("2"))
        {
            temp.SetInteger("State", 2);
        }
        //Turn off constrainst for bouncing movement
        ObRigidBody.constraints = RigidbodyConstraints2D.None;
        //Speed modifier, uses -1 to get proper directional movement
        int modifier = (int)(-1 * (ObRigidBody.velocity.x / Mathf.Abs(ObRigidBody.velocity.x)))+4;
        //Add bounce force
        ObRigidBody.AddForce(
                        new Vector2((Mathf.Abs(ObRigidBody.velocity.x) + bounceX) * modifier, bounceY), ForceMode2D.Impulse);
        //Makes sure ob gets destroyed in case gets stuck somewhere
        ObRigidBody.gameObject.GetComponent<Obstacles>().StartCoroutine("DestroyAfterTime",2);
        
    }
    /// <summary>
    /// Currently broken/not in use but will be used to start the speedboost affect
    /// </summary>
    public static void activateSpdBoost()
    {
        if (!GameManager.isEnteredBoost)
        {
            // record when the boost started for
            // when it comes time to deactivate the boost
            boostStartTime = Time.time;
            // use a boolean to keep track of the boost being activated
            // to enter the deactivation phase
            GameManager.isEnteredBoost = true;
            // increase the spd by a spdIncr input 

            // w/ lerp
            //LerpSpd(+1);

            // w/o lerp
            GameManager.baseSpeed += SpdBoost;
            //print("Speed after being increased: " + GameManager.baseSpeed);
            // adjust the speed of all obstacles
            updateAllObstaclesSpd();
            // make squirrel temporarily invincible by moving it to the invincible layer so that the
            // speed boost is more enjoyable for the player
            GameManager.SetNotColliding(true);
            // make it increase for a 1/4th of the time
            // then make it slow down/deactivate for a 1/4th of the time (something to test)
            // for next time, do a lerp where you only take one of the values from the vector
            // to slowly increase or decrease spd
        }
    }
    /// <summary>
    /// Currently broken/not in use but will be used to end the speedboost affect
    /// </summary>
    public static void deactivateSpdBoost()
    {
        if (GameManager.isEnteredBoost)
        {
            // reset the baseSpd by subtracting the SpdBoost
            
            // w/ lerp
            //LerpSpd(-1);

            // w/o lerp  Add a check that it doesn't go below zero
            GameManager.baseSpeed -= SpdBoost;
            //print("The speed after being reset: " + GameManager.baseSpeed);
            // reset the spd of all obstacles
            updateAllObstaclesSpd();
            // start coroutine like in spdBoost method
            //gameManager.StartCoroutine("PowerUpCoolDown", SpdBoostDuration);
            // reset the entered boolean
            GameManager.isEnteredBoost = false;
        }
    }

    ///<summary>
    /// This method iterates through all the obstacles in the scene
    /// and updates their speed accordingly for changes in baseSpd
    ///</summary>
    private static void updateAllObstaclesSpd() 
    {
        var crossing = GameObject.FindObjectsOfType<CrossScreen>();
        foreach(CrossScreen eachObs in crossing)
        {
            eachObs.updateSpeed();
        }
    }

    /// <summary>
    /// Creates an acorn where the object used to be
    /// </summary>
    public static void SpawnAcorn(GameObject gameObject /*GameManager gameManager*/)
    {
        gameObject.tag = "Acorn";
        if (!gameManager)
        {
            gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();
        }
        
        SpriteRenderer spriteRenderer = gameObject.GetComponent<SpriteRenderer>();
        //Since obs pivots are at the bottom finds offset to spawn acorn at its center
        Vector3 offset = new Vector3(
            spriteRenderer.bounds.extents.x * (0.5f - spriteRenderer.sprite.pivot.x / spriteRenderer.sprite.rect.width),
            spriteRenderer.bounds.extents.y * (0.5f - spriteRenderer.sprite.pivot.y / spriteRenderer.sprite.rect.height),
            0f
        );
        //Create acorn where obs used to be
        GameObject acorn = Instantiate(
            gameManager.getAcornPreFab(),
            gameObject.transform.position + offset,
            Quaternion.identity);
        acorn.GetComponent<Acorn>().Invoke("ScoreAcorn", .7f); //Calls score on delay so player sees acorn
        acorn.transform.localScale = new Vector3(1, 1, 1); //Makes it big

    }
    public static void SpawnObAcornShape(GameObject gameObject)
    {
        Instantiate(
            gameObject.GetComponent<Obstacles>().AcornCluster,
            gameObject.transform.position,
            Quaternion.identity);
    }

    /// <summary>
    /// Starts coroutine that shoots the amo during gun powerup
    /// </summary>
    public void AcornGun()
    {
        Player player = GameManager.player.GetComponent<Player>();
        if (!player.gun.enabled) //Only allow one instance of gun to exist at a time
        {
            StartCoroutine("Shoot");
        }
    }
    /// <summary>
    /// Shoots amo
    /// </summary>
    /// <returns></returns>
    public IEnumerator Shoot()
    {
        gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();
        Player player = GameManager.player.GetComponent<Player>();

        // Activates gun on player
        player.gun.enabled = true;
        gunActive = true;

        //While gun is active shoot amo
        while (GameManager.powerUps["Gun"] && GameManager.isAlive)
        {
            Instantiate(gameManager.amo, player.ammoSpawnTransform.position, Quaternion.identity);

            //Plays GunFire FMOD Event
            GameManager.audioManager.PlayOneShot(FMODEvents.instance.GunFire, this.transform.position);

            //Time between shots, might need to scale for speed increases?
            yield return new WaitForSeconds(.7f);
        }
        
        // Deactivates gun on player
        player.gun.enabled = false;
        gunActive = false;
    }
    /// <summary>
    /// Not working, mean to sync the gun with the run cycle
    /// </summary>
    /// <param name="target">What to sync to (the squierrl</param>
    /// <param name="toSync">What needs to be sunk (the gun)</param>
    public static void SyncAns(Animator target, Animator toSync)
    {
        //Try turning off animator and then turning it on with motiontime?
        //print(toSync.gameObject);
        float time = target.GetCurrentAnimatorStateInfo(0).normalizedTime;
        time = time % target.GetCurrentAnimatorStateInfo(0).length;
        //print(time);
        //toSync.playbackTime = time;
        //toSync.SetFloat("Time", time);
        //toSync.Update(0);
        toSync.Update(time);
        //print(target.GetCurrentAnimatorStateInfo(0).normalizedTime + " : " + toSync.GetCurrentAnimatorStateInfo(0).normalizedTime);
        //toSync["GunEquipRun"].normalizedTime = target["sq_v2_run"].normalizedTime;
        //print("sunk");
    }

    /// <summary>
    /// Turns off all powerups and their effects
    /// Used to clear powerups before entering new scene
    /// </summary>
    public static void DeactivateAllPowerUps()
    {
        List<string> enabled = GameManager.EnabledPowerUps();
        //Turn off all enabled power ups before switching to new scene
        //Might have an issue with isSpeedBoostEnabled
        foreach (string item in enabled)
        {
            if (GameManager.powerUps[item])
            {
                GameManager.powerUps[item] = false;
            }
        }
        
        CollisonPowerDown(); //Makes sure isInvc is set back to false
        deactivateSpdBoost();
    }

    //Can be used to lerp speed for speedboost currently not in place, might not be needed
    public static void LerpSpd(float sign) 
    {
        float timeElasped = 0;
        float orgSpd = GameManager.baseSpeed;
        float newSpd = GameManager.baseSpeed += (SpdBoost * sign);
        for(float t = timeElasped; t < SpdBoostDuration; t += Time.deltaTime) 
        {
            GameManager.baseSpeed = Mathf.Lerp(orgSpd, newSpd, t / SpdBoostDuration);
        }
        // clamp baseSpd to newSpd so that baseSpd fully reaches the new spd w/o cutoff
        // not sure if we need this though
        //GameManager.baseSpeed = newSpd;
    }
    
}
