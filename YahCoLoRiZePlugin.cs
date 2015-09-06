// This program is distributed under the terms of the GNU General Public
// License (2015).
/******************************************************************************\
 * IceChat 9 Internet Relay Chat Client
 *
 * Copyright (C) 2012 Paul Vanderzee <snerf@icechat.net>
 *                                    <www.icechat.net> 
 *                                    
 * This Plugin software is authored by Scott Swift, YahCoLoRiZe.com, 2015
 * for IceChat 9
 * 
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2, or (at your option)
 * any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 675 Mass Ave, Cambridge, MA 02139, USA.
 *
 * Please consult the LICENSE.txt file included with this project for
 * more details
 *
\******************************************************************************/

#region Using directives

using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;

#endregion

namespace IceChatPlugin
{
  #region Plugin

  public class Plugin : IPluginIceChat
  {
    #region Our Commands

    // Commands we accept from the input window...
    // (can be changed to anything you want but must be lower-case!)
    internal const string C_PLAY = "/cplay"; // Controls text-playback from YahCoLoRiZe
    internal const string C_CHAN = "/cchan"; // Remotely change the channel (room) text is sent to
    internal const string C_TIME = "/ctime"; // Remotely change text playback speed (in milliseconds)
    internal const string C_SEND = "/cx"; // Send a line of text to YahCoLoRiZe for processing
    internal const string C_HELP = "/chelp"; // Echo a list of commands

    // C_PLAY arguement strings
    internal const string A_START = "start"; // Starts/restarts text-playback
    internal const string A_STOP = "stop"; // Stops text-playback
    internal const string A_PAUSE = "pause"; // Pauses text-playback
    internal const string A_RESUME = "resume"; // Resumes text-playback

    #endregion

    #region Constants

    // Max # of bytes to send/receive in the data field of COLORIZENETSTRUCT
    // (Can't be changed!)
    internal const int CNS_DATALEN = 2048;
    // Max # of bytes to send/receive in the ChanNick field of COLORIZENETSTRUCT
    // (Can't be changed!)
    internal const int CNS_CHANNICKLEN = 512;

    internal const int ICECHAT_ID = 3; // Permanent Client ID assigned to IceChat

    // Registered with RegisterWindowsMessage()
    internal const string YAHCOLORIZE_SIGNITURE = "WM_ColorizeNet";

    internal const string YAHCOLORIZE_CLASSNAME = "TDTSColor";
    internal const string YAHCOLORIZE_WINDOWNAME = "YahCoLoRiZe";

    // Remote commands
    internal const int REMOTE_COMMAND_START = 0;
    internal const int REMOTE_COMMAND_STOP = 1;
    internal const int REMOTE_COMMAND_PAUSE = 2;
    internal const int REMOTE_COMMAND_RESUME = 3;
    internal const int REMOTE_COMMAND_CHANNEL = 4;
    internal const int REMOTE_COMMAND_TIME = 5;
    internal const int REMOTE_COMMAND_ID = 6;
    internal const int REMOTE_COMMAND_FILE = 7;
    internal const int REMOTE_COMMAND_TEXT = 8;
    #endregion

    //declare the standard properties
    private string m_Name;
    private string m_Author;
    private string m_Version;

    //all the events get declared here, do not change
    public override event OutGoingCommandHandler OnCommand;

    public override string Name { get { return m_Name; } }
    public override string Version { get { return m_Version; } }
    public override string Author { get { return m_Author; } }

    // Var for custom windows-message
    private int RWM_ColorizeNet = 0;

    private FormMsgPump msgPump = null;

    public Plugin()
    {
      //set your default values here
      m_Name = "YahCoLoRiZe Unicode Plugin";
      m_Author = "Dxzl, (type /chelp for help)";
      m_Version = "1.5";

      RWM_ColorizeNet = NativeMethods.RegisterWindowMessage(YAHCOLORIZE_SIGNITURE);

      // Need a message-pump to receive WM_COPY message from YahCoLoRiZe
      msgPump = new FormMsgPump(RWM_ColorizeNet);

      // Tell YahCoLoRiZe our window-handle and version...
      SendToYahCoLoRiZe(PopulateStruct(REMOTE_COMMAND_ID, m_Version));
    }

    public override void Dispose()
    {
      if (msgPump != null)
        msgPump.Close();
    }

    public override void Initialize()
    {
      // The GlobalNotifier class (see below) defines the OnDataReceived event which
      // is manually fired from FormMsgPump when the WM_COPYDATA message is
      // received from YahCoLoRiZe. Here we subscribe to the event...
      GlobalNotifier.DataReceived += new VoidEventHandler(GlobalNotifier_DataReceived);
    }

    #region Print Help Text

    private void PrintHelpText()
    {
      const char CTRL_B = '\x2'; // Bold on/off
      const char CTRL_C = '\x3'; // Color

      SendCommandToIceChat("/echo --------------------------------------------------------------");
      SendCommandToIceChat("/echo   YOU MUST BE RUNNING THE PROGRAM YahCoLoRiZe.exe!!!");
      SendCommandToIceChat("/echo   Visit www.YahCoLoRiZe.com to download YahCoLoRiZe");
      SendCommandToIceChat("/echo --------------------------------------------------------------");
      SendCommandToIceChat("/echo   Commands:");
      SendCommandToIceChat("/echo   " + CTRL_B + "/cchan" + CTRL_B + " #MyNewRoom => (sets the chat channel)");
      SendCommandToIceChat("/echo   " + CTRL_B + "/ctime 1000" + CTRL_B + " => (sets the play speed to 1 second/line)");
      SendCommandToIceChat("/echo   " + CTRL_B + "/cx HELLO!" + CTRL_B + " => (colorizes and sends HELLO! to #MyNewRoom)");
      SendCommandToIceChat("/echo   *note: you can type a number after /cx 0-19");
      SendCommandToIceChat("/echo   " + CTRL_B + "/cx 0 Hi!" + CTRL_B + " => (sends HI! with random text-effect applied)");
      SendCommandToIceChat("/echo   " + CTRL_B + "/cx 7 Greetings!" + CTRL_B + " => (alternates colors...)");
      SendCommandToIceChat("/echo   You can process ASCII art or song-lyrics in YahCoLoRiZe");
      SendCommandToIceChat("/echo   and play it into a room remotely:");
      SendCommandToIceChat("/echo   " + CTRL_B + "/cplay start" + CTRL_B + " => starts document playback into a room");
      SendCommandToIceChat("/echo   " + CTRL_B + "/cplay stop" + CTRL_B + " => stops document playback");
      SendCommandToIceChat("/echo   " + CTRL_B + "/cplay pause" + CTRL_B + " => pauses playback");
      SendCommandToIceChat("/echo   " + CTRL_B + "/cplay resume" + CTRL_B + " => resumes playback");
      SendCommandToIceChat("/echo --------------------------------------------------------------");
      SendCommandToIceChat("/echo   Use the \"Courier New\" font to get even text borders. Also,");
      SendCommandToIceChat("/echo   in YahCoLoRiZe, click the Client tab and select " +
          CTRL_C + "11I" + CTRL_C + "00c" + CTRL_C + "11e" + CTRL_C + "00C" + CTRL_C + "11h" +
          CTRL_C + "00a" + CTRL_C + "11t" + CTRL_C + ".");
      SendCommandToIceChat("/echo --------------------------------------------------------------");
    }

    //
    // This version is COOL! But the default font "Veranda" makes it look bad :(
    // We need a MONOSPACED font for YahCoLoRiZe such as "Courier New" :)
    //

    //private void PrintHelpText()
    //{
    //    const char CTRL_B = '\x2'; // Bold on/off
    //    const char CTRL_C = '\x3'; // Color

    //    SendCommandToIceChat("/echo " + CTRL_C + "00,02                                                                   " + CTRL_C);
    //    SendCommandToIceChat("/echo " + CTRL_C + "00,02  " + CTRL_C + "00,12 --------------------------------------------------------------" + CTRL_C + "00,02  " + CTRL_C);
    //    SendCommandToIceChat("/echo " + CTRL_C + "00,02  " + CTRL_C + "00,12 YOU MUST BE RUNNING THE PROGRAM YahCoLoRiZe.exe!!!            " + CTRL_C + "00,02  " + CTRL_C);
    //    SendCommandToIceChat("/echo " + CTRL_C + "00,02  " + CTRL_C + "00,12 Visit www.YahCoLoRiZe.com to download " + CTRL_C + "01,07Y" + CTRL_C + "07,01a" + CTRL_C + "01,07h" + CTRL_C + "07,01C" + CTRL_C + "01,07o" + CTRL_C + "07,01L" + CTRL_C + "01,07o" + CTRL_C + "07,01R" + CTRL_C + "01,07i" + CTRL_C + "07,01Z" + CTRL_C + "01,07e" + CTRL_C + "00,12.            " + CTRL_C + "00,02  " + CTRL_C);
    //    SendCommandToIceChat("/echo " + CTRL_C + "00,02  " + CTRL_C + "00,12 --------------------------------------------------------------" + CTRL_C + "00,02  " + CTRL_C);
    //    SendCommandToIceChat("/echo " + CTRL_C + "00,02  " + CTRL_C + "00,12  Commands:                                                    " + CTRL_C + "00,02  " + CTRL_C);
    //    SendCommandToIceChat("/echo " + CTRL_C + "00,02  " + CTRL_C + "00,12  " + CTRL_B + "/cchan #MyNewRoom" + CTRL_B + " => (sets the chat channel)                 " + CTRL_C + "00,02  " + CTRL_C);
    //    SendCommandToIceChat("/echo " + CTRL_C + "00,02  " + CTRL_C + "00,12  " + CTRL_B + "/ctime 1000" + CTRL_B + " => (sets the play speed to 1 second/line)        " + CTRL_C + "00,02  " + CTRL_C);
    //    SendCommandToIceChat("/echo " + CTRL_C + "00,02  " + CTRL_C + "00,12  " + CTRL_B + "/cx HELLO!" + CTRL_B + " => (colorizes and sends HELLO! to #MyNewRoom)  " + CTRL_C + "00,02  " + CTRL_C);
    //    SendCommandToIceChat("/echo " + CTRL_C + "00,02  " + CTRL_C + "00,12  *note: you can type a number after /cx 0-19               " + CTRL_C + "00,02  " + CTRL_C);
    //    SendCommandToIceChat("/echo " + CTRL_C + "00,02  " + CTRL_C + "00,12  " + CTRL_B + "/cx 0 Hi" + CTRL_B + "! => (sends HI! with random text-effect applied)  " + CTRL_C + "00,02  " + CTRL_C);
    //    SendCommandToIceChat("/echo " + CTRL_C + "00,02  " + CTRL_C + "00,12  " + CTRL_B + "/cx 7 Greetings!" + CTRL_B + " => (alternates colors...)                " + CTRL_C + "00,02  " + CTRL_C);
    //    SendCommandToIceChat("/echo " + CTRL_C + "00,02  " + CTRL_C + "00,12  You can process ASCII art or song-lyrics in YahCoLoRiZe      " + CTRL_C + "00,02  " + CTRL_C);
    //    SendCommandToIceChat("/echo " + CTRL_C + "00,02  " + CTRL_C + "00,12  and play it into a room remotely:                            " + CTRL_C + "00,02  " + CTRL_C);
    //    SendCommandToIceChat("/echo " + CTRL_C + "00,02  " + CTRL_C + "00,12  " + CTRL_B + "/cplay" + CTRL_B + " => starts document playback into a room               " + CTRL_C + "00,02  " + CTRL_C);
    //    SendCommandToIceChat("/echo " + CTRL_C + "00,02  " + CTRL_C + "00,12  " + CTRL_B + "/cstop" + CTRL_B + " => stops document playback                            " + CTRL_C + "00,02  " + CTRL_C);
    //    SendCommandToIceChat("/echo " + CTRL_C + "00,02  " + CTRL_C + "00,12  " + CTRL_B + "/cpause" + CTRL_B + " => pauses playback                                   " + CTRL_C + "00,02  " + CTRL_C);
    //    SendCommandToIceChat("/echo " + CTRL_C + "00,02  " + CTRL_C + "00,12  " + CTRL_B + "/cresume" + CTRL_B + " => resumes playback                                 " + CTRL_C + "00,02  " + CTRL_C);
    //    SendCommandToIceChat("/echo " + CTRL_C + "00,02  " + CTRL_C + "00,12 --------------------------------------------------------------" + CTRL_C + "00,02  " + CTRL_C);
    //    SendCommandToIceChat("/echo " + CTRL_C + "00,02  " + CTRL_C + "00,12 YahCoLoRiZe is designed for the " + CTRL_B + "Courier New" + CTRL_B + " font...           " + CTRL_C + "00,02  " + CTRL_C);
    //    SendCommandToIceChat("/echo " + CTRL_C + "00,02  " + CTRL_C + "00,12 (In YahCoLoRiZe, click the Client Tab and select " + CTRL_C + "12,00I" + CTRL_C + "00,12c" + CTRL_C + "12,00e" + CTRL_C + "00,12C" + CTRL_C + "12,00h" + CTRL_C + "00,12a" + CTRL_C + "12,00t" + CTRL_C + "00,12)     " + CTRL_C + "00,02  " + CTRL_C);
    //    SendCommandToIceChat("/echo " + CTRL_C + "00,02  " + CTRL_C + "00,12 --------------------------------------------------------------" + CTRL_C + "00,02  " + CTRL_C);
    //    SendCommandToIceChat("/echo " + CTRL_C + "00,02                                                                   " + CTRL_C);
    //}

    // Examples of using /cx:
    // /cx I AM SHOUTING!!!!! => (processes text in YahCoLoRiZe and plays it to /chan)
    // /cx 0 Hello World! => (applys a random text-effect)
    // /cx 1 Hello World! => (applys random foreground color)
    // /cx 2 Hello World! => (applys random background color)
    // /cx 3 Hello World! => (applys random foreground and background color)
    // ...4 => increment fg color
    // ...5 => increment bg color
    // ...6 => increment fg/bg color
    // ...7 => alternate fg color
    // ...8 => alternate bg color
    // ...9 => alternate fg/bg color
    // ...10 => alternate underline fg color
    // ...11 => alternate underline random color
    // ...12 => alternate underline bold
    // ...13 => alternate underline italics (or inverse video in some clients)
    // ...14 => alternate underline bold/italics
    // ...15 => alternate underline bold/color
    // ...16 => alternate italics/color
    // ...17 => alternate color
    // ...18 => (spare)
    // ...19 => (spare)
    #endregion

    #region InputText Override

    // Intercept user's typed command...
    public override PluginArgs InputText(PluginArgs args)
    {
      if (string.IsNullOrEmpty(args.Command))
        return args;

      string saveCommand = args.Command; // Save to return to other plugins intact!

      string command = saveCommand.Trim().ToLower(); // Version of "Command" we will work with...

      switch (command)
      {
        case C_HELP:

          PrintHelpText();
        saveCommand = string.Empty; // Clear command-string if we handled it...
          break;

        default:

          if (ProcessParameterizedCommands(command, args))
          saveCommand = string.Empty; // Clear command-string if we handled it...
          break;
      }

      args.Command = saveCommand;
      return args;
    }

    private bool ProcessParameterizedCommands(string command, PluginArgs args)
    // Returns true if we handled the command; otherwise, false.
    {
      if (string.IsNullOrEmpty(command) || args == null)
        return false;

      // /CX my line of text
      if (command.Length > C_SEND.Length + 1 && command.Substring(0, C_SEND.Length + 1).ToLower() == C_SEND + " ")
      {
        command = command.Substring(C_SEND.Length + 1, command.Length - (C_SEND.Length + 1));

        // Send to YahCoLoRiZe for processing...
        SendToYahCoLoRiZe(PopulateStruct(command, args));
        return true;
      }

      // /CCHAN #NewRoom
      if (command.Length > C_CHAN.Length + 1 && command.Substring(0, C_CHAN.Length + 1).ToLower() == C_CHAN + " ")
      {
        command = command.Substring(C_CHAN.Length + 1, command.Length - (C_CHAN.Length + 1));
        SendToYahCoLoRiZe(PopulateStruct(REMOTE_COMMAND_CHANNEL, command));
        return true;
      }

      // /CTIME 3000
      if (command.Length > C_TIME.Length + 1 && command.Substring(0, C_TIME.Length + 1).ToLower() == C_TIME + " ")
      {
        command = command.Substring(C_TIME.Length + 1, command.Length - (C_TIME.Length + 1));

        SendToYahCoLoRiZe(PopulateStruct(REMOTE_COMMAND_TIME, command));
        return true;
      }

      // /CPLAY start|stop|pause|resume or file-path
      if (command.Length > C_PLAY.Length + 1 && command.Substring(0, C_PLAY.Length + 1).ToLower() == C_PLAY + " ")
      {
        // Get the rest of the /cplay command and trim it
        command = command.Substring(C_PLAY.Length + 1, command.Length - (C_PLAY.Length + 1)).Trim();

        if (command == A_START)
          SendToYahCoLoRiZe(PopulateStruct(REMOTE_COMMAND_START));
        else if (command == A_STOP)
          SendToYahCoLoRiZe(PopulateStruct(REMOTE_COMMAND_STOP));
        else if (command == A_PAUSE)
          SendToYahCoLoRiZe(PopulateStruct(REMOTE_COMMAND_PAUSE));
        else if (command == A_RESUME)
          SendToYahCoLoRiZe(PopulateStruct(REMOTE_COMMAND_RESUME));
        else
          SendToYahCoLoRiZe(PopulateStruct(REMOTE_COMMAND_FILE, command));
        return true;
      }

      return false;
    }

    #endregion

    #region Populate Outgoing COLORIZENETSTRUCT

    // Send remote play, stop, pause or resume
    public COLORIZENETSTRUCT PopulateStruct(int commandID)
    {
      return new COLORIZENETSTRUCT(commandID);
    }

    public COLORIZENETSTRUCT PopulateStruct(string sData, PluginArgs args)
    {
      COLORIZENETSTRUCT cs = PopulateStruct(REMOTE_COMMAND_TEXT, sData);

      string sChanNick;

      if (args != null && args.Connection != null)
      {
        // Get the channel or nickname who is sending the text
        sChanNick = args.Connection.Parse("$currentwindow"); // (Thanks for showing me this trick Paul!)
        cs.serverID = args.Connection.ServerSetting.ID;
      }
      else
      {
        sChanNick = "status"; // Tell YahCoLoRiZe to target the Console window
        cs.serverID = -1;
      }

      // Flag to YahCoLoRiZe that this text will be in UTF-8 instead of ANSI
      cs.commandData = 1;

      // Send the channel or nick to YahCoLoRiZe so it knows who to send text back to. YahCoLoRiZe
      // will return one or more "/msg <channel|nick> <text>" or "/echo <text>" commands to
      // FormMsgPump which will then dispatch them to IceChat with OnCommand().

      int len;

      // Trim the string in a loop until it fits our byte array...
      while ((len = Encoding.UTF8.GetByteCount(sChanNick)) > CNS_CHANNICKLEN - 1)
        sChanNick = sChanNick.Substring(0, sChanNick.Length - 1);

      // sIn, firstIndex, charCount, byteOut[], writeIndex
      cs.lenChanNick = Encoding.UTF8.GetBytes(sChanNick, 0, sChanNick.Length, cs.chanNick, 0);  // Convert C# string to a UTF-8 byte-array

      return cs;
    }

    // Set new channel or send the current version
    public COLORIZENETSTRUCT PopulateStruct(int commandID, string sData)
    {
      COLORIZENETSTRUCT cs = new COLORIZENETSTRUCT(commandID);

      int len;

      // Trim the string in a loop until it fits our byte array...
      while ((len = Encoding.UTF8.GetByteCount(sData)) > CNS_DATALEN-1)
        sData = sData.Substring(0, sData.Length-1);

      // sIn, firstIndex, charCount, byteOut[], writeIndex
      cs.lenData = Encoding.UTF8.GetBytes(sData, 0, sData.Length, cs.data, 0);  // Convert C# string to a UTF-8 byte-array
      cs.data[cs.lenData] = 0; // null utf-8 char

      return cs;
    }

    #endregion

    #region Send Input Control's Text To YahCoLoRiZe

    private bool SendToYahCoLoRiZe(COLORIZENETSTRUCT cns)
    {
      bool RetVal = false;
      IntPtr cnsMemory = IntPtr.Zero;
      IntPtr cdsMemory = IntPtr.Zero;
      int sizecns = Marshal.SizeOf(cns);

      try
      {
        // Allocate memory and move Struct to it
        cnsMemory = Marshal.AllocHGlobal(sizecns);

        // Note: On the first call to the StructureToPtr method after a memory block has been
        // allocated, fDeleteOld must be false, because there are no contents to clear.
        Marshal.StructureToPtr(cns, cnsMemory, false);

        // Populate CopyDataStruct
        COPYDATASTRUCT cds = new COPYDATASTRUCT();
        cds.cbData = sizecns;
        cds.lpData = cnsMemory;
        cds.dwData = (IntPtr)RWM_ColorizeNet;

        // Allocate memory and move CopyDataStruct to it
        cdsMemory = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(COPYDATASTRUCT)));

        // Note: On the first call to the StructureToPtr method after a memory block has been
        // allocated, fDeleteOld must be false, because there are no contents to clear.
        Marshal.StructureToPtr(cds, cdsMemory, false);

        // Find the YahCoLoRiZe main window (if app is running)...
        IntPtr WHnd = NativeMethods.FindWindow(YAHCOLORIZE_CLASSNAME, YAHCOLORIZE_WINDOWNAME);
        if (WHnd.Equals(System.IntPtr.Zero)) // Try the class-name alone...
          WHnd = NativeMethods.FindWindow(YAHCOLORIZE_CLASSNAME, null);
        if (WHnd.Equals(System.IntPtr.Zero)) // Try the window-name alone...
          WHnd = NativeMethods.FindWindow(null, YAHCOLORIZE_WINDOWNAME);

        // Send the message to target 
        if (!WHnd.Equals(System.IntPtr.Zero))
        {

          const uint TIMEOUT_INTERVAL = 5000; // 5 seconds

          //fuFlags:
          const uint SMTO_NORMAL = 0x0000; //Abort after specified timeout
          //const uint SMTO_BLOCK = 0x0001; //Prevents the calling thread from processing any other requests until the function returns.
          //const uint SMTO_ABORTIFHUNG = 0x0002; //The function returns without waiting for the time-out period to elapse if the receiving thread appears to not respond or "hangs."
          //const uint SMTO_NOTIMEOUTIFNOTHUNG = 0x0008; //The function does not enforce the time-out period as long as the receiving thread is processing messages.
          //const uint SMTO_ERRORONEXIT = 0x0020; //The function should return 0 if the receiving window is destroyed or its owning thread dies while the message is being processed.

          // Send user's input text to YahCoLoRiZe via WM_COPYDATA and pass it our
          // "invisible" window-form's handle to reply back to...
          IntPtr ret = IntPtr.Zero;
          NativeMethods.SendMessageTimeout(WHnd, WM_COPYDATA, msgPump.Handle, cdsMemory,
                            SMTO_NORMAL, TIMEOUT_INTERVAL, ref ret);
        }

        RetVal = true;
      }

      finally
      {
        // Free allocated memory 
        if (cnsMemory != IntPtr.Zero) { try { Marshal.FreeHGlobal(cnsMemory); } catch { } }
        if (cdsMemory != IntPtr.Zero) { try { Marshal.FreeHGlobal(cdsMemory); } catch { } }
      }

      return RetVal;
    }

    #endregion

    #region Send Command To IceChat

    void SendCommandToIceChat(string s)
    {
      PluginArgs args = new PluginArgs();

      // Tell OnCommand handler to use the current connection...
      args.Extra = "current";
      args.Command = s;

      // We have the data from YahCoLoRiZe formatted as a command.
      // Here we sent it to IceChat for handling...
      if (OnCommand != null)
        OnCommand(args);
    }
    #endregion

    #region Incoming Data Event Handler

    // Called when data has been received from YahCoLoRiZe from the message-loop in FormMsgPump
    void GlobalNotifier_DataReceived(COLORIZENETSTRUCT cns)
    {
      if (cns.data != null && cns.lenData > 0 && cns.clientID == ICECHAT_ID)
      {
        string newStr;

        int len = cns.lenData;

        if (len > CNS_DATALEN)
          len = CNS_DATALEN;

        if (cns.commandData == 1)
          newStr = Encoding.UTF8.GetString(cns.data, 0, len);
        else
          newStr = Encoding.Default.GetString(cns.data, 0, len);

        SendCommandToIceChat(newStr);
      }
    }
    #endregion

    #region Native API Signatures and Types

    /// <summary>
    /// An application sends the WM_COPYDATA message to pass data to another 
    /// application
    /// </summary>
    internal const int WM_COPYDATA = 0x004A;

    /// <summary>
    /// The COPYDATASTRUCT structure contains data to be passed to another 
    /// application by the WM_COPYDATA message. 
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct COPYDATASTRUCT
    {
      public IntPtr dwData;
      public int cbData;
      public IntPtr lpData;
    }

    /// <summary>
    /// The class exposes Windows APIs to be used in this code sample.
    /// </summary>
    [SuppressUnmanagedCodeSecurity]
    internal class NativeMethods
    {
      public NativeMethods() { }

      /// <summary>
      /// Sends the specified message to a window or windows. The SendMessage 
      /// function calls the window procedure for the specified window and does 
      /// not return until the window procedure has processed the message. 
      /// </summary>
      /// <param name="hWnd">
      /// Handle to the window whose window procedure will receive the message.
      /// </param>
      /// <param name="Msg">Specifies the message to be sent.</param>
      /// <param name="wParam">
      /// Specifies additional message-specific information.
      /// </param>
      /// <param name="lParam">
      /// Specifies additional message-specific information.
      /// </param>
      /// <returns></returns>
      [DllImport("user32.dll", CharSet = CharSet.Ansi)]
      public static extern int SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

      [DllImport("User32.dll")]
      public static extern int SendMessageTimeout(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam, uint fuFlags, uint timeout, ref IntPtr ret);

      /// <summary>
      /// The FindWindow function retrieves a handle to the top-level window 
      /// whose class name and window name match the specified strings. This 
      /// function does not search child windows. This function does not 
      /// perform a case-sensitive search.
      /// </summary>
      /// <param name="lpClassName">Class name</param>
      /// <param name="lpWindowName">Window caption</param>
      /// <returns></returns>
      [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
      public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

      [DllImport("user32.dll")]
      public extern static int RegisterWindowMessage(string lpString);
    }

    // Struct passed to and from YahCoLoRiZe via COPYDATASTRUCT
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct COLORIZENETSTRUCT
    {
      [MarshalAs(UnmanagedType.I8)]
      public Int64 lspare;
      [MarshalAs(UnmanagedType.I4)]
      public Int32 clientID;
      [MarshalAs(UnmanagedType.I4)]
      public Int32 commandID;
      [MarshalAs(UnmanagedType.I4)]
      public Int32 commandData;
      [MarshalAs(UnmanagedType.I4)]
      public Int32 serverID;
      [MarshalAs(UnmanagedType.I4)]
      public Int32 channelID;
      [MarshalAs(UnmanagedType.I4)]
      public Int32 lenChanNick;
      [MarshalAs(UnmanagedType.I4)]
      public Int32 lenData;
      [MarshalAs(UnmanagedType.ByValArray, SizeConst = CNS_CHANNICKLEN)]
      public byte[] chanNick;
      [MarshalAs(UnmanagedType.ByValArray, SizeConst = CNS_DATALEN)]
      public byte[] data;

      public COLORIZENETSTRUCT(int commandID)
      {
        this.commandID = commandID;
        commandData = -1;
        lspare = 0;
        clientID = ICECHAT_ID;
        channelID = -1;
        serverID = -1;
        lenData = 0;
        lenChanNick = 0;
        chanNick = new byte[CNS_CHANNICKLEN];
        data = new byte[CNS_DATALEN];
      }
    }

    #endregion
  }

  #endregion

  #region Global Notifier Class (lets Plugin class know when FormMsgPump got data...)

  public delegate void VoidEventHandler(Plugin.COLORIZENETSTRUCT cns);

  public static class GlobalNotifier
  {

    public static event VoidEventHandler DataReceived;

    public static void OnDataReceived(Plugin.COLORIZENETSTRUCT cns)
    {
      if (DataReceived != null)
        DataReceived(cns);
    }
  }
  #endregion
}
