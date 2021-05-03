using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using Microsoft.FlightSimulator.SimConnect;

namespace CS_Client2
{
    public partial class Form1 : Form
    {
        // User-defined win32 event
        const int WM_USER_SIMCONNECT = 0x0402;

        // SimConnect object
        SimConnect simconnect = null;

        const String WASM_SIM_PREFIX = "MobiFlight";
        const uint dwSizeOfDouble = 8;

        const string clientDataName = "CUST_VAR_DATA";

        //This is the name that is used to register the special event used to trigger the 
        //SimVar reading code in the WASM module.  It must exactly match the name used by
        //the WASM module.
        const string clientDataEventName = "MobiFlight.CUST_VAR";

        enum ENUM_VARIOUS
        {
            objectID = 0,
            DefaultGroup = 1,
            ClientDataID = 2,
            DEFINITION_1 = 12,

            //This is the ID used for the special triggering event.
            //It must be arranged that this ID does not conflict with any of those 
            //loaded from the file events.txt, in other words this must be 
            //larger than the maximum number of possible entries in this file.
            WASM_VAR_DATA_REQUEST = 5000
        }

        private Dictionary<int, string> wasmEvents = new Dictionary<int, String>();
        private Dictionary<string, int> wasmEventNames = new Dictionary<String, int>();
        private Dictionary<int, string> wasmVars = new Dictionary<int, String>();
        private Dictionary<string, int> wasmVarNames = new Dictionary<String, int>();

        enum EVENTS
        {
            PITOT_TOGGLE,
            FLAPS_INC,
            FLAPS_DEC,
            FLAPS_UP,
            FLAPS_DOWN,
        };

        public Form1()
        {
            InitializeComponent();

            loadWASM_Events();
            loadWASM_Vars();

            setButtons(true, false);
        }

        // Simconnect client will send a win32 message when there is
        // a packet to process. ReceiveMessage must be called to
        // trigger the events. This model keeps simconnect processing on the main thread.
        protected override void DefWndProc(ref Message m)
        {
            if (m.Msg == WM_USER_SIMCONNECT)
            {
                if (simconnect != null)
                {
                    simconnect.ReceiveMessage();
                }
            }
            else
            {
                base.DefWndProc(ref m);
            }
        }

        private void loadWASM_Vars()
        {
            //The data parameter passed with the special event for triggering the WASM custom SimVar code
            //must excatly match the index of the particular SimVar that is being queried.
            //The easiest way to do this is to load the SimVars from the exact same file that the WASM module
            //loads them from.

            int iNdx = 0;
            StreamReader sr = new StreamReader("wasm_vars.txt");

            while (!sr.EndOfStream)
            {
                var line = sr.ReadLine();
                if (!line.StartsWith("//"))
                {
                    string[] tokens = line.Split('#');
                    if (tokens.Length != 2) throw new Exception("Invalid WASM_Var definition: " + line);

                    var svName = WASM_SIM_PREFIX + "." + tokens[0];

                    wasmVars.Add(iNdx, svName);
                    wasmVarNames.Add(svName, iNdx);

                    iNdx++;
                }
            }
            sr.Close();
        }

        private void loadWASM_Events()
        {
            int iNdx = 0;
            StreamReader sr = new StreamReader("events.txt");

            while (!sr.EndOfStream)
            {
                var line = sr.ReadLine();
                if (!line.StartsWith("//"))
                {
                    string[] tokens = line.Split('#');
                    if (tokens.Length != 2) throw new Exception("Invalid WASM_Event definition: " + line);

                    var evName = WASM_SIM_PREFIX + "." + tokens[0];

                    if (!wasmEventNames.ContainsKey(evName))
                    {
                        wasmEvents.Add(iNdx, evName);
                        wasmEventNames.Add(evName, iNdx);
                    }

                    iNdx++;
                }
            }
            sr.Close();
        }

        private void RegisterEvents()
        {
            foreach (int key in wasmEvents.Keys)
            {
                simconnect.MapClientEventToSimEvent((ENUM_VARIOUS)key, wasmEvents[key]);
                simconnect.AddClientEventToNotificationGroup(ENUM_VARIOUS.DefaultGroup, (ENUM_VARIOUS)key, false);
            }

            simconnect.MapClientEventToSimEvent(ENUM_VARIOUS.WASM_VAR_DATA_REQUEST, clientDataEventName);
            simconnect.AddClientEventToNotificationGroup(ENUM_VARIOUS.DefaultGroup, ENUM_VARIOUS.WASM_VAR_DATA_REQUEST, false);
        }

        private void RegisterVars()
        {
            //Map an ID to the Client Data Area.
            simconnect.MapClientDataNameToID(clientDataName, ENUM_VARIOUS.ClientDataID);

            foreach (int key in wasmVars.Keys)
            {
                uint offs = (uint)key * dwSizeOfDouble;

                //Add a double to the data definition.
                simconnect.AddToClientDataDefinition((ENUM_VARIOUS)key, offs, dwSizeOfDouble, 0, 0);

                //This call is required to work around a bug in the Managed SimConnect library which does not size Client data correctly
                //as detailed in this forum post https://www.fsdeveloper.com/forum/threads/client-data-area-problems.13182/#post-154889
                simconnect.RegisterStruct<SIMCONNECT_RECV_CLIENT_DATA, Double>((ENUM_VARIOUS)key);
            }
        }

        private void transmitEvent(string eventName)
        {
            if (simconnect != null)
            {
                if (!wasmEventNames.ContainsKey(eventName))
                {
                    displayText("Invalid event: " + eventName);
                    return;
                }

                uint evtIndex = (uint)wasmEventNames[eventName];

                displayText("Transmitting WASM event [" + eventName + "]");
                simconnect.TransmitClientEvent((uint)ENUM_VARIOUS.objectID, (ENUM_VARIOUS)evtIndex, 0, ENUM_VARIOUS.DefaultGroup, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
            }
        }

        private void requestSimVarData(string varName)
        {
            if (simconnect != null)
            {
                if (!wasmVarNames.ContainsKey(varName))
                {
                    displayText("Invalid event: " + varName);
                    return;
                }

                uint varIndex = (uint)wasmVarNames[varName];

                displayText("Transmitting data request for WASM_SimVar [" + varName + "], " + varIndex);
                simconnect.TransmitClientEvent((uint)ENUM_VARIOUS.objectID, ENUM_VARIOUS.WASM_VAR_DATA_REQUEST, varIndex, ENUM_VARIOUS.DefaultGroup, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                Thread.Sleep(50); //Give the data some time to rattle around in the sim before going to look for it
                simconnect.RequestClientData(ENUM_VARIOUS.ClientDataID, (ENUM_VARIOUS)varIndex, (ENUM_VARIOUS)varIndex, SIMCONNECT_CLIENT_DATA_PERIOD.ONCE, SIMCONNECT_CLIENT_DATA_REQUEST_FLAG.DEFAULT, 0, 0, 0);
            }
        }

        private void setButtons(bool bConnect, bool bDisconnect)
        {
            btnConnect.Enabled = bConnect;
            //buttonDisconnect.Enabled = bDisconnect;
        }

        private void closeConnection()
        {
            if (simconnect != null)
            {
                // Dispose serves the same purpose as SimConnect_Close()
                simconnect.Dispose();
                simconnect = null;
                displayText("Connection closed");
            }
        }

        // Set up all the SimConnect related event handlers
        private void initClientEvent()
        {
            try
            {
                // listen to connect and quit msgs
                simconnect.OnRecvOpen += new SimConnect.RecvOpenEventHandler(simconnect_OnRecvOpen);
                simconnect.OnRecvQuit += new SimConnect.RecvQuitEventHandler(simconnect_OnRecvQuit);

                // listen to exceptions
                simconnect.OnRecvException += new SimConnect.RecvExceptionEventHandler(simconnect_OnRecvException);

                // listen to events
                simconnect.OnRecvEvent += new SimConnect.RecvEventEventHandler(simconnect_OnRecvEvent);

                //listen to client data events
                simconnect.OnRecvClientData += new SimConnect.RecvClientDataEventHandler(simconnect_OnRecvClientData);

                // subscribe to pitot heat switch toggle
                simconnect.MapClientEventToSimEvent(EVENTS.PITOT_TOGGLE, "PITOT_HEAT_TOGGLE");
                simconnect.AddClientEventToNotificationGroup(ENUM_VARIOUS.DefaultGroup, EVENTS.PITOT_TOGGLE, false);

                // subscribe to all four flaps controls
                simconnect.MapClientEventToSimEvent(EVENTS.FLAPS_UP, "FLAPS_UP");
                simconnect.AddClientEventToNotificationGroup(ENUM_VARIOUS.DefaultGroup, EVENTS.FLAPS_UP, false);
                simconnect.MapClientEventToSimEvent(EVENTS.FLAPS_DOWN, "FLAPS_DOWN");
                simconnect.AddClientEventToNotificationGroup(ENUM_VARIOUS.DefaultGroup, EVENTS.FLAPS_DOWN, false);
                simconnect.MapClientEventToSimEvent(EVENTS.FLAPS_INC, "FLAPS_INCR");
                simconnect.AddClientEventToNotificationGroup(ENUM_VARIOUS.DefaultGroup, EVENTS.FLAPS_INC, false);
                simconnect.MapClientEventToSimEvent(EVENTS.FLAPS_DEC, "FLAPS_DECR");
                simconnect.AddClientEventToNotificationGroup(ENUM_VARIOUS.DefaultGroup, EVENTS.FLAPS_DEC, false);

                RegisterEvents();
                RegisterVars();

                // set the group priority
                simconnect.SetNotificationGroupPriority(ENUM_VARIOUS.DefaultGroup, SimConnect.SIMCONNECT_GROUP_PRIORITY_HIGHEST);
            }
            catch (COMException ex)
            {
                displayText(ex.Message);
            }
        }

        void simconnect_OnRecvClientData(SimConnect sender, SIMCONNECT_RECV_CLIENT_DATA data)
        {
            double d = (double)(data.dwData[0]);

            Debug.WriteLine("handleRecvClientDataEvent(): " + data.dwRequestID + ", " + data.dwDefineID + ", " + data.dwID + ", " + d);
            displayText("Received Client Data [" + wasmVars[(int)data.dwDefineID] + "]: " + d);
        }

        void simconnect_OnRecvOpen(SimConnect sender, SIMCONNECT_RECV_OPEN data)
        {
            displayText("Connected to MSFS");
        }

        // The case where the user closes MSFS
        void simconnect_OnRecvQuit(SimConnect sender, SIMCONNECT_RECV data)
        {
            displayText("MSFS has exited");
            closeConnection();
        }

        void simconnect_OnRecvException(SimConnect sender, SIMCONNECT_RECV_EXCEPTION data)
        {
            displayText("Exception received: " + data.dwException);
        }

        void simconnect_OnRecvEvent(SimConnect sender, SIMCONNECT_RECV_EVENT recEvent)
        {
            switch (recEvent.uEventID)
            {
                case (uint)EVENTS.PITOT_TOGGLE:

                    displayText("PITOT switched");
                    break;

                case (uint)EVENTS.FLAPS_UP:

                    displayText("Flaps Up");
                    break;

                case (uint)EVENTS.FLAPS_DOWN:

                    displayText("Flaps Down");
                    break;

                case (uint)EVENTS.FLAPS_INC:

                    displayText("Flaps Inc");
                    break;

                case (uint)EVENTS.FLAPS_DEC:

                    displayText("Flaps Dec");
                    break;
            }
        }

        // Response number
        int response = 1;

        // Output text - display a maximum of 10 lines
        string output = "\n\n\n\n\n\n\n\n\n\n";

        void displayText(string s)
        {
            // remove first string from output
            output = output.Substring(output.IndexOf("\n") + 1);

            // add the new string
            output += "\n" + response++ + ": " + s;

            // display it
            richResponse.Text = output;
        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            if (simconnect == null)
            {
                try
                {
                    // the constructor is similar to SimConnect_Open in the native API
                    simconnect = new SimConnect("Managed Client Events", this.Handle, WM_USER_SIMCONNECT, null, 0);

                    setButtons(false, true);

                    initClientEvent();

                }
                catch (COMException ex)
                {
                    displayText("Unable to connect to MSFS " + ex.Message);
                }
            }
            else
            {
                displayText("Error - try again");
                closeConnection();

                setButtons(true, false);
            }
        }

        private void btnDisconnect_Click(object sender, EventArgs e)
        {
            closeConnection();
            setButtons(true, false);
        }

        // The case where the user closes the client
        private void Form1_FormClosed_1(object sender, FormClosedEventArgs e)
        {
            closeConnection();
        }

        private void btnRequestData_Click(object sender, EventArgs e)
        {
            requestSimVarData("MobiFlight.DA62_ICE_LIGHT_MAX_STATE_ENABLED");
            requestSimVarData("MobiFlight.DA62_DEICE_PUMP");
        }

        private void btnSendEvent_Click(object sender, EventArgs e)
        {
            transmitEvent("MobiFlight.AS1000_PFD_SOFTKEYS_6");
        }
    }
}
