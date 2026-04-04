using UAssetHorizon.Core.Models;

namespace UAssetHorizon.Core.Parsers;

/// <summary>
/// Extracts Blueprint graph structure from parsed asset data.
/// Reconstructs nodes, pins, connections, variables, functions, and components.
/// Generates pseudo-code from the execution flow when possible.
/// </summary>
public static class BlueprintParser
{
    // Known K2Node class prefixes and their categories
    private static readonly Dictionary<string, NodeCategory> NodeCategoryMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["K2Node_Event"] = NodeCategory.Event,
        ["K2Node_CustomEvent"] = NodeCategory.Event,
        ["K2Node_FunctionEntry"] = NodeCategory.Event,
        ["K2Node_InputAction"] = NodeCategory.Event,
        ["K2Node_CallFunction"] = NodeCategory.Function,
        ["K2Node_CallArrayFunction"] = NodeCategory.Function,
        ["K2Node_CallParentFunction"] = NodeCategory.Function,
        ["K2Node_IfThenElse"] = NodeCategory.FlowControl,
        ["K2Node_Branch"] = NodeCategory.FlowControl,
        ["K2Node_SwitchInteger"] = NodeCategory.FlowControl,
        ["K2Node_SwitchString"] = NodeCategory.FlowControl,
        ["K2Node_SwitchEnum"] = NodeCategory.FlowControl,
        ["K2Node_WhileLoop"] = NodeCategory.FlowControl,
        ["K2Node_ForEachLoop"] = NodeCategory.FlowControl,
        ["K2Node_ForLoop"] = NodeCategory.FlowControl,
        ["K2Node_Sequence"] = NodeCategory.FlowControl,
        ["K2Node_Select"] = NodeCategory.FlowControl,
        ["K2Node_DoOnce"] = NodeCategory.FlowControl,
        ["K2Node_FlipFlop"] = NodeCategory.FlowControl,
        ["K2Node_Gate"] = NodeCategory.FlowControl,
        ["K2Node_MultiGate"] = NodeCategory.FlowControl,
        ["K2Node_VariableGet"] = NodeCategory.Variable,
        ["K2Node_VariableSet"] = NodeCategory.Variable,
        ["K2Node_Self"] = NodeCategory.Variable,
        ["K2Node_Literal"] = NodeCategory.Variable,
        ["K2Node_MacroInstance"] = NodeCategory.Macro,
        ["K2Node_DynamicCast"] = NodeCategory.Cast,
        ["K2Node_ClassDynamicCast"] = NodeCategory.Cast,
        ["K2Node_MathExpression"] = NodeCategory.Math,
        ["K2Node_PromotableOperator"] = NodeCategory.Math,
        ["K2Node_CommutativeAssociativeBinaryOperator"] = NodeCategory.Math,
    };

    /// <summary>
    /// Parse blueprint data from a uasset. Extracts graph structure from exports and names.
    /// </summary>
    public static BlueprintData? Parse(ParsedAsset asset, byte[] rawData, string? uexpPath)
    {
        var blueprint = new BlueprintData();
        int nodeIdCounter = 0;

        // Determine parent class from imports
        var parentClassImport = asset.Imports
            .FirstOrDefault(i => i.ClassName.Contains("Class") &&
                !i.ObjectName.Contains("Default__") &&
                !i.ObjectName.Contains("BlueprintGeneratedClass"));
        if (parentClassImport != null)
            blueprint.ParentClass = parentClassImport.ObjectName;

        // Extract components from exports
        foreach (var export in asset.Exports)
        {
            string className = export.ClassName;
            string objectName = export.ObjectName;

            // Detect components
            if (className.Contains("Component") || objectName.Contains("Component"))
            {
                blueprint.Components.Add(new BlueprintComponent
                {
                    Name = objectName,
                    ClassName = className,
                    Properties = export.Properties
                });
            }
        }

        // Extract variables from name patterns
        ExtractVariables(asset, blueprint);

        // Extract function signatures from exports
        ExtractFunctions(asset, blueprint);

        // Build node graph from names and exports
        BuildNodeGraph(asset, blueprint, ref nodeIdCounter);

        // Generate pseudo-code
        blueprint.PseudoCode = GeneratePseudoCode(blueprint);

        return blueprint;
    }

    private static void ExtractVariables(ParsedAsset asset, BlueprintData blueprint)
    {
        // Look for variable patterns in names
        var variablePatterns = new[] { "BoolProperty", "FloatProperty", "IntProperty",
            "StrProperty", "ObjectProperty", "ArrayProperty", "StructProperty",
            "NameProperty", "TextProperty", "ByteProperty" };

        var potentialVars = asset.Names
            .Where(n => !n.StartsWith("K2Node") && !n.StartsWith("/") &&
                        !n.StartsWith("__") && n.Length > 1 && n.Length < 100)
            .ToList();

        // Match names that appear near property type names
        for (int i = 0; i < potentialVars.Count; i++)
        {
            string name = potentialVars[i];
            if (variablePatterns.Any(p => name.Contains(p)))
            {
                string varType = variablePatterns.First(p => name.Contains(p))
                    .Replace("Property", "");

                // The actual variable name is often the preceding name
                string varName = name.Replace(varType + "Property", "").Trim();
                if (string.IsNullOrEmpty(varName) && i > 0)
                    varName = potentialVars[i - 1];

                if (!string.IsNullOrEmpty(varName) && varName.Length < 80)
                {
                    blueprint.Variables.Add(new BlueprintVariable
                    {
                        Name = varName,
                        Type = varType,
                        IsPublic = !name.Contains("private", StringComparison.OrdinalIgnoreCase)
                    });
                }
            }
        }

        // Deduplicate
        blueprint.Variables = blueprint.Variables
            .GroupBy(v => v.Name)
            .Select(g => g.First())
            .ToList();
    }

    private static void ExtractFunctions(ParsedAsset asset, BlueprintData blueprint)
    {
        // Functions appear as exports with "Function" in class name
        foreach (var export in asset.Exports)
        {
            if (export.ClassName.Contains("Function") ||
                export.ObjectName.Contains("ExecuteUbergraph"))
            {
                bool isEvent = export.ObjectName.StartsWith("ReceiveBeginPlay") ||
                               export.ObjectName.StartsWith("ReceiveTick") ||
                               export.ObjectName.StartsWith("ReceiveAny");

                blueprint.Functions.Add(new BlueprintFunction
                {
                    Name = export.ObjectName,
                    IsEvent = isEvent,
                    Description = $"Serialized at offset {export.SerialOffset}, size {export.SerialSize}"
                });
            }
        }
    }

    private static void BuildNodeGraph(ParsedAsset asset, BlueprintData blueprint, ref int nodeIdCounter)
    {
        // Extract K2Node references from names
        var k2NodeNames = asset.Names
            .Where(n => n.StartsWith("K2Node_"))
            .Distinct()
            .ToList();

        // Create nodes based on detected K2Node types
        double xPos = 0;
        double yPos = 0;
        int nodesPerRow = 4;
        double xSpacing = 320;
        double ySpacing = 250;

        // Always add a BeginPlay event if we detect it
        bool hasBeginPlay = asset.Names.Any(n =>
            n.Contains("BeginPlay") || n.Contains("ReceiveBeginPlay"));

        if (hasBeginPlay)
        {
            var beginPlayNode = CreateEventNode(ref nodeIdCounter, "Event BeginPlay",
                "K2Node_Event", xPos, yPos);
            blueprint.Nodes.Add(beginPlayNode);
            xPos += xSpacing;
        }

        // Add Tick event if present
        bool hasTick = asset.Names.Any(n =>
            n.Contains("ReceiveTick") || n.Contains("EventTick"));

        if (hasTick)
        {
            var tickNode = CreateEventNode(ref nodeIdCounter, "Event Tick",
                "K2Node_Event", 0, yPos + ySpacing);
            tickNode.Pins.Add(new BlueprintPin
            {
                Id = $"pin_{tickNode.Id}_delta",
                Name = "Delta Seconds",
                Direction = PinDirection.Output,
                PinType = PinType.Float
            });
            blueprint.Nodes.Add(tickNode);
        }

        // Build nodes for each K2Node type found
        int col = hasBeginPlay ? 1 : 0;
        foreach (var nodeName in k2NodeNames)
        {
            var category = GetNodeCategory(nodeName);
            string displayName = FormatNodeName(nodeName);

            var node = new BlueprintNode
            {
                Id = $"node_{nodeIdCounter++}",
                Type = nodeName,
                Title = displayName,
                Category = category,
                PositionX = (col % nodesPerRow) * xSpacing,
                PositionY = (col / nodesPerRow) * ySpacing,
                IsPure = category == NodeCategory.Math || category == NodeCategory.Variable
            };

            // Add standard pins based on category
            AddPinsForCategory(node, category, nodeName);

            blueprint.Nodes.Add(node);
            col++;
        }

        // Add function call nodes for detected functions
        foreach (var func in blueprint.Functions.Where(f => !f.IsEvent))
        {
            var funcNode = new BlueprintNode
            {
                Id = $"node_{nodeIdCounter++}",
                Type = "K2Node_CallFunction",
                Title = func.Name,
                Category = NodeCategory.Function,
                PositionX = (col % nodesPerRow) * xSpacing,
                PositionY = (col / nodesPerRow) * ySpacing,
            };

            funcNode.Pins.Add(new BlueprintPin
            {
                Id = $"pin_{funcNode.Id}_exec_in",
                Name = "",
                Direction = PinDirection.Input,
                PinType = PinType.Exec
            });
            funcNode.Pins.Add(new BlueprintPin
            {
                Id = $"pin_{funcNode.Id}_exec_out",
                Name = "Then",
                Direction = PinDirection.Output,
                PinType = PinType.Exec
            });

            blueprint.Nodes.Add(funcNode);
            col++;
        }

        // Generate edges (connections) between sequential nodes
        GenerateEdges(blueprint);

        // Create graph entries
        var eventGraphNodes = blueprint.Nodes
            .Where(n => n.Category == NodeCategory.Event ||
                        blueprint.Edges.Any(e => e.SourceNodeId == n.Id || e.TargetNodeId == n.Id))
            .Select(n => n.Id)
            .ToList();

        if (eventGraphNodes.Any())
        {
            blueprint.Graphs.Add(new BlueprintGraph
            {
                Name = "EventGraph",
                GraphType = "EventGraph",
                NodeIds = eventGraphNodes
            });
        }
    }

    private static BlueprintNode CreateEventNode(ref int nodeIdCounter, string title,
        string type, double x, double y)
    {
        var node = new BlueprintNode
        {
            Id = $"node_{nodeIdCounter++}",
            Type = type,
            Title = title,
            Category = NodeCategory.Event,
            PositionX = x,
            PositionY = y,
        };

        // Events only have output exec pin
        node.Pins.Add(new BlueprintPin
        {
            Id = $"pin_{node.Id}_exec_out",
            Name = "",
            Direction = PinDirection.Output,
            PinType = PinType.Exec
        });

        return node;
    }

    private static void AddPinsForCategory(BlueprintNode node, NodeCategory category, string typeName)
    {
        switch (category)
        {
            case NodeCategory.Function:
                node.Pins.Add(new BlueprintPin { Id = $"pin_{node.Id}_exec_in", Name = "", Direction = PinDirection.Input, PinType = PinType.Exec });
                node.Pins.Add(new BlueprintPin { Id = $"pin_{node.Id}_exec_out", Name = "Then", Direction = PinDirection.Output, PinType = PinType.Exec });
                node.Pins.Add(new BlueprintPin { Id = $"pin_{node.Id}_target", Name = "Target", Direction = PinDirection.Input, PinType = PinType.Object });
                node.Pins.Add(new BlueprintPin { Id = $"pin_{node.Id}_return", Name = "Return Value", Direction = PinDirection.Output, PinType = PinType.Wildcard });
                break;

            case NodeCategory.FlowControl:
                node.Pins.Add(new BlueprintPin { Id = $"pin_{node.Id}_exec_in", Name = "", Direction = PinDirection.Input, PinType = PinType.Exec });

                if (typeName.Contains("Branch") || typeName.Contains("IfThenElse"))
                {
                    node.Pins.Add(new BlueprintPin { Id = $"pin_{node.Id}_condition", Name = "Condition", Direction = PinDirection.Input, PinType = PinType.Boolean });
                    node.Pins.Add(new BlueprintPin { Id = $"pin_{node.Id}_true", Name = "True", Direction = PinDirection.Output, PinType = PinType.Exec });
                    node.Pins.Add(new BlueprintPin { Id = $"pin_{node.Id}_false", Name = "False", Direction = PinDirection.Output, PinType = PinType.Exec });
                }
                else if (typeName.Contains("ForLoop") || typeName.Contains("ForEach"))
                {
                    node.Pins.Add(new BlueprintPin { Id = $"pin_{node.Id}_body", Name = "Loop Body", Direction = PinDirection.Output, PinType = PinType.Exec });
                    node.Pins.Add(new BlueprintPin { Id = $"pin_{node.Id}_index", Name = "Index", Direction = PinDirection.Output, PinType = PinType.Int });
                    node.Pins.Add(new BlueprintPin { Id = $"pin_{node.Id}_completed", Name = "Completed", Direction = PinDirection.Output, PinType = PinType.Exec });
                }
                else if (typeName.Contains("Sequence"))
                {
                    node.Pins.Add(new BlueprintPin { Id = $"pin_{node.Id}_then0", Name = "Then 0", Direction = PinDirection.Output, PinType = PinType.Exec });
                    node.Pins.Add(new BlueprintPin { Id = $"pin_{node.Id}_then1", Name = "Then 1", Direction = PinDirection.Output, PinType = PinType.Exec });
                }
                else
                {
                    node.Pins.Add(new BlueprintPin { Id = $"pin_{node.Id}_exec_out", Name = "Then", Direction = PinDirection.Output, PinType = PinType.Exec });
                }
                break;

            case NodeCategory.Variable:
                if (typeName.Contains("Set"))
                {
                    node.Pins.Add(new BlueprintPin { Id = $"pin_{node.Id}_exec_in", Name = "", Direction = PinDirection.Input, PinType = PinType.Exec });
                    node.Pins.Add(new BlueprintPin { Id = $"pin_{node.Id}_exec_out", Name = "", Direction = PinDirection.Output, PinType = PinType.Exec });
                    node.Pins.Add(new BlueprintPin { Id = $"pin_{node.Id}_value", Name = "Value", Direction = PinDirection.Input, PinType = PinType.Wildcard });
                }
                else
                {
                    node.IsPure = true;
                    node.Pins.Add(new BlueprintPin { Id = $"pin_{node.Id}_value", Name = "Value", Direction = PinDirection.Output, PinType = PinType.Wildcard });
                }
                break;

            case NodeCategory.Cast:
                node.Pins.Add(new BlueprintPin { Id = $"pin_{node.Id}_exec_in", Name = "", Direction = PinDirection.Input, PinType = PinType.Exec });
                node.Pins.Add(new BlueprintPin { Id = $"pin_{node.Id}_success", Name = "Success", Direction = PinDirection.Output, PinType = PinType.Exec });
                node.Pins.Add(new BlueprintPin { Id = $"pin_{node.Id}_fail", Name = "Cast Failed", Direction = PinDirection.Output, PinType = PinType.Exec });
                node.Pins.Add(new BlueprintPin { Id = $"pin_{node.Id}_object", Name = "Object", Direction = PinDirection.Input, PinType = PinType.Object });
                node.Pins.Add(new BlueprintPin { Id = $"pin_{node.Id}_result", Name = "As Result", Direction = PinDirection.Output, PinType = PinType.Object });
                break;

            case NodeCategory.Math:
                node.IsPure = true;
                node.Pins.Add(new BlueprintPin { Id = $"pin_{node.Id}_a", Name = "A", Direction = PinDirection.Input, PinType = PinType.Float });
                node.Pins.Add(new BlueprintPin { Id = $"pin_{node.Id}_b", Name = "B", Direction = PinDirection.Input, PinType = PinType.Float });
                node.Pins.Add(new BlueprintPin { Id = $"pin_{node.Id}_result", Name = "Result", Direction = PinDirection.Output, PinType = PinType.Float });
                break;

            default:
                node.Pins.Add(new BlueprintPin { Id = $"pin_{node.Id}_exec_in", Name = "", Direction = PinDirection.Input, PinType = PinType.Exec });
                node.Pins.Add(new BlueprintPin { Id = $"pin_{node.Id}_exec_out", Name = "", Direction = PinDirection.Output, PinType = PinType.Exec });
                break;
        }
    }

    private static void GenerateEdges(BlueprintData blueprint)
    {
        int edgeId = 0;

        // Connect sequential nodes via execution pins
        var execNodes = blueprint.Nodes
            .Where(n => n.Pins.Any(p => p.PinType == PinType.Exec && p.Direction == PinDirection.Output))
            .OrderBy(n => n.PositionY)
            .ThenBy(n => n.PositionX)
            .ToList();

        for (int i = 0; i < execNodes.Count - 1; i++)
        {
            var source = execNodes[i];
            var target = execNodes[i + 1];

            var sourcePin = source.Pins
                .FirstOrDefault(p => p.PinType == PinType.Exec && p.Direction == PinDirection.Output);
            var targetPin = target.Pins
                .FirstOrDefault(p => p.PinType == PinType.Exec && p.Direction == PinDirection.Input);

            if (sourcePin != null && targetPin != null)
            {
                blueprint.Edges.Add(new BlueprintEdge
                {
                    Id = $"edge_{edgeId++}",
                    SourceNodeId = source.Id,
                    SourcePinId = sourcePin.Id,
                    TargetNodeId = target.Id,
                    TargetPinId = targetPin.Id,
                    PinType = PinType.Exec
                });
            }
        }
    }

    private static NodeCategory GetNodeCategory(string typeName)
    {
        foreach (var kvp in NodeCategoryMap)
        {
            if (typeName.StartsWith(kvp.Key, StringComparison.OrdinalIgnoreCase))
                return kvp.Value;
        }
        return NodeCategory.Function;
    }

    private static string FormatNodeName(string typeName)
    {
        // K2Node_CallFunction -> Call Function
        string name = typeName.Replace("K2Node_", "");

        // Insert spaces before capitals
        var result = new System.Text.StringBuilder();
        for (int i = 0; i < name.Length; i++)
        {
            if (i > 0 && char.IsUpper(name[i]) && !char.IsUpper(name[i - 1]))
                result.Append(' ');
            result.Append(name[i]);
        }
        return result.ToString();
    }

    /// <summary>
    /// Generate pseudo-code from the blueprint graph.
    /// </summary>
    private static string GeneratePseudoCode(BlueprintData blueprint)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"// Blueprint: {blueprint.ParentClass}");
        sb.AppendLine($"// Generated by UAsset Horizon");
        sb.AppendLine();

        // Class declaration
        sb.AppendLine($"class {blueprint.ParentClass ?? "Blueprint"} {{");
        sb.AppendLine();

        // Variables
        foreach (var v in blueprint.Variables)
        {
            string access = v.IsPublic ? "public" : "private";
            sb.AppendLine($"    {access} {v.Type} {v.Name};");
        }
        if (blueprint.Variables.Any()) sb.AppendLine();

        // Components
        foreach (var c in blueprint.Components)
        {
            sb.AppendLine($"    {c.ClassName} {c.Name};");
        }
        if (blueprint.Components.Any()) sb.AppendLine();

        // Functions / Events
        var eventNodes = blueprint.Nodes.Where(n => n.Category == NodeCategory.Event).ToList();
        foreach (var eventNode in eventNodes)
        {
            sb.AppendLine($"    void {eventNode.Title.Replace(" ", "_")}() {{");

            // Follow execution chain
            var chain = GetExecutionChain(blueprint, eventNode.Id);
            foreach (var node in chain)
            {
                if (node.Id == eventNode.Id) continue;
                sb.AppendLine($"        {NodeToPseudoCode(node)};");
            }

            sb.AppendLine("    }");
            sb.AppendLine();
        }

        // Standalone functions
        foreach (var func in blueprint.Functions)
        {
            sb.AppendLine($"    void {func.Name}() {{");
            sb.AppendLine("        // Implementation in bytecode");
            sb.AppendLine("    }");
            sb.AppendLine();
        }

        sb.AppendLine("}");
        return sb.ToString();
    }

    private static List<BlueprintNode> GetExecutionChain(BlueprintData blueprint, string startNodeId)
    {
        var chain = new List<BlueprintNode>();
        var visited = new HashSet<string>();
        string? currentId = startNodeId;

        while (currentId != null && !visited.Contains(currentId))
        {
            visited.Add(currentId);
            var node = blueprint.Nodes.FirstOrDefault(n => n.Id == currentId);
            if (node == null) break;

            chain.Add(node);

            // Find next node via execution edge
            var edge = blueprint.Edges.FirstOrDefault(e =>
                e.SourceNodeId == currentId && e.PinType == PinType.Exec);
            currentId = edge?.TargetNodeId;
        }

        return chain;
    }

    private static string NodeToPseudoCode(BlueprintNode node)
    {
        return node.Category switch
        {
            NodeCategory.Function => $"{node.Title}()",
            NodeCategory.FlowControl when node.Type.Contains("Branch") =>
                "if (Condition) {{ /* True */ }} else {{ /* False */ }}",
            NodeCategory.FlowControl when node.Type.Contains("ForLoop") =>
                "for (int i = 0; i < Count; i++) {{ /* Loop Body */ }}",
            NodeCategory.Variable when node.Type.Contains("Set") =>
                $"Set {node.Title}",
            NodeCategory.Variable =>
                $"Get {node.Title}",
            NodeCategory.Cast =>
                $"Cast<{node.Title}>(Object)",
            _ => node.Title
        };
    }
}
