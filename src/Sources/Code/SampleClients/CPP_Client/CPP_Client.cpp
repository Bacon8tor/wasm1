#include <Windows.h>
#include "SimConnect.h"
#include <iostream>
#include <vector>
#include <string>
#include <fstream>
#include <map>
#include "..\..\MobiFlight.h"

#pragma region Declarations

HRESULT hr;
HANDLE hSimconnect;

// Reference to user aircraft.
SIMCONNECT_OBJECT_ID objectID = SIMCONNECT_OBJECT_ID_USER;

const char* FileEventsMobiFlight = "events.txt";
const char* FileVarsMobiFlight = "wasm_vars.txt";

std::map<std::string, int> CodeEventNames;
std::map<std::string, int> CodeVarNames;

const int INVALID_WASM_EVENT = -1;
const int INVALID_WASM_SIMVAR = -1;
SIMCONNECT_CLIENT_DATA_ID ClientDataID = 1;

static enum DATA_REQUEST_ID {
    //Must ensure that this ID does not conflict with any of those 
    //loaded from the file events.txt, in other words this must be 
    //larger than the maximum number of possible entries in this file.
    WASM_VAR_DATA_REQUEST = 5000,
};

#pragma endregion

void RegisterEvents() {

    HRESULT hr;

    DWORD eventID = 0;

	for (const auto& value : CodeEvents) {
		std::string eventCommand = value.second;
		std::string eventName = std::string(MobiFlightEventPrefix) + value.first;

        CodeEventNames.insert(std::make_pair(eventName, eventID));

		hr = SimConnect_MapClientEventToSimEvent(hSimconnect, eventID, eventName.c_str());
		hr = SimConnect_AddClientEventToNotificationGroup(hSimconnect, MOBIFLIGHT_GROUP::DEFAULT, eventID, false);

#if _DEBUG
		if (hr != S_OK) fprintf(stderr, "MobiFlight: Error on registering Event %s with ID %u for code %s", eventName.c_str(), eventID, eventCommand.c_str());
		//else fprintf(stderr, "MobiFlight: Success on registering Event %s with ID %u for code %s", eventName.c_str(), eventID, eventCommand.c_str());
#endif

		eventID++;
	}

    hr = SimConnect_MapClientEventToSimEvent(hSimconnect, WASM_VAR_DATA_REQUEST, clientDataEventName);
    hr = SimConnect_AddClientEventToNotificationGroup(hSimconnect, MOBIFLIGHT_GROUP::DEFAULT, WASM_VAR_DATA_REQUEST, false);
    
    SimConnect_SetNotificationGroupPriority(hSimconnect, MOBIFLIGHT_GROUP::DEFAULT, SIMCONNECT_GROUP_PRIORITY_HIGHEST);
}

void RegisterVars() {
	DWORD varID = 0;

	for (const auto& value : CodeVars) {
		std::string varCommand = value.second;
		std::string varName = std::string(MobiFlightEventPrefix) + value.first;

        CodeVarNames.insert(std::make_pair(varName, varID));

		DWORD offs = varID * dwSizeOfDouble;
		// Add a double to the data definition.
		SimConnect_AddToClientDataDefinition(hSimconnect, varID, offs, dwSizeOfDouble);
//		fprintf(stderr, "MobiFlight: SimConnect_AddToClientDataDefinition index: %u, offset: %u", varID, offs);

#if _DEBUG
//		fprintf(stderr, "MobiFlight: Registered Var %s with ID %u for code %s", varName.c_str(), varID, varCommand.c_str());
#endif

		varID++;
	}
}

DWORD GetCustEventIndex(const char* eventName) {

    if (CodeEventNames.find(eventName) != CodeEventNames.end())
        return CodeEventNames.at(eventName);
    else
        return INVALID_WASM_EVENT;
}

DWORD GetCustSimVarIndex(const char* varName) {

    if (CodeVarNames.find(varName) != CodeVarNames.end())
        return CodeVarNames.at(varName);
    else
        return INVALID_WASM_SIMVAR;
}

// SimConnect dispatch routine.
void CALLBACK dispatchRoutine(SIMCONNECT_RECV* pData, DWORD cbData, void* pContext) {
    printf("Dispatch ID = %d.\n", pData->dwID);
    DWORD receivedID = pData->dwID;
    switch (receivedID)
    {
    case SIMCONNECT_RECV_ID_OPEN: {

        break;
    }
    case SIMCONNECT_RECV_ID_CLIENT_DATA: {
        // Cast incoming data into interpretable format for this event.
        SIMCONNECT_RECV_CLIENT_DATA* pObjData = (SIMCONNECT_RECV_CLIENT_DATA*)pData;

        // Obtain the client data area contents in the expected format.
        double* pUserData = (double*)&pObjData->dwData;

        // For demonstration, the actual data value is pointed to by pUserData.
        double myData = *pUserData;

        std::string var_name = std::string(CodeVars[pObjData->dwDefineID].first);

        printf("Request ID = %d.\n", pObjData->dwRequestID);
        printf("Received client data [%s] = %f.\n", var_name.c_str(), *pUserData);
        break;
    }
    default:
        break;
    }
}

void transmitEvent(std::string eventName) {
    DWORD evt_Index = GetCustEventIndex(eventName.c_str());

    if (evt_Index == INVALID_WASM_EVENT) {
        std::cout << "Error: Invalid WASM_Event: " << eventName << "\n";
    }
    else {
        std::cout << "Transmitting WASM event [" << eventName << "]" << "\n";
        SimConnect_TransmitClientEvent(hSimconnect, objectID, evt_Index, 0, SIMCONNECT_GROUP_PRIORITY_HIGHEST, SIMCONNECT_EVENT_FLAG_GROUPID_IS_PRIORITY);
    }
}

void transmitVarRequest(std::string varName) {
    DWORD varWASM_Index = GetCustSimVarIndex(varName.c_str());

    if (varWASM_Index == INVALID_WASM_SIMVAR) {
        std::cout << "Error: Invalid WASM_SimVar: " << varName << "\n";
    }
    else {
        std::cout << "Transmitting data request for WASM_SimVar [" << varName << "]" << "\n";
        SimConnect_TransmitClientEvent(hSimconnect, objectID, WASM_VAR_DATA_REQUEST, varWASM_Index, SIMCONNECT_GROUP_PRIORITY_HIGHEST, SIMCONNECT_EVENT_FLAG_GROUPID_IS_PRIORITY);
        SimConnect_RequestClientData(hSimconnect, ClientDataID, varWASM_Index, varWASM_Index, SIMCONNECT_CLIENT_DATA_PERIOD_ONCE, SIMCONNECT_CLIENT_DATA_REQUEST_FLAG_DEFAULT, 0, 0, 0);
    }
}

int main()
{
    std::cout << "Simconnect Client Data Area testing.\n";

    // load defintions
    LoadEventDefinitions(FileEventsMobiFlight);
    LoadVarDefinitions(FileVarsMobiFlight);

    // Open the connection.
    hr = SimConnect_Open(&hSimconnect, "CPP_Client", nullptr, 0, 0, 0);
    if (S_OK == hr) {
        std::cout << "Connected to the Simulator.\n";

        RegisterEvents();

        // Map an ID to the Client Data Area.
        hr = SimConnect_MapClientDataNameToID(hSimconnect, clientDataName, ClientDataID);

        RegisterVars();

        /*************** RUN THE DISPATCH *****************************/
        if (S_OK == hr) {
            int quit = 0;
            while (!quit) {
                // Perform callback routine.
                SimConnect_CallDispatch(hSimconnect, dispatchRoutine, NULL);

                // Request data from the WASM.
                transmitVarRequest("MobiFlight.DA62_DEICE_PUMP");
                transmitVarRequest("MobiFlight.DA62_ICE_LIGHT_MAX_STATE_ENABLED");

                // Send an event to the WASM.
                transmitEvent("MobiFlight.AS1000_PFD_SOFTKEYS_6");

                Sleep(2000);
            }
        }
    }
    else {
        std::cout << "Error: Not connected to the Simulator.\n";
    }

    // Clear the client data area.
    SimConnect_ClearClientDataDefinition(hSimconnect, DEFINITION_1);
    // Close the connection.
    SimConnect_Close(hSimconnect);

    system("PAUSE");
}
