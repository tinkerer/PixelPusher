using UnityEngine;
using System.Collections;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

public class udptest : MonoBehaviour {

    private const int listenPort = 9897;
    private UdpClient listener;
    private IPEndPoint groupEP;
	// Use this for initialization
	void Start () {
        listener = new UdpClient(listenPort);
        groupEP = new IPEndPoint(IPAddress.Any, listenPort);


	}
	
	// Update is called once per frame
	void Update () {

        Console.WriteLine("Waiting for broadcast");
        byte[] bytes = listener.Receive(ref groupEP);

        Console.WriteLine("Received broadcast from {0} :\n {1}\n",
        groupEP.ToString(),
        Encoding.ASCII.GetString(bytes, 0, bytes.Length));
            
	
	}




    void OnDisable(){
            listener.Close();
    }
    

    
}