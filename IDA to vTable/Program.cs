using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace IDA_to_vTable
{
    
    internal class Program
    {
        static void Main(string[] args)
        {
            // TODO: Handle args

            Console.Title = "IDA to vTable - by Flash_";

            Logger.Log("Enter file path of IDA vTable (ex: C:\\Users\\user\\Desktop\\vtable.txt)");


            // Read the input file

            string[] lines;

            try
            {
                lines = System.IO.File.ReadAllLines(Console.ReadLine());
            }
            catch (Exception e)
            {
                Logger.Log("The file could not be read" , Logger.LogType.Warning);
                Debug.WriteLine(e.Message);
                Console.ReadLine();
                return;
            }
            List<string> functionNames = new();
            List<string> functionIndexes = new();
            List<Function> functionList1 = new();



            int index = -1; // -1 because of the typeinfo 

            // Read all lines
            foreach (string line in lines)
            {
                bool shouldIncrementIndex = true;
                // Parse the line
                var function = Utils.ParseString(line, index);

                // Check if the line was a valid function
                if (function == null)
                    shouldIncrementIndex = false;
                else
                {
                    // Guess the return type
                    var returnType = function.ReturnType;

                    functionList1.Add(new Function("class", function.Name, function.Args, returnType));
                    
                }

                if (shouldIncrementIndex)
                    index++;
            }




            Logger.Log("Would you like to merge with a pre-existing vtable? (Experimental - This may not work correctly/at all) y/N", Logger.LogType.Warning);

            if (Console.ReadLine().ToLower().Contains('y'))
            {
                Utils.Merge(functionList1);
            }

            int newIndex = 1;

            Function lastFunction = null;

            // Function Indexing
            foreach (Function func in functionList1)
            {

                functionNames.Add($"{func.ReturnType} {func.Name}({func.Args});");
                
                if (lastFunction != null && lastFunction.Name != func.Name && !func.Name.Contains('~'))
                    functionIndexes.Add($"int {func.Name} = {newIndex};");


                if (!func.Name.Contains('~'))
                    newIndex++;

                lastFunction = func;
            }


            // Export/Save
            Logger.Log("Enter file name to save as");
            string path = Environment.CurrentDirectory + @"\" + Console.ReadLine();
            File.WriteAllLines(path + "_vtables.txt", functionNames);
            File.WriteAllLines(path + "_indexs.txt", functionIndexes);

            Logger.Log("File saved to: " + path);

            Logger.Log("Open directory? y/N");
            if (Console.ReadLine().ToLower().Contains('y'))
                Process.Start("explorer.exe", Environment.CurrentDirectory);

            Console.ReadKey();
        }
    }
}