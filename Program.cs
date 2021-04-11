using System;
using System.IO;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace jbTaskAssembly
{
    static class Program
    {
        public static void Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("usage : <jbTaskAssembly to .dll> <path to output directory>");
                return;
            }

            var targetPath = args[0];
            var resultPath = args[1];
            var buildFiles = GetBuildFromPath(targetPath);

            var module = ModuleDefinition.ReadModule(targetPath);
            foreach (var type in module.Types)
            {
                foreach (var method in type.Methods)
                {
                    if (!method.HasBody)
                    {
                        continue;
                    }

                    var ilProcessor = method.Body.GetILProcessor();
                    var bodyInstructions = method.Body.Instructions;
                    var bodyInstructionsSize = bodyInstructions.Count;
                    for (var i = 0; i < bodyInstructionsSize; ++i)
                    {
                        var currentInstruction = bodyInstructions[i];
                        if (currentInstruction.OpCode == OpCodes.Add)
                        {
                            var subtractInstruction = ilProcessor.Create(OpCodes.Sub);

                            ilProcessor.Replace(currentInstruction, subtractInstruction);
                        }

                        if (currentInstruction.OpCode == OpCodes.Call)
                        {
                            var additionDecimal = module.ImportReference(typeof(decimal).GetMethod("op_Addition"))
                                .Resolve();
                            if (((MethodReference) currentInstruction.Operand).Resolve().Equals(additionDecimal))
                            {
                                var subtractDecimal =
                                    module.ImportReference(typeof(decimal).GetMethod("op_Subtraction"));
                                var callSubtractInstruction = ilProcessor.Create(OpCodes.Call, subtractDecimal);
                                ilProcessor.Replace(currentInstruction, callSubtractInstruction);
                            }
                        }
                    }
                }
            }

            CreateModifiedBuild(buildFiles, resultPath);
            module.Write(Path.Combine(resultPath, Path.GetFileName(targetPath)));
        }

        private static void CreateModifiedBuild(string[] buildFiles, string resultPath)
        {
            foreach (var f in buildFiles)
            {
                try
                {
                    File.Copy(f, Path.Combine(resultPath, Path.GetFileName(f)));
                }
                catch (IOException copyError)
                {
                    Console.WriteLine(copyError.Message);
                }
            }
        }

        private static string[] GetBuildFromPath(string path)
        {
            var buildDirName = Path.GetDirectoryName(path);
            var fileEntries = Directory.GetFiles(buildDirName);
            return fileEntries;
        }
    }
}
