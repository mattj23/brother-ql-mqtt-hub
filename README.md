# Brother QL Hub (MQTT/SignalR)

This ASP.NET 6, server-side Blazor web application is a centralized API and web GUI for managing a scalable set of BrotherQL thermal printers. The printers are connected to the hub through other computers running client software (based on the BrotherQL Python libraries).  This typically takes the form of a single-board computer like a Raspberry Pi.  Bi-directional transport is achieved through MQTT or SignalR, the latter removing the need for a separate broker and providing convenience in environments already set up for HTTP.

## Functionality

The application recieves status information from printers while also maintaining a record of them by their unique serial numbers.  Printer online/offline status is tracked, and traditional HTTP API requests made to the hub itself can be used to dispatch images to be printed to online printers.

A tag/category system allows for printers with user-set characteristics to be selected through the API when print requests are made. For instance, a category called "Color" can be created and tags associated with it under the names "Red", "Blue", and "Green".  These might represent the color of paper loaded into the specific printers.  These tags can be set by the user and associated with specific printers, meaning that a print request could specify printing from any printer tagged "Red".  Additionally, characteristics like printer model and paper size can be used for selection.

## Category System

A "Category" is associated with a set of "Tags".  Each printer may be associated with one tag per category.  API calls to print may specify requirements for the printer to be used, which includes tags.

Tags can be set on printers in the interface with dropdown selections.