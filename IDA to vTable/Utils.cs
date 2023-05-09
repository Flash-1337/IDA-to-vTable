using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace IDA_to_vTable
{
    internal class Utils
    {

        public static Function ParseFunctionString(string input)
        {
            Logger.Log("Parsing function with input: " + input);
            // Use a regular expression to extract the class name, method name, and args
            Regex regex = new(@"; (?<class>\w+)::(?<method>\w+)\((?<args>[^)]*)\)");
            Match match = regex.Match(input);

            // Extract the class name, method name, and args from the regex match
            string className = match.Groups["class"].Value;
            string methodName = match.Groups["method"].Value;
            string methodArgs = match.Groups["args"].Value;
            
            Function function = new Function(className, methodName, methodArgs);
            function.ReturnType = GuessReturnType(methodName); // This can be "Guessed" from ida however it is not always correct
                                                               // and I cannot accuratly guess the return type and even so most people when they merge change it anyway
            Logger.Log("Function: " + function.ToString(), Logger.LogType.Info, true);

            return function;
        }

        public static Function ConvertToFunction(string input)
        {
            Regex regex = new(@"^\s*(?<returnType>virtual\s+[\w\s]+\*?)\s+(?<name>\w+)\s*\((?<args>[^)]*)\)");
            Match match = regex.Match(input);
            if (match.Success)
            {
                Function obj = new("class",
                    match.Groups["name"].Value,
                    match.Groups["args"].Value,
                    match.Groups["returnType"].Value);
                return obj;
            }
            return null;
        }

        public static bool IsValidFunc(string input)
        {
            Regex regex = new(@"^\s*\.rodata:[0-9A-F]+\s+((dq\s+offset\s+)?[_A-Z0-9]+\d+\w*\s*)?;\s*\w+::\w+\([^)]*\)?$");
            return regex.IsMatch(input);
        }

        public static bool alreadyFoundDestructor;

        public static Function ParseDestructorString(string input)
        {
            if (alreadyFoundDestructor)
                return null;

            // Player::~Player()
            var regex = new Regex(@"; (?<class>\w+)::~(?<classC>\w+)");
            var match = regex.Match(input);

            var className = match.Groups["class"].Value;

            alreadyFoundDestructor = true;

            return new Function(className, "~" + className, "");
        }


        public static Function ParseString(string input, int index)
        {
            if (input.Contains('~') && input.Contains("dq offset")) // Destructor
                return ParseDestructorString(input);

            if (input.Contains("___cxa_pure_virtual")) // Pure virtual function
                return new Function("Class", "Function" + index, "");


            if (!IsValidFunc(input)) // Not a valid function
                return null;

            return ParseFunctionString(input);
        }


        public static string GuessReturnType(string name)
        {
            // Check if the function name starts with "is" or "has"
            if (name.StartsWith("is") || name.StartsWith("has") || name.StartsWith("can"))
            {
                // If the function name starts with "is" or "has", it is likely to return a boolean value
                return "virtual bool";
            }

            // If the function name does not match any of the above patterns, it is likely to be a normal void
            return "virtual void";
        }

        public static void Merge(List<Function> functions)
        {

            Logger.Log("Input path to old table");

            string oldVtablePath = Console.ReadLine();

            // interepret the vtable from the input file

            if (!File.Exists(oldVtablePath))
            {
                Logger.Log("File does not exist", Logger.LogType.Warning);
                Console.ReadLine();
                return;
            }

            string[] oldVtableStr = File.ReadAllLines(oldVtablePath);

            List<Function> oldvTableFunctions = new();

            foreach (string line in oldVtableStr)
            {
                oldvTableFunctions.Add(Utils.ConvertToFunction(line));
            }

            foreach (Function newFunc in functions)
            {
                foreach (Function oldFunc in oldvTableFunctions)
                {
                    if (newFunc == null || oldFunc == null || newFunc.wasChanged || oldFunc.wasChanged)
                        continue;

                    if (newFunc.Name == oldFunc.Name)
                    {
                        if (newFunc.Args != oldFunc.Args)
                            newFunc.Args = oldFunc.Args;

                        if (newFunc.ReturnType != oldFunc.ReturnType)
                            newFunc.ReturnType = oldFunc.ReturnType;


                        newFunc.wasChanged = true;
                        oldFunc.wasChanged = true;

                        continue;
                    }


                }
            }
        }
    }
}
