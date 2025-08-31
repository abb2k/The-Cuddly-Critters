using UnityEngine;
using System.Collections;   

public class CouncilGuys : MonoBehaviour
{
  
    public Animator mainCouncilGuy; 
    private Animator[] allCouncil;

    void Awake()
    {
        GameObject[] objs = GameObject.FindGameObjectsWithTag("CouncilMembers");
        allCouncil = new Animator[objs.Length];
        for (int i = 0; i < objs.Length; i++)
            allCouncil[i] = objs[i].GetComponent<Animator>();
    }
/* JUST FOR TESTINGG
     void Start()
    {
        StartCoroutine(TestAfterDelay());
    }

    private IEnumerator TestAfterDelay()
    {
        yield return new WaitForSeconds(5f);
        
                DisableAll();  
        // Example: DisableAll(); or PlayWin(); etc.
    }
    */

//When fighting level starts and ends - turn off animations when fighting and turn back on when in original level
    // Enable/Disable all Animators
    public void EnableAll()  { foreach (var a in allCouncil) if (a) a.enabled = true;  if (mainCouncilGuy) mainCouncilGuy.enabled = true; }
    public void DisableAll() { foreach (var a in allCouncil) if (a) a.enabled = false; if (mainCouncilGuy) mainCouncilGuy.enabled = false; }

    // Main council guy animations 
    //should be in idle by default
    public void PlayIdle()    { if (mainCouncilGuy) mainCouncilGuy.Play("MainCouncilGuyIdle"); }

    //should play talking animations when talking intro and such
    public void PlayTalking() { if (mainCouncilGuy) mainCouncilGuy.Play("MainCouncilGuyTalking"); }

    //should play shake head animation ater you lose along with boo sounds
    public void PlayLose()    { if (mainCouncilGuy) mainCouncilGuy.Play("MainCouncilGuyShakeHead"); }

    //should play win animation when you win along with applause sounds
    public void PlayWin()     { if (mainCouncilGuy) mainCouncilGuy.Play("MainCouncilGuyCheering"); }


   
}
