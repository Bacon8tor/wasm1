#pragma once
//Stuff common to both the WASM module and CPP client.

enum MOBIFLIGHT_GROUP
{
	DEFAULT
};

static enum DATA_DEFINE_ID {
	DEFINITION_1 = 12,
};

const char* MobiFlightEventPrefix = "MobiFlight.";
const char* clientDataName = "CUST_VAR_DATA";
const char* clientDataEventName = "MobiFlight.CUST_VAR";

const std::string EMPTY_CMD = std::string("()");

const DWORD dwSizeOfDouble = 8;

// Definition of the client data area format
double data = 1.;

std::vector<std::pair<std::string, std::string>> CodeEvents;
std::vector<std::pair<std::string, std::string>> CodeVars;

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

void LoadEventDefinitions(const char* fileName) {
	std::ifstream file(fileName);
	std::string line;

	while (std::getline(file, line)) {
		if (line.find("//") != std::string::npos) continue;
		std::string line2 = line.substr(0, line.size() - 1); //remove the trailing garbage
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

