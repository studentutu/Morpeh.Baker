/*
THIS FILE IS PART OF Animation Instancing PROJECT
AnimationInstancing.cs - The core part of the Animation Instancing library

©2017 Jin Xiaoyu. All Rights Reserved.
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EventReceiver : MonoBehaviour
{

    public void NewEvent(AnimationInstancing.AnimationEvent evt)
    {
        //Debug.Log(log);
        //Debug.Log("PrintEvent: " + evt.stringParameter + " called at: " + Time.time);
    }
}
