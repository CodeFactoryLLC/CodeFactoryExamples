# Add Members Overview

## Overview:
Add Missing Members is automation that focuses on implementing missing interface members using a specific implementation pattern.

### Goals:
This example provides extensive tour of the capabilites of the code factory framework. This includes the following. 

#### Using Statement Management
Shows how to confirm target using statements exists and how to add using statements to a source code file.

#### Using the Namespace Manager to Manage Type Definitions
Shows how to capture all the namespaces implemented in a source code file and use them to shorten the full namespace implementation when implementing types.

#### Locate Interface Members Not Implemented
Shows how to use the framework to find interface members that are not implemented on a class in the source code file.

#### Usage of Source Formatting
Shows how to user the source formatters to capture source code you wish inject into files in visual studio.

#### Updating of Source Code by Target Location
Shows how to use the c# model system to determine how to inject code into the target source file. 

#### Using C# Models to Make Implementation Decisions
Shows how to read model data from code file objects to make implementation decisions for code automation.

## Automation Requirements for Add Missing Members
The following outlines the automation that is implemented as part of **Add Missing Members**

- Checks to make sure there are members from the inherited interfaces that are missing.
- Checks to makes sure Microsoft logging extensions is added to the using statements for the source code file and adds if missing.
- Checks to make sure there is a **_logger** readonly field is created in the class. If no field is found added the logger.
- Checks to make sure there is a constructor that injects the ILogger into the _logger field. If not it will build a constructor. 
- Checks all types from the missing members to make sure their namespaces are registered in the using statements. Any missing using namespaces are added as using statements to the source code file.
- If a property is missing implements the following.
    - Will create xml documentation for the field that backs the property
    - Will create a field that backs the property and adds it the end of the class definition.
    - If the property has xml documentation will be added to the implemented property.
    - If the property has attribute definitions will be added the the implemented property.
    - Will create a standard property definition and adds it to the end of the class definition.
- If event  is missing implements the following.
    - If the event has xml documentation will be added to the implemented event.
    - If the event has attribute definitions will be added the the implemented event.
    - Will inject a standard event syntax at the end of the implementing class.
- If a method is missing the following is implemented.
    - If the method has xml documentation will be added to the implemented method
    - If the method has attribute definitions will be added the the implemented method.
    - If the return type of the method is a **Task** type will implement the method with the **async** keyword.
    - Will inject information logging for all enterance and exit points from the method.
    - If the method has parameters will inject bounds checking for **string** and **none value** types.
    - If the method has a return type will creater a **result** variable and set the default value for the return variable.
    - Adds a try block to the method.
    - Adds a catch block to the method and logs the captured exception before rethrowing the exception
    - If a return type is required will create the return statement.