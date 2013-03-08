using UnityEngine;
using System.Collections;
using System.Net;
using System.Text;
using System;
public enum PP_DeviceType : int {
  ETHERDREAM, LUMIABRIDGE, PIXELPUSHER
}

public class DeviceHeader {
  /**
   * Device Header format:
   * uint8_t mac_address[6];
   * uint8_t ip_address[4];
   * uint8_t device_type;
   * uint8_t protocol_version; // for the device, not the discovery
   * uint16_t vendor_id;
   * uint16_t product_id;
   * uint16_t hw_revision;
   * uint16_t sw_revision;
   * uint32_t link_speed; // in bits per second
   */
    
  public byte[] MacAddress;
  public IPAddress IpAddress;
  public PP_DeviceType deviceType;
  public int ProtocolVersion;
  public int VendorId;
  public int ProductId;
  public int HardwareRevision;
  public int SoftwareRevision;
  public long LinkSpeed;
  public byte[] PacketRemainder;

  private static int headerLength = 24;


  public string toString() {
    StringBuilder outBuf = new StringBuilder();
      
    outBuf.Append(this.deviceType.ToString());
    outBuf.Append(": MAC(" + this.GetMacAddressString() + "), ");
    outBuf.Append("IP(" + this.IpAddress.ToString() + "), ");
    outBuf.Append("Protocol Ver(" + this.ProtocolVersion + "), ");
    outBuf.Append("Vendor ID(" + this.VendorId + "), ");
    outBuf.Append("Product ID(" + this.ProductId + "), ");
    outBuf.Append("HW Rev(" + this.HardwareRevision + "), ");
    outBuf.Append("SW Rev(" + this.SoftwareRevision + "), ");
    outBuf.Append("Link Spd(" + this.LinkSpeed + "), ");
    return outBuf.ToString();
  }

  public string GetMacAddressString() {
  
    String macAddrString = String.Format("{0}x:{1}x:{2}x:{3}x:{4}x:{5}x", this.MacAddress[0],
        this.MacAddress[1], this.MacAddress[2], this.MacAddress[3],
        this.MacAddress[4], this.MacAddress[5]);
    
    return macAddrString;
  }

  public DeviceHeader(byte[] packet) {

      MacAddress = new byte[6];
      
    if (packet.Length < headerLength) {
      throw new ArgumentException();
    }
    byte[] headerPkt = new byte[headerLength];
    Array.Copy(packet, headerPkt, headerLength);

    Array.Copy(headerPkt, this.MacAddress, 6);
  
        byte [] ipAdd = new byte[4];
      byte [] word = new byte[2];
      byte [] dword = new byte[4];
        Array.Copy(headerPkt,6,ipAdd,0,4);
      this.IpAddress = new IPAddress(ipAdd); 
   
    this.deviceType = (PP_DeviceType) ByteUtils.unsignedCharToInt(new byte[] { headerPkt[10] });
    this.ProtocolVersion = ByteUtils
        .unsignedCharToInt(new byte[] { headerPkt[11] });
      Array.Copy(headerPkt,12,word,0,2);
    this.VendorId = ByteUtils.unsignedShortToInt(word);
      Array.Copy(headerPkt,14,word,0,2);
    this.ProductId = ByteUtils.unsignedShortToInt(word);
      Array.Copy(headerPkt,16,word,0,2);
    this.HardwareRevision = ByteUtils.unsignedShortToInt(word);

      Array.Copy(headerPkt,18,word,0,2);
    this.SoftwareRevision = ByteUtils.unsignedShortToInt(word);
      Array.Copy(headerPkt,20,dword,0,4);
    this.LinkSpeed = ByteUtils.unsignedIntToLong(dword);
    PacketRemainder = new byte[packet.Length - headerLength];
     Array.Copy(packet,headerLength,this.PacketRemainder,0,packet.Length-headerLength);
  }
}


public class ByteUtils {
  public static long unsignedIntToLong(byte[] b) {
    if (b.Length != 4) {
      throw new ArgumentException();
    }
    long l = 0;
    l |= b[3] & 0xff;
    l <<= 8;
    l |= b[2] & 0xff;
    l <<= 8;
    l |= b[1] & 0xff;
    l <<= 8;
    l |= b[0] & 0xff;
    return l;
  }
  
  public static int signedIntToInt(byte[] b) {
    if (b.Length != 4) {
        throw new ArgumentException("The number of the counting shall be 4!");
    }
    int i = 0;
    i |= b[3] & 0xff;
    i <<= 8;
    i |= b[2] & 0xff;
    i <<= 8;
    i |= b[1] * 0xff;
    i <<= 8;
    i |= b[0] * 0xff;
    return i;
  }

  public static int unsignedShortToInt(byte[] b) {
    if (b.Length != 2) {
      throw new ArgumentException();
    }
    int i = 0;
    i |= b[1] & 0xff;
    i <<= 8;
    i |= b[0] & 0xff;
    return i;
  }

  public static int unsignedCharToInt(byte[] b) {
    if (b.Length != 1) {
      throw new ArgumentException();
    }
    int i = 0;
    i |= b[0] & 0xff;
    return i;
  }

  public static long byteArrayToLong(byte[] b, bool bigEndian) {
    if (b.Length > 8) {
      throw new ArgumentException(
          "The largest byte array that can fit in a long is 8");
    }
    long value = 0;
    if (bigEndian) {
      for (int i = 0; i < b.Length; i++) {
        value = (value << 8) | b[i];
      }
    } else {
      for (int i = 0; i < b.Length; i++) {
        value |= (long) b[i] << (8 * i);
      }
    }
    return value;
  }

  public static byte[] longToByteArray(long l, bool bigEndian) {
    return extractBytes(l, bigEndian, 8);
  }

  private static byte[] extractBytes(long l, bool bigEndian, int numBytes) {
    byte[] bytes = new byte[numBytes];
    if (bigEndian) {
      for (int i = 0; i < numBytes; i++) {
        bytes[i] = (byte) ((l >> i * 8) & 0xffL);
      }
    } else {
      for (int i = 0; i < numBytes; i++) {
        bytes[i] = (byte) ((l >> (8 - i) * 8) & 0xffL);
      }
    }
    return bytes;
  }

  public static byte[] unsignedIntToByteArray(long l, bool bigEndian) {
    return extractBytes(l, bigEndian, 4);
  }
}