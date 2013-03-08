/*

 

    -----------------------

    UDP-Receive (send to)

    -----------------------

    // [url]http://msdn.microsoft.com/de-de/library/bb979228.aspx#ID0E3BAC[/url]

    

    

    // > receive 

    // 127.0.0.1 : 8051

    

    // send

    // nc -u 127.0.0.1 8051

 

*/

using UnityEngine;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;


using System;

using System.Text;

using System.Net;

using System.Net.Sockets;

using System.Threading;



public class UDPReceive : MonoBehaviour
{


    public Color32 myColor;
    public Color32 endColor;
    public string lastReceivedUDPPacket = "";

    public string allReceivedUDPPackets = ""; // clean up this from time to time!


    // start from unity3d
    DeviceRegistry dr; 
    public void Start()
    {
        dr = new DeviceRegistry();
        dr.startPushing();
        
    }
    Texture2D myTexture2D;
    public RenderTexture rt;

    public void Update()
    {
        if (dr.getStrips().Count > 0)
        {
            
            int L = dr.getStrips()[0].getLength();

            RenderTexture.active = rt;

            myTexture2D = new Texture2D(1, L, TextureFormat.RGB24, false);
            myTexture2D.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            myTexture2D.Apply();


            myTexture2D.ReadPixels(new Rect(0, 0, myTexture2D.width, myTexture2D.height), 0, 0);
            myTexture2D.Apply();
            Color32 [] _Color32 = myTexture2D.GetPixels32();
            UnityEngine.Debug.Log("color32 len: " + _Color32.Length);
            for (int k = 0; k < dr.getStrips().Count; k++ )
            {                                
                for (int i = 0; i < L; i++)
                {
                    Pixel p = new Pixel();



                    Color32 _Color = _Color32[i];
                  
                    p.red = _Color.r;
                    p.green = _Color.g;
                    p.blue = _Color.b;
                    dr.getStrips()[k].setPixel(p, i);
                }
            }
        }
    }

    // OnGUI

    void OnGUI()
    {

        Rect rectObj = new Rect(40, 10, 200, 400);

        GUIStyle style = new GUIStyle();

        style.alignment = TextAnchor.UpperLeft;

        GUI.Box(rectObj, "# make this better later "

                , style);

    }



    // init

    public void OnDestroy()
    {
        dr.Destroy();
    }
}
public class CardThread
{

    
  private long threadSleepMsec = 4;
  private long threadExtraDelayMsec = 0;
  private long threadSendTime = 0;
  private long bandwidthEstimate = 0;
  private PixelPusher pusher;
  private byte[] packet;
  private byte[] udppacket;
  private IPEndPoint remoteEndPoint;
  private UdpClient udpsocket;
  private bool _cancel;
  private int pusherPort;
  private IPAddress cardAddress;
  private long packetNumber;
    
  public CardThread(PixelPusher pusher, int pusherPort) {
    this.pusher = pusher;
    this.pusherPort = pusherPort;
    this.cardAddress = pusher.getIp();
    try {
        
        remoteEndPoint = new IPEndPoint(this.cardAddress, pusherPort);

        this.udpsocket = new UdpClient();
        

    } catch (SocketException se) {
      UnityEngine.Debug.LogError("SocketException: " + se.Message);
    }
    this.packet = new byte[1460];
    
    this.packetNumber = 0;
    this._cancel = false;
    if (pusher.getUpdatePeriod() > 100 && pusher.getUpdatePeriod() < 1000000)
      this.threadSleepMsec = (pusher.getUpdatePeriod() / 1000) + 1;
  }

  public void setExtraDelay(long msec) {
    threadExtraDelayMsec = msec;
  }

  public int getBandwidthEstimate() {
    return (int) bandwidthEstimate;
  }

  private Thread myThread;
   public void startThread()
   {


        UnityEngine.Debug.Log("CardThread::startThread()");
        

              UnityEngine.Debug.Log("Sending to  : " +  pusherPort);

        myThread = new Thread(
            new ThreadStart(run));
        myThread.IsBackground = true;
        myThread.Start();



   }
  public void run() {
      while (!_cancel)
      {
      int bytesSent;
      Stopwatch stopWatch = new Stopwatch();
      stopWatch.Start();
      bytesSent = sendPacketToPusher(pusher);
      stopWatch.Stop();
      bandwidthEstimate = bytesSent / (stopWatch.ElapsedMilliseconds / 1000);
    }
  }

  public bool cancel() {
      this._cancel = true;
    return true;
  }

  private int sendPacketToPusher(PixelPusher pusher) {
    int packetLength = 0;
    int totalLength = 0;
      
    int stripPerPacket = pusher.getMaxStripsPerPacket();
    List<Strip> remainingStrips = new List<Strip>(pusher.getStrips());
    while (!(remainingStrips.Count==0)) {
      if (pusher.getUpdatePeriod() > 100 && pusher.getUpdatePeriod() < 10000000)
        this.threadSleepMsec = (pusher.getUpdatePeriod() / 1000) + 1;
      byte[] packetNumberArray = ByteUtils.unsignedIntToByteArray(packetNumber, true);
      for(int i = 0; i < packetNumberArray.Length; i++) {
        this.packet[packetLength++] = packetNumberArray[i];
      }
      packetNumber++;
      /* System.err.println(" Packet number array = length "+ packetLength + 
       *      " seq "+ packetNumber +" data " + String.format("%02x, %02x, %02x, %02x", 
       *          packetNumberArray[0], packetNumberArray[1], packetNumberArray[2], packetNumberArray[3]));
       */
      for (int i = 0; i < stripPerPacket; i++) {
        if (remainingStrips.Count == 0) {
          break;
        }
        Strip strip = remainingStrips[0];
        remainingStrips.RemoveAt(0);
        byte[] stripPacket = strip.serialize();
        this.packet[packetLength++] = (byte) strip.getStripNumber();
        for (int j = 0; j < stripPacket.Length; j++) {
          this.packet[packetLength + j] = stripPacket[j];
        }
        packetLength += stripPacket.Length;
      }


      try {
          Stopwatch stopWatch = new Stopwatch();
          stopWatch.Start();
          udpsocket.Send(packet, packetLength, remoteEndPoint);
        stopWatch.Stop();
        
        threadSendTime = stopWatch.ElapsedMilliseconds;
        stopWatch.Reset(); 
        
      } catch (SocketException ioe) {
        UnityEngine.Debug.LogError("IOException: " + ioe.Message);
      }
      totalLength += packetLength;

      try {
        Thread.Sleep((int)(threadSleepMsec + threadExtraDelayMsec + threadSendTime + pusher.getExtraDelay()));
      } catch (ThreadInterruptedException e) {
        UnityEngine.Debug.LogError(e.StackTrace);
      }
      packetLength = 0;
    }
    return totalLength;
  }


}


public class SceneThread {

  private static int PUSHER_PORT = 9897;

  private Dictionary<String, PixelPusher> pusherMap;
  private Dictionary<String, CardThread> cardThreadMap;
  byte[] packet;
  int packetLength;
  private int extraDelay = 0;

  private bool drain;

  private bool running;

  public SceneThread() {
    this.pusherMap = new Dictionary<String, PixelPusher>();
    this.cardThreadMap = new Dictionary<String, CardThread>();
    this.drain = false;
    this.running = false;

  }

  public long getTotalBandwidth() {
    long totalBandwidth=0;
    foreach (CardThread thread in cardThreadMap.Values) {
      totalBandwidth += thread.getBandwidthEstimate();
    }
    return totalBandwidth;
  }
  
  public void setExtraDelay(int msec) {
    extraDelay = msec;
    foreach (CardThread thread in cardThreadMap.Values) {
      thread.setExtraDelay(msec);
    }
  }


  public void update(DeviceRegistry observable, object update)
  {
    if (!drain) {
      Dictionary<String, PixelPusher> incomingPusherMap = ((DeviceRegistry) observable)
          .getPusherMap(); // all observed pushers
      Dictionary<String, PixelPusher> newPusherMap = new Dictionary<String, PixelPusher>(
          incomingPusherMap);
      Dictionary<String, PixelPusher> deadPusherMap = new Dictionary<String, PixelPusher>(
          pusherMap);

      foreach (String key in newPusherMap.Keys) {
        if (pusherMap.ContainsKey(key)) { // if we already know about it
          newPusherMap.Remove(key); // remove it from the new pusher map (is
                                    // old)
        }
      }
      foreach (String key in pusherMap.Keys) {
        if (newPusherMap.ContainsKey(key)) { // if it's in the new pusher map
          deadPusherMap.Remove(key); // it can't be dead
        }
      }

      foreach (String key in newPusherMap.Keys) {
        CardThread newCardThread = new CardThread(newPusherMap[key],
            PUSHER_PORT);
        if (running) {
          newCardThread.startThread();
          newCardThread.setExtraDelay(extraDelay);
        }
        cardThreadMap[key] = newCardThread;
      }
      foreach (String key in deadPusherMap.Keys) {
        UnityEngine.Debug.Log("Killing old CardThread " + key);
        cardThreadMap[key].cancel();
        cardThreadMap.Remove(key);
      }
    }
  }

  public bool isRunning() {
    return this.running;
  }

  
  public void run() {
    this.running = true;
    this.drain = false;
    foreach (CardThread thread in cardThreadMap.Values) {
      thread.startThread();
    }
  }

  public bool cancel() {
    this.drain = true;
    foreach (String key in cardThreadMap.Keys) {
      cardThreadMap[key].cancel();
      cardThreadMap.Remove(key);
    }
    return true;
  }

     private Thread myThread;
   public void startThread()
   {


        UnityEngine.Debug.Log("SceneThread::startThread()");
        
        myThread = new Thread(
            new ThreadStart(run));
        myThread.IsBackground = true;
        myThread.Start();



   }
}



public class DeviceRegistry {


    List<SceneThread> observers;
  private UdpClient udp;
  private static int DISCOVERY_PORT = 7331;
  private static int MAX_DISCONNECT_SECONDS = 10;
  private static long EXPIRY_TIMER_MSEC = 1000L;

  private Dictionary<String, PixelPusher> pusherMap;
  private Dictionary<String, DateTime> pusherLastSeenMap;

  private Timer expiryTimer;

  private SceneThread sceneThread;

  private Dictionary<int, PusherGroup> groupMap;

  private List<PixelPusher> sortedPushers;

  public Dictionary<String, PixelPusher> getPusherMap() {
    return pusherMap;
  }

  public void setExtraDelay(int msec) {
    sceneThread.setExtraDelay(msec);
  }

  public long getTotalBandwidth() {
    return sceneThread.getTotalBandwidth();
  }

  public List<Strip> getStrips() {
    List<Strip> strips = new List<Strip>();
    foreach (PixelPusher p in this.sortedPushers) {
      strips.AddRange(p.getStrips());
    }
    return strips;
  }

  public List<Strip> getStrips(int groupNumber) {
    return this.groupMap[groupNumber].getStrips();
  }

  
    
    public void time_task_run(object state) {
      UnityEngine.Debug.Log("Expiry and preening task running");
      foreach (String deviceMac in pusherMap.Keys) {
        int lastSeenSeconds = (pusherLastSeenMap[deviceMac] - DateTime.Now).Seconds;
        if (lastSeenSeconds > MAX_DISCONNECT_SECONDS) {
            ((DeviceRegistry)state).expireDevice(deviceMac);
        }
      }
    }
   

  private class DefaultPusherComparator  {

    
    public int compare(PixelPusher arg0, PixelPusher arg1) {
      int group0 = arg0.getGroupOrdinal();
      int group1 = arg1.getGroupOrdinal();
      if (group0 != group1) {
        if (group0 < group1)
          return -1;
        return 1;
      }

      int ord0 = arg0.getControllerOrdinal();
      int ord1 = arg1.getControllerOrdinal();
      if (ord0 != ord1) {
        if (ord0 < ord1)
          return -1;
        return 1;
      }

      return arg0.getMacAddress().CompareTo(arg1.getMacAddress());
    }

  }
    
    public void addObserver(SceneThread observer)
    {
          observers.Add(observer);
    }

    public void notifyObservers(PixelPusher device = null)
    {
        foreach (SceneThread observer in observers)
        {
            observer.update(this, (object)device);
        }
    }
    Thread receiveThread;

  public DeviceRegistry() {
      observers = new List<SceneThread>();
    udp = new UdpClient(DISCOVERY_PORT);
    pusherMap = new Dictionary<String, PixelPusher>();
    groupMap = new Dictionary<int, PusherGroup>();
    sortedPushers = new List<PixelPusher>();
    pusherLastSeenMap = new Dictionary<String, DateTime>();
    //udp.setReceiveHandler("receive");
    //udp.log(false);
    //udp.listen(true);

      
    receiveThread = new Thread(
            new ThreadStart(ReceiveData));
        receiveThread.IsBackground = true;
        receiveThread.Start();



    this.expiryTimer = new Timer(new TimerCallback(time_task_run),this,0, 30);
    
    this.sceneThread = new SceneThread();

    this.addObserver(this.sceneThread);
  }
    public string lastReceivedUDPPacket;
   private bool running = true;
    private void ReceiveData()
    {


        while (running)
        {
            try
            {
                
                IPEndPoint anyIP = new IPEndPoint(IPAddress.Any, 0);

                byte[] data = udp.Receive(ref anyIP);
                receive(data);
                
                string text = Encoding.UTF8.GetString(data);

                lastReceivedUDPPacket = text;
            }
            catch (Exception err)
            {
                UnityEngine.Debug.Log(err.ToString());
            }
        }
    }  
    
    
    
    
  public void expireDevice(String macAddr) {
    UnityEngine.Debug.Log("Device gone: " + macAddr);
    PixelPusher pusher = pusherMap[macAddr];
      pusherMap.Remove(macAddr);
    pusherLastSeenMap.Remove(macAddr);
    sortedPushers.Remove(pusher);
    this.groupMap[pusher.getGroupOrdinal()].removePusher(pusher);

    
    this.notifyObservers();
  }

  public void setStripValues(String macAddress, int stripNumber, Pixel[] pixels) {
    this.pusherMap[macAddress].setStripValues(stripNumber, pixels);

  }

  public void startPushing() {
    if (!sceneThread.isRunning()) {
      sceneThread.startThread();
    }
  }

  public void stopPushing() {
    if (sceneThread.isRunning()) {
      sceneThread.cancel();
    }
  }


  public void receive(byte[] data) {
    // This is for the UDP callback, this should not be called directly
    DeviceHeader header = new DeviceHeader(data);
    String macAddr = header.GetMacAddressString();
    if (header.deviceType != PP_DeviceType.PIXELPUSHER) {
      UnityEngine.Debug.Log("Ignoring non-PixelPusher discovery packet from "
          + header.toString());
      return;
    }
    PixelPusher device = new PixelPusher(header.PacketRemainder, header);
    // Set the timestamp for the last time this device checked in
    pusherLastSeenMap[macAddr] =  DateTime.Now;
    if (!pusherMap.ContainsKey(macAddr)) {
      // We haven't seen this device before
      addNewPusher(macAddr, device);
    } else {
      if (pusherMap[macAddr] != device ) { // we already saw it
        updatePusher(macAddr, device);
      } else {
        // The device is identical, nothing has changed
        UnityEngine.Debug.Log("Device still present: " + macAddr);
        // if we dropped more than occasional packets, slow down a little
        if (device.getDeltaSequence() > 2)
            pusherMap[macAddr].increaseExtraDelay(5);
        UnityEngine.Debug.Log(device.toString());
      }
    }
  }

  private void updatePusher(String macAddr, PixelPusher device) {
    // We already knew about this device at the given MAC, but its details
    // have changed
    UnityEngine.Debug.Log("Device changed: " + macAddr);
    pusherMap[macAddr].copyHeader(device);
    
    
    this.notifyObservers(device);
  }

  private void addNewPusher(String macAddr, PixelPusher pusher) {
    UnityEngine.Debug.Log("New device: " + macAddr +" has group ordinal "+ pusher.getGroupOrdinal());
    pusherMap[macAddr]= pusher;
    UnityEngine.Debug.Log("Adding to sorted list");
    sortedPushers.Add(pusher);
    UnityEngine.Debug.Log("Adding to group map");
    if (groupMap[pusher.getGroupOrdinal()] != null) {
      groupMap[pusher.getGroupOrdinal()].addPusher(pusher);
    } else {
      // we need to create a PusherGroup since it doesn't exist yet.
      PusherGroup pg = new PusherGroup();
      groupMap[pusher.getGroupOrdinal()] = pg; 
    }
    UnityEngine.Debug.Log("Notifying observers");


    
    this.notifyObservers(pusher);
  }
  public void Destroy()
  {
      expiryTimer.Dispose();
      running = false;
      receiveThread.Abort();
      receiveThread = null;
  }
}