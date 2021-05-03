## MobiFlight WASM Module

This standalone WASM module for Microsoft Flight Simulator 2020 enables us to send events to the sim and execute them in the context of the aircraft gauges system.
With this module you can interface your home cockpit hardware more efficiently.

This module also is released together with the MobiFlight Connector application as part of the MobiFlight Open Source Project - [https://mobiflight.com].

Additions by Elephant42
Added the ability to return custom Simvars such as lvars etc. via SimConnect.  Based on the method used in this code https://github.com/markrielaart/msfs-wasm-lvar-access.
