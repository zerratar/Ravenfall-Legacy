using Shinobytes.Core.ScriptParser;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Assets
{
    public class FindBadNamedScripts : EditorWindow
    {
        [MenuItem("Ravenfall/Tools/Scripts/Find Bad Named Scripts")]
        public static void ShowWindow()
        {
            EditorWindow.GetWindow(typeof(FindBadNamedScripts));
        }

        public void OnGUI()
        {
            if (GUILayout.Button("Find bad named scripts in the project"))
            {
                FindAll();
            }
        }
        private static void FindAll()
        {
            var scriptFiles = System.IO.Directory.GetFiles(Application.dataPath, "*.cs", System.IO.SearchOption.AllDirectories);
            var validator = new MonoBehaviourScriptNameValidator();
            var badCount = 0;
            var okCount = 0;
            var naCount = 0;

            foreach (var file in scriptFiles)
            {
                var result = validator.Validate(file);
                switch (result.Result)
                {
                    case ValidationResultType.Bad:
                        ++badCount;
                        break;
                    case ValidationResultType.NotApplicable:
                        ++naCount;
                        break;
                    case ValidationResultType.OK:
                        ++okCount;
                        break;
                }

                if (result.Result == ValidationResultType.Bad)
                {
                    if (result.BadASTDefinitions.Length > 1)
                    {
                        var reportString = "";
                        foreach (var bad in result.BadASTDefinitions)
                        {
                            reportString += " * found: " + bad.FullName + " expected: " + result.ExpectedName + "\r\n";
                        }

                        UnityEngine.Debug.LogError("Multiple (" + result.BadASTDefinitions.Length + ") Bad MonoBehaviours found.\r\n" + reportString);
                    }
                    else
                    {
                        var bad = result.BadASTDefinitions[0];
                        UnityEngine.Debug.LogError("Bad MonoBehaviour found: " + bad.FullName + " expected: " + result.ExpectedName);
                    }
                }
            }
            var total = (badCount + okCount + naCount);
            UnityEngine.Debug.LogError("Out of " + total + ": " + okCount + " OK, " + naCount + " N/A, " + badCount + " bad");
        }
    }

    public class MonoBehaviourScriptNameValidator
    {
        private readonly Lexer lexer;

        public MonoBehaviourScriptNameValidator()
        {
            this.lexer = new Lexer();
        }
        public MonoBehaviourScriptNameValidationResult Validate(string file)
        {
            // test
            var text = System.IO.File.ReadAllText(file);
            var tokens = lexer.Tokenize(text);
            var name = System.IO.Path.GetFileNameWithoutExtension(file);
            var structure = Parse(name, tokens);

            var result = ValidationResultType.NotApplicable;
            var badDefinitions = new List<TypeDefinition>();
            for (int i = 0; i < structure.Definitions.Length; i++)
            {
                var def = structure.Definitions[i];
                if (def.BaseType.Name == "MonoBehaviour")
                {
                    if (result == ValidationResultType.NotApplicable)
                    {
                        result = ValidationResultType.OK;
                    }

                    if (def.Name != name)
                    {
                        result = ValidationResultType.Bad;
                        badDefinitions.Add(def);
                    }
                }
            }

            return new MonoBehaviourScriptNameValidationResult(name, structure, badDefinitions.ToArray(), result);
        }

        private HighLevelAbstractSyntaxTree Parse(string name, TokenStream tokens)
        {
            var types = new List<TypeDefinition>();
            var parserContext = new ParserContext();
            while (!tokens.EndOfStream)
            {
                switch (tokens.Current.Value)
                {
                    case "namespace":
                        ParseNamespace(parserContext, tokens);
                        continue;
                    case "class":
                        types.Add(ParseClass(parserContext, tokens));
                        continue;
                    case "enum":
                        types.Add(ParseEnum(parserContext, tokens));
                        continue;
                    case "struct":
                        types.Add(ParseStruct(parserContext, tokens));
                        continue;
                }

                tokens.Next();
            }

            return new HighLevelAbstractSyntaxTree(parserContext.Namespace, types.ToArray());
        }

        private void ParseNamespace(ParserContext ctx, TokenStream tokens)
        {
            var ns = "";
            while (!tokens.NextIs(TokenType.LCurlyBracket))
            {
                var token = tokens.Next();
                ns += token.Value;
            }

            ctx.Namespace = ns;
        }
        private TypeDefinition ParseStruct(ParserContext ctx, TokenStream tokens)
        {
            return new TypeDefinition(string.Empty);
        }
        private TypeDefinition ParseEnum(ParserContext ctx, TokenStream tokens)
        {
            return new TypeDefinition(string.Empty);
        }
        private TypeDefinition ParseClass(ParserContext ctx, TokenStream tokens)
        {
            return new TypeDefinition(string.Empty);
        }

        private class ParserContext
        {
            public string Namespace;
        }
    }

    public class HighLevelAbstractSyntaxTree
    {
        public readonly string Namespace;
        public readonly TypeDefinition[] Definitions;

        public HighLevelAbstractSyntaxTree(string @namespace, TypeDefinition[] definitions)
        {
            this.Namespace = @namespace;
            this.Definitions = definitions;
        }
    }

    public class TypeReference
    {
        public readonly string Name;
        public readonly string FullName;
        public TypeReference(string fullName)
        {
            this.FullName = fullName;
            if (fullName.IndexOf('.') > 0)
            {
                this.Name = fullName.Split('.').LastOrDefault();
            }
            else
            {
                this.Name = this.FullName;
            }
        }
    }

    public class TypeDefinition : TypeReference
    {
        public readonly TypeReference BaseType;
        public TypeDefinition(string fullName, string baseTypeFullName = null)
            : base(fullName)
        {
            if (!string.IsNullOrEmpty(baseTypeFullName))
                this.BaseType = new TypeReference(baseTypeFullName);
        }

        public TypeDefinition(string fullName, TypeReference baseType)
            : base(fullName)
        {
            this.BaseType = baseType;
        }
    }

    public class MonoBehaviourScriptNameValidationResult
    {
        public readonly HighLevelAbstractSyntaxTree AST;
        public readonly TypeDefinition[] BadASTDefinitions;
        public readonly string ExpectedName;
        public readonly bool HasMultipleClasses;
        public readonly ValidationResultType Result;
        public MonoBehaviourScriptNameValidationResult(string expectedName, HighLevelAbstractSyntaxTree ast, TypeDefinition[] badDefinitions, ValidationResultType result)
        {
            ExpectedName = expectedName;
            Result = result;
            AST = ast;
            BadASTDefinitions = badDefinitions;
            HasMultipleClasses = ast.Definitions.Length > 0;
        }
    }
    public enum ValidationResultType : int
    {
        NotApplicable = 0,
        OK = 1,
        Bad = 2
    }
}