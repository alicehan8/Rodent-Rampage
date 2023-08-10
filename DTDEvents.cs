using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DevToDev.Analytics;

/// <summary>
/// DevToDev Event functions 
/// </summary>
public class DTDEvents : MonoBehaviour
{
    public static int numRestart = 0; //keeps track of the number of restarts

    /// <summary>
    /// Logs whenever a player beats last highscore.
    /// </summary>
    /// <param name="score">Name of the player's highest score.</param>
    public static void HighScore(long score)
    {
        var parameters = new DTDCustomEventParameters();
        parameters.Add(key: "NewHighScore", value: score);

        DTDAnalytics.CustomEvent(eventName: "HighScore", parameters: parameters);
    }

    /// <summary>
    /// tracks the number of coins collected during each run 
    /// </summary>
    /// <param name="coins">Name of the number of coins collected</param>
    public static void coinsCollected(long coins, long distance)
    {
        var parameters = new DTDCustomEventParameters();
        parameters.Add(key: "coins", value: coins);
        parameters.Add(key: "distance", value: distance);

        DTDAnalytics.CustomEvent(eventName: "CoinsCollected", parameters: parameters);
    }

    /// <summary>
    /// reports which area the player chose as well as which layer tunnel it was 
    /// </summary>
    /// <param name="name">name of the gameobject that was chosen, which would be the layer</param>
    /// <param name="area">area that the player chose and will now be switching to</param>
    public static void AreaSwitch(string name, string area)
    {
        var parameters = new DTDCustomEventParameters();
        parameters.Add(key: "Layer", value: name);
        parameters.Add(key: "NewArea", value: area);

        DTDAnalytics.CustomEvent(eventName: "AreaSwitch", parameters: parameters);
    }

    /// <summary>
    /// reports what powerups were picked up and at what distance
    /// </summary>
    /// <param name="powerup">name of the powerup that was picked up</param>
    /// <param name="distance">distance at which the powerup was picked up</param>
    public static void PowerupPickup(string powerup, long distance)
    {
        var parameters = new DTDCustomEventParameters();
        parameters.Add(key: "Powerup", value: powerup);
        parameters.Add(key: "Distance", value: distance);

        DTDAnalytics.CustomEvent(eventName: "PowerupPickup", parameters: parameters);
    }

    /// <summary>
    /// keeps track of what the last obstacle hit was before the player died 
    /// </summary>
    /// <param name="obstacle">name of the obstacle that killed the player</param>
    public static void LastObstacle(string obstacle)
    {
        var parameters = new DTDCustomEventParameters();
        parameters.Add(key: "LastObstacle", value: obstacle);

        DTDAnalytics.CustomEvent(eventName: "LastObstacle", parameters: parameters);
    }

    
    //TODO: figure out how to actually call this function when the session is over
    /// <summary>
    /// keeps track of the number of restarts the player does during a session
    /// </summary>
    /// <param name="numRestart"></param>
    public static void RestartCount(long numRestart)
    { 
        var parameters = new DTDCustomEventParameters();
        parameters.Add(key: "RestartCount", value: numRestart);

        DTDAnalytics.CustomEvent(eventName: "RestartCount", parameters: parameters);
    }
}
