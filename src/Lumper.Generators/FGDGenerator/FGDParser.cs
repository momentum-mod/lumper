namespace Lumper.Generators.FGDGenerator;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

public class RawEntity
{
    public required string ClassType { get; init; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public List<string> InheritedBases { get; set; } = [];
    public List<RawProperty> Properties { get; set; } = [];
    public List<RawProperty> Inputs { get; set; } = [];
    public List<RawProperty> Outputs { get; set; } = [];
}

public class RawProperty
{
    public required string Key { get; init; }
    public required string Type { get; set; }
    public string? DisplayName { get; set; }
    public string? DefaultValue { get; set; }
    public string? Description { get; set; }

    public Dictionary<string, string> Choices { get; set; } = [];
}

public class TokenStream(List<Token> tokens)
{
    private readonly List<Token> _tokens = tokens;
    private int _pos;

    public Token Peek()
    {
        return _pos < _tokens.Count ? _tokens[_pos] : new Token(TokenType.EOF, "");
    }

    public Token Consume()
    {
        return _pos < _tokens.Count ? _tokens[_pos++] : new Token(TokenType.EOF, "");
    }

    public bool Match(string val)
    {
        if (Peek().Value == val)
        {
            _ = Consume();
            return true;
        }
        return false;
    }
}

public static class FGDParser
{
    public static List<RawEntity> Parse(string fgdContent)
    {
        var stream = new TokenStream(FGDLexer.Tokenize(fgdContent));
        var entities = new List<RawEntity>();

        while (stream.Peek().Type != TokenType.EOF)
        {
            if (stream.Match("@"))
            {
                Token classTypeToken = stream.Consume();

                if (
                    classTypeToken.Type == TokenType.Word
                    && classTypeToken.Value.EndsWith("Class", StringComparison.OrdinalIgnoreCase)
                )
                {
                    entities.Add(ParseEntity(stream, classTypeToken.Value));
                }
                else
                {
                    while (stream.Peek().Type != TokenType.EOF && stream.Peek().Value != "@")
                    {
                        _ = stream.Consume();
                    }
                }
            }
            else
            {
                _ = stream.Consume();
            }
        }
        return entities;
    }

    private static RawEntity ParseEntity(TokenStream stream, string classType)
    {
        var entity = new RawEntity { ClassType = classType, Name = "Unknown" };

        // Parse inherited bases: base(A, B, C)
        while (stream.Peek().Value != "=" && stream.Peek().Value != "[" && stream.Peek().Type != TokenType.EOF)
        {
            Token token = stream.Consume();
            if (token.Value == "base")
            {
                _ = stream.Match("(");
                while (!stream.Match(")") && stream.Peek().Type != TokenType.EOF)
                {
                    Token baseToken = stream.Consume();
                    if (baseToken.Type == TokenType.Word)
                    {
                        entity.InheritedBases.Add(baseToken.Value);
                        _ = stream.Match(",");
                    }
                }
            }
        }

        // Parse name: entity_name : "Description"
        if (stream.Match("="))
        {
            entity.Name = stream.Consume().Value;
            if (stream.Match(":"))
            {
                if (stream.Peek().Type == TokenType._String)
                {
                    entity.Description = stream.Consume().Value;
                }
            }
        }

        // Parse properties
        if (stream.Match("["))
        {
            while (!stream.Match("]") && stream.Peek().Type != TokenType.EOF)
            {
                if (string.Equals(stream.Peek().Value, "input", StringComparison.OrdinalIgnoreCase))
                {
                    _ = stream.Consume();
                    entity.Inputs.Add(ParseIO(stream));
                }
                else if (string.Equals(stream.Peek().Value, "output", StringComparison.OrdinalIgnoreCase))
                {
                    _ = stream.Consume();
                    entity.Outputs.Add(ParseIO(stream));
                }
                else if (stream.Peek().Value.StartsWith("linedivider", StringComparison.OrdinalIgnoreCase))
                {
                    _ = ParseProperty(stream);
                }
                else
                {
                    entity.Properties.Add(ParseProperty(stream));
                }
            }
        }
        return entity;
    }

    private static RawProperty ParseIO(TokenStream stream)
    {
        var io = new RawProperty { Key = stream.Consume().Value, Type = "void" };

        if (stream.Match("("))
        {
            io.Type = stream.Consume().Value;
            _ = stream.Match(")");
        }

        if (stream.Match(":"))
        {
            if (stream.Peek().Type is TokenType._String or TokenType.Word)
            {
                io.Description = stream.Consume().Value;
            }
        }
        return io;
    }

    private static RawProperty ParseProperty(TokenStream stream)
    {
        var prop = new RawProperty { Key = stream.Consume().Value, Type = "string" };

        // Match: key(type)
        if (stream.Match("("))
        {
            prop.Type = stream.Consume().Value;
            _ = stream.Match(")");
        }

        // Ignore optional modifiers ( readonly )
        while (!":=[]".Contains(stream.Peek().Value) && stream.Peek().Type != TokenType.EOF)
        {
            _ = stream.Consume();
        }

        //Match: : "Display Name" : "Default Value" : "Description"
        int colonCount = 0;
        while (stream.Match(":"))
        {
            string? val = stream.Peek().Type is TokenType._String or TokenType.Word ? stream.Consume().Value : null;

            if (colonCount == 0)
                prop.DisplayName = val;
            else if (colonCount == 1)
                prop.DefaultValue = val;
            else if (colonCount == 2)
                prop.Description = val;

            colonCount++;
        }

        //Match: = [ choices ]
        if (stream.Match("="))
        {
            if (stream.Match("["))
            {
                var choices = new List<KeyValuePair<string, string>>();

                while (!stream.Match("]") && stream.Peek().Type != TokenType.EOF)
                {
                    string choiceKey = stream.Consume().Value;
                    string choiceDisplay = choiceKey;

                    if (stream.Match(":"))
                    {
                        if (stream.Peek().Type is TokenType._String or TokenType.Word)
                        {
                            choiceDisplay = stream.Consume().Value;
                        }
                    }

                    choices.Add(new KeyValuePair<string, string>(choiceKey, choiceDisplay));

                    if (stream.Match(":"))
                    {
                        if (stream.Peek().Type is TokenType._String or TokenType.Word)
                        {
                            _ = stream.Consume();
                        }
                    }
                }

                bool allKeysAreIntegers = choices.All(choice => int.TryParse(choice.Key, out _));

                IOrderedEnumerable<KeyValuePair<string, string>> orderedChoices = allKeysAreIntegers
                    ? choices.OrderBy(choice => int.Parse(choice.Key, CultureInfo.InvariantCulture))
                    : choices.OrderBy(choice => choice.Key, StringComparer.OrdinalIgnoreCase);

                var choiceDict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (KeyValuePair<string, string> choice in orderedChoices)
                {
                    choiceDict[choice.Key] = choice.Value;
                }

                prop.Choices = choiceDict;
            }
        }
        return prop;
    }
}
