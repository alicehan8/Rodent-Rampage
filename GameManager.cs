using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FMOD.Studio;
using FMODUnity;
using UnityEngine.SceneManagement;

/// <summary>
/// GameManager handles managing the current game state
/// such as if the game is over, determining the score,
/// timing of powerups, etc.
/// An instance of GameManager is CARRIED OVER through scenes
/// </summary>
public class GameManager : MonoBehaviour
{
    //Varibles are static so any script can refence them without having to store the GameManager in them
    // CurrScore relates to the amount of acorns the player has collected
    public static int CurrScore;
    public static float baseSpeed = 7; //The basespeed all moving objects should have
    public static bool isAlive = false; //Keeps track of if squirel is going, false when squirel faints
    public static bool isInvinc = false; //Is the player invincable to obs collisons
    public static bool isGameEnded = false; //Has the game ended
    
    public static bool isEnteredBoost = false; //Is the player in a speed boost 
    public static float CurrDist; //The current yards travled by player
    //List of the different scenes the player can travel to
    public static List<string> SceneOptions = new List<string> { "Park", "Area1" }; 
    //Dictonary key is type of powerup and value is status, used to see what state powerups are in
    public static Dictionary<string, bool> powerUps = new Dictionary<string, bool>();

    [SerializeField] public static AudioManager audioManager;

    //This is FMOD Event for Ambience
    private EventInstance ambience;

    //FMOD Event for Music
    private EventInstance music;

    //FMOD Event for pauseSnapshot
    private EventInstance pauseSnapshot;

    //FMOD Event for MagnetLoop
    private EventInstance magnetLoop;

    //Public GameObjects that are passed in via inspector
    public GameObject amo; //Prefab asset Amo gun shoots
    public GameObject AcornPrefab; //Prefab asset for Acorn 
    
    //Player pref strings for saving score and total acorns 
    public static string PrefsScoreName = "prefsScore";
    public static string PrefsBankName = "prefsAcornBank"; //Currently isn't working (new acorns aren't being added?)

    public static GameObject player;  //Public refence of the player for any script to access (limits use of Find)

    //public ParticleSystem SpdBoostPartSys;
    public static ParticleSystem SpdBoostPartSys; //Particle effect for speed boost (currently not used)

    public static bool MagnetSoundOn = false;

    //Do not delete, keep incase bring speedboost back and need to refence
   /* [Header("Variables involving speed boost powerup")]
    [Range(0f, 100f)]
    public float SpdBoost = 1f;
    [Range(0f, 5f)]
    public float SpdBoostDuration = 3f;*/

    //speeding up variables
    //TODO: fine tune these variables 
    private float speedUpTime = 20; //Time at which BaseSpeed is increased
    private float waitSpeedUp = 20; //Time between baseSpeed increases
    private float speedUpAmt = .5f; //How much game should speed up
    private float maxSpeed = 40; //Max speed of the game

    [SerializeField]
    private bool isReset = false; //True if should reset playerPrefs

    private float gameTime = 0; //Keeps track of time player started playing)
    private bool isAddedToBank = false; //Have acorns been added to bank
    private bool isScoreUpdate = false; //high score is updated 

    private Scene scene; //Active scene

    //Keeps track of gameManager instance?
    public static GameManager instance { get; private set; }

    //Contains the names of all active powerups, contains multiple instances of the name if powerups are stacked
    //This allows getting multiple powerups to extend active duration (james)
    public List<string> activePowers = new List<string>(); 

    /// <summary>
    /// This method is called when the object's script is first intialized;
    /// it handles setting variables to prepare for the start of the
    /// game or the loading of the current scene.
    /// </summary>
    private void Awake()
    {
        //Should help with framerate issues
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;
        //If there is no instance of the GameMangaer make this
        //Make sure it doesn't destroy when loading new scenes
        //if there is an instance already set up (was carried over from previous scene) destroy this one
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else 
        {
            Destroy(gameObject);
        }
        //Puts all the following varibles in their default state
        isAlive = true; 
        isGameEnded = false;
        //Set up power up dictionary
        powerUps["Beserk"] = false;
        powerUps["Shield"] = false;
        powerUps["Magnet"] = false;
        powerUps["2xTimes"] = false;
        powerUps["SpdBoost"] = false;
        powerUps["Gun"] = false;

        // store Playerpref for highscore
        PlayerPrefs.GetFloat(PrefsScoreName, CurrDist);
        // store Playerpref for acorns in bank
        PlayerPrefs.GetInt(PrefsBankName, 0);
        // reset the CurrScore and CurrDist for the run
        CurrScore = 0;
        CurrDist = 0;

        // reset gameTime to the beginning time, done so it doesn't use time when not running
        gameTime = Time.time;

        // if the player is null, find it to avoid a null error
        //Note in subsequent scenes the new player for that scene will assign itself to player var here in Player script
        if(SceneManager.GetActiveScene().buildIndex !=0 && player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player");
        }

        audioManager = FindObjectOfType<AudioManager>();
        //Reset basespeed to 7 (start speed)
        baseSpeed = 7;

        //Resets these to default state
        isAddedToBank = false;
        isEnteredBoost = false;
        isScoreUpdate = false;

        scene = SceneManager.GetActiveScene(); //Assign scene to current scene
        
        // for testing purposes,
        // using a bool in inspector to click on and off for
        // reseting the player prefs
        if (isReset)
        {
            PlayerPrefs.DeleteAll();
            // reset the volumes?
        }
    }

    private void Start()
    {
        // Note: Set up music in Start to ensure the FMODEvents singleton is instanced
        music = AudioManager.instance.CreateInstance(FMODEvents.instance.MusicSwitcher);
        ambience = AudioManager.instance.CreateInstance(FMODEvents.instance.Ambience);
        pauseSnapshot = AudioManager.instance.CreateInstance(FMODEvents.instance.PauseSnapshot);
        magnetLoop = AudioManager.instance.CreateInstance(FMODEvents.instance.MagnetLoop);

        music.start();
        pauseSnapshot.start();
        magnetLoop.start();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SetGameMusic(scene.name);

        ControlMagnet(false);

        if (SceneManager.GetActiveScene().name.Equals("MainMenu"))
        {
            isAlive = false;
            //player.GetComponent<Player>().TurnOffLeafBlower();
        }
        else
        {
            isAlive = true;
            if (player == null)
            {
                player = GameObject.FindGameObjectWithTag("Player");
            }
            player.GetComponent<Player>().BeserkFootsteps(false);
        }
    }


    /// <summary>
    /// This method is called every frame (different from update in the consistency of calling frames, since it is
    // physics based). 
    // It handles updating the current distance the player has traveled only if the game is not over/the player is 
    // alive. When the player dies, the best distance is updated only if the distance traveled was greater.
    /// </summary>
    private void FixedUpdate()
    {
        //IF YOU ARE RELOADING SCENES DON'T USE TIME.time 
        //This is time since the GAME not the SCENE restarted
        if (isAlive) 
        {
            // calculating the distance travled by player since last call
            CurrDist += (baseSpeed * Time.deltaTime);

            // reset bool for adding to bank
            isAddedToBank = false;

            //Comparing the time to when it should speed up
            if (Time.time - gameTime >= speedUpTime && baseSpeed < maxSpeed)
            {
                //adjusting the speed based on the time of the game 
                baseSpeed += speedUpAmt;
                speedUpTime += waitSpeedUp;

                //changing the speed of all objects that are already crossing the screen
                var crossing = FindObjectsOfType<CrossScreen>();
                for(int i = 0; i < crossing.Length; i++)
                {
                    crossing[i].updateSpeed();
                }
            }
        }
        else
        {
            //If player has finished run update their info
            updateBestDist();
            updateAcornBank();

        }
    }

    /// <summary>
    /// This method determines if the current best distance score needs to be updated or not. The best distance score
    /// should only be updated if the current distance is greater than the current best distance 
    /// </summary>
    public void updateBestDist() 
    {
        if(CurrDist >= PlayerPrefs.GetFloat(PrefsScoreName, CurrDist) && !isScoreUpdate)
        {
            // if the current score is higher than the current high score,
            // update the high score to be equal to the current score
            // and set the high score in player prefs
            PlayerPrefs.SetFloat(PrefsScoreName, CurrDist);
            //print("Best dist was updated");
            DTDEvents.HighScore((long)CurrDist);
            isScoreUpdate = true;
        }
    }

    /// <summary>
    /// (Currently seems to not update)
    /// This method updates how many total acorns the player has (for using in the shop) by adding the
    /// CurrScore to the acorn bank. The method checks if the CurrScore is greater than the amount in the bank
    /// and if the amount has been added already or not so that the CurrScore is not added over and over
    /// which would exponentially increase the amount in the bank.
    /// </summary>
    public void updateAcornBank() 
    {
        int AmtInBank = PlayerPrefs.GetInt(PrefsBankName, 0);
        //print("The amount in bank before adding to the bank " + AmtInBank);
        if(!isAddedToBank) 
        {
            DTDEvents.coinsCollected((long)CurrScore, (long)GameManager.CurrDist);
            PlayerPrefs.SetInt(PrefsBankName, CurrScore + AmtInBank);
            //print("The amount in bank after adding to the bank " + PlayerPrefs.GetInt(PrefsBankName, CurrScore));
            isAddedToBank = true;
        }

    }
   
    /// <summary>
    /// Coroutine that acts as timer for powerup lifespan
    /// Based on power up type waits until its saved time to set powerup to false
    /// </summary>
    /// <param type="name">type of powerup to time</param>
    /// <returns></returns>
    public IEnumerator PowerUpTimer(string name, float time)
    {
        Debug.Log(name + " for " + time + " seconds");
        activePowers.Add(name); //Add an instance of this powerup to the active list

        //Turn on the power up timer
        powerUps[name] = true; //Update dictionary to show the power up is on

        //if (name.Equals("Magnet"))
        //{  
        //    ControlMagnet(true);
        //}

        yield return new WaitForSeconds(time); //Waits for its saved time before ending it
        activePowers.Remove(name); //Remove an (ONE) instance of this powerup from the active list

        //If no instances of this powerup are active, turn it off
        if (!activePowers.Contains(name))
        {
            //powerUps[name] = false; //Once wait is over set powerup back to false
            Debug.Log("Stopping powerup" + name);
            StartCoroutine(PowerUpCoolDown(1, name)); //Turns off powerup functions and temp makes player invinvble

            //StopMagnet();
        }
    }

    //Returns prefab for Acorn
    public GameObject getAcornPreFab()
    {
        return AcornPrefab;
    }

    /// <summary>
    /// This method makes the player temporarily invincible after a powerup wears off
    /// so that if the player crashes into an obstacle for example when the powerup is wearing off
    /// they do not immediately die. A similar effect is in Subway Surfers when a powerup is wearing off, it starts
    /// to flash to give the player visual feedback. 
    /// </summary>
    public IEnumerator PowerUpCoolDown(float time, string name)
    {
        for(int i = 0; i < 4; i++)
        {
            player.GetComponent<SpriteRenderer>().color = new Color(1, 1, 1, 0.5f);
            yield return new WaitForSeconds(.1f);
            player.GetComponent<SpriteRenderer>().color = new Color(1, 1, 1, 1);
            yield return new WaitForSeconds(.1f);
        }
        powerUps[name] = false; //Once wait is over set powerup back to false
        //Based on power up that is ending preform its ending actions
        switch (name)
        {
            case "Shield":
                PowerUpFunctions.CollisonPowerDown();
                PowerUpFunctions.DisableShield();
                break;
            case "Beserk":
                player.GetComponent<Player>().BeserkFootsteps(false);
                PowerUpFunctions.CollisonPowerDown();
                PowerUpFunctions.ChangePlayerSize(.5f);
                break;
            case "2xTimes":
                PowerUpFunctions.ChangeAllAcorns(0);
                break;
            case "SpdBoost":
                PowerUpFunctions.deactivateSpdBoost();
                break;
            case "Magnet":
                ControlMagnet(false);
                Player.GetMagnetParticle.Stop();
                break;
            default:
                break;
        }
        PowerUpFunctions.CollisonPowerDown(); //Makes sure isInvic is turned back off
        //Once a powerup wears off is invisible for a second
        //powerUps[name] = false; //Once wait is over set powerup back to false
        SetNotColliding(true); //Moves player to invinc layer
        yield return new WaitForSeconds(time);
        SetNotColliding(false); //Brings back to PLAYER layer
    }

    /// <summary>
    /// Returns a list of all active powerups
    /// </summary>
    /// <returns></returns>
    public static List<string> EnabledPowerUps()
    {
        List<string> temp = new List<string>();
        foreach (string key in powerUps.Keys)
        {
            if (powerUps[key])
            {
                temp.Add(key);
            }
        }
        return temp;
    }

    /// <summary>
    /// Finds all animators (whose gameobjects are active) and turns them off
    /// </summary>
    public static void turnOffAllAns()
    {
        Animator[] animators = FindObjectsByType<Animator>(FindObjectsSortMode.InstanceID);
        foreach (Animator animator in animators)
        {
            animator.enabled = false;
        }
    }

    /// <summary>
    /// Stops all movement of objects that use CrossScreen for movement
    /// Used when game ends to stop things from moving
    /// </summary>
    public static void StopAllMovement()
    {
        CrossScreen[] objs = FindObjectsOfType<CrossScreen>();
        foreach (var obj in objs)
        {
            obj.Stop();
        }
    }

    /// <summary>
    /// Function for debugging to print dictionaries 
    /// </summary>
    /// <param name="dict"></param>
    private void printDict(Dictionary<string,bool> dict)
    {
        string temp = "";
        foreach (string item in dict.Keys)
        {
            temp += " " + item + ":" + dict[item];
        }
        print(temp);
    }

    //Resets all important variables when restart is clicked 
    public void Reset()
    {
        isAlive = true;
        isGameEnded = false;
        //Set up power up dictionary
        powerUps["Beserk"] = false;
        powerUps["Shield"] = false;
        powerUps["Magnet"] = false;
        powerUps["2xTimes"] = false;
        powerUps["SpdBoost"] = false;
        powerUps["Gun"] = false;

        // store Playerpref for highscore
        PlayerPrefs.GetFloat(PrefsScoreName, CurrDist);
        // store Playerpref for acorns in bank
        PlayerPrefs.GetInt(PrefsBankName, 0);
        // reset the CurrScore and CurrDist
        CurrScore = 0;
        CurrDist = 0;

        // reset gameTime to the beginning time
        gameTime = Time.time;

        audioManager = FindObjectOfType<AudioManager>();

        baseSpeed = 7;
        speedUpTime = 20;

        isAddedToBank = false;
        isEnteredBoost = false;
        isScoreUpdate = false;

        SetPauseSnapshot(false);
        ControlMagnet(false);
    }

    public static void SetNotColliding(bool isNotColliding)
    {
        if (isNotColliding) 
        {
            player.layer = 6; 
        } //Moves player to invinc layer
        else if (!EnabledPowerUps().Contains("SpdBoost"))
        {
            player.layer = 3;
            print("isInvc" + isInvinc);
        } //Brings back to PLAYER layer
    }

    public void SetGameMusic(string scene)
    {
        
        //Music setup based on scene
        if (scene.Equals("MainScene"))
        {
            ambience.start();
            music.setParameterByName("MusicSelector", 1);
        }
        else if (scene.Equals("MainMenu"))
        {
            ambience.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
            music.setParameterByName("MusicSelector", 0);
        }
    }

    /// <summary>
	/// method for changing the parameter for the pause snapshot
	/// </summary>
	/// <param name="on">boolean for whether the game is paused and snapshot is on</param>
    public void SetPauseSnapshot(bool on)
    {
        if (on)
        {
            pauseSnapshot.setParameterByName("GamePause", 0);
        }
        else
        {
            pauseSnapshot.setParameterByName("GamePause", 1);
        }
    }

    public void ControlMagnet(bool on)
    {
        if (on)
        {
            magnetLoop.setParameterByName("MagnetMute", 0);
            MagnetSoundOn = true;
        }
        else
        {
            magnetLoop.setParameterByName("MagnetMute", 1);
            MagnetSoundOn = false;
        }
    }

    ///<summary>
    /// Changes the value of the isScoreUpdate to whatever boolean is passed in
    ///</summary>
    public void SetScoreUpdateBool(bool updateBool)
    {
        isScoreUpdate = updateBool;
    }

}

