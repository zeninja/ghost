using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chat : MonoBehaviour {
    public List<string> chatHistory = new List<string>(); // history of all chat texts
    private string currentMessage = string.Empty; 

    private void OnGui(){
        // we want to be able to draw the history of the chat 
        // box to type out message

        GUILayout.BeginHorizontal(GUILayout.Width(250));
        //create current message box
        currentMessage = GUILayout.TextField(currentMessage);
        //button to send message 
        if (GUILayout.Button("Send")){
            if(!string.IsNullOrEmpty(currentMessage.Trim())){
                //6 min mark taking away a lot of stuff about online stuff
                // if there is no message cannot press send
                ChatMessage(currentMessage);
                currentMessage = string.Empty;
            }
        }
        GUILayout.EndHorizontal();
        //simple listing of all the strings

        foreach (string c  in chatHistory){
            GUILayout.Label(c); 
        }


    }

    public void ChatMessage(string message){
        chatHistory.Add(message);
        
    }
    // Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
