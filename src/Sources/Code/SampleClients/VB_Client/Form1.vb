Imports System.Runtime.InteropServices
Imports Microsoft.FlightSimulator.SimConnect

Public Class Form1


    ' User-defined win32 event
    Private Const WM_USER_SIMCONNECT As Integer = &H402

    'SimConnect object
    Dim simconnect As SimConnect = Nothing

    Enum MY_EVENTS
        PITOT_TOGGLE
        FLAPS_INC
        FLAPS_DEC
        FLAPS_UP
        FLAPS_DOWN
    End Enum

    Enum NOTIFICATION_GROUPS
        GROUP0
    End Enum

    Public Sub New()
        ' This call is required by the designer.
        InitializeComponent()

        ' Add any initialization after the InitializeComponent() call.
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

            'subscribe to pitot heat switch toggle
            simconnect.MapClientEventToSimEvent(MY_EVENTS.PITOT_TOGGLE, "PITOT_HEAT_TOGGLE")
            simconnect.AddClientEventToNotificationGroup(NOTIFICATION_GROUPS.GROUP0, MY_EVENTS.PITOT_TOGGLE, False)

            'subscribe to all four flaps controls
            simconnect.MapClientEventToSimEvent(MY_EVENTS.FLAPS_UP, "FLAPS_UP")
            simconnect.AddClientEventToNotificationGroup(NOTIFICATION_GROUPS.GROUP0, MY_EVENTS.FLAPS_UP, False)
            simconnect.MapClientEventToSimEvent(MY_EVENTS.FLAPS_DOWN, "FLAPS_DOWN")
            simconnect.AddClientEventToNotificationGroup(NOTIFICATION_GROUPS.GROUP0, MY_EVENTS.FLAPS_DOWN, False)
            simconnect.MapClientEventToSimEvent(MY_EVENTS.FLAPS_INC, "FLAPS_INCR")
            simconnect.AddClientEventToNotificationGroup(NOTIFICATION_GROUPS.GROUP0, MY_EVENTS.FLAPS_INC, False)
            simconnect.MapClientEventToSimEvent(MY_EVENTS.FLAPS_DEC, "FLAPS_DECR")
            simconnect.AddClientEventToNotificationGroup(NOTIFICATION_GROUPS.GROUP0, MY_EVENTS.FLAPS_DEC, False)

            'set the group priority
            simconnect.SetNotificationGroupPriority(NOTIFICATION_GROUPS.GROUP0, SimConnect.SIMCONNECT_GROUP_PRIORITY_HIGHEST)
        Catch ex As COMException
            displayText(ex.Message)
        End Try

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
        displayText("Exception received: " + data.dwException)
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

    'Output text - display a maximum of 10 lines
    Private output As String = "\n\n\n\n\n\n\n\n\n\n"

    Sub displayText(ByVal s As String)

        'remove first string from output
        output = output.Substring(output.IndexOf("\n") + 1)

        'add the New string
        output += "\n" + response + ++": " + s

        'display it
        richResponse.Text = output

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

End Class
