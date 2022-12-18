# IDA-to-vTable

## Description 
### A simple tool to convert a IDA vtables to a usable virtual function table 


## Code Breakdown (Thanks to ChatGPT)

The Function class is a simple class that stores information about a function. It has four properties: Name, Class, Args, and ReturnType. The Name and Class properties store the name of the function and the name of the class that the function belongs to, respectively. The Args property stores a string representation of the function's arguments, and the ReturnType property stores the return type of the function.

The ParseFunctionString method is a static method that takes a string as input and extracts the class name, method name, and parameters of a function from the string using a regular expression. It then returns a new Function object with the extracted class name, method name, and parameters.

The ConvertToObject method is a static method that takes a string as input and uses a regular expression to extract the return type, name, and arguments of a function from the string. It then returns a new Function object with the extracted information.

The IsValidFunc method is a static method that takes a string as input and returns a boolean indicating whether the string represents a valid function. It does this by checking the string against a regular expression.

The ParseConstructorString method is a static method that takes a string as input and extracts the class name of a constructor from the string using a regular expression. It then returns a new Function object with the class name and the name "~Class" (where "Class" is the extracted class name).

The ParseString method is a static method that takes a string and an integer as input and attempts to parse the string as a function, constructor, or pure virtual function. If the string represents a constructor, the method calls the ParseConstructorString method to extract the class name. If the string represents a pure virtual function, it returns a new Function object with the class name "Class" and the name "Function" followed by the integer index. If the string is not a valid function, it returns null.

The GuessReturnType method is a static method that takes a string as input and attempts to guess the return type of a function based on its name. If the function name starts with "is", "has", or "can", it returns "virtual bool", otherwise it returns "virtual int".
