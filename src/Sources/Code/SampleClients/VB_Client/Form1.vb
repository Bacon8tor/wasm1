Imports System.Runtime.InteropServices
Imports System.Threading
Imports Microsoft.FlightSimulator.SimConnect

Public Class Form1

    ' User-defined win32 event
    Private Const WM_USER_SIMCONNECT As Integer = &H402

    Private Const WASM_SIM_PREFIX As String = "MobiFlight"
    Private Const dwSizeOfDouble As UInteger = 8

    Private Const clientDataName As String = "CUST_VAR_DATA"

    'This Is the name that Is used to register the special event used to trigger the 
    'SimVar reading code in the WASM module.  It must exactly match the name used by
    'the WASM module.
    Private Const clientDataEventName As String = "MobiFlight.CUST_VAR"

    Enum ENUM_VARIOUS
        objectID = 0
        DefaultGroup = 1
        ClientDataID = 2
        DEFINITION_1 = 12

        'This is the ID used for the special triggering event.
        'It must be arranged that this ID does Not conflict with any of those 
        'loaded from the file events.txt, in other words this must be 
        'larger than the maximum number of possible entries in this file.
        WASM_VAR_DATA_REQUEST = 5000
    End Enum

    Private wasmEvents As New Dictionary(Of Integer, String)
    Private wasmEventNames As New Dictionary(Of String, Integer)
    Private wasmVars As New Dictionary(Of Integer, String)
    Private wasmVarNames As New Dictionary(Of String, Integer)

    'SimConnect object
    Dim simconnect As SimConnect = Nothing

    Enum MY_EVENTS
        PITOT_TOGGLE = 3000 'Avoid clash with events loaded from file
        FLAPS_INC
        FLAPS_DEC
        FLAPS_UP
        FLAPS_DOWN
    End Enum

    Public Sub New()
        ' This call is required by the designer.
        InitializeComponent()

        ' Add any initialization after the InitializeComponent() call.
        loadWASM_Events()
        loadWASM_Vars()

        setButtons(True, False)

    End Sub

    'Simconnect client will send a win32 message when there Is
    'a packet to process. ReceiveMessage must be called to
    'trigger the events. This model keeps simconnect processing on the main thread.
    Protected Overrides Sub DefWndProc(ByRef m As Message)

        If m.Msg = WM_USER_SIMCONNECT Then
            If simconnect IsNot Nothing Then
                simconnect.ReceiveMessage()
            End If
        Else
            MyBase.DefWndProc(m)
        End If

    End Sub

    Private Sub loadWASM_Vars()
        'The data parameter passed with the special event for triggering the WASM custom SimVar code
        'must excatly match the index of the particular SimVar that Is being queried.
        'The easiest way to do this Is to load the SimVars from the exact same file that the WASM module
        'loads them from.

        Dim iNdx As Integer = 0
        Dim sr = New IO.StreamReader("wasm_vars.txt")
        Do While Not sr.EndOfStream
            Dim line = sr.ReadLine()
            If Not line.StartsWith("//") Then
                Dim tokens() As String = line.Split("#"c)
                If tokens.Length <> 2 Then Throw New Exception("Invalid WASM_Var definition: " & line)
                Dim svName = WASM_SIM_PREFIX & "." & tokens(0)

                wasmVars.Add(iNdx, svName)
                wasmVarNames.Add(svName, iNdx)

                iNdx += 1
            End If
        Loop
        sr.Close()

    End Sub

    Private Sub loadWASM_Events()

        Dim iNdx As Integer = 0
        Dim sr = New IO.StreamReader("events.txt")
        Do While Not sr.EndOfStream
            Dim line = sr.ReadLine()
            If Not line.StartsWith("//") Then
                Dim tokens() As String = line.Split("#"c)
                If tokens.Length <> 2 Then Throw New Exception("Invalid WASM_Event definition: " & line)
                Dim evName = WASM_SIM_PREFIX & "." & tokens(0)

                If Not wasmEventNames.ContainsKey(evName) Then
                    wasmEvents.Add(iNdx, evName)
                    wasmEventNames.Add(evName, iNdx)
                End If

                iNdx += 1
            End If
        Loop
        sr.Close()

    End Sub

    Private Sub RegisterEvents()

        For Each key In wasmEvents.Keys
            simconnect.MapClientEventToSimEvent(CType(key, ENUM_VARIOUS), wasmEvents(key))
            simconnect.AddClientEventToNotificationGroup(ENUM_VARIOUS.DefaultGroup, CType(key, ENUM_VARIOUS), False)
        Next

        simconnect.MapClientEventToSimEvent(ENUM_VARIOUS.WASM_VAR_DATA_REQUEST, clientDataEventName)
        simconnect.AddClientEventToNotificationGroup(ENUM_VARIOUS.DefaultGroup, ENUM_VARIOUS.WASM_VAR_DATA_REQUEST, False)

    End Sub

    Private Sub RegisterVars()

        'Map an ID to the Client Data Area.
        simconnect.MapClientDataNameToID(clientDataName, ENUM_VARIOUS.ClientDataID)

        For Each key In wasmVars.Keys
            Dim offs As UInteger = key * dwSizeOfDouble

            'Add a double to the data definition.
            simconnect.AddToClientDataDefinition(CType(key, ENUM_VARIOUS), offs, dwSizeOfDouble, 0, 0)

            'This call is required to work around a bug in the Managed SimConnect library which does not size Client data correctly
            'as detailed in this forum post https://www.fsdeveloper.com/forum/threads/client-data-area-problems.13182/#post-154889
            simconnect.RegisterStruct(Of SIMCONNECT_RECV_CLIENT_DATA, Double)(CType(key, ENUM_VARIOUS))
        Next

    End Sub

    Private Sub transmitEvent(ByVal eventName As String, Optional ByVal eventData As Integer = 0)

        If simconnect IsNot Nothing Then
            If Not wasmEventNames.ContainsKey(eventName) Then
                displayText("Invalid event: " & eventName)
                Exit Sub
            End If

            Dim evtIndex As UInteger = wasmEventNames(eventName)

            displayText("Transmitting WASM event [" & eventName & "]")
            simconnect.TransmitClientEvent(ENUM_VARIOUS.objectID, CType(evtIndex, ENUM_VARIOUS), CType(eventData, UInteger), ENUM_VARIOUS.DefaultGroup, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY)
        End If

    End Sub

    Private Sub requestSimVarData(ByVal varName As String)

        If simconnect IsNot Nothing Then
            If Not wasmVarNames.ContainsKey(varName) Then
                displayText("Invalid event: " & varName)
                Exit Sub
            End If

            Dim varIndex = wasmVarNames(varName)

            displayText("Transmitting data request for WASM_SimVar [" & varName & "], " & varIndex)
            simconnect.TransmitClientEvent(ENUM_VARIOUS.objectID, ENUM_VARIOUS.WASM_VAR_DATA_REQUEST, varIndex, ENUM_VARIOUS.DefaultGroup, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY)
            Thread.Sleep(50) 'Give the data some time to rattle around in the sim before going to look for it 
            simconnect.RequestClientData(ENUM_VARIOUS.ClientDataID, CType(varIndex, ENUM_VARIOUS), CType(varIndex, ENUM_VARIOUS), SIMCONNECT_CLIENT_DATA_PERIOD.ONCE, SIMCONNECT_CLIENT_DATA_REQUEST_FLAG.DEFAULT, 0, 0, 0)
        End If

    End Sub

    Private Sub setButtons(ByVal bConnect As Boolean, ByVal bDisconnect As Boolean)

        btnConnect.Enabled = bConnect
        btnDisconnect.Enabled = bDisconnect

    End Sub

    Private Sub closeConnection()

        If simconnect IsNot Nothing Then
            'Dispose serves the same purpose as SimConnect_Close()
            simconnect.Dispose()
            simconnect = Nothing
            displayText("Connection closed")

        End If

    End Sub

    'Set up all the SimConnect related event handlers
    Private Sub initClientEvent()

        Try
            'listen to connect And quit msgs
            AddHandler simconnect.OnRecvOpen, AddressOf simconnect_OnRecvOpen
            AddHandler simconnect.OnRecvQuit, AddressOf simconnect_OnRecvQuit

            'listen to exceptions
            AddHandler simconnect.OnRecvException, AddressOf simconnect_OnRecvException

            'listen to events
            AddHandler simconnect.OnRecvEvent, AddressOf simconnect_OnRecvEvent

            'listen to client data events
            AddHandler simconnect.OnRecvClientData, AddressOf handleRecvClientDataEvent

            'subscribe to pitot heat switch toggle
            simconnect.MapClientEventToSimEvent(MY_EVENTS.PITOT_TOGGLE, "PITOT_HEAT_TOGGLE")
            simconnect.AddClientEventToNotificationGroup(ENUM_VARIOUS.DefaultGroup, MY_EVENTS.PITOT_TOGGLE, False)

            'subscribe to all four flaps controls
            simconnect.MapClientEventToSimEvent(MY_EVENTS.FLAPS_UP, "FLAPS_UP")
            simconnect.AddClientEventToNotificationGroup(ENUM_VARIOUS.DefaultGroup, MY_EVENTS.FLAPS_UP, False)
            simconnect.MapClientEventToSimEvent(MY_EVENTS.FLAPS_DOWN, "FLAPS_DOWN")
            simconnect.AddClientEventToNotificationGroup(ENUM_VARIOUS.DefaultGroup, MY_EVENTS.FLAPS_DOWN, False)
            simconnect.MapClientEventToSimEvent(MY_EVENTS.FLAPS_INC, "FLAPS_INCR")
            simconnect.AddClientEventToNotificationGroup(ENUM_VARIOUS.DefaultGroup, MY_EVENTS.FLAPS_INC, False)
            simconnect.MapClientEventToSimEvent(MY_EVENTS.FLAPS_DEC, "FLAPS_DECR")
            simconnect.AddClientEventToNotificationGroup(ENUM_VARIOUS.DefaultGroup, MY_EVENTS.FLAPS_DEC, False)

            RegisterEvents()
            RegisterVars()

            'set the group priority
            simconnect.SetNotificationGroupPriority(ENUM_VARIOUS.DefaultGroup, SimConnect.SIMCONNECT_GROUP_PRIORITY_HIGHEST)
        Catch ex As COMException
            displayText(ex.Message)
        End Try

    End Sub

    Private Sub handleRecvClientDataEvent(sender As SimConnect, data As SIMCONNECT_RECV_CLIENT_DATA)

        Dim d As Double = CType(data.dwData(0), Double)
        Debug.WriteLine("handleRecvClientDataEvent(): " & data.dwRequestID & ", " & data.dwDefineID & ", " & data.dwID & ", " & d)
        displayText("Received Client Data [" & wasmVars(data.dwDefineID) & "]: " & d)

    End Sub

    Sub simconnect_OnRecvOpen(ByVal sender As SimConnect, ByVal data As SIMCONNECT_RECV_OPEN)
        displayText("Connected to MSFS")
    End Sub

    'The case where the user closes MSFS
    Sub simconnect_OnRecvQuit(ByVal sender As SimConnect, ByVal data As SIMCONNECT_RECV)

        displayText("MSFS has exited")
        closeConnection()

    End Sub

    Sub simconnect_OnRecvException(ByVal sender As SimConnect, ByVal data As SIMCONNECT_RECV_EXCEPTION)
        displayText("Exception received: " + data.dwException.ToString)
    End Sub

    Sub simconnect_OnRecvEvent(ByVal sender As SimConnect, ByVal recEvent As SIMCONNECT_RECV_EVENT)

        Select Case recEvent.uEventID
            Case MY_EVENTS.PITOT_TOGGLE
                displayText("PITOT switched")
            Case MY_EVENTS.FLAPS_UP
                displayText("Flaps Up")
            Case MY_EVENTS.FLAPS_DOWN
                displayText("Flaps Down")
            Case MY_EVENTS.FLAPS_INC
                displayText("Flaps Inc")
            Case MY_EVENTS.FLAPS_DEC
                displayText("Flaps Dec")
        End Select

    End Sub

    'Response number
    Private response As Integer = 1

    Sub displayText(ByVal s As String)

        Static counter As Integer = 0

        richResponse.AppendText(counter & ". " & s & vbNewLine)
        richResponse.ScrollToCaret()

        counter += 1

    End Sub

    Private Sub btnConnect_Click(sender As Object, e As EventArgs) Handles btnConnect.Click

        If simconnect Is Nothing Then
            Try
                'the constructor Is similar to SimConnect_Open in the native API
                simconnect = New SimConnect("Managed Client Events", Me.Handle, WM_USER_SIMCONNECT, Nothing, 0)
                initClientEvent()

                setButtons(False, True)
            Catch ex As COMException
                displayText("Unable to connect to MSFS " + ex.Message)
            End Try
        Else
            displayText("Error - try again")
            closeConnection()

            setButtons(True, False)
        End If

    End Sub

    Private Sub btnDisconnect_Click(sender As Object, e As EventArgs) Handles btnDisconnect.Click

        closeConnection()
        setButtons(True, False)

    End Sub

    Private Sub Form1_FormClosed(sender As Object, e As FormClosedEventArgs) Handles MyBase.FormClosed

        'The case where the user closes the client
        closeConnection()

    End Sub

    Private Sub btnRequestData_Click(sender As Object, e As EventArgs) Handles btnRequestData.Click

        requestSimVarData("MobiFlight.DA62_ICE_LIGHT_MAX_STATE_ENABLED")
        requestSimVarData("MobiFlight.DA62_DEICE_PUMP")
        requestSimVarData("MobiFlight.DA62_IceLightState")
    End Sub

    Private Sub btnSendEvent_Click(sender As Object, e As EventArgs) Handles btnSendEvent.Click


        transmitEvent("MobiFlight.AS1000_PFD_SOFTKEYS_6", -10)
        'transmitEvent("AXIS_RUDDER_SET", -16000)

    End Sub

End Class
