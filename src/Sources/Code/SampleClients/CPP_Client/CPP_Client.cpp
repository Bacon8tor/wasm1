#include <Windows.h>
#include "SimConnect.h"
#include <iostream>
#include <vector>
#include <string>
#include <fstream>
#include <map>

HRESULT hr;
HANDLE hSimconnect;

// Reference to user aircraft.
SIMCONNECT_OBJECT_ID objectID = SIMCONNECT_OBJECT_ID_USER;


const char* MobiFlightEventPrefix = "MobiFlight.";
const char* FileEventsMobiFlight = "events.txt";
const char* FileEventsUser = "events.user.txt";
const char* FileVarsMobiFlight = "wasm_vars.txt";
const char* FileVarsUser = "wasm_vars.user.txt";
const std::string EMPTY_CMD = std::string("()");

std::vector<std::pair<std::string, std::string>> CodeVars;
std::map<std::string, int> CodeVarNames;

const int INVALID_WASM_SIMVAR = -1;
const int WASM_SIMVAR_INDEX_BASE = 5000;
SIMCONNECT_CLIENT_DATA_ID ClientDataID = WASM_SIMVAR_INDEX_BASE;
DWORD dwSizeOfDouble = 8;

enum MOBIFLIGHT_GROUP
{
	DEFAULT
};

const char* clientDataName = "CUST_VAR_DATA";
const char* clientDataEventName = "CUST_VAR";

static enum DATA_DEFINE_ID {
	DEFINITION_1 = 12,
};

static enum DATA_REQUEST_ID {
	WASM_VAR_DATA_REQUEST = 1000,
};

std::pair<std::string, std::string> splitIntoPair(std::string value, char delimiter) {
    auto index = value.find(delimiter);
    std::pair<std::string, std::string> result;
    if (index != std::string::npos) {

        // Split around ':' character
        result = std::make_pair(
            value.substr(0, index),
            value.substr(index + 1)
        );

        // Trim any leading ' ' in the value part
        // (you may wish to add further conditions, such as '\t')
        while (!result.second.empty() && result.second.front() == ' ') {
            result.second.erase(0, 1);
        }
    }
    else {
        // Split around ':' character
        result = std::make_pair(
            value,
            std::string("(>H:" + value + ")")
        );
    }

    return result;
}

void LoadVarDefinitions(const char* fileName) {
	std::ifstream file(fileName);
	std::string line;

	while (std::getline(file, line)) {
		if (line.find("//") != std::string::npos) continue;
		std::string line2 = line.substr(0, line.size() - 1); //remove the trailing garbage
		std::pair<std::string, std::string> codeEvent = splitIntoPair(line2, '#');
		CodeVars.push_back(codeEvent);
	}

	file.close();
}

void RegisterEvents() {

    HRESULT hr;

//    DWORD eventID = 0;
//
//	for (const auto& value : CodeEvents) {
//		std::string eventCommand = value.second;
//		std::string eventName = std::string(MobiFlightEventPrefix) + value.first;
//
//		hr = SimConnect_MapClientEventToSimEvent(g_hSimConnect, eventID, eventName.c_str());
//		hr = SimConnect_AddClientEventToNotificationGroup(g_hSimConnect, MOBIFLIGHT_GROUP::DEFAULT, eventID, false);
//
//#if _DEBUG
//		if (hr != S_OK) fprintf(stderr, "MobiFlight: Error on registering Event %s with ID %u for code %s", eventName.c_str(), eventID, eventCommand.c_str());
//		else fprintf(stderr, "MobiFlight: Success on registering Event %s with ID %u for code %s", eventName.c_str(), eventID, eventCommand.c_str());
//#endif
//
//		eventID++;
//	}

    hr = SimConnect_MapClientEventToSimEvent(hSimconnect, WASM_VAR_DATA_REQUEST, clientDataEventName);
    hr = SimConnect_AddClientEventToNotificationGroup(hSimconnect, MOBIFLIGHT_GROUP::DEFAULT, WASM_VAR_DATA_REQUEST, false);
    
    SimConnect_SetNotificationGroupPriority(hSimconnect, MOBIFLIGHT_GROUP::DEFAULT, SIMCONNECT_GROUP_PRIORITY_HIGHEST);
}

DWORD GetCustSimVarIndex(const char *varName) {

    if (CodeVarNames.find(varName) != CodeVarNames.end())
        return CodeVarNames.at(varName);
    else
        return INVALID_WASM_SIMVAR;
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

// Definition of the client data area format
double data = 1.;

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

int main()
{
    std::cout << "Simconnect Client Data Area testing.\n";

    // load defintions
    LoadVarDefinitions(FileVarsMobiFlight);
    int varDefinition = CodeVars.size();
    LoadVarDefinitions(FileVarsUser);

    // Open the connection.
    hr = SimConnect_Open(&hSimconnect, "CPP_Client", nullptr, 0, 0, 0);
    if (S_OK == hr) {
        std::cout << "Connected to the Simulator.\n";

        RegisterEvents();

        // Map an ID to the Client Data Area.
        hr = SimConnect_MapClientDataNameToID(hSimconnect, clientDataName, ClientDataID);

        // Set up a custom Client Data Area.
        DWORD sz = CodeVars.size() * dwSizeOfDouble;
        hr &= SimConnect_CreateClientData(hSimconnect, ClientDataID, sz, SIMCONNECT_CLIENT_DATA_REQUEST_FLAG_DEFAULT);
        fprintf(stderr, "MobiFlight: SimConnect_CreateClientData size: %u", sz);

        RegisterVars();

        //// Request the client data periodically.
        //hr &= SimConnect_RequestClientData(hSimconnect, ClientDataID, REQUEST_1, DEFINITION_1, SIMCONNECT_CLIENT_DATA_PERIOD_SECOND, SIMCONNECT_CLIENT_DATA_REQUEST_FLAG_DEFAULT);

        /*************** RUN THE DISPATCH *****************************/
        if (S_OK == hr) {
            std::cout << "Setup client data area: OK.\n";
            // Set initial client data.
            hr = SimConnect_SetClientData(hSimconnect, ClientDataID, DEFINITION_1, SIMCONNECT_CLIENT_DATA_SET_FLAG_DEFAULT, 0, sizeof(data), &data);
            int quit = 0;
            while (!quit) {
                // Perform callback routine.
                SimConnect_CallDispatch(hSimconnect, dispatchRoutine, NULL);

                // Wait for data from the WASM.
                const char* varName = "MobiFlight.DA62_DEICE_PUMP";

                DWORD varWASM_Index = GetCustSimVarIndex(varName);
                if(varWASM_Index == INVALID_WASM_SIMVAR) {
                    std::cout << "Error: Invalid WASM_SimVar: " << varName << "\n";
                }
                else {
                    std::cout << "Transmitting data request for WASM_SimVar [" << varName << "]" << "\n";
                    SimConnect_TransmitClientEvent(hSimconnect, objectID, WASM_VAR_DATA_REQUEST, varWASM_Index, SIMCONNECT_GROUP_PRIORITY_HIGHEST, SIMCONNECT_EVENT_FLAG_GROUPID_IS_PRIORITY);
                    SimConnect_RequestClientData(hSimconnect, ClientDataID, varWASM_Index, varWASM_Index, SIMCONNECT_CLIENT_DATA_PERIOD_ONCE, SIMCONNECT_CLIENT_DATA_REQUEST_FLAG_DEFAULT, 0, 0, 0);
                }

                // Take it easy, no need to rush these events.
                Sleep(1000);
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
