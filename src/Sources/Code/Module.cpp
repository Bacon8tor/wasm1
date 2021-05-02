#include <MSFS\MSFS.h>
#include <MSFS\MSFS_WindowsTypes.h>
#include <SimConnect.h>
#include <MSFS\Legacy\gauges.h>
#include <vector>
#include <string>
#include <iostream>
#include <fstream>
#include "Module.h"
#include <map>

HANDLE g_hSimConnect;
const char* version = "0.3.0";
const char* MobiFlightEventPrefix = "MobiFlight.";
const char* FileEventsMobiFlight = "modules/events.txt";
const char* FileEventsUser = "modules/events.user.txt";
const char* FileVarsMobiFlight = "modules/wasm_vars.txt";
const char* FileVarsUser = "modules/wasm_vars.user.txt";
const std::string EMPTY_CMD = std::string("()");

std::vector<std::pair<std::string, std::string>> CodeEvents;
std::vector<std::pair<std::string, std::string>> CodeVars;
std::vector<ID> CodeVarIDs;
//std::map<std::string, int> CodeVarNames;

const int WASM_SIMVAR_INDEX_BASE = 5000;
SIMCONNECT_CLIENT_DATA_ID ClientDataID = WASM_SIMVAR_INDEX_BASE;
DWORD dwSizeOfDouble = 8;

enum MOBIFLIGHT_GROUP
{
	DEFAULT
};

enum eEvents
{
	EVENT_FLIGHT_LOADED
};

enum eVars
{
	VAR_FLIGHT_LOADED,
};

const char* clientDataName = "CUST_VAR_DATA";

static enum DATA_DEFINE_ID {
	DEFINITION_1 = 12,
};

static enum DATA_REQUEST_ID {
	REQUEST_1 = 10,
};

// Definition of the client data area format
double data = 1.;

void CALLBACK MyDispatchProc(SIMCONNECT_RECV* pData, DWORD cbData, void* pContext);

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

void LoadEventDefinitions(const char * fileName) {
	std::ifstream file(fileName);
	std::string line;

	while (std::getline(file, line)) {
		if (line.find("//") != std::string::npos) continue;

		std::pair<std::string, std::string> codeEvent = splitIntoPair(line, '#');
		CodeEvents.push_back(codeEvent);
	}

	file.close();
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
	DWORD eventID = 0;

	for (const auto& value : CodeEvents) {
		std::string eventCommand = value.second;
		std::string eventName = std::string(MobiFlightEventPrefix) + value.first;

		HRESULT hr = SimConnect_MapClientEventToSimEvent(g_hSimConnect, eventID, eventName.c_str());
		hr = SimConnect_AddClientEventToNotificationGroup(g_hSimConnect, MOBIFLIGHT_GROUP::DEFAULT, eventID, false);

#if _DEBUG
		if (hr != S_OK) fprintf(stderr, "MobiFlight: Error on registering Event %s with ID %u for code %s", eventName.c_str(), eventID, eventCommand.c_str());
		else fprintf(stderr, "MobiFlight: Success on registering Event %s with ID %u for code %s", eventName.c_str(), eventID, eventCommand.c_str());
#endif

		eventID++;
	}

	SimConnect_SetNotificationGroupPriority(g_hSimConnect, MOBIFLIGHT_GROUP::DEFAULT, SIMCONNECT_GROUP_PRIORITY_HIGHEST);
}

void RegisterVars() {
	DWORD varID = 0;

	for (const auto& value : CodeVars) {
		std::string varCommand = value.second;
		std::string varName = std::string(MobiFlightEventPrefix) + value.first;

		//CodeVarNames.insert(std::make_pair(varName, varID));

		PCSTRINGZ var_name = varCommand.c_str();
		ID cust_var = check_named_variable(var_name);
		CodeVarIDs.push_back(cust_var);
		//if (cust_var < 0)
		//	fprintf(stderr, "MobiFlight.getVarFromSim() invalid named_variable: %u, %s: %i\n", varID, var_name, varCommand.length());
		//else
		//	fprintf(stderr, "MobiFlight.getVarFromSim() get_named_variable_value is valid - %s:\n", var_name);

		DWORD offs = varID * dwSizeOfDouble;
		// Add a double to the data definition.
		SimConnect_AddToClientDataDefinition(g_hSimConnect, varID, offs, dwSizeOfDouble);
		fprintf(stderr, "MobiFlight: SimConnect_AddToClientDataDefinition index: %u, offset: %u", varID, offs);

#if _DEBUG
		fprintf(stderr, "MobiFlight: Registered Var %s with ID %u for code %s", varName.c_str(), varID, varCommand.c_str());
#endif

		varID++;
	}
}

extern "C" MSFS_CALLBACK void module_init(void)
{
	// load defintions
	LoadEventDefinitions(FileEventsMobiFlight);
	int eventDefinition = CodeEvents.size();
	LoadEventDefinitions(FileEventsUser);

	LoadVarDefinitions(FileVarsMobiFlight);
	int varDefinition = CodeVars.size();
	LoadVarDefinitions(FileVarsUser);

	g_hSimConnect = 0;
	HRESULT hr = SimConnect_Open(&g_hSimConnect, "Standalone Module", (HWND) NULL, 0, 0, 0);
	if (hr != S_OK)
	{
		fprintf(stderr, "Could not open SimConnect connection.\n");
		return;
	}
	hr = SimConnect_SubscribeToSystemEvent(g_hSimConnect, EVENT_FLIGHT_LOADED, "FlightLoaded");
	if (hr != S_OK)
	{
		fprintf(stderr, "Could not subscribe to \"FlightLoaded\" system event.\n");
		return;
	}
	
	RegisterEvents();

	// Map an ID to the Client Data Area.
	hr = SimConnect_MapClientDataNameToID(g_hSimConnect, clientDataName, ClientDataID);

	// Set up a custom Client Data Area.
	DWORD sz = CodeVars.size() * dwSizeOfDouble;
	hr &= SimConnect_CreateClientData(g_hSimConnect, ClientDataID, sz, SIMCONNECT_CLIENT_DATA_REQUEST_FLAG_DEFAULT);
	fprintf(stderr, "MobiFlight: SimConnect_CreateClientData size: %u", sz);

	RegisterVars();

	hr = SimConnect_CallDispatch(g_hSimConnect, MyDispatchProc, NULL);
	if (hr != S_OK)
	{
		fprintf(stderr, "Could not set dispatch proc.\n");
		return;
	}

	fprintf(stderr, "MobiFlight: Module Init Complete. Version: %s", version);

	fprintf(stderr, "MobiFlight: Loaded %u event defintions in total.", CodeEvents.size());
	fprintf(stderr, "MobiFlight: Loaded %u built-in event defintions.", eventDefinition);
	fprintf(stderr, "MobiFlight: Loaded %u user event defintions.", CodeEvents.size()-eventDefinition);

	fprintf(stderr, "MobiFlight: Loaded %u var defintions in total.", CodeVars.size());
	fprintf(stderr, "MobiFlight: Loaded %u built-in var defintions.", varDefinition);
	fprintf(stderr, "MobiFlight: Loaded %u user var defintions.", CodeVars.size() - varDefinition);
}

extern "C" MSFS_CALLBACK void module_deinit(void)
{

	if (!g_hSimConnect)
		return;
	HRESULT hr = SimConnect_Close(g_hSimConnect);
	if (hr != S_OK)
	{
		fprintf(stderr, "Could not close SimConnect connection.\n");
		return;
	}

}

bool getVarFromSim(UINT32 iIndex)
{
	std::string named_var = std::string(CodeVars[iIndex].second);
	int sz = sizeof(data);
	//fprintf(stderr, "getVarFromSim(): %u, %s\n", iIndex, named_var.c_str());

	FLOAT64 lvarValue = 0.0;

	//switch (iIndex) {
	//	case 1: 
	//		lvarValue = 1.0123;
	//		break;
	//	case 2: {
	//		lvarValue = 2.3456;
	//		break;
	//	}
	//	default: {
	//		// No default for now.
	//		break;
	//	}
	//}

	PCSTRINGZ var_name = named_var.c_str();
	ID cust_var = CodeVarIDs[iIndex];

	if (cust_var < 0) {
		fprintf(stderr, "MobiFlight.getVarFromSim() invalid named_variable: %s\n", var_name);
		lvarValue = 0;
	}
	else {
		//fprintf(stderr, "MobiFlight.getVarFromSim() get_named_variable_value: %s\n", var_name);
		lvarValue = get_named_variable_value(cust_var);
	}

	data = lvarValue;

	//fprintf(stderr, "MobiFlight.getVarFromSim() SimConnect_SetClientData: %s-%u: %f\n", named_var.c_str(), iIndex, data);
	SimConnect_SetClientData(g_hSimConnect, ClientDataID, iIndex, SIMCONNECT_CLIENT_DATA_SET_FLAG_DEFAULT, 0, sz, &data);

	//fprintf(stderr, "MobiFlight.getVarFromSim() end\n");

	return true;
}

void CALLBACK MyDispatchProc(SIMCONNECT_RECV* pData, DWORD cbData, void* pContext)
{
	switch (pData->dwID)
	{
	case SIMCONNECT_RECV_ID_EVENT: {
		SIMCONNECT_RECV_EVENT* evt = (SIMCONNECT_RECV_EVENT*)pData;
		int eventID = evt->uEventID;
		int groupID = evt->uGroupID;
		UINT32 evData = evt->dwData;
		//std::string evData = evt->;

		//fprintf(stderr, "Received eventID: %u\n", eventID);

		if (eventID < CodeEvents.size()) {
			//fprintf(stderr, "Passed check eventID: %u\n", eventID);
			// We got a Code Event or a User Code Event
			if (eventID == 1) {
				//fprintf(stderr, "CUSTOM_VAR %u, %u, %s\n", eventID, groupID, evData);

				//std::make_shared<std::string>("TESTY")
				getVarFromSim(evData);
			}
			else {
				int CodeEventId = eventID;
				std::string command = std::string(CodeEvents[CodeEventId].second);
				if (command == EMPTY_CMD) {
					std::string name = std::string(CodeEvents[CodeEventId].first);
					fprintf(stderr, "Got a Custom event %s\n", name.c_str());
				}
				else {
					//fprintf(stderr, "execute %s\n", command.c_str());
					execute_calculator_code(command.c_str(), nullptr, nullptr, nullptr);
				}
			}
		} 
		else {
			fprintf(stderr, "MobiFlight: OOF! - EventID out of range:%u\n", eventID);
		}
		
		break;
	}

	default:
		break;
	}
	fprintf(stderr, "Received event end\n");
}
