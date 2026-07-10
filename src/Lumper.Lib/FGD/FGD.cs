namespace Lumper.Lib.Fgd;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

public record FgdInput(string Name, string ParameterType, string Description);

public record FgdOutput(string Name, string ParameterType, string Description);

[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Naming",
    "CA1720:Identifier contains type name",
    Justification = "These enum members intentionally mirror common FGD schema types."
)]
public enum FgdValueType
{
    Unknown,
    Angle,
    Axis,
    Boolean,
    Choices,
    Color255,
    Flags,
    Float,
    Integer,
    Material,
    Origin,
    Point,
    Sound,
    Sprite,
    String,
    Studio,
    TargetDestination,
    TargetSource,
    Vector,
    Void,
}

public record FgdProperty(
    string Name,
    // Runtime classification of the raw Fgd property type.
    FgdValueType ValueType,
    string DisplayName,
    string DefaultValue,
    string Description,
    IReadOnlyDictionary<string, string> Choices
);

public record FgdEntity(
    string ClassName,
    string ClassType,
    string Description,
    IReadOnlyDictionary<string, FgdProperty> Properties,
    IReadOnlyDictionary<string, FgdInput> Inputs,
    IReadOnlyDictionary<string, FgdOutput> Outputs
);

public record FgdEntityRaw(
    string ClassName,
    string ClassType,
    string Description,
    IReadOnlyList<string> Bases,
    IReadOnlyDictionary<string, FgdProperty> Properties,
    IReadOnlyDictionary<string, FgdInput> Inputs,
    IReadOnlyDictionary<string, FgdOutput> Outputs
);

public static class Fgd
{
    public static IReadOnlyDictionary<string, FgdEntity> Parse(string input)
    {
        return ResolveInheritance(ParseRaw(input));
    }

    public static IReadOnlyDictionary<string, FgdEntityRaw> ParseRaw(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return new Dictionary<string, FgdEntityRaw>(StringComparer.OrdinalIgnoreCase);

        var stream = new TokenStream(Tokenize(input));
        var parsedEntities = new List<ParsedEntity>();

        while (stream.Peek().Type != TokenType.Eof)
        {
            if (stream.Match("@"))
            {
                Token classTypeToken = stream.Consume();
                if (
                    classTypeToken.Type == TokenType.Word
                    && classTypeToken.Value.EndsWith("Class", StringComparison.OrdinalIgnoreCase)
                )
                {
                    parsedEntities.Add(ParseEntity(stream, classTypeToken.Value));
                    continue;
                }

                while (stream.Peek().Type != TokenType.Eof && stream.Peek().Value != "@")
                {
                    _ = stream.Consume();
                }

                continue;
            }

            _ = stream.Consume();
        }

        var parsedByName = new Dictionary<string, ParsedEntity>(StringComparer.OrdinalIgnoreCase);
        foreach (ParsedEntity entity in parsedEntities)
        {
            parsedByName[entity.Name] = entity;
        }

        var dedupedEntities = new Dictionary<string, FgdEntityRaw>(StringComparer.OrdinalIgnoreCase);
        foreach (
            ParsedEntity entity in parsedByName.Values.OrderBy(entity => entity.Name, StringComparer.OrdinalIgnoreCase)
        )
        {
            dedupedEntities[entity.Name] = new FgdEntityRaw(
                ClassName: entity.Name,
                ClassType: entity.ClassType,
                Description: entity.Description ?? string.Empty,
                Bases: entity.InheritedBases.ToArray(),
                Properties: entity.Properties,
                Inputs: entity.Inputs,
                Outputs: entity.Outputs
            );
        }

        return dedupedEntities;
    }

    public static IReadOnlyDictionary<string, FgdEntity> ResolveInheritance(
        IReadOnlyDictionary<string, FgdEntityRaw> rawEntities
    )
    {
        var cache = new Dictionary<string, FgdEntity>(StringComparer.OrdinalIgnoreCase);
        var result = new Dictionary<string, FgdEntity>(StringComparer.OrdinalIgnoreCase);

        foreach (FgdEntityRaw ent in rawEntities.Values)
        {
            if (string.Equals(ent.ClassType, "BaseClass", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            result[ent.ClassName] = ResolveOne(
                ent,
                rawEntities,
                cache,
                new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            );
        }

        return result;
    }

    private static FgdEntity ResolveOne(
        FgdEntityRaw ent,
        IReadOnlyDictionary<string, FgdEntityRaw> rawEntities,
        Dictionary<string, FgdEntity> cache,
        HashSet<string> visiting
    )
    {
        if (cache.TryGetValue(ent.ClassName, out FgdEntity? cached))
            return cached;

        if (!visiting.Add(ent.ClassName))
        {
            // Handle circular base reference
            return new FgdEntity(
                ent.ClassName,
                ent.ClassType,
                ent.Description,
                new Dictionary<string, FgdProperty>(StringComparer.OrdinalIgnoreCase),
                new Dictionary<string, FgdInput>(StringComparer.OrdinalIgnoreCase),
                new Dictionary<string, FgdOutput>(StringComparer.OrdinalIgnoreCase)
            );
        }

        var properties = new Dictionary<string, FgdProperty>(StringComparer.OrdinalIgnoreCase);
        var inputs = new Dictionary<string, FgdInput>(StringComparer.OrdinalIgnoreCase);
        var outputs = new Dictionary<string, FgdOutput>(StringComparer.OrdinalIgnoreCase);

        foreach (string baseName in ent.Bases)
        {
            // Skip bases not in the Fgd
            if (!rawEntities.TryGetValue(baseName, out FgdEntityRaw? baseDecl))
            {
                continue;
            }

            FgdEntity baseDef = ResolveOne(baseDecl, rawEntities, cache, visiting);
            foreach (KeyValuePair<string, FgdProperty> kv in baseDef.Properties)
                properties[kv.Key] = kv.Value;
            foreach (KeyValuePair<string, FgdInput> kv in baseDef.Inputs)
                inputs[kv.Key] = kv.Value;
            foreach (KeyValuePair<string, FgdOutput> kv in baseDef.Outputs)
                outputs[kv.Key] = kv.Value;
        }

        foreach (KeyValuePair<string, FgdProperty> kv in ent.Properties)
            properties[kv.Key] = kv.Value;
        foreach (KeyValuePair<string, FgdInput> kv in ent.Inputs)
            inputs[kv.Key] = kv.Value;
        foreach (KeyValuePair<string, FgdOutput> kv in ent.Outputs)
            outputs[kv.Key] = kv.Value;

        var result = new FgdEntity(ent.ClassName, ent.ClassType, ent.Description, properties, inputs, outputs);
        cache[ent.ClassName] = result;
        visiting.Remove(ent.ClassName);
        return result;
    }

    private static ParsedEntity ParseEntity(TokenStream stream, string classType)
    {
        var entity = new ParsedEntity { ClassType = classType, Name = "Unknown" };

        while (stream.Peek().Value != "=" && stream.Peek().Value != "[" && stream.Peek().Type != TokenType.Eof)
        {
            Token token = stream.Consume();
            if (!string.Equals(token.Value, "base", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            _ = stream.Match("(");
            while (!stream.Match(")") && stream.Peek().Type != TokenType.Eof)
            {
                Token baseToken = stream.Consume();
                if (baseToken.Type == TokenType.Word)
                {
                    entity.InheritedBases.Add(baseToken.Value);
                }

                _ = stream.Match(",");
            }
        }

        if (stream.Match("="))
        {
            entity.Name = stream.Consume().Value;
            if (stream.Match(":") && stream.Peek().Type == TokenType.String)
            {
                entity.Description = stream.Consume().Value;
            }
        }

        if (!stream.Match("["))
        {
            return entity;
        }

        while (!stream.Match("]") && stream.Peek().Type != TokenType.Eof)
        {
            if (string.Equals(stream.Peek().Value, "input", StringComparison.OrdinalIgnoreCase))
            {
                _ = stream.Consume();
                ParsedIo input = ParseIo(stream);
                entity.Inputs[input.Key] = new FgdInput(input.Key, input.Type, input.Description ?? string.Empty);
            }
            else if (string.Equals(stream.Peek().Value, "output", StringComparison.OrdinalIgnoreCase))
            {
                _ = stream.Consume();
                ParsedIo output = ParseIo(stream);
                entity.Outputs[output.Key] = new FgdOutput(output.Key, output.Type, output.Description ?? string.Empty);
            }
            else
            {
                ParsedProperty property = ParseProperty(stream);
                if (!property.IsLineDivider)
                {
                    entity.Properties[property.Key] = new FgdProperty(
                        property.Key,
                        GetValueType(property.Type),
                        property.DisplayName ?? string.Empty,
                        property.DefaultValue ?? string.Empty,
                        property.Description ?? string.Empty,
                        property.Choices
                    );
                }
            }
        }

        return entity;
    }

    private static ParsedIo ParseIo(TokenStream stream)
    {
        var io = new ParsedIo { Key = stream.Consume().Value, Type = "void" };

        if (stream.Match("("))
        {
            io.Type = stream.Consume().Value;
            _ = stream.Match(")");
        }

        if (stream.Match(":") && stream.Peek().Type is TokenType.String or TokenType.Word)
        {
            io.Description = stream.Consume().Value;
        }

        return io;
    }

    private static ParsedProperty ParseProperty(TokenStream stream)
    {
        var property = new ParsedProperty { Key = stream.Consume().Value, Type = "string" };

        if (stream.Match("("))
        {
            property.Type = stream.Consume().Value;
            _ = stream.Match(")");
        }

        property.IsLineDivider = property.Key.StartsWith("linedivider", StringComparison.OrdinalIgnoreCase);

        while (!":=[]".Contains(stream.Peek().Value, StringComparison.Ordinal) && stream.Peek().Type != TokenType.Eof)
        {
            _ = stream.Consume();
        }

        int colonCount = 0;
        while (stream.Match(":"))
        {
            string? value = stream.Peek().Type is TokenType.String or TokenType.Word ? stream.Consume().Value : null;

            if (colonCount == 0)
            {
                property.DisplayName = value;
            }
            else if (colonCount == 1)
            {
                property.DefaultValue = value;
            }
            else if (colonCount == 2)
            {
                property.Description = value;
            }

            colonCount++;
        }

        if (!stream.Match("=") || !stream.Match("["))
        {
            return property;
        }

        var choices = new List<KeyValuePair<string, string>>();
        while (!stream.Match("]") && stream.Peek().Type != TokenType.Eof)
        {
            string choiceKey = stream.Consume().Value;
            string choiceDisplay = choiceKey;

            if (stream.Match(":") && stream.Peek().Type is TokenType.String or TokenType.Word)
            {
                choiceDisplay = stream.Consume().Value;
            }

            choices.Add(new KeyValuePair<string, string>(choiceKey, choiceDisplay));

            if (stream.Match(":") && stream.Peek().Type is TokenType.String or TokenType.Word)
            {
                _ = stream.Consume();
            }
        }

        bool allKeysAreIntegers = choices.All(choice => int.TryParse(choice.Key, out _));
        IEnumerable<KeyValuePair<string, string>> orderedChoices = allKeysAreIntegers
            ? choices.OrderBy(choice => int.Parse(choice.Key, CultureInfo.InvariantCulture))
            : choices.OrderBy(choice => choice.Key, StringComparer.OrdinalIgnoreCase);

        foreach (KeyValuePair<string, string> choice in orderedChoices)
        {
            property.Choices[choice.Key] = choice.Value;
        }

        return property;
    }

    private static List<Token> Tokenize(string input)
    {
        var tokens = new List<Token>();
        int i = 0;

        while (i < input.Length)
        {
            char c = input[i];
            if (char.IsWhiteSpace(c))
            {
                i++;
                continue;
            }

            if (c == '/' && i + 1 < input.Length && input[i + 1] == '/')
            {
                while (i < input.Length && input[i] != '\n')
                {
                    i++;
                }

                continue;
            }

            if (c == '"')
            {
                i++;
                int start = i;
                while (i < input.Length && input[i] != '"')
                {
                    i++;
                }

                tokens.Add(new Token(TokenType.String, input[start..i]));
                if (i < input.Length)
                {
                    i++;
                }

                continue;
            }

            if ("@=:[](),".Contains(c))
            {
                tokens.Add(new Token(TokenType.Symbol, c.ToString()));
                i++;
                continue;
            }

            int wordStart = i;
            while (i < input.Length && !char.IsWhiteSpace(input[i]) && !"@=:[](),\"".Contains(input[i]))
            {
                i++;
            }

            tokens.Add(new Token(TokenType.Word, input[wordStart..i]));
        }

        tokens.Add(new Token(TokenType.Eof, string.Empty));
        return tokens;
    }

    private static FgdValueType GetValueType(string rawType)
    {
        return rawType.ToLowerInvariant() switch
        {
            "angle" or "angle_negative_pitch" => FgdValueType.Angle,
            "axis" => FgdValueType.Axis,
            "bool" or "boolean" => FgdValueType.Boolean,
            "choices" => FgdValueType.Choices,
            "color255" => FgdValueType.Color255,
            "flags" => FgdValueType.Flags,
            "float" => FgdValueType.Float,
            "integer" => FgdValueType.Integer,
            "material" => FgdValueType.Material,
            "origin" => FgdValueType.Origin,
            "point" => FgdValueType.Point,
            "sound" or "soundscape" => FgdValueType.Sound,
            "sprite" => FgdValueType.Sprite,
            "string" or "instance_file" or "instance_parm" or "instance_variable" => FgdValueType.String,
            "studio" => FgdValueType.Studio,
            "target_destination" => FgdValueType.TargetDestination,
            "target_source" => FgdValueType.TargetSource,
            "vector" or "vecline" => FgdValueType.Vector,
            "void" => FgdValueType.Void,
            _ => FgdValueType.Unknown,
        };
    }

    private enum TokenType
    {
        Word,
        String,
        Symbol,
        Eof,
    }

    private sealed record Token(TokenType Type, string Value);

    private sealed class TokenStream(List<Token> tokens)
    {
        private int _position;

        public Token Peek()
        {
            return _position < tokens.Count ? tokens[_position] : new Token(TokenType.Eof, string.Empty);
        }

        public Token Consume()
        {
            return _position < tokens.Count ? tokens[_position++] : new Token(TokenType.Eof, string.Empty);
        }

        public bool Match(string value)
        {
            if (Peek().Value != value)
            {
                return false;
            }

            _ = Consume();
            return true;
        }
    }

    private sealed class ParsedEntity
    {
        public required string ClassType { get; init; }
        public required string Name { get; set; }
        public string? Description { get; set; }
        public List<string> InheritedBases { get; } = [];
        public Dictionary<string, FgdProperty> Properties { get; } = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, FgdInput> Inputs { get; } = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, FgdOutput> Outputs { get; } = new(StringComparer.OrdinalIgnoreCase);
    }

    private sealed class ParsedIo
    {
        public required string Key { get; init; }
        public required string Type { get; set; }
        public string? Description { get; set; }
    }

    private sealed class ParsedProperty
    {
        public required string Key { get; init; }
        public required string Type { get; set; }
        public string? DisplayName { get; set; }
        public string? DefaultValue { get; set; }
        public string? Description { get; set; }
        public bool IsLineDivider { get; set; }
        public Dictionary<string, string> Choices { get; } = new(StringComparer.OrdinalIgnoreCase);
    }
}
