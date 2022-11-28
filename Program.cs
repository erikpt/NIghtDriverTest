using CavemanTcp;
using System.Drawing;
using System.Net;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

Main();


void Main() {

    Console.WriteLine("NighDriver Simple Test App");


    CavemanTcpClient client = new CavemanTcpClient("192.168.0.108:49152");

    client.Events.ClientConnected += (s, e) =>
    {
        Console.WriteLine("Client connected!");
    };
    client.Events.ClientDisconnected += (s, e) =>
    {
        Console.WriteLine("Client disconnected!");
    };
    
    client.Connect(10);

    SocketRequest request = new();
    request.unCommandID = WIFI_COMMAND.WIFI_COMMAND_PIXELDATA64;
    request.unChannelID = 0;
    request.ulSeconds = 0;
    request.ulMicros = 0;
    byte[] bPixelValues = new byte[0];
    appendBytes(ref bPixelValues,setPixelValues(0xff, 0x55, 0x77));
    appendBytes(ref bPixelValues,setPixelValues(0xff, 0xff, 0x00));
    appendBytes(ref bPixelValues,setPixelValues(0xff, 0x00, 0x77));
    appendBytes(ref bPixelValues,setPixelValues(0x00, 0xff, 0x00));
    appendBytes(ref bPixelValues,setPixelValues(0xff, 0x00, 0x00));
    for (int i = 0; i < 139; i++)
    {
        if (i % 5 == 0 || i % 11 == 0)
        {
            appendBytes(ref bPixelValues, setPixelValuesByColor(Color.DeepPink, 50));
        }
        else if (i % 3 == 0)
        {
            appendBytes(ref bPixelValues, setPixelValuesByColor(Color.White, 30));
        }
        else
        {
            appendBytes(ref bPixelValues, setPixelValuesByColor(Color.Navy, 20));
        }
    }
    request.uLength = (uint)bPixelValues.Length/3*2;
    byte[] requestBytes = getRequestPacketBytes(request);
    appendBytes(ref requestBytes, bPixelValues);
    appendBytes(ref requestBytes, bPixelValues);

    for (int i = 0; i < 100; i++)
    {
        client.Send(requestBytes);
        Thread.Sleep(30);
    }
    ReadResult rr = null;
    try
    {
        rr = client.Read(64);
        SocketResponse response = getResponseFromBytes(rr.Data);
        Console.WriteLine($"Wi-Fi Signal Strength: {response.dWifiSignal}");
    }
    finally
    {
        client.Disconnect();
        Console.WriteLine("Press any key to exit. . .");
    }

    ConsoleKeyInfo key = Console.ReadKey(false);
    if (key.KeyChar == 'r' || key.KeyChar == 'R')
    {
        Main();
    }

}

byte[] setPixelValues(byte red, byte green, byte blue)
{
    byte[] bPixelValues = new byte[3];
    bPixelValues[0] = red;
    bPixelValues[1] = green;
    bPixelValues[2] = blue;

    return bPixelValues;
}

byte[] setPixelValuesByColor(Color color, int brightnessPercent = 100)
{
    Color thisColor = color;
    byte[] bPixelValues = new byte[3];
    if (brightnessPercent < 100)
    {
        float fBrightness = (float)brightnessPercent / 100;
        thisColor = ColorFromAhsb(color.A, color.GetHue(), color.GetSaturation(), fBrightness);
    } 
    else
    {

    }
    bPixelValues[0] = thisColor.R;
    bPixelValues[1] = thisColor.G;
    bPixelValues[2] = thisColor.B;

    return bPixelValues;
}

//void Events_DataReceived(object? sender, DataReceivedEventArgs e)
//{
//    Console.WriteLine("Packet recieved!");
//    SocketResponse response = getResponseFromBytes(e.Data.Array);
//    Console.WriteLine($"Wi-Fi Signal Strength: {response.dWifiSignal}");
//}

/// <summary>
/// Creates a Color from alpha, hue, saturation and brightness.
/// </summary>
/// <param name="alpha">The alpha channel value.</param>
/// <param name="hue">The hue value.</param>
/// <param name="saturation">The saturation value.</param>
/// <param name="brightness">The brightness value.</param>
/// <returns>A Color with the given values.</returns>
Color ColorFromAhsb(int alpha, float hue, float saturation, float brightness)
{
    if (0 > alpha
        || 255 < alpha)
    {
        throw new ArgumentOutOfRangeException(
            "alpha",
            alpha,
            "Value must be within a range of 0 - 255.");
    }

    if (0f > hue
        || 360f < hue)
    {
        throw new ArgumentOutOfRangeException(
            "hue",
            hue,
            "Value must be within a range of 0 - 360.");
    }

    if (0f > saturation
        || 1f < saturation)
    {
        throw new ArgumentOutOfRangeException(
            "saturation",
            saturation,
            "Value must be within a range of 0 - 1.");
    }

    if (0f > brightness
        || 1f < brightness)
    {
        throw new ArgumentOutOfRangeException(
            "brightness",
            brightness,
            "Value must be within a range of 0 - 1.");
    }

    if (0 == saturation)
    {
        return Color.FromArgb(
                            alpha,
                            Convert.ToInt32(brightness * 255),
                            Convert.ToInt32(brightness * 255),
                            Convert.ToInt32(brightness * 255));
    }

    float fMax, fMid, fMin;
    int iSextant, iMax, iMid, iMin;

    if (0.5 < brightness)
    {
        fMax = brightness - (brightness * saturation) + saturation;
        fMin = brightness + (brightness * saturation) - saturation;
    }
    else
    {
        fMax = brightness + (brightness * saturation);
        fMin = brightness - (brightness * saturation);
    }

    iSextant = (int)Math.Floor(hue / 60f);
    if (300f <= hue)
    {
        hue -= 360f;
    }

    hue /= 60f;
    hue -= 2f * (float)Math.Floor(((iSextant + 1f) % 6f) / 2f);
    if (0 == iSextant % 2)
    {
        fMid = (hue * (fMax - fMin)) + fMin;
    }
    else
    {
        fMid = fMin - (hue * (fMax - fMin));
    }

    iMax = Convert.ToInt32(fMax * 255);
    iMid = Convert.ToInt32(fMid * 255);
    iMin = Convert.ToInt32(fMin * 255);

    switch (iSextant)
    {
        case 1:
            return Color.FromArgb(alpha, iMid, iMax, iMin);
        case 2:
            return Color.FromArgb(alpha, iMin, iMax, iMid);
        case 3:
            return Color.FromArgb(alpha, iMin, iMid, iMax);
        case 4:
            return Color.FromArgb(alpha, iMid, iMin, iMax);
        case 5:
            return Color.FromArgb(alpha, iMax, iMin, iMid);
        default:
            return Color.FromArgb(alpha, iMax, iMid, iMin);
    }
}


void Events_Disconnected(object? sender, ClientDisconnectedEventArgs e)
{
    Console.WriteLine($"Disconnected from NightDriver strip server: {e.Reason}");
    //throw new NotImplementedException();
}


void Events_Connected(object? sender, ClientConnectedEventArgs e)
{
    Console.WriteLine($"Connected to NightDriver strip server {e.IpPort}");
    //throw new NotImplementedException();
}



byte[] getRequestPacketBytes(SocketRequest req)
{
    int size = Marshal.SizeOf(req);
    byte[] arr = new byte[size];

    IntPtr ptr = IntPtr.Zero;
    try
    {
        ptr = Marshal.AllocHGlobal(arr.Length);
        Marshal.StructureToPtr(req, ptr, false);
        Marshal.Copy(ptr, arr, 0, arr.Length);
    }
    finally
    {
        Marshal.FreeHGlobal(ptr);
    }
    return arr;
}

SocketResponse getResponseFromBytes(byte[] bDataBytes)
{
    SocketResponse response = new SocketResponse();
    int size = Marshal.SizeOf(response);
    IntPtr ptr = IntPtr.Zero;
    try
    {
        ptr = Marshal.AllocHGlobal(size);
        Marshal.Copy(bDataBytes, 0, ptr, size);
        response = (SocketResponse)Marshal.PtrToStructure(ptr, typeof(SocketResponse));
    }
    finally 
    { 
        Marshal.FreeHGlobal(ptr); 
    }
    return response;
}


void appendBytes(ref byte[] source, byte[] bytes)
{
    int i = source.Length;
    Array.Resize<byte>(ref source, i + bytes.Length);
    bytes.CopyTo(source, i);
}

public enum  WIFI_COMMAND : ushort
{
    WIFI_COMMAND_PIXELDATA = 0,             // Wifi command contains color data for the strip
    WIFI_COMMAND_VU = 1,             // Wifi command to set the current VU reading
    WIFI_COMMAND_CLOCK = 2,             // Wifi command telling us current time at the server
    WIFI_COMMAND_PIXELDATA64 = 3,             // Wifi command with color data and 64-bit clock vals
    WIFI_COMMAND_STATS = 4,             // Wifi command to request stats from chip back to server
    WIFI_COMMAND_REBOOT = 5,             // Wifi command to reboot the client chip (that's us!)
    WIFI_COMMAND_VU_SIZE = 16,
    WIFI_COMMAND_CLOCK_SIZE = 20,
}

public struct SocketRequest
{
    public WIFI_COMMAND unCommandID; //2 bytes
    public ushort unChannelID; //2 bytes (set to 1 for single channel, 0-based channel assignments are deprecated)
    public uint uLength;  //4 bytes (number of 24 bit pixels being set)
    public ulong ulSeconds; //8 bytes (set this to 0)
    public ulong ulMicros;  //8 bytes (set this to 0)
    //public byte[] RGB;     //Variable (24 bit color data, one per PIXEL, specified in length above.
}

// SocketResponse
// Response data sent back to server ever time we receive a packet
public struct SocketResponse
{
    public uint uSize;              // 4
    public uint uFlashVersion;      // 4
    public ulong ulCurrentClock;    // 8
    public ulong ulOldestPacket;    // 8
    public ulong ulNewestPacket;    // 8
    public double dBrightness;      // 8
    public double dWifiSignal;      // 8
    public uint uBufferSize;        // 4
    public uint uBufferPos;         // 4
    public uint uFpsDrawing;        // 4    
    public uint uWatts;             // 4
};
