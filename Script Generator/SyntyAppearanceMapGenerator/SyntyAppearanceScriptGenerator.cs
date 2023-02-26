
using RavenNest.Models;
using Shinobytes.Linq;
using System.Reflection;
using UnityEngine;

namespace SyntyAppearanceMapGenerator
{
    public class SyntyAppearanceScriptGenerator
    {
        public static void Generate(string output)
        {
            var ctx = new GeneratorContext(output);

            //ResetAppearance(ctx);

            LoadAppearance(ctx);

            //InitAppearance(ctx);
        }

        private static void LoadAppearance(GeneratorContext ctx)
        {
            // we have a "GetAll()" for model fields
            // since we want to iterate through all available models
            // then compare the names if they match.
            // to speed things up; we should instead access each individual
            // model field and their index directly.
            var indent = 0;

            Write(ctx.Out, "/// <summary>", indent);
            Write(ctx.Out, "///     This method has been generated, do not modify unless necessary as any changes may be overwritten in the future.", indent);
            Write(ctx.Out, "/// </summary>", indent);
            Write(ctx.Out, "private void LoadAppearance(SyntyAppearance appearance)", indent);
            {
                Write(ctx.Out, "{", indent++);

                var props = ctx.FieldsLookup;
                // get all models and deactivate them.
                foreach (var prop in ctx.SyntyProperties)
                {
                    var key = prop.Name;
                    //var varName = "v_" + key;
                    var varName = "appearance." + key;
                    if (props.TryGetValue(prop.Name, out var p))
                    {
                        //Write(ctx.Out, "var " + varName + " = appearance." + key + ";", indent);

                        if (p.FieldType == typeof(Color))
                        {
                            Write(ctx.Out, p.Name + " = GetColorFromHex(" + varName + ");", indent);
                        }
                        else
                        {
                            Write(ctx.Out, p.Name + " = " + varName + ";", indent);
                        }
                    }
                }
            }
            Write(ctx.Out, "}", --indent);
        }

        private static void ResetAppearance(GeneratorContext ctx)
        {
            // we have a "GetAll()" for model fields
            // since we want to iterate through all available models
            // then compare the names if they match.
            // to speed things up; we should instead access each individual
            // model field and their index directly.
            var indent = 0;

            Write(ctx.Out, "/// <summary>", indent);
            Write(ctx.Out, "///     This method has been generated, do not modify unless necessary as any changes may be overwritten in the future.", indent);
            Write(ctx.Out, "/// </summary>", indent);
            Write(ctx.Out, "public void ResetAppearance()", indent);
            {
                Write(ctx.Out, "{", indent++);
                // get all models and deactivate them.
                foreach (var modelField in ctx.ModelFields)
                {
                    var key = modelField.Name;
                    Write(ctx.Out, "for (var i = 0; i < " + key + ".Length; ++i)", indent);
                    {
                        Write(ctx.Out, "{", indent++);
                        {
                            Write(ctx.Out, key + "[i].SetActive(false);", indent);
                        }
                        Write(ctx.Out, "}", --indent);
                    }
                }


                Write(ctx.Out, "if (useMeshCombiner && (meshCombiner?.isMeshesCombineds ?? false))", indent);
                Write(ctx.Out, "meshCombiner?.UndoCombineMeshes();", indent + 1);
            }
            Write(ctx.Out, "}", --indent);
        }

        private static void InitAppearance(GeneratorContext ctx)
        {
            // we have a "GetAll()" for model fields
            // since we want to iterate through all available models
            // then compare the names if they match.
            // to speed things up; we should instead access each individual
            // model field and their index directly.
            var indent = 0;

            Write(ctx.Out, "/// <summary>", indent);
            Write(ctx.Out, "///     This method has been generated, do not modify unless necessary as any changes may be overwritten in the future.", indent);
            Write(ctx.Out, "/// </summary>", indent);
            Write(ctx.Out, "private void InitAppearance()", indent);
            {
                Write(ctx.Out, "{", indent++);
                Write(ctx.Out, "capeLogoMaterials.Clear();", indent);
                var lastWasGenderMale = false;
                var lastWasGenderFemale = false;

                foreach (var modelField in ctx.ModelFields)
                {
                    // in this part we will skip headCoverings, masks and hats
                    if (modelField.Name.ToLower() == "headcoverings" || modelField.Name.ToLower() == "masks" || modelField.Name.ToLower() == "hats")
                    {
                        continue;
                    }

                    var isGenderMale = false;
                    var isGenderFemale = false;

                    var index = "??"; // needs to be an expression most likely. index is decided based on AppearanceFields values.
                    var key = modelField.Name;
                    var indexName = "i_" + key;
                    var varName = "m_" + key;
                    var rendererName = "r_" + key;

                    var appearance = ctx.AppearanceFields.FirstOrDefault(x => x.Name.Equals(key, StringComparison.OrdinalIgnoreCase));
                    if (appearance == null)
                    {
                        appearance = ctx.AppearanceFields.FirstOrDefault(x =>
                            key.StartsWith("male" + x.Name, StringComparison.OrdinalIgnoreCase));
                        isGenderMale = appearance != null;
                    }
                    if (appearance == null)
                    {
                        appearance = ctx.AppearanceFields.FirstOrDefault(x =>
                            key.StartsWith("female" + x.Name, StringComparison.OrdinalIgnoreCase));
                        isGenderFemale = appearance != null;
                    }
                    if (appearance == null)
                    {
                        appearance = ctx.AppearanceFields.FirstOrDefault(x =>
                            key.StartsWith(x.Name, StringComparison.OrdinalIgnoreCase));
                    }
                    if (appearance == null)
                    {
                        Write(ctx.Out, "// " + key + " does not have an appearance field mapped to it, skipped.", indent);
                        continue;
                    }

                    index = appearance.Name;

                    if (appearance.FieldType == typeof(int[]))
                    {
                        Write(ctx.Out, "for (var i = 0; i < " + index + ".Length; ++i)", indent);
                        {
                            Write(ctx.Out, "{", indent++);
                            {
                                Write(ctx.Out, "var itemIndex = " + index + "[i];", indent);
                                Write(ctx.Out, "if (itemIndex >= 0)", indent);
                                Write(ctx.Out, "{", indent++);
                                {
                                    //Write(ctx.Out, varName + " = " + key + "[" + indexName + "];", indent);
                                    Write(ctx.Out, key + "[itemIndex].SetActive(true);", indent);
                                }
                                Write(ctx.Out, "}", --indent);
                            }
                            Write(ctx.Out, "}", --indent);
                        }
                        continue;
                    }

                    if (isGenderMale || isGenderFemale)
                    {
                        if (lastWasGenderMale != isGenderMale || lastWasGenderFemale != isGenderFemale)
                        {
                            // end the previous if statement.
                            if (lastWasGenderFemale || lastWasGenderMale)
                            {
                                Write(ctx.Out, "}", --indent);
                            }

                            Write(ctx.Out, "if (Gender == " + (isGenderMale ? "Gender.Male" : "Gender.Female") + ")", indent);
                            Write(ctx.Out, "{", indent++);
                        }
                    }

                    Write(ctx.Out, "var " + indexName + " = " + index + ";", indent);
                    Write(ctx.Out, "if (" + indexName + " >= 0 && " + indexName + " < " + key + ".Length)", indent);
                    Write(ctx.Out, "{", indent++);
                    {
                        Write(ctx.Out, "var " + varName + " = " + key + "[" + indexName + "];", indent);
                        Write(ctx.Out, "var " + rendererName + " = " + varName + ".GetComponent<SkinnedMeshRenderer>();", indent);
                        Write(ctx.Out, varName + ".SetActive(true);", indent);

                        // update materials for this model
                        var lowerKey = key.ToLower();
                        if (lowerKey == "capes")
                        {
                            Write(ctx.Out, "capeLogoMaterials.Add(" + rendererName + ".material);", indent);
                        }

                        if (lowerKey == "femaleheads" || lowerKey == "maleheads")
                        {
                            Write(ctx.Out, rendererName + ".material.SetColor(\"_Color_Eyes\", EyeColor);", indent);
                            Write(ctx.Out, rendererName + ".material.SetColor(\"_Color_Skin\", SkinColor);", indent);
                            Write(ctx.Out, rendererName + ".material.SetColor(\"_Color_Stubble\", StubbleColor);", indent);
                            Write(ctx.Out, rendererName + ".material.SetColor(\"_Color_BodyArt\", WarPaintColor);", indent);
                        }
                        else if (lowerKey == "malefacialhairs" || lowerKey == "maleeyebrows" || lowerKey == "femaleeyebrows" || lowerKey == "hairs")
                        {
                            Write(ctx.Out, rendererName + ".material.SetColor(\"_Color_Hair\", BeardColor);", indent);
                        }
                        else if (lowerKey == "malefacialhairs" || lowerKey == "maleeyebrows" || lowerKey == "femaleeyebrows")
                        {
                            Write(ctx.Out, rendererName + ".material.SetColor(\"_Color_Hair\", BeardColor);", indent);
                        }
                        else if (lowerKey == "hairs")
                        {
                            Write(ctx.Out, rendererName + ".material.SetColor(\"_Color_Hair\", HairColor);", indent);
                        }
                        else if (lowerKey != "capes")
                        {
                            Write(ctx.Out, "if (" + indexName + " == 0)", indent);
                            Write(ctx.Out, "{", indent++);
                            Write(ctx.Out, rendererName + ".material.SetColor(\"_Color_Skin\", SkinColor);", indent);
                            Write(ctx.Out, "}", --indent);
                        }
                    }
                    Write(ctx.Out, "}", --indent);

                    //if (isGenderMale || isGenderFemale)
                    //{
                    //    Write(ctx.Out, "}", --indent);
                    //}

                    lastWasGenderMale = isGenderMale;
                    lastWasGenderFemale = isGenderFemale;
                }

                if (lastWasGenderMale || lastWasGenderFemale)
                {
                    Write(ctx.Out, "}", --indent);
                }
                Write(ctx.Out, "}", --indent);
            }

        }

        public static void Write(System.Text.StringBuilder sb, string line, int indent = 0)
        {
            for (var i = 0; i < indent; ++i)
            {
                sb.Append("\t");
                Console.Write("\t");
            }

            sb.AppendLine(line);
            Console.WriteLine(line);
        }

        private class GeneratorContext
        {
            public string OutputFile { get; set; }
            public FieldInfo[] Fields { get; internal set; }
            public List<FieldInfo> AppearanceFields { get; internal set; }
            public Dictionary<string, FieldInfo> FieldsLookup { get; internal set; }
            public PropertyInfo[] SyntyProperties { get; internal set; }
            public List<FieldInfo> ModelFields { get; internal set; }
            public System.Text.StringBuilder Out { get; internal set; }
            public GeneratorContext(string output)
            {
                const BindingFlags GameObjectsBindingFlags = BindingFlags.NonPublic | BindingFlags.Instance;
                const BindingFlags FieldBindingFlags = BindingFlags.Public | BindingFlags.Instance;
                const BindingFlags SyntyPropertiesBindingFlags = BindingFlags.Public | BindingFlags.Instance;
                Out = new System.Text.StringBuilder();
                OutputFile = output;
                Fields = typeof(SyntyPlayerAppearance).GetFields(FieldBindingFlags);
                ModelFields = typeof(SyntyPlayerAppearance).GetFields(GameObjectsBindingFlags).AsList(x => x.FieldType == typeof(GameObject[]));
                FieldsLookup = LinqExtensions.ToDictionary(Fields, x => x.Name, x => x);
                SyntyProperties = typeof(SyntyAppearance).GetProperties(SyntyPropertiesBindingFlags);
                AppearanceFields = Fields.AsList(x => x.FieldType == typeof(int) || x.FieldType == typeof(int[]));
            }
        }
    }
}
