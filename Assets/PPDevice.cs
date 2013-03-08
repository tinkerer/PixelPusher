using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;

public class PPDevice
    {
    
 }



public class Pixel
{

    public byte red;
    public byte green;
    public byte blue;

    public Pixel()
    {
        red = 0;
        green = 0;
        blue = 0;
    }

    public void setColor(int color)
    {
        this.blue = (byte)(color & 0xff);
        this.green = (byte)((color >> 8) & 0xff);
        this.red = (byte)((color >> 16) & 0xff);
    }

    public void setColor(Pixel pixel)
    {
        this.red = pixel.red;
        this.blue = pixel.blue;
        this.green = pixel.green;
    }

    public Pixel(Pixel pixel)
    {
        this.red = pixel.red;
        this.blue = pixel.blue;
        this.green = pixel.green;
    }

    public Pixel(byte red, byte green, byte blue)
    {
        this.red = red;
        this.green = green;
        this.blue = blue;
    }

}


public class Strip {

  private Pixel[] pixels;
  private PixelPusher pusher;
  private int stripNumber;

  public Strip(PixelPusher pusher, int stripNumber, int length) {
    this.pixels = new Pixel[length];
    for (int i = 0; i < this.pixels.Length; i++) {
      this.pixels[i] = new Pixel();
    }
    this.pusher = pusher;
    this.stripNumber = stripNumber;
  }

  public int getLength() {
    return pixels.Length;
  }

  public String getMacAddress() {
    return this.pusher.getMacAddress();
  }

  public int getStripNumber() {
    return stripNumber;
  }

  public long getStripIdentifier() {
    // Return a compact reversible identifier
    return -1;
  }

  public void setPixels(Pixel[] pixels) {
     Array.Copy(pixels, this.pixels, this.pixels.Length);
  }

  public void setPixel(int color, int position) {
    this.pixels[position].setColor(color);
  }
  
  public void setPixel(Pixel pixel, int position) {
    this.pixels[position].setColor(pixel);
  }

  public byte[] serialize() {
    byte[] msg = new byte[pixels.Length * 3];
    int i = 0;
    foreach (Pixel pixel in pixels) {
      //if (pixel == null)
       // pixel = new Pixel();
      msg[i++] = pixel.red;
      msg[i++] = pixel.green;
      msg[i++] = pixel.blue;
    }
    return msg;
  }

}

public class PixelPusher  {
  /**
   * uint8_t strips_attached;
   * uint8_t max_strips_per_packet;
   * uint16_t pixels_per_strip; // uint16_t used to make alignment work
   * uint32_t update_period; // in microseconds
   * uint32_t power_total; // in PWM units
   * uint32_t delta_sequence; // difference between received and expected
   * sequence numbers
   * int32_t controller_ordinal;  // configured order number for controller
   * int32_t group_ordinal;  // configured group number for this controller
   */

  private List<Strip> strips;
  long extraDelayMsec = 0;

  /**
   * @return the stripsAttached
   */
  public int getNumberOfStrips() {
    return strips.Count;
  }

  public List<Strip> getStrips() {
    return this.strips;
  }

  public Strip getStrip(int stripNumber) {
    return this.strips[stripNumber];
  }

  /**
   * @return the maxStripsPerPacket
   */
  public int getMaxStripsPerPacket() {
    return maxStripsPerPacket;
  }

  /**
   * @return the pixelsPerStrip
   */
  public int getPixelsPerStrip() {
    if (this.strips.Count == 0) {
      return 0;
    }
    return this.strips[0].getLength();
  }

  /**
   * @return the updatePeriod
   */
  public long getUpdatePeriod() {
    return updatePeriod;
  }

  /**
   * @return the powerTotal
   */
  public long getPowerTotal() {
    return powerTotal;
  }

  public long getDeltaSequence() {
    return deltaSequence;
  }
  public void increaseExtraDelay(long i) {
    extraDelayMsec += i;
    Debug.LogError("Extra delay now "+extraDelayMsec);
  }
  public long getExtraDelay() {
    return extraDelayMsec;
  }
  public void setExtraDelay(long i) {
    extraDelayMsec = i;
  }
  public int getControllerOrdinal() {
      return controllerOrdinal;
  }

  public int getGroupOrdinal() {
    return groupOrdinal;
  }

  private int maxStripsPerPacket;
  private long updatePeriod;
  private long powerTotal;
  private long deltaSequence;
  private int controllerOrdinal;
  private int groupOrdinal;

  public void setStripValues(int stripNumber, Pixel[] pixels) {
    this.strips[stripNumber].setPixels(pixels);
  }

  public PixelPusher(byte[] packet, DeviceHeader header) {
      this.header = header;
    if (packet.Length < 12) {
      throw new ArgumentException("packet length < 12");
    }
      byte [] hword = new byte[1];
      byte [] word = new byte[2];
      byte [] dword = new byte[4];
      Array.Copy(packet,hword, 1);
    int stripsAttached = ByteUtils.unsignedCharToInt(hword);
      Array.Copy(packet, 2, word,0, 2);
    int pixelsPerStrip = ByteUtils.unsignedShortToInt(word);
      Array.Copy(packet,1,hword,0, 1);
    maxStripsPerPacket = ByteUtils.unsignedCharToInt(hword);

       Array.Copy(packet, 4, dword,0, 4);
    updatePeriod = ByteUtils.unsignedIntToLong(dword);
       Array.Copy(packet, 8, dword,0, 4);
    powerTotal = ByteUtils.unsignedIntToLong(dword);
      Array.Copy(packet, 12, dword,0, 4);
    deltaSequence = ByteUtils.unsignedIntToLong(dword);
      Array.Copy(packet, 16, dword,0, 4);
    controllerOrdinal = (int) ByteUtils.unsignedIntToLong(dword);
      Array.Copy(packet, 20, dword,0, 4);
    groupOrdinal = (int) ByteUtils.unsignedIntToLong(dword);
      this.strips = new List<Strip>();
    
    for (int stripNo = 0; stripNo < stripsAttached; stripNo++) {
      this.strips.Add(new Strip(this, stripNo, pixelsPerStrip));
    }

  }

  public int hashCode() {
    int prime = 31;
    int result = 1;
    result = prime * result + getPixelsPerStrip();
    result = prime * result + getNumberOfStrips();
    return result;
  }

  
  public bool equals(object obj) {
    if (this == obj)
      return true;
    if (obj == null)
      return false;
      
    if (this.GetType() != obj.GetType())
      return false;
    PixelPusher other = (PixelPusher) obj;
    if (getPixelsPerStrip() != other.getPixelsPerStrip())
      return false;
    if (getNumberOfStrips() != other.getNumberOfStrips())
      return false;
    return true;
  }

  public void copyHeader(PixelPusher device) {
    this.controllerOrdinal = device.controllerOrdinal;
    this.deltaSequence = device.deltaSequence;
    this.groupOrdinal = device.groupOrdinal;
    this.maxStripsPerPacket = device.maxStripsPerPacket;
    this.powerTotal = device.powerTotal;
    this.updatePeriod = device.updatePeriod;

  }

  
  public int compareTo(PixelPusher comp) {
    int group0 = this.getGroupOrdinal();
    int group1 = ((PixelPusher) comp).getGroupOrdinal();
    if (group0 != group1) {
      if (group0 < group1)
        return -1;
      return 1;
    }
    int ord0 = this.getControllerOrdinal();
    int ord1 = ((PixelPusher) comp).getControllerOrdinal();
    if (ord0 != ord1) {
      if (ord0 < ord1)
        return -1;
      return 1;
    }

    return this.getMacAddress().CompareTo(comp.getMacAddress());
  }

    private DeviceHeader header;

    public String getMacAddress()
    {
        return header.GetMacAddressString();
    }

    public IPAddress getIp()
        {
            return header.IpAddress;
        }

public PP_DeviceType getDeviceType()
        {
            return header.deviceType;
        }

public  int getProtocolVersion()
        {
            return header.ProtocolVersion;
        }

        public int getVendorId()
        {
       return header.VendorId;
        }

       public  int getProductId()
{
    return header.ProductId;
}
        public int getHardwareRevision()
{
    return header.HardwareRevision;
}
        public int getSoftwareRevision()
        {
            return header.SoftwareRevision;
        }

        public long getLinkSpeed()
        {
            return header.LinkSpeed;
        }
        
        public String toString()
        {
            return "Mac: " + header.GetMacAddressString() + ", IP: "
                + header.IpAddress.ToString()
               + " # Strips(" + getNumberOfStrips()
        + ") Max Strips Per Packet(" + maxStripsPerPacket
        + ") PixelsPerStrip (" + getPixelsPerStrip() + ") Update Period ("
        + updatePeriod + ") Power Total (" + powerTotal + ") Delta Sequence ( "
        + deltaSequence + ") Group (" +groupOrdinal +") Controller ("
        + controllerOrdinal + " )";
  }

}





public class PusherGroup
{

    private List<PixelPusher> pushers;

    PusherGroup(List<PixelPusher> pushers)
    {
        this.pushers = pushers;
    }

    public PusherGroup()
    {
        pushers = new List<PixelPusher>();
    }

    public List<PixelPusher> getPushers()
    {
        return this.pushers;
    }

    public List<Strip> getStrips() {
    List<Strip> strips = new List<Strip>();
    foreach (PixelPusher p in this.pushers) {
      strips.AddRange(p.getStrips());
    }
    return strips;
  }

    public void removePusher(PixelPusher pusher)
    {
        this.pushers.Remove(pusher);
    }

    public void addPusher(PixelPusher pusher)
    {
        this.pushers.Add(pusher);
    }
}



